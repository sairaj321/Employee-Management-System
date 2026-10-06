using System.Security.Claims;
using EMS.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace EMS.Infrastructure.Security;

public class PermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }

    public PermissionRequirement(string permission)
    {
        Permission = permission;
    }
}

public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        // Admin wildcard check or exact permission match
        var permissions = context.User.FindAll("permissions").Select(c => c.Value).ToList();
        var roles = context.User.FindAll(ClaimTypes.Role).Concat(context.User.FindAll("roles")).Select(c => c.Value).ToList();

        if (roles.Contains("Admin") || permissions.Contains("*") || permissions.Contains(requirement.Permission))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        return Task.CompletedTask;
    }
}

public class OwnResourceRequirement : IAuthorizationRequirement
{
    public string BasePermission { get; }

    public OwnResourceRequirement(string basePermission)
    {
        BasePermission = basePermission;
    }
}

public class OwnResourceAuthorizationHandler : AuthorizationHandler<OwnResourceRequirement>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IUnitOfWork _unitOfWork;

    public OwnResourceAuthorizationHandler(IHttpContextAccessor httpContextAccessor, IUnitOfWork unitOfWork)
    {
        _httpContextAccessor = httpContextAccessor;
        _unitOfWork = unitOfWork;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, OwnResourceRequirement requirement)
    {
        var roles = context.User.FindAll(ClaimTypes.Role).Concat(context.User.FindAll("roles")).Select(c => c.Value).ToList();
        var permissions = context.User.FindAll("permissions").Select(c => c.Value).ToList();

        // Admin or unrestricted role/permission has full access
        if (roles.Contains("Admin") || permissions.Contains("*") || permissions.Contains(requirement.BasePermission))
        {
            context.Succeed(requirement);
            return;
        }

        // HR has global read access for Employee, Department, Attendance, Leave, Salary
        if ((roles.Contains("HR") || roles.Contains("HR Manager")) && (requirement.BasePermission.StartsWith("Employee.") ||
                                                                       requirement.BasePermission.StartsWith("Attendance.") ||
                                                                       requirement.BasePermission.StartsWith("Leave.") ||
                                                                       requirement.BasePermission.StartsWith("Salary.") ||
                                                                       requirement.BasePermission.StartsWith("Department.")))
        {
            context.Succeed(requirement);
            return;
        }

        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null) return;

        var routeData = httpContext.GetRouteData();
        int? targetEmployeeId = null;

        // For Leave routes, route 'id' represents the Leave ID, not Employee ID
        if (requirement.BasePermission.StartsWith("Leave.") && routeData.Values.TryGetValue("id", out var leaveIdVal) && int.TryParse(leaveIdVal?.ToString(), out var leaveId))
        {
            var leave = await _unitOfWork.Leaves.GetByIdAsync(leaveId);
            if (leave != null)
            {
                targetEmployeeId = leave.EmployeeId;
            }
        }
        else if (routeData.Values.TryGetValue("id", out var idVal) && int.TryParse(idVal?.ToString(), out var parsedId))
        {
            targetEmployeeId = parsedId;
        }
        else if (routeData.Values.TryGetValue("employeeId", out var empIdVal) && int.TryParse(empIdVal?.ToString(), out var parsedEmpId))
        {
            targetEmployeeId = parsedEmpId;
        }
        else if (httpContext.Request.Query.TryGetValue("employeeId", out var qEmpId) && int.TryParse(qEmpId.ToString(), out var parsedQId))
        {
            targetEmployeeId = parsedQId;
        }

        var callerEmployeeIdClaim = context.User.FindFirst("employeeId")?.Value;
        if (int.TryParse(callerEmployeeIdClaim, out var callerEmployeeId))
        {
            // If targetEmployeeId is not specified in route/query (e.g. general own check-in/apply-leave or team pending approvals query), succeed if they have .Own or .Team
            if (!targetEmployeeId.HasValue)
            {
                if (roles.Contains("Manager") ||
                    permissions.Contains($"{requirement.BasePermission}.Own") ||
                    permissions.Contains($"{requirement.BasePermission}.Team") ||
                    permissions.Contains(requirement.BasePermission))
                {
                    context.Succeed(requirement);
                    return;
                }
            }
            else if (targetEmployeeId.Value == callerEmployeeId)
            {
                if (permissions.Contains($"{requirement.BasePermission}.Own") || permissions.Contains(requirement.BasePermission))
                {
                    context.Succeed(requirement);
                    return;
                }
            }

            // Check if caller is Manager of the target employee (.Team scope) and not approving own resource
            if (targetEmployeeId.HasValue && (roles.Contains("Manager") || permissions.Contains($"{requirement.BasePermission}.Team")))
            {
                var targetEmployee = await _unitOfWork.Employees.GetByIdAsync(targetEmployeeId.Value);
                if (targetEmployee != null && targetEmployee.ManagerId == callerEmployeeId && targetEmployee.Id != callerEmployeeId)
                {
                    context.Succeed(requirement);
                    return;
                }
            }
        }
    }
}
