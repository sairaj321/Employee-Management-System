using EMS.Application.DTOs;
using EMS.Application.Exceptions;
using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using EMS.Domain.Enums;
using EMS.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace EMS.Application.Services;

public class AttendanceService : IAttendanceService
{
    private readonly IUnitOfWork _uow;
    private readonly IAuditService _auditService;
    private readonly ILogger<AttendanceService> _logger;

    public AttendanceService(IUnitOfWork uow, IAuditService auditService, ILogger<AttendanceService> logger)
    {
        _uow = uow;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<AttendanceDto> CheckInAsync(int employeeId, int actingUserId, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var existing = await _uow.Attendance.GetTodayAttendanceAsync(employeeId, today, ct);

        if (existing != null && existing.CheckInTime != null)
        {
            _logger.LogWarning("CHECK_IN_REJECTED_DUPLICATE. EmployeeId: {EmployeeId}", employeeId);
            throw new ConflictException("Already checked in for today.");
        }

        var now = DateTime.UtcNow;
        var status = (now.Hour > 9 || (now.Hour == 9 && now.Minute > 30)) ? AttendanceStatus.Late : AttendanceStatus.Present;

        Attendance attendance;
        if (existing != null)
        {
            existing.CheckInTime = now;
            existing.Status = status;
            _uow.Attendance.Update(existing);
            attendance = existing;
        }
        else
        {
            attendance = new Attendance
            {
                EmployeeId = employeeId,
                Date = today,
                CheckInTime = now,
                Status = status
            };
            await _uow.Attendance.AddAsync(attendance, ct);
        }

        await _uow.SaveChangesAsync(ct);
        await _auditService.LogAsync(actingUserId, "CHECK_IN", nameof(Attendance), attendance.Id.ToString(), null, attendance, ct: ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("CHECK_IN. EmployeeId: {EmployeeId}, Status: {Status}", employeeId, status);
        return MapAttendanceDto(attendance);
    }

    public async Task<AttendanceDto> CheckOutAsync(int employeeId, int actingUserId, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var attendance = await _uow.Attendance.GetTodayAttendanceAsync(employeeId, today, ct);

        if (attendance == null || attendance.CheckInTime == null)
        {
            throw new ConflictException("No active check-in found for today.");
        }

        attendance.CheckOutTime = DateTime.UtcNow;
        _uow.Attendance.Update(attendance);
        await _uow.SaveChangesAsync(ct);

        await _auditService.LogAsync(actingUserId, "CHECK_OUT", nameof(Attendance), attendance.Id.ToString(), null, attendance, ct: ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("CHECK_OUT. EmployeeId: {EmployeeId}", employeeId);
        return MapAttendanceDto(attendance);
    }

    public async Task<AttendanceDto?> GetTodayStatusAsync(int employeeId, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var attendance = await _uow.Attendance.GetTodayAttendanceAsync(employeeId, today, ct);
        return attendance != null ? MapAttendanceDto(attendance) : null;
    }

    public async Task<IReadOnlyList<AttendanceDto>> GetHistoryAsync(AttendanceQueryDto query, CancellationToken ct = default)
    {
        var items = await _uow.Attendance.GetAttendancesAsync(query.EmployeeId, query.From, query.To, query.DepartmentId, ct);
        return items.Select(MapAttendanceDto).ToList();
    }

    private static AttendanceDto MapAttendanceDto(Attendance a)
    {
        return new AttendanceDto
        {
            Id = a.Id,
            EmployeeId = a.EmployeeId,
            EmployeeName = a.Employee != null ? $"{a.Employee.FirstName} {a.Employee.LastName}" : string.Empty,
            EmployeeCode = a.Employee?.EmployeeCode ?? string.Empty,
            DepartmentName = a.Employee?.Department?.Name ?? string.Empty,
            Date = a.Date,
            CheckInTime = a.CheckInTime,
            CheckOutTime = a.CheckOutTime,
            Status = a.Status
        };
    }
}

public class LeaveService : ILeaveService
{
    private readonly IUnitOfWork _uow;
    private readonly IAuditService _auditService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<LeaveService> _logger;

    public LeaveService(
        IUnitOfWork uow,
        IAuditService auditService,
        INotificationService notificationService,
        ILogger<LeaveService> logger)
    {
        _uow = uow;
        _auditService = auditService;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<LeaveDto> ApplyAsync(ApplyLeaveDto dto, int actingUserId, CancellationToken ct = default)
    {
        if (dto.EndDate < dto.StartDate)
            throw new ValidationException("EndDate", "End date must be on or after start date.");

        var employeeId = dto.EmployeeId.HasValue && dto.EmployeeId.Value > 0
            ? dto.EmployeeId.Value
            : (await _uow.Employees.GetByUserIdAsync(actingUserId, ct))?.Id ?? throw new NotFoundException("Employee profile not found for user.");

        if (await _uow.Leaves.HasOverlappingLeaveAsync(employeeId, dto.StartDate, dto.EndDate, ct: ct))
        {
            throw new ConflictException("You already have an active or pending leave application for this date range.");
        }

        var leaveType = await _uow.Leaves.GetLeaveTypeByIdAsync(dto.LeaveTypeId, ct);
        if (leaveType == null)
            throw new NotFoundException(nameof(LeaveType), dto.LeaveTypeId);

        // Calculate leave days requested
        int requestedDays = (dto.EndDate.DayNumber - dto.StartDate.DayNumber) + 1;

        // Calculate remaining available balance for the leave year
        int leaveYear = dto.StartDate.Year;
        var existingLeaves = await _uow.Leaves.GetLeavesAsync(employeeId, null, null, null, null, ct);
        var yearLeaves = existingLeaves.Where(l => (l.StartDate.Year == leaveYear || l.EndDate.Year == leaveYear) && l.LeaveTypeId == dto.LeaveTypeId).ToList();

        int usedDays = yearLeaves.Where(l => l.Status == LeaveStatus.Approved).Sum(l => (l.EndDate.DayNumber - l.StartDate.DayNumber) + 1);
        int pendingDays = yearLeaves.Where(l => l.Status == LeaveStatus.Pending).Sum(l => (l.EndDate.DayNumber - l.StartDate.DayNumber) + 1);
        int availableDays = Math.Max(0, leaveType.DefaultDaysPerYear - usedDays - pendingDays);

        if (requestedDays > availableDays)
        {
            throw new ConflictException($"Insufficient leave balance for {leaveType.Name}. Available: {availableDays} days, Requested: {requestedDays} days.");
        }

        var leave = new Leave
        {
            EmployeeId = employeeId,
            LeaveTypeId = dto.LeaveTypeId,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            Reason = dto.Reason.Trim(),
            Status = LeaveStatus.Pending
        };

        await _uow.Leaves.AddAsync(leave, ct);
        await _uow.SaveChangesAsync(ct);

        await _auditService.LogAsync(actingUserId, "LEAVE_APPLIED", nameof(Leave), leave.Id.ToString(), null, leave, ct: ct);
        await _uow.SaveChangesAsync(ct);

        // Notify manager if assigned
        var employee = await _uow.Employees.GetByIdAsync(employeeId, ct);
        if (employee?.Manager?.UserId != null)
        {
            await _notificationService.NotifyAsync(
                employee.Manager.UserId,
                "LEAVE_REQUEST",
                "New Leave Application",
                $"{employee.FirstName} {employee.LastName} applied for leave from {dto.StartDate:yyyy-MM-dd} to {dto.EndDate:yyyy-MM-dd}.",
                ct);
        }

        _logger.LogInformation("LEAVE_APPLIED. LeaveId: {LeaveId}, EmployeeId: {EmployeeId}, AvailableRemaining: {Remaining}", leave.Id, employeeId, availableDays - requestedDays);
        var resultDto = MapLeaveDto(leave);
        resultDto.AvailableDaysRemaining = availableDays - requestedDays;
        return resultDto;
    }

    public async Task<LeaveDto> ApproveAsync(int leaveId, ApproveLeaveDto dto, int actingUserId, CancellationToken ct = default)
    {
        var leave = await _uow.Leaves.GetByIdAsync(leaveId, ct);
        if (leave == null)
            throw new NotFoundException(nameof(Leave), leaveId);

        if (leave.Status == LeaveStatus.Cancelled)
            throw new ConflictException("Cannot approve or reject a cancelled leave request.");

        var approverEmployee = await _uow.Employees.GetByUserIdAsync(actingUserId, ct);
        var user = await _uow.Users.GetByIdAsync(actingUserId, ct);
        var roles = user?.UserRoles.Select(ur => ur.Role.Name).ToList() ?? new List<string>();
        var perms = user?.UserRoles.SelectMany(ur => ur.Role.RolePermissions).Select(rp => rp.Permission.Code).ToList() ?? new List<string>();

        bool isHrOrAdmin = roles.Contains("Admin") || roles.Contains("HR") || roles.Contains("HR Manager") || perms.Contains("*") || perms.Contains("Leave.Approve");

        var targetEmployee = leave.Employee ?? await _uow.Employees.GetByIdAsync(leave.EmployeeId, ct);

        // Check if caller is the direct manager of the leave applicant
        bool isManagerOfEmployee = approverEmployee != null && targetEmployee != null && targetEmployee.ManagerId == approverEmployee.Id;

        // Prevent self-approval (e.g. manager approving their own leave) unless system Admin
        if (approverEmployee != null && leave.EmployeeId == approverEmployee.Id && !roles.Contains("Admin"))
        {
            throw new ForbiddenException("Employees cannot approve or reject their own leave requests.");
        }

        if (!isHrOrAdmin && !isManagerOfEmployee)
        {
            throw new ForbiddenException("Only the employee's reporting manager or HR can approve or reject leave requests.");
        }

        var oldStatus = leave.Status;
        leave.Status = dto.Approved ? LeaveStatus.Approved : LeaveStatus.Rejected;
        leave.ApprovedBy = approverEmployee?.Id;
        leave.ApprovedAt = DateTime.UtcNow;

        _uow.Leaves.Update(leave);
        await _uow.SaveChangesAsync(ct);

        var actionName = dto.Approved ? "LEAVE_APPROVED" : "LEAVE_REJECTED";
        await _auditService.LogAsync(actingUserId, actionName, nameof(Leave), leave.Id.ToString(), new { Status = oldStatus }, leave, ct: ct);
        await _uow.SaveChangesAsync(ct);

        // Notify employee
        if (targetEmployee?.UserId != null)
        {
            await _notificationService.NotifyAsync(
                targetEmployee.UserId,
                actionName,
                $"Leave Application {leave.Status}",
                $"Your leave request from {leave.StartDate:yyyy-MM-dd} to {leave.EndDate:yyyy-MM-dd} has been {leave.Status.ToString().ToLower()}.",
                ct);
        }

        _logger.LogInformation("LEAVE_STATUS_CHANGED. LeaveId: {LeaveId}, Status: {Status}, By: {UserId}", leaveId, leave.Status, actingUserId);
        return MapLeaveDto(leave);
    }

    public async Task CancelAsync(int leaveId, int actingUserId, CancellationToken ct = default)
    {
        var leave = await _uow.Leaves.GetByIdAsync(leaveId, ct);
        if (leave == null)
            throw new NotFoundException(nameof(Leave), leaveId);

        var callerEmployee = await _uow.Employees.GetByUserIdAsync(actingUserId, ct);
        var user = await _uow.Users.GetByIdAsync(actingUserId, ct);
        var roles = user?.UserRoles.Select(ur => ur.Role.Name).ToList() ?? new List<string>();
        var perms = user?.UserRoles.SelectMany(ur => ur.Role.RolePermissions).Select(rp => rp.Permission.Code).ToList() ?? new List<string>();
        bool isGlobalAdminOrHr = roles.Contains("Admin") || roles.Contains("HR") || roles.Contains("HR Manager") || perms.Contains("*") || perms.Contains("Leave.Approve");

        if (!isGlobalAdminOrHr && (callerEmployee == null || leave.EmployeeId != callerEmployee.Id))
        {
            throw new ForbiddenException("You can only cancel your own leave requests.");
        }

        if (leave.Status == LeaveStatus.Cancelled)
            throw new ConflictException("Leave is already cancelled.");

        leave.Status = LeaveStatus.Cancelled;
        _uow.Leaves.Update(leave);
        await _uow.SaveChangesAsync(ct);

        await _auditService.LogAsync(actingUserId, "LEAVE_CANCELLED", nameof(Leave), leaveId.ToString(), null, leave, ct: ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("LEAVE_CANCELLED. LeaveId: {LeaveId}", leaveId);
    }

    public async Task<IReadOnlyList<LeaveDto>> GetLeavesAsync(int? employeeId, LeaveStatus? status, DateOnly? from, DateOnly? to, int? managerId, CancellationToken ct = default)
    {
        var items = await _uow.Leaves.GetLeavesAsync(employeeId, status, from, to, managerId, ct);
        return items.Select(MapLeaveDto).ToList();
    }

    public async Task<IReadOnlyList<LeaveTypeDto>> GetLeaveTypesAsync(CancellationToken ct = default)
    {
        var types = await _uow.Leaves.GetAllLeaveTypesAsync(ct);
        return types.Select(lt => new LeaveTypeDto { Id = lt.Id, Name = lt.Name, DefaultDaysPerYear = lt.DefaultDaysPerYear }).ToList();
    }

    public async Task<IReadOnlyList<LeaveBalanceDto>> GetLeaveBalancesAsync(int employeeId, int? year, CancellationToken ct = default)
    {
        var targetYear = year ?? DateTime.UtcNow.Year;
        var employee = await _uow.Employees.GetByIdAsync(employeeId, ct);
        if (employee == null)
            throw new NotFoundException(nameof(Employee), employeeId);

        var leaveTypes = await _uow.Leaves.GetAllLeaveTypesAsync(ct);
        var leaves = await _uow.Leaves.GetLeavesAsync(employeeId, null, null, null, null, ct);

        var yearLeaves = leaves.Where(l => l.StartDate.Year == targetYear || l.EndDate.Year == targetYear).ToList();

        var balances = new List<LeaveBalanceDto>();
        foreach (var lt in leaveTypes)
        {
            var usedDays = yearLeaves
                .Where(l => l.LeaveTypeId == lt.Id && l.Status == LeaveStatus.Approved)
                .Sum(l => (l.EndDate.DayNumber - l.StartDate.DayNumber) + 1);

            var pendingDays = yearLeaves
                .Where(l => l.LeaveTypeId == lt.Id && l.Status == LeaveStatus.Pending)
                .Sum(l => (l.EndDate.DayNumber - l.StartDate.DayNumber) + 1);

            balances.Add(new LeaveBalanceDto
            {
                EmployeeId = employeeId,
                EmployeeName = $"{employee.FirstName} {employee.LastName}",
                LeaveTypeId = lt.Id,
                LeaveTypeName = lt.Name,
                Year = targetYear,
                TotalAllocatedDays = lt.DefaultDaysPerYear,
                UsedDays = usedDays,
                PendingDays = pendingDays
            });
        }

        return balances;
    }

    private static LeaveDto MapLeaveDto(Leave l)
    {
        return new LeaveDto
        {
            Id = l.Id,
            EmployeeId = l.EmployeeId,
            EmployeeName = l.Employee != null ? $"{l.Employee.FirstName} {l.Employee.LastName}" : string.Empty,
            EmployeeCode = l.Employee?.EmployeeCode ?? string.Empty,
            LeaveTypeId = l.LeaveTypeId,
            LeaveTypeName = l.LeaveType?.Name ?? string.Empty,
            StartDate = l.StartDate,
            EndDate = l.EndDate,
            Reason = l.Reason,
            Status = l.Status,
            ApprovedBy = l.ApprovedBy,
            ApproverName = l.Approver != null ? $"{l.Approver.FirstName} {l.Approver.LastName}" : null,
            ApprovedAt = l.ApprovedAt
        };
    }
}

public class SalaryService : ISalaryService
{
    private readonly IUnitOfWork _uow;
    private readonly IAuditService _auditService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<SalaryService> _logger;

    public SalaryService(
        IUnitOfWork uow,
        IAuditService auditService,
        INotificationService notificationService,
        ILogger<SalaryService> logger)
    {
        _uow = uow;
        _auditService = auditService;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<SalaryStructureDto> GetStructureByEmployeeIdAsync(int employeeId, CancellationToken ct = default)
    {
        var structure = await _uow.Salary.GetStructureByEmployeeIdAsync(employeeId, ct);
        if (structure == null)
            throw new NotFoundException($"Salary structure for employee #{employeeId} not found.");

        return MapStructureDto(structure);
    }

    public async Task<SalaryStructureDto> UpdateStructureAsync(int employeeId, UpdateSalaryStructureDto dto, int actingUserId, CancellationToken ct = default)
    {
        var structure = await _uow.Salary.GetStructureByEmployeeIdAsync(employeeId, ct);
        var old = structure != null ? new { structure.BasicSalary, structure.HRA, structure.OtherAllowances, structure.PFDeduction } : null;

        if (structure == null)
        {
            structure = new SalaryStructure
            {
                EmployeeId = employeeId,
                BasicSalary = dto.BasicSalary,
                HRA = dto.HRA,
                OtherAllowances = dto.OtherAllowances,
                PFDeduction = dto.PFDeduction,
                EffectiveFrom = dto.EffectiveFrom ?? DateTime.UtcNow
            };
            await _uow.Salary.AddStructureAsync(structure, ct);
        }
        else
        {
            structure.BasicSalary = dto.BasicSalary;
            structure.HRA = dto.HRA;
            structure.OtherAllowances = dto.OtherAllowances;
            structure.PFDeduction = dto.PFDeduction;
            if (dto.EffectiveFrom.HasValue)
                structure.EffectiveFrom = dto.EffectiveFrom.Value;

            _uow.Salary.UpdateStructure(structure);
        }

        await _uow.SaveChangesAsync(ct);
        await _auditService.LogAsync(actingUserId, "SALARY_UPDATED", nameof(SalaryStructure), structure.Id.ToString(), old, structure, ct: ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("SALARY_UPDATED. EmployeeId: {EmployeeId}", employeeId);
        return MapStructureDto(structure);
    }

    public async Task<PayslipDto> GeneratePayslipAsync(int employeeId, int month, int year, int actingUserId, CancellationToken ct = default)
    {
        var existing = await _uow.Salary.GetPayslipAsync(employeeId, month, year, ct);
        if (existing != null)
            throw new ConflictException($"Payslip for employee #{employeeId} for {month:D2}/{year} has already been generated.");

        var structure = await _uow.Salary.GetStructureByEmployeeIdAsync(employeeId, ct);
        if (structure == null)
            throw new NotFoundException($"Salary structure not defined for employee #{employeeId}. Cannot generate payslip.");

        var gross = structure.BasicSalary + structure.HRA + structure.OtherAllowances;
        var deductions = structure.PFDeduction;
        var net = gross - deductions;

        var payslip = new Payslip
        {
            EmployeeId = employeeId,
            Month = month,
            Year = year,
            GrossSalary = gross,
            Deductions = deductions,
            NetSalary = net,
            GeneratedAt = DateTime.UtcNow
        };

        await _uow.Salary.AddPayslipAsync(payslip, ct);
        await _uow.SaveChangesAsync(ct);

        await _auditService.LogAsync(actingUserId, "PAYSLIP_GENERATED", nameof(Payslip), payslip.Id.ToString(), null, payslip, ct: ct);
        await _uow.SaveChangesAsync(ct);

        // Notify employee
        var employee = await _uow.Employees.GetByIdAsync(employeeId, ct);
        if (employee?.UserId != null)
        {
            await _notificationService.NotifyAsync(
                employee.UserId,
                "PAYSLIP_GENERATED",
                "New Payslip Available",
                $"Your payslip for {month:D2}/{year} has been generated with Net Pay of ${net:N2}.",
                ct);
        }

        _logger.LogInformation("PAYSLIP_GENERATED. EmployeeId: {EmployeeId}, Month: {Month}, Year: {Year}", employeeId, month, year);
        return MapPayslipDto(payslip, employee);
    }

    public async Task<IReadOnlyList<PayslipDto>> GetPayslipsAsync(int employeeId, int? year, CancellationToken ct = default)
    {
        var payslips = await _uow.Salary.GetPayslipsByEmployeeAsync(employeeId, year, ct);
        return payslips.Select(p => MapPayslipDto(p, p.Employee)).ToList();
    }

    public async Task<PayslipDto> GetPayslipByIdAsync(int id, CancellationToken ct = default)
    {
        var payslip = await _uow.Salary.GetPayslipByIdAsync(id, ct);
        if (payslip == null)
            throw new NotFoundException(nameof(Payslip), id);

        return MapPayslipDto(payslip, payslip.Employee);
    }

    private static SalaryStructureDto MapStructureDto(SalaryStructure s)
    {
        return new SalaryStructureDto
        {
            Id = s.Id,
            EmployeeId = s.EmployeeId,
            EmployeeName = s.Employee != null ? $"{s.Employee.FirstName} {s.Employee.LastName}" : null,
            BasicSalary = s.BasicSalary,
            HRA = s.HRA,
            OtherAllowances = s.OtherAllowances,
            PFDeduction = s.PFDeduction,
            EffectiveFrom = s.EffectiveFrom
        };
    }

    private static PayslipDto MapPayslipDto(Payslip p, Employee? emp)
    {
        return new PayslipDto
        {
            Id = p.Id,
            EmployeeId = p.EmployeeId,
            EmployeeName = emp != null ? $"{emp.FirstName} {emp.LastName}" : string.Empty,
            EmployeeCode = emp?.EmployeeCode ?? string.Empty,
            DepartmentName = emp?.Department?.Name ?? string.Empty,
            Month = p.Month,
            Year = p.Year,
            GrossSalary = p.GrossSalary,
            Deductions = p.Deductions,
            NetSalary = p.NetSalary,
            GeneratedAt = p.GeneratedAt
        };
    }
}

public class ProjectService : IProjectService
{
    private readonly IUnitOfWork _uow;
    private readonly IAuditService _auditService;

    public ProjectService(IUnitOfWork uow, IAuditService auditService)
    {
        _uow = uow;
        _auditService = auditService;
    }

    public async Task<ProjectDto> CreateAsync(CreateProjectDto dto, int actingUserId, CancellationToken ct = default)
    {
        var project = new Project
        {
            Name = dto.Name.Trim(),
            Description = dto.Description?.Trim(),
            StartDate = DateTime.SpecifyKind(dto.StartDate, DateTimeKind.Utc),
            EndDate = dto.EndDate.HasValue ? DateTime.SpecifyKind(dto.EndDate.Value, DateTimeKind.Utc) : null,
            ManagerId = dto.ManagerId,
            Status = "Active"
        };

        await _uow.Projects.AddAsync(project, ct);
        await _uow.SaveChangesAsync(ct);

        await _auditService.LogAsync(actingUserId, "PROJECT_CREATED", nameof(Project), project.Id.ToString(), null, project, ct: ct);
        await _uow.SaveChangesAsync(ct);

        return await GetByIdAsync(project.Id, ct);
    }

    public async Task<ProjectDto> UpdateAsync(int id, UpdateProjectDto dto, int actingUserId, CancellationToken ct = default)
    {
        var proj = await _uow.Projects.GetByIdAsync(id, ct);
        if (proj == null)
            throw new NotFoundException(nameof(Project), id);

        var old = new { proj.Name, proj.Description, proj.StartDate, proj.EndDate, proj.Status, proj.ManagerId };
        proj.Name = dto.Name.Trim();
        proj.Description = dto.Description?.Trim();
        proj.StartDate = DateTime.SpecifyKind(dto.StartDate, DateTimeKind.Utc);
        proj.EndDate = dto.EndDate.HasValue ? DateTime.SpecifyKind(dto.EndDate.Value, DateTimeKind.Utc) : null;
        proj.Status = dto.Status;
        proj.ManagerId = dto.ManagerId;

        _uow.Projects.Update(proj);
        await _uow.SaveChangesAsync(ct);

        await _auditService.LogAsync(actingUserId, "PROJECT_UPDATED", nameof(Project), id.ToString(), old, proj, ct: ct);
        await _uow.SaveChangesAsync(ct);

        return await GetByIdAsync(id, ct);
    }

    public async Task<ProjectDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var proj = await _uow.Projects.GetByIdAsync(id, ct);
        if (proj == null)
            throw new NotFoundException(nameof(Project), id);

        return MapProjectDto(proj);
    }

    public async Task<IReadOnlyList<ProjectDto>> GetAllAsync(int? managerId, CancellationToken ct = default)
    {
        var list = await _uow.Projects.GetAllAsync(managerId, ct);
        return list.Select(MapProjectDto).ToList();
    }

    public async Task AssignEmployeeAsync(int projectId, AssignEmployeeDto dto, int actingUserId, CancellationToken ct = default)
    {
        var proj = await _uow.Projects.GetByIdAsync(projectId, ct);
        if (proj == null)
            throw new NotFoundException(nameof(Project), projectId);

        var emp = await _uow.Employees.GetByIdAsync(dto.EmployeeId, ct);
        if (emp == null)
            throw new NotFoundException(nameof(Employee), dto.EmployeeId);

        if (await _uow.Projects.IsEmployeeAssignedAsync(projectId, dto.EmployeeId, ct))
            throw new ConflictException("Employee is already assigned to this project.");

        var ep = new EmployeeProject
        {
            ProjectId = projectId,
            EmployeeId = dto.EmployeeId,
            AllocatedFrom = DateTime.SpecifyKind(dto.AllocatedFrom, DateTimeKind.Utc),
            AllocatedTo = dto.AllocatedTo.HasValue ? DateTime.SpecifyKind(dto.AllocatedTo.Value, DateTimeKind.Utc) : null
        };

        await _uow.Projects.AssignEmployeeAsync(ep, ct);
        await _uow.SaveChangesAsync(ct);

        await _auditService.LogAsync(actingUserId, "PROJECT_EMPLOYEE_ASSIGNED", nameof(EmployeeProject), $"{projectId}-{dto.EmployeeId}", null, ep, ct: ct);
        await _uow.SaveChangesAsync(ct);
    }

    public async Task RemoveEmployeeAssignmentAsync(int projectId, int employeeId, int actingUserId, CancellationToken ct = default)
    {
        await _uow.Projects.RemoveEmployeeAssignmentAsync(projectId, employeeId, ct);
        await _uow.SaveChangesAsync(ct);

        await _auditService.LogAsync(actingUserId, "PROJECT_EMPLOYEE_REMOVED", nameof(EmployeeProject), $"{projectId}-{employeeId}", null, null, ct: ct);
        await _uow.SaveChangesAsync(ct);
    }

    private static ProjectDto MapProjectDto(Project p)
    {
        return new ProjectDto
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            StartDate = p.StartDate,
            EndDate = p.EndDate,
            Status = p.Status,
            ManagerId = p.ManagerId,
            ManagerName = p.Manager != null ? $"{p.Manager.FirstName} {p.Manager.LastName}" : null,
            AssignedEmployees = p.EmployeeProjects.Select(ep => new ProjectEmployeeDto
            {
                EmployeeId = ep.EmployeeId,
                EmployeeName = ep.Employee != null ? $"{ep.Employee.FirstName} {ep.Employee.LastName}" : string.Empty,
                EmployeeCode = ep.Employee?.EmployeeCode ?? string.Empty,
                AllocatedFrom = ep.AllocatedFrom,
                AllocatedTo = ep.AllocatedTo
            }).ToList()
        };
    }
}

public class ReportService : IReportService
{
    private readonly IUnitOfWork _uow;

    public ReportService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<IReadOnlyList<HeadcountByDepartmentReportDto>> GetHeadcountByDepartmentAsync(CancellationToken ct = default)
    {
        var departments = await _uow.Departments.GetAllAsync(ct);
        return departments.Select(d => new HeadcountByDepartmentReportDto
        {
            DepartmentId = d.Id,
            DepartmentName = d.Name,
            ActiveEmployees = d.Employees.Count(e => e.Status == EmployeeStatus.Active),
            TotalEmployees = d.Employees.Count
        }).ToList();
    }

    public async Task<IReadOnlyList<AttendanceSummaryReportDto>> GetAttendanceSummaryAsync(DateOnly? from, DateOnly? to, int? departmentId, CancellationToken ct = default)
    {
        var attendances = await _uow.Attendance.GetAttendancesAsync(null, from, to, departmentId, ct);
        var grouped = attendances.GroupBy(a => a.Date);

        return grouped.Select(g => new AttendanceSummaryReportDto
        {
            Date = g.Key,
            TotalPresent = g.Count(a => a.Status == AttendanceStatus.Present),
            TotalAbsent = g.Count(a => a.Status == AttendanceStatus.Absent),
            TotalLate = g.Count(a => a.Status == AttendanceStatus.Late),
            TotalHalfDay = g.Count(a => a.Status == AttendanceStatus.HalfDay)
        }).OrderByDescending(r => r.Date).ToList();
    }

    public async Task<IReadOnlyList<LeaveUtilizationReportDto>> GetLeaveUtilizationAsync(int? year, CancellationToken ct = default)
    {
        var (employees, _) = await _uow.Employees.GetPagedAsync(1, 1000, null, null, ct);
        var leaves = await _uow.Leaves.GetLeavesAsync(null, null, null, null, null, ct);

        return employees.Select(e =>
        {
            var empLeaves = leaves.Where(l => l.EmployeeId == e.Id);
            if (year.HasValue)
            {
                empLeaves = empLeaves.Where(l => l.StartDate.Year == year.Value);
            }
            return new LeaveUtilizationReportDto
            {
                EmployeeId = e.Id,
                EmployeeName = $"{e.FirstName} {e.LastName}",
                DepartmentName = e.Department?.Name ?? string.Empty,
                TotalLeavesTaken = empLeaves.Where(l => l.Status == LeaveStatus.Approved).Sum(l => (l.EndDate.DayNumber - l.StartDate.DayNumber) + 1),
                PendingRequests = empLeaves.Count(l => l.Status == LeaveStatus.Pending)
            };
        }).ToList();
    }

    public async Task<PayrollSummaryReportDto> GetPayrollSummaryAsync(int month, int year, CancellationToken ct = default)
    {
        var payslips = await _uow.Salary.GetPayrollSummaryAsync(month, year, ct);
        return new PayrollSummaryReportDto
        {
            Month = month,
            Year = year,
            TotalEmployeesPaid = payslips.Count,
            TotalGrossSalary = payslips.Sum(p => p.GrossSalary),
            TotalDeductions = payslips.Sum(p => p.Deductions),
            TotalNetSalary = payslips.Sum(p => p.NetSalary)
        };
    }

    public async Task<IReadOnlyList<ProjectAllocationReportDto>> GetProjectAllocationAsync(int? projectId, CancellationToken ct = default)
    {
        var projects = await _uow.Projects.GetAllAsync(ct: ct);
        if (projectId.HasValue && projectId.Value > 0)
            projects = projects.Where(p => p.Id == projectId.Value).ToList();

        return projects.Select(p => new ProjectAllocationReportDto
        {
            ProjectId = p.Id,
            ProjectName = p.Name,
            ManagerName = p.Manager != null ? $"{p.Manager.FirstName} {p.Manager.LastName}" : "Unassigned",
            Status = p.Status,
            AssignedMembersCount = p.EmployeeProjects.Count
        }).ToList();
    }
}
