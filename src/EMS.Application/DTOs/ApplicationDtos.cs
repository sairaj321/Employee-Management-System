using System.Text.Json.Serialization;
using EMS.Domain.Enums;

namespace EMS.Application.DTOs;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public object? Errors { get; set; }
    public string? TraceId { get; set; }

    public static ApiResponse<T> SuccessResult(T data, string message = "Success") =>
        new() { Success = true, Message = message, Data = data, Errors = Array.Empty<object>() };

    public static ApiResponse<T> FailureResult(string message, object? errors = null, string? traceId = null) =>
        new() { Success = false, Message = message, Data = default, Errors = errors ?? Array.Empty<object>(), TraceId = traceId };
}

public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; set; } = new List<T>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / (PageSize > 0 ? PageSize : 1));
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}

// Auth DTOs
public class LoginRequestDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    public UserDto User { get; set; } = null!;
}

public class RefreshTokenRequestDto
{
    public string RefreshToken { get; set; } = string.Empty;
}

public class ChangePasswordDto
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

public class UserDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public bool IsLocked { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? EmployeeId { get; set; }
    public string? FullName { get; set; }
    public List<string> Roles { get; set; } = new();
    public List<string> Permissions { get; set; } = new();
}

public class CreateUserDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public List<int> RoleIds { get; set; } = new();
}

public class RoleDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<PermissionDto> Permissions { get; set; } = new();
}

public class PermissionDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
}

// Employee DTOs
public class CreateEmployeeDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public int DepartmentId { get; set; }
    public int PositionId { get; set; }
    public int? ManagerId { get; set; }
    public DateTime JoiningDate { get; set; }
    public EmploymentType EmploymentType { get; set; } = EmploymentType.Permanent;
    public string? InitialPassword { get; set; }
    public List<int>? RoleIds { get; set; }
}

public class UpdateEmployeeDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public int DepartmentId { get; set; }
    public int PositionId { get; set; }
    public int? ManagerId { get; set; }
    public EmploymentType EmploymentType { get; set; }
    public EmployeeStatus Status { get; set; }
}

public class EmployeeDto
{
    public int Id { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public int DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public int PositionId { get; set; }
    public string? PositionTitle { get; set; }
    public int? ManagerId { get; set; }
    public string? ManagerName { get; set; }
    public DateTime JoiningDate { get; set; }
    public EmploymentType EmploymentType { get; set; }
    public EmployeeStatus Status { get; set; }
    public int UserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class EmployeeQueryDto
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Search { get; set; }
    public int? DepartmentId { get; set; }
}

// Department DTOs
public class CreateDepartmentDto
{
    public string Name { get; set; } = string.Empty;
    public string? Location { get; set; }
    public int? ManagerId { get; set; }
}

public class UpdateDepartmentDto
{
    public string Name { get; set; } = string.Empty;
    public string? Location { get; set; }
    public int? ManagerId { get; set; }
}

public class DepartmentDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Location { get; set; }
    public int? ManagerId { get; set; }
    public string? ManagerName { get; set; }
    public int EmployeeCount { get; set; }
}

// Position DTOs
public class CreatePositionDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class UpdatePositionDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class PositionDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int EmployeeCount { get; set; }
}

// Attendance DTOs
public class CheckInRequestDto
{
    public int? EmployeeId { get; set; }
}

public class AttendanceDto
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string EmployeeCode { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public DateTime? CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public AttendanceStatus Status { get; set; }
    public double? TotalHours => (CheckInTime.HasValue && CheckOutTime.HasValue) ? (CheckOutTime.Value - CheckInTime.Value).TotalHours : null;
}

public class AttendanceQueryDto
{
    public int? EmployeeId { get; set; }
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
    public int? DepartmentId { get; set; }
}

// Leave DTOs
public class ApplyLeaveDto
{
    public int? EmployeeId { get; set; }
    public int LeaveTypeId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class ApproveLeaveDto
{
    public bool Approved { get; set; }
    public string? Comments { get; set; }
}

public class LeaveTypeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DefaultDaysPerYear { get; set; }
}

public class LeaveBalanceDto
{
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public int LeaveTypeId { get; set; }
    public string LeaveTypeName { get; set; } = string.Empty;
    public int Year { get; set; }
    public int TotalAllocatedDays { get; set; }
    public int UsedDays { get; set; }
    public int PendingDays { get; set; }
    public int AvailableDays => Math.Max(0, TotalAllocatedDays - UsedDays - PendingDays);
}

public class LeaveDto
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string EmployeeCode { get; set; } = string.Empty;
    public int LeaveTypeId { get; set; }
    public string LeaveTypeName { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public int DaysCount => (EndDate.DayNumber - StartDate.DayNumber) + 1;
    public string Reason { get; set; } = string.Empty;
    public LeaveStatus Status { get; set; }
    public int? ApprovedBy { get; set; }
    public string? ApproverName { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public int? AvailableDaysRemaining { get; set; }
}

// Salary DTOs
public class SalaryStructureDto
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string? EmployeeName { get; set; }
    public decimal BasicSalary { get; set; }
    public decimal HRA { get; set; }
    public decimal OtherAllowances { get; set; }
    public decimal PFDeduction { get; set; }
    public decimal GrossSalary => BasicSalary + HRA + OtherAllowances;
    public decimal NetSalary => GrossSalary - PFDeduction;
    public DateTime EffectiveFrom { get; set; }
}

public class UpdateSalaryStructureDto
{
    public decimal BasicSalary { get; set; }
    public decimal HRA { get; set; }
    public decimal OtherAllowances { get; set; }
    public decimal PFDeduction { get; set; }
    public DateTime? EffectiveFrom { get; set; }
}

public class PayslipDto
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string EmployeeCode { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public int Month { get; set; }
    public int Year { get; set; }
    public decimal GrossSalary { get; set; }
    public decimal Deductions { get; set; }
    public decimal NetSalary { get; set; }
    public DateTime GeneratedAt { get; set; }
}

public class GeneratePayslipDto
{
    public int Month { get; set; }
    public int Year { get; set; }
}

// Project DTOs
public class CreateProjectDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? ManagerId { get; set; }
}

public class UpdateProjectDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string Status { get; set; } = "Active";
    public int? ManagerId { get; set; }
}

public class ProjectDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string Status { get; set; } = "Active";
    public int? ManagerId { get; set; }
    public string? ManagerName { get; set; }
    public List<ProjectEmployeeDto> AssignedEmployees { get; set; } = new();
}

public class ProjectEmployeeDto
{
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string EmployeeCode { get; set; } = string.Empty;
    public DateTime AllocatedFrom { get; set; }
    public DateTime? AllocatedTo { get; set; }
}

public class AssignEmployeeDto
{
    public int EmployeeId { get; set; }
    public DateTime AllocatedFrom { get; set; }
    public DateTime? AllocatedTo { get; set; }
}

// Audit Log DTOs
public class AuditLogDto
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public string? UserEmail { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? IpAddress { get; set; }
    public string? CorrelationId { get; set; }
    public DateTime Timestamp { get; set; }
}

public class AuditLogQueryDto
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? EntityName { get; set; }
    public int? UserId { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
}

// Notification DTOs
public class NotificationDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}

// Report DTOs
public class HeadcountByDepartmentReportDto
{
    public int DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public int ActiveEmployees { get; set; }
    public int TotalEmployees { get; set; }
}

public class AttendanceSummaryReportDto
{
    public DateOnly Date { get; set; }
    public int TotalPresent { get; set; }
    public int TotalAbsent { get; set; }
    public int TotalLate { get; set; }
    public int TotalHalfDay { get; set; }
}

public class LeaveUtilizationReportDto
{
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public int TotalLeavesTaken { get; set; }
    public int PendingRequests { get; set; }
}

public class PayrollSummaryReportDto
{
    public int Month { get; set; }
    public int Year { get; set; }
    public int TotalEmployeesPaid { get; set; }
    public decimal TotalGrossSalary { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal TotalNetSalary { get; set; }
}

public class ProjectAllocationReportDto
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string ManagerName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int AssignedMembersCount { get; set; }
}
