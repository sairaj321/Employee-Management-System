using EMS.Application.DTOs;
using EMS.Domain.Entities;
using EMS.Domain.Enums;

namespace EMS.Application.Interfaces;

public interface IPasswordHasher
{
    (string Hash, string Salt) HashPassword(string password);
    bool VerifyPassword(string password, string storedHash, string storedSalt);
}

public interface ITokenService
{
    (string AccessToken, int ExpiresIn) GenerateAccessToken(User user, Employee? employee, IEnumerable<string> roles, IEnumerable<string> permissions);
    string GenerateRefreshToken();
    string HashToken(string token);
}

public interface IAuthService
{
    Task<LoginResponseDto> LoginAsync(LoginRequestDto request, string? ipAddress, CancellationToken ct = default);
    Task<LoginResponseDto> RefreshTokenAsync(string refreshToken, string? ipAddress, CancellationToken ct = default);
    Task LogoutAsync(string refreshToken, int actingUserId, CancellationToken ct = default);
    Task ChangePasswordAsync(int userId, ChangePasswordDto dto, CancellationToken ct = default);
    Task<UserDto> GetCurrentUserAsync(int userId, CancellationToken ct = default);
    Task<IReadOnlyList<UserDto>> GetAllUsersAsync(CancellationToken ct = default);
    Task<IReadOnlyList<RoleDto>> GetAllRolesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<PermissionDto>> GetAllPermissionsAsync(CancellationToken ct = default);
}

public interface IEmployeeService
{
    Task<EmployeeDto> CreateAsync(CreateEmployeeDto dto, int actingUserId, CancellationToken ct = default);
    Task<EmployeeDto> UpdateAsync(int id, UpdateEmployeeDto dto, int actingUserId, CancellationToken ct = default);
    Task DeactivateAsync(int id, int actingUserId, CancellationToken ct = default);
    Task<EmployeeDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<EmployeeDto?> GetByUserIdAsync(int userId, CancellationToken ct = default);
    Task<PagedResult<EmployeeDto>> GetPagedAsync(EmployeeQueryDto query, CancellationToken ct = default);
    Task<IReadOnlyList<EmployeeDto>> GetTeamMembersAsync(int managerEmployeeId, CancellationToken ct = default);
}

public interface IDepartmentService
{
    Task<DepartmentDto> CreateAsync(CreateDepartmentDto dto, int actingUserId, CancellationToken ct = default);
    Task<DepartmentDto> UpdateAsync(int id, UpdateDepartmentDto dto, int actingUserId, CancellationToken ct = default);
    Task DeleteAsync(int id, int actingUserId, CancellationToken ct = default);
    Task<DepartmentDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<DepartmentDto>> GetAllAsync(CancellationToken ct = default);
}

public interface IPositionService
{
    Task<PositionDto> CreateAsync(CreatePositionDto dto, int actingUserId, CancellationToken ct = default);
    Task<PositionDto> UpdateAsync(int id, UpdatePositionDto dto, int actingUserId, CancellationToken ct = default);
    Task DeleteAsync(int id, int actingUserId, CancellationToken ct = default);
    Task<PositionDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<PositionDto>> GetAllAsync(CancellationToken ct = default);
}

public interface IAttendanceService
{
    Task<AttendanceDto> CheckInAsync(int employeeId, int actingUserId, CancellationToken ct = default);
    Task<AttendanceDto> CheckOutAsync(int employeeId, int actingUserId, CancellationToken ct = default);
    Task<AttendanceDto?> GetTodayStatusAsync(int employeeId, CancellationToken ct = default);
    Task<IReadOnlyList<AttendanceDto>> GetHistoryAsync(AttendanceQueryDto query, CancellationToken ct = default);
}

public interface ILeaveService
{
    Task<LeaveDto> ApplyAsync(ApplyLeaveDto dto, int actingUserId, CancellationToken ct = default);
    Task<LeaveDto> ApproveAsync(int leaveId, ApproveLeaveDto dto, int actingUserId, CancellationToken ct = default);
    Task CancelAsync(int leaveId, int actingUserId, CancellationToken ct = default);
    Task<IReadOnlyList<LeaveDto>> GetLeavesAsync(int? employeeId, LeaveStatus? status, DateOnly? from, DateOnly? to, int? managerId, CancellationToken ct = default);
    Task<IReadOnlyList<LeaveTypeDto>> GetLeaveTypesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<LeaveBalanceDto>> GetLeaveBalancesAsync(int employeeId, int? year, CancellationToken ct = default);
}

public interface ISalaryService
{
    Task<SalaryStructureDto> GetStructureByEmployeeIdAsync(int employeeId, CancellationToken ct = default);
    Task<SalaryStructureDto> UpdateStructureAsync(int employeeId, UpdateSalaryStructureDto dto, int actingUserId, CancellationToken ct = default);
    Task<PayslipDto> GeneratePayslipAsync(int employeeId, int month, int year, int actingUserId, CancellationToken ct = default);
    Task<IReadOnlyList<PayslipDto>> GetPayslipsAsync(int employeeId, int? year, CancellationToken ct = default);
    Task<PayslipDto> GetPayslipByIdAsync(int id, CancellationToken ct = default);
}

public interface IProjectService
{
    Task<ProjectDto> CreateAsync(CreateProjectDto dto, int actingUserId, CancellationToken ct = default);
    Task<ProjectDto> UpdateAsync(int id, UpdateProjectDto dto, int actingUserId, CancellationToken ct = default);
    Task<ProjectDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<ProjectDto>> GetAllAsync(int? managerId, CancellationToken ct = default);
    Task AssignEmployeeAsync(int projectId, AssignEmployeeDto dto, int actingUserId, CancellationToken ct = default);
    Task RemoveEmployeeAssignmentAsync(int projectId, int employeeId, int actingUserId, CancellationToken ct = default);
}

public interface IAuditService
{
    Task LogAsync(int? userId, string action, string entityName, string entityId, object? oldValue, object? newValue, string? correlationId = null, string? ipAddress = null, CancellationToken ct = default);
    Task<PagedResult<AuditLogDto>> GetPagedAsync(AuditLogQueryDto query, CancellationToken ct = default);
}

public interface INotificationService
{
    Task NotifyAsync(int userId, string type, string title, string message, CancellationToken ct = default);
    Task<IReadOnlyList<NotificationDto>> GetUserNotificationsAsync(int userId, bool unreadOnly, CancellationToken ct = default);
    Task<int> GetUnreadCountAsync(int userId, CancellationToken ct = default);
    Task MarkAsReadAsync(int notificationId, int userId, CancellationToken ct = default);
}

public interface IReportService
{
    Task<IReadOnlyList<HeadcountByDepartmentReportDto>> GetHeadcountByDepartmentAsync(CancellationToken ct = default);
    Task<IReadOnlyList<AttendanceSummaryReportDto>> GetAttendanceSummaryAsync(DateOnly? from, DateOnly? to, int? departmentId, CancellationToken ct = default);
    Task<IReadOnlyList<LeaveUtilizationReportDto>> GetLeaveUtilizationAsync(int? year, CancellationToken ct = default);
    Task<PayrollSummaryReportDto> GetPayrollSummaryAsync(int month, int year, CancellationToken ct = default);
    Task<IReadOnlyList<ProjectAllocationReportDto>> GetProjectAllocationAsync(int? projectId, CancellationToken ct = default);
}
