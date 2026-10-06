using EMS.Application.DTOs;
using EMS.Application.Exceptions;
using EMS.Application.Interfaces;
using EMS.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EMS.API.Controllers;

[ApiController]
[Route("api/attendance")]
public class AttendanceController : BaseApiController
{
    private readonly IAttendanceService _attendanceService;

    public AttendanceController(IAttendanceService attendanceService)
    {
        _attendanceService = attendanceService;
    }

    [HttpPost("check-in")]
    [Authorize(Policy = "Attendance.CheckIn.Own")]
    public async Task<IActionResult> CheckIn([FromBody] CheckInRequestDto? request, CancellationToken ct)
    {
        int targetEmployeeId;
        if (HasGlobalAttendanceRead && request?.EmployeeId.HasValue == true && request.EmployeeId.Value > 0)
        {
            targetEmployeeId = request.EmployeeId.Value;
        }
        else
        {
            if (!CallerEmployeeId.HasValue)
                throw new NotFoundException("Employee profile not linked to caller.");
            targetEmployeeId = CallerEmployeeId.Value;
        }

        var result = await _attendanceService.CheckInAsync(targetEmployeeId, ActingUserId, ct);
        return OkResponse(result, "Check-in recorded successfully.");
    }

    [HttpPost("check-out")]
    [Authorize(Policy = "Attendance.CheckIn.Own")]
    public async Task<IActionResult> CheckOut([FromBody] CheckInRequestDto? request, CancellationToken ct)
    {
        int targetEmployeeId;
        if (HasGlobalAttendanceRead && request?.EmployeeId.HasValue == true && request.EmployeeId.Value > 0)
        {
            targetEmployeeId = request.EmployeeId.Value;
        }
        else
        {
            if (!CallerEmployeeId.HasValue)
                throw new NotFoundException("Employee profile not linked to caller.");
            targetEmployeeId = CallerEmployeeId.Value;
        }

        var result = await _attendanceService.CheckOutAsync(targetEmployeeId, ActingUserId, ct);
        return OkResponse(result, "Check-out recorded successfully.");
    }

    [HttpGet("today")]
    [Authorize(Policy = "Attendance.Read.Own")]
    public async Task<IActionResult> GetTodayStatus([FromQuery] int? employeeId, CancellationToken ct)
    {
        int targetEmployeeId;
        if (HasGlobalAttendanceRead && employeeId.HasValue && employeeId.Value > 0)
        {
            targetEmployeeId = employeeId.Value;
        }
        else
        {
            if (!CallerEmployeeId.HasValue)
                throw new NotFoundException("Employee profile not found.");
            targetEmployeeId = CallerEmployeeId.Value;
        }

        var result = await _attendanceService.GetTodayStatusAsync(targetEmployeeId, ct);
        return OkResponse(result);
    }

    [HttpGet]
    [Authorize(Policy = "Attendance.Read.Own")]
    public async Task<IActionResult> GetHistory([FromQuery] AttendanceQueryDto query, CancellationToken ct)
    {
        if (!HasGlobalAttendanceRead)
        {
            // Standard employee / non-privileged role: strictly bind to the authenticated caller's EmployeeId
            if (!CallerEmployeeId.HasValue)
                return OkResponse(Array.Empty<AttendanceDto>());

            query.EmployeeId = CallerEmployeeId.Value;
            query.DepartmentId = null;
        }

        var result = await _attendanceService.GetHistoryAsync(query, ct);
        return OkResponse(result);
    }
}

[ApiController]
[Route("api/leaves")]
public class LeavesController : BaseApiController
{
    private readonly ILeaveService _leaveService;

    public LeavesController(ILeaveService leaveService)
    {
        _leaveService = leaveService;
    }

    [HttpPost]
    [Authorize(Policy = "Leave.Create.Own")]
    public async Task<IActionResult> Apply([FromBody] ApplyLeaveDto dto, CancellationToken ct)
    {
        if (!HasGlobalLeaveRead)
        {
            if (!CallerEmployeeId.HasValue)
                throw new NotFoundException("Employee profile not linked to caller.");
            dto.EmployeeId = CallerEmployeeId.Value;
        }
        else
        {
            if (!dto.EmployeeId.HasValue && CallerEmployeeId.HasValue)
                dto.EmployeeId = CallerEmployeeId.Value;
        }

        var result = await _leaveService.ApplyAsync(dto, ActingUserId, ct);
        return CreatedResponse($"/api/leaves/{result.Id}", result, "Leave application submitted.");
    }

    [HttpGet]
    [Authorize(Policy = "Leave.Read.Own")]
    public  async Task<IActionResult> GetLeaves(
        [FromQuery] int? employeeId,
        [FromQuery] LeaveStatus? status,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] int? managerId,
        [FromQuery] bool? teamOnly,
        CancellationToken ct)
    {
        if (!HasGlobalLeaveRead)
        {
            if (!CallerEmployeeId.HasValue)
                return OkResponse(Array.Empty<LeaveDto>());

            if (HasTeamLeaveRead)
            {
                if (teamOnly == true || (managerId.HasValue && managerId.Value == CallerEmployeeId.Value))
                {
                    // Manager querying team leaves
                    managerId = CallerEmployeeId.Value;
                    employeeId = null;
                }
                else if (employeeId.HasValue && employeeId.Value != CallerEmployeeId.Value)
                {
                    // Manager querying specific direct report
                    managerId = CallerEmployeeId.Value;
                }
                else
                {
                    // Default to own leaves unless explicitly requesting team
                    employeeId = CallerEmployeeId.Value;
                    managerId = null;
                }
            }
            else
            {
                // Standard employee: strictly query records belonging to the authenticated employee
                employeeId = CallerEmployeeId.Value;
                managerId = null;
            }
        }

        var result = await _leaveService.GetLeavesAsync(employeeId, status, from, to, managerId, ct);
        return OkResponse(result);
    }

    [HttpGet("pending-approvals")]
    [Authorize(Policy = "Leave.Approve.Team")]
    public async Task<IActionResult> GetPendingApprovals(CancellationToken ct)
    {
        int? managerId = null;
        if (!HasGlobalLeaveRead)
        {
            managerId = CallerEmployeeId;
        }

        // Returns only pending leave requests (approved/rejected leaves are excluded)
        var result = await _leaveService.GetLeavesAsync(null, LeaveStatus.Pending, null, null, managerId, ct);
        return OkResponse(result);
    }

    [HttpGet("balances")]
    [Authorize(Policy = "Leave.Read.Own")]
    public async Task<IActionResult> GetLeaveBalances([FromQuery] int? employeeId, [FromQuery] int? year, CancellationToken ct)
    {
        int targetEmployeeId;
        if (HasGlobalLeaveRead && employeeId.HasValue && employeeId.Value > 0)
        {
            targetEmployeeId = employeeId.Value;
        }
        else
        {
            if (!CallerEmployeeId.HasValue)
                throw new NotFoundException("Employee profile not linked to caller.");
            targetEmployeeId = CallerEmployeeId.Value;
        }

        var result = await _leaveService.GetLeaveBalancesAsync(targetEmployeeId, year, ct);
        return OkResponse(result);
    }

    [HttpPut("{id:int}/approve")]
    [Authorize(Policy = "Leave.Approve.Team")]
    public async Task<IActionResult> Approve(int id, [FromBody] ApproveLeaveDto dto, CancellationToken ct)
    {
        var result = await _leaveService.ApproveAsync(id, dto, ActingUserId, ct);
        return OkResponse(result, "Leave request status updated.");
    }

    [HttpPut("{id:int}/reject")]
    [Authorize(Policy = "Leave.Approve.Team")]
    public async Task<IActionResult> Reject(int id, CancellationToken ct)
    {
        var dto = new ApproveLeaveDto { Approved = false, Comments = "Rejected by reviewer" };
        var result = await _leaveService.ApproveAsync(id, dto, ActingUserId, ct);
        return OkResponse(result, "Leave request rejected.");
    }

    [HttpPost("{id:int}/cancel")]
    [Authorize(Policy = "Leave.Create.Own")]
    public async Task<IActionResult> Cancel(int id, CancellationToken ct)
    {
        await _leaveService.CancelAsync(id, ActingUserId, ct);
        return NoContent();
    }

    [HttpGet("types")]
    [Authorize]
    public async Task<IActionResult> GetLeaveTypes(CancellationToken ct)
    {
        var result = await _leaveService.GetLeaveTypesAsync(ct);
        return OkResponse(result);
    }
}

[ApiController]
[Route("api")]
public class SalaryController : BaseApiController
{
    private readonly ISalaryService _salaryService;

    public SalaryController(ISalaryService salaryService)
    {
        _salaryService = salaryService;
    }

    [HttpGet("employees/{id:int}/salary")]
    [Authorize(Policy = "Salary.Read.Own")]
    public async Task<IActionResult> GetSalaryStructure(int id, CancellationToken ct)
    {
        var result = await _salaryService.GetStructureByEmployeeIdAsync(id, ct);
        return OkResponse(result);
    }

    [HttpPut("employees/{id:int}/salary")]
    [Authorize(Policy = "Salary.Update")]
    public async Task<IActionResult> UpdateSalaryStructure(int id, [FromBody] UpdateSalaryStructureDto dto, CancellationToken ct)
    {
        var result = await _salaryService.UpdateStructureAsync(id, dto, ActingUserId, ct);
        return OkResponse(result, "Salary structure updated.");
    }

    [HttpPost("employees/{id:int}/payslips/generate")]
    [Authorize(Policy = "Salary.Update")]
    public async Task<IActionResult> GeneratePayslip(int id, [FromQuery] int month, [FromQuery] int year, CancellationToken ct)
    {
        var result = await _salaryService.GeneratePayslipAsync(id, month, year, ActingUserId, ct);
        return CreatedResponse($"/api/payslips/{result.Id}", result, "Payslip generated.");
    }

    [HttpGet("employees/{id:int}/payslips")]
    [Authorize(Policy = "Salary.Read.Own")]
    public async Task<IActionResult> GetPayslips(int id, [FromQuery] int? year, CancellationToken ct)
    {
        var result = await _salaryService.GetPayslipsAsync(id, year, ct);
        return OkResponse(result);
    }

    [HttpGet("payslips/{id:int}")]
    [Authorize]
    public async Task<IActionResult> GetPayslipById(int id, CancellationToken ct)
    {
        var result = await _salaryService.GetPayslipByIdAsync(id, ct);
        return OkResponse(result);
    }
}

[ApiController]
[Route("api/projects")]
public class ProjectsController : BaseApiController
{
    private readonly IProjectService _projectService;

    public ProjectsController(IProjectService projectService)
    {
        _projectService = projectService;
    }

    [HttpGet]
    [Authorize(Policy = "Project.Read")]
    public async Task<IActionResult> GetAll([FromQuery] int? managerId, CancellationToken ct)
    {
        var result = await _projectService.GetAllAsync(managerId, ct);
        return OkResponse(result);
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "Project.Read")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _projectService.GetByIdAsync(id, ct);
        return OkResponse(result);
    }

    [HttpPost]
    [Authorize(Policy = "Project.Create")]
    public async Task<IActionResult> Create([FromBody] CreateProjectDto dto, CancellationToken ct)
    {
        var result = await _projectService.CreateAsync(dto, ActingUserId, ct);
        return CreatedResponse($"/api/projects/{result.Id}", result, "Project created.");
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "Project.Update")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateProjectDto dto, CancellationToken ct)
    {
        var result = await _projectService.UpdateAsync(id, dto, ActingUserId, ct);
        return OkResponse(result, "Project updated.");
    }

    [HttpPost("{id:int}/employees")]
    [Authorize(Policy = "Project.Assign.Team")]
    public async Task<IActionResult> AssignEmployee(int id, [FromBody] AssignEmployeeDto dto, CancellationToken ct)
    {
        await _projectService.AssignEmployeeAsync(id, dto, ActingUserId, ct);
        return OkResponse(true, "Employee assigned to project.");
    }

    [HttpDelete("{id:int}/employees/{employeeId:int}")]
    [Authorize(Policy = "Project.Assign.Team")]
    public async Task<IActionResult> RemoveEmployee(int id, int employeeId, CancellationToken ct)
    {
        await _projectService.RemoveEmployeeAssignmentAsync(id, employeeId, ActingUserId, ct);
        return NoContent();
    }
}

[ApiController]
[Route("api/audit-logs")]
public class AuditLogsController : BaseApiController
{
    private readonly IAuditService _auditService;

    public AuditLogsController(IAuditService auditService)
    {
        _auditService = auditService;
    }

    [HttpGet]
    [Authorize(Policy = "AuditLog.Read")]
    public async Task<IActionResult> GetPaged([FromQuery] AuditLogQueryDto query, CancellationToken ct)
    {
        var result = await _auditService.GetPagedAsync(query, ct);
        return OkResponse(result);
    }
}

[ApiController]
[Route("api/notifications")]
public class NotificationsController : BaseApiController
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetNotifications([FromQuery] bool unreadOnly, CancellationToken ct)
    {
        var result = await _notificationService.GetUserNotificationsAsync(ActingUserId, unreadOnly, ct);
        return OkResponse(result);
    }

    [HttpGet("unread-count")]
    [Authorize]
    public async Task<IActionResult> GetUnreadCount(CancellationToken ct)
    {
        var count = await _notificationService.GetUnreadCountAsync(ActingUserId, ct);
        return OkResponse(new { unreadCount = count });
    }

    [HttpPut("{id:int}/read")]
    [Authorize]
    public async Task<IActionResult> MarkAsRead(int id, CancellationToken ct)
    {
        await _notificationService.MarkAsReadAsync(id, ActingUserId, ct);
        return NoContent();
    }
}

[ApiController]
[Route("api/reports")]
public class ReportsController : BaseApiController
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet("employees-by-department")]
    [Authorize(Policy = "Department.Read")]
    public async Task<IActionResult> GetEmployeesByDepartment(CancellationToken ct)
    {
        var result = await _reportService.GetHeadcountByDepartmentAsync(ct);
        return OkResponse(result);
    }

    [HttpGet("attendance-summary")]
    [Authorize(Policy = "Attendance.Read.Own")]
    public async Task<IActionResult> GetAttendanceSummary([FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] int? departmentId, CancellationToken ct)
    {
        var result = await _reportService.GetAttendanceSummaryAsync(from, to, departmentId, ct);
        return OkResponse(result);
    }

    [HttpGet("leave-utilization")]
    [Authorize(Policy = "Leave.Read.Own")]
    public async Task<IActionResult> GetLeaveUtilization([FromQuery] int? year, CancellationToken ct)
    {
        var result = await _reportService.GetLeaveUtilizationAsync(year, ct);
        return OkResponse(result);
    }

    [HttpGet("payroll-summary")]
    [Authorize(Policy = "Salary.Update")]
    public async Task<IActionResult> GetPayrollSummary([FromQuery] int month, [FromQuery] int year, CancellationToken ct)
    {
        var result = await _reportService.GetPayrollSummaryAsync(month, year, ct);
        return OkResponse(result);
    }

    [HttpGet("project-allocation")]
    [Authorize(Policy = "Project.Read")]
    public async Task<IActionResult> GetProjectAllocation([FromQuery] int? projectId, CancellationToken ct)
    {
        var result = await _reportService.GetProjectAllocationAsync(projectId, ct);
        return OkResponse(result);
    }
}
