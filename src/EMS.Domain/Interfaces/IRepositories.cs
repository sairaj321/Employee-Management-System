using EMS.Domain.Entities;
using EMS.Domain.Enums;

namespace EMS.Domain.Interfaces;

public interface IEmployeeRepository
{
    Task<Employee?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Employee?> GetByUserIdAsync(int userId, CancellationToken ct = default);
    Task<Employee?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<(IReadOnlyList<Employee> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, string? search, int? departmentId, CancellationToken ct = default);
    Task<IReadOnlyList<Employee>> GetByManagerIdAsync(int managerId, CancellationToken ct = default);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default);
    Task<bool> ExistsByCodeAsync(string code, CancellationToken ct = default);
    Task<string> GetNextEmployeeCodeAsync(CancellationToken ct = default);
    Task AddAsync(Employee employee, CancellationToken ct = default);
    void Update(Employee employee);
    void Remove(Employee employee);
}

public interface IDepartmentRepository
{
    Task<Department?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<Department>> GetAllAsync(CancellationToken ct = default);
    Task<bool> ExistsByNameAsync(string name, int? excludeId = null, CancellationToken ct = default);
    Task AddAsync(Department department, CancellationToken ct = default);
    void Update(Department department);
    void Remove(Department department);
}

public interface IPositionRepository
{
    Task<Position?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<Position>> GetAllAsync(CancellationToken ct = default);
    Task<bool> ExistsByTitleAsync(string title, int? excludeId = null, CancellationToken ct = default);
    Task AddAsync(Position position, CancellationToken ct = default);
    void Update(Position position);
    void Remove(Position position);
}

public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<IReadOnlyList<User>> GetAllAsync(CancellationToken ct = default);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default);
    Task AddAsync(User user, CancellationToken ct = default);
    void Update(User user);
    Task<RefreshToken?> GetRefreshTokenAsync(string tokenHash, CancellationToken ct = default);
    Task AddRefreshTokenAsync(RefreshToken token, CancellationToken ct = default);
    Task RevokeRefreshTokenFamilyAsync(int userId, CancellationToken ct = default);
    Task<IReadOnlyList<Role>> GetAllRolesAsync(CancellationToken ct = default);
    Task<Role?> GetRoleByIdAsync(int roleId, CancellationToken ct = default);
    Task<IReadOnlyList<Permission>> GetAllPermissionsAsync(CancellationToken ct = default);
}

public interface IAttendanceRepository
{
    Task<Attendance?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Attendance?> GetTodayAttendanceAsync(int employeeId, DateOnly date, CancellationToken ct = default);
    Task<IReadOnlyList<Attendance>> GetAttendancesAsync(int? employeeId, DateOnly? from, DateOnly? to, int? departmentId, CancellationToken ct = default);
    Task<bool> ExistsForDateAsync(int employeeId, DateOnly date, CancellationToken ct = default);
    Task AddAsync(Attendance attendance, CancellationToken ct = default);
    void Update(Attendance attendance);
}

public interface ILeaveRepository
{
    Task<Leave?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<Leave>> GetLeavesAsync(int? employeeId, LeaveStatus? status, DateOnly? from, DateOnly? to, int? managerId, CancellationToken ct = default);
    Task<IReadOnlyList<LeaveType>> GetAllLeaveTypesAsync(CancellationToken ct = default);
    Task<LeaveType?> GetLeaveTypeByIdAsync(int id, CancellationToken ct = default);
    Task<bool> HasOverlappingLeaveAsync(int employeeId, DateOnly startDate, DateOnly endDate, int? excludeLeaveId = null, CancellationToken ct = default);
    Task AddAsync(Leave leave, CancellationToken ct = default);
    void Update(Leave leave);
}

public interface ISalaryRepository
{
    Task<SalaryStructure?> GetStructureByEmployeeIdAsync(int employeeId, CancellationToken ct = default);
    Task AddStructureAsync(SalaryStructure salaryStructure, CancellationToken ct = default);
    void UpdateStructure(SalaryStructure salaryStructure);
    Task<Payslip?> GetPayslipByIdAsync(int id, CancellationToken ct = default);
    Task<Payslip?> GetPayslipAsync(int employeeId, int month, int year, CancellationToken ct = default);
    Task<IReadOnlyList<Payslip>> GetPayslipsByEmployeeAsync(int employeeId, int? year, CancellationToken ct = default);
    Task<IReadOnlyList<Payslip>> GetPayrollSummaryAsync(int month, int year, CancellationToken ct = default);
    Task AddPayslipAsync(Payslip payslip, CancellationToken ct = default);
}

public interface IProjectRepository
{
    Task<Project?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<Project>> GetAllAsync(int? managerId = null, CancellationToken ct = default);
    Task AddAsync(Project project, CancellationToken ct = default);
    void Update(Project project);
    Task<bool> IsEmployeeAssignedAsync(int projectId, int employeeId, CancellationToken ct = default);
    Task AssignEmployeeAsync(EmployeeProject employeeProject, CancellationToken ct = default);
    Task RemoveEmployeeAssignmentAsync(int projectId, int employeeId, CancellationToken ct = default);
}

public interface IAuditLogRepository
{
    Task AddAsync(AuditLog log, CancellationToken ct = default);
    Task<(IReadOnlyList<AuditLog> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, string? entityName, int? userId, DateTime? from, DateTime? to, CancellationToken ct = default);
}

public interface INotificationRepository
{
    Task AddAsync(Notification notification, CancellationToken ct = default);
    Task<IReadOnlyList<Notification>> GetUserNotificationsAsync(int userId, bool unreadOnly = false, CancellationToken ct = default);
    Task<int> GetUnreadCountAsync(int userId, CancellationToken ct = default);
    Task<Notification?> GetByIdAsync(int id, CancellationToken ct = default);
    void Update(Notification notification);
}

public interface IUnitOfWork
{
    IEmployeeRepository Employees { get; }
    IDepartmentRepository Departments { get; }
    IPositionRepository Positions { get; }
    IUserRepository Users { get; }
    ILeaveRepository Leaves { get; }
    IAttendanceRepository Attendance { get; }
    ISalaryRepository Salary { get; }
    IProjectRepository Projects { get; }
    IAuditLogRepository AuditLogs { get; }
    INotificationRepository Notifications { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
