using EMS.Domain.Entities;
using EMS.Domain.Enums;
using EMS.Domain.Interfaces;
using EMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EMS.Infrastructure.Repositories;

public class AttendanceRepository : IAttendanceRepository
{
    private readonly AppDbContext _context;

    public AttendanceRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Attendance?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _context.Attendances
            .Include(a => a.Employee)
            .FirstOrDefaultAsync(a => a.Id == id, ct);
    }

    public async Task<Attendance?> GetTodayAttendanceAsync(int employeeId, DateOnly date, CancellationToken ct = default)
    {
        return await _context.Attendances
            .FirstOrDefaultAsync(a => a.EmployeeId == employeeId && a.Date == date, ct);
    }

    public async Task<IReadOnlyList<Attendance>> GetAttendancesAsync(int? employeeId, DateOnly? from, DateOnly? to, int? departmentId, CancellationToken ct = default)
    {
        var query = _context.Attendances
            .Include(a => a.Employee)
                .ThenInclude(e => e.Department)
            .AsNoTracking()
            .AsQueryable();

        if (employeeId.HasValue && employeeId.Value > 0)
            query = query.Where(a => a.EmployeeId == employeeId.Value);

        if (from.HasValue)
            query = query.Where(a => a.Date >= from.Value);

        if (to.HasValue)
            query = query.Where(a => a.Date <= to.Value);

        if (departmentId.HasValue && departmentId.Value > 0)
            query = query.Where(a => a.Employee.DepartmentId == departmentId.Value);

        return await query.OrderByDescending(a => a.Date).ToListAsync(ct);
    }

    public async Task<bool> ExistsForDateAsync(int employeeId, DateOnly date, CancellationToken ct = default)
    {
        return await _context.Attendances.AnyAsync(a => a.EmployeeId == employeeId && a.Date == date, ct);
    }

    public async Task AddAsync(Attendance attendance, CancellationToken ct = default)
    {
        await _context.Attendances.AddAsync(attendance, ct);
    }

    public void Update(Attendance attendance)
    {
        _context.Attendances.Update(attendance);
    }
}

public class LeaveRepository : ILeaveRepository
{
    private readonly AppDbContext _context;

    public LeaveRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Leave?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _context.Leaves
            .Include(l => l.Employee)
            .Include(l => l.LeaveType)
            .Include(l => l.Approver)
            .FirstOrDefaultAsync(l => l.Id == id, ct);
    }

    public async Task<IReadOnlyList<Leave>> GetLeavesAsync(int? employeeId, LeaveStatus? status, DateOnly? from, DateOnly? to, int? managerId, CancellationToken ct = default)
    {
        var query = _context.Leaves
            .Include(l => l.Employee)
                .ThenInclude(e => e.Department)
            .Include(l => l.LeaveType)
            .Include(l => l.Approver)
            .AsNoTracking()
            .AsQueryable();

        if (employeeId.HasValue && employeeId.Value > 0)
            query = query.Where(l => l.EmployeeId == employeeId.Value);

        if (status.HasValue)
            query = query.Where(l => l.Status == status.Value);

        if (from.HasValue)
            query = query.Where(l => l.EndDate >= from.Value);

        if (to.HasValue)
            query = query.Where(l => l.StartDate <= to.Value);

        if (managerId.HasValue && managerId.Value > 0)
            query = query.Where(l => l.Employee.ManagerId == managerId.Value);

        return await query.OrderByDescending(l => l.StartDate).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<LeaveType>> GetAllLeaveTypesAsync(CancellationToken ct = default)
    {
        return await _context.LeaveTypes.AsNoTracking().ToListAsync(ct);
    }

    public async Task<LeaveType?> GetLeaveTypeByIdAsync(int id, CancellationToken ct = default)
    {
        return await _context.LeaveTypes.FirstOrDefaultAsync(lt => lt.Id == id, ct);
    }

    public async Task<bool> HasOverlappingLeaveAsync(int employeeId, DateOnly startDate, DateOnly endDate, int? excludeLeaveId = null, CancellationToken ct = default)
    {
        return await _context.Leaves
            .Where(l => l.EmployeeId == employeeId &&
                        (l.Status == LeaveStatus.Approved || l.Status == LeaveStatus.Pending) &&
                        (!excludeLeaveId.HasValue || l.Id != excludeLeaveId.Value))
            .AnyAsync(l => l.StartDate <= endDate && l.EndDate >= startDate, ct);
    }

    public async Task AddAsync(Leave leave, CancellationToken ct = default)
    {
        await _context.Leaves.AddAsync(leave, ct);
    }

    public void Update(Leave leave)
    {
        _context.Leaves.Update(leave);
    }
}

public class SalaryRepository : ISalaryRepository
{
    private readonly AppDbContext _context;

    public SalaryRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<SalaryStructure?> GetStructureByEmployeeIdAsync(int employeeId, CancellationToken ct = default)
    {
        return await _context.SalaryStructures
            .Include(s => s.Employee)
            .FirstOrDefaultAsync(s => s.EmployeeId == employeeId, ct);
    }

    public async Task AddStructureAsync(SalaryStructure salaryStructure, CancellationToken ct = default)
    {
        await _context.SalaryStructures.AddAsync(salaryStructure, ct);
    }

    public void UpdateStructure(SalaryStructure salaryStructure)
    {
        _context.SalaryStructures.Update(salaryStructure);
    }

    public async Task<Payslip?> GetPayslipByIdAsync(int id, CancellationToken ct = default)
    {
        return await _context.Payslips
            .Include(p => p.Employee)
                .ThenInclude(e => e.Department)
            .FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    public async Task<Payslip?> GetPayslipAsync(int employeeId, int month, int year, CancellationToken ct = default)
    {
        return await _context.Payslips
            .Include(p => p.Employee)
            .FirstOrDefaultAsync(p => p.EmployeeId == employeeId && p.Month == month && p.Year == year, ct);
    }

    public async Task<IReadOnlyList<Payslip>> GetPayslipsByEmployeeAsync(int employeeId, int? year, CancellationToken ct = default)
    {
        var query = _context.Payslips
            .Include(p => p.Employee)
            .Where(p => p.EmployeeId == employeeId)
            .AsNoTracking()
            .AsQueryable();

        if (year.HasValue)
            query = query.Where(p => p.Year == year.Value);

        return await query.OrderByDescending(p => p.Year).ThenByDescending(p => p.Month).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Payslip>> GetPayrollSummaryAsync(int month, int year, CancellationToken ct = default)
    {
        return await _context.Payslips
            .Include(p => p.Employee)
                .ThenInclude(e => e.Department)
            .Where(p => p.Month == month && p.Year == year)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task AddPayslipAsync(Payslip payslip, CancellationToken ct = default)
    {
        await _context.Payslips.AddAsync(payslip, ct);
    }
}

public class ProjectRepository : IProjectRepository
{
    private readonly AppDbContext _context;

    public ProjectRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Project?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _context.Projects
            .Include(p => p.Manager)
            .Include(p => p.EmployeeProjects)
                .ThenInclude(ep => ep.Employee)
            .FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    public async Task<IReadOnlyList<Project>> GetAllAsync(int? managerId = null, CancellationToken ct = default)
    {
        var query = _context.Projects
            .Include(p => p.Manager)
            .Include(p => p.EmployeeProjects)
                .ThenInclude(ep => ep.Employee)
            .AsNoTracking()
            .AsQueryable();

        if (managerId.HasValue && managerId.Value > 0)
            query = query.Where(p => p.ManagerId == managerId.Value);

        return await query.OrderByDescending(p => p.StartDate).ToListAsync(ct);
    }

    public async Task AddAsync(Project project, CancellationToken ct = default)
    {
        await _context.Projects.AddAsync(project, ct);
    }

    public void Update(Project project)
    {
        _context.Projects.Update(project);
    }

    public async Task<bool> IsEmployeeAssignedAsync(int projectId, int employeeId, CancellationToken ct = default)
    {
        return await _context.EmployeeProjects.AnyAsync(ep => ep.ProjectId == projectId && ep.EmployeeId == employeeId, ct);
    }

    public async Task AssignEmployeeAsync(EmployeeProject employeeProject, CancellationToken ct = default)
    {
        await _context.EmployeeProjects.AddAsync(employeeProject, ct);
    }

    public async Task RemoveEmployeeAssignmentAsync(int projectId, int employeeId, CancellationToken ct = default)
    {
        var ep = await _context.EmployeeProjects.FirstOrDefaultAsync(x => x.ProjectId == projectId && x.EmployeeId == employeeId, ct);
        if (ep != null)
            _context.EmployeeProjects.Remove(ep);
    }
}

public class AuditLogRepository : IAuditLogRepository
{
    private readonly AppDbContext _context;

    public AuditLogRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(AuditLog log, CancellationToken ct = default)
    {
        await _context.AuditLogs.AddAsync(log, ct);
    }

    public async Task<(IReadOnlyList<AuditLog> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, string? entityName, int? userId, DateTime? from, DateTime? to, CancellationToken ct = default)
    {
        var query = _context.AuditLogs
            .Include(a => a.User)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(entityName))
            query = query.Where(a => a.EntityName.ToLower() == entityName.ToLower());

        if (userId.HasValue && userId.Value > 0)
            query = query.Where(a => a.UserId == userId.Value);

        if (from.HasValue)
            query = query.Where(a => a.Timestamp >= from.Value);

        if (to.HasValue)
            query = query.Where(a => a.Timestamp <= to.Value);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(a => a.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }
}

public class NotificationRepository : INotificationRepository
{
    private readonly AppDbContext _context;

    public NotificationRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Notification notification, CancellationToken ct = default)
    {
        await _context.Notifications.AddAsync(notification, ct);
    }

    public async Task<IReadOnlyList<Notification>> GetUserNotificationsAsync(int userId, bool unreadOnly = false, CancellationToken ct = default)
    {
        var query = _context.Notifications
            .Where(n => n.UserId == userId)
            .AsNoTracking()
            .AsQueryable();

        if (unreadOnly)
            query = query.Where(n => !n.IsRead);

        return await query.OrderByDescending(n => n.CreatedAt).Take(50).ToListAsync(ct);
    }

    public async Task<int> GetUnreadCountAsync(int userId, CancellationToken ct = default)
    {
        return await _context.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead, ct);
    }

    public async Task<Notification?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _context.Notifications.FirstOrDefaultAsync(n => n.Id == id, ct);
    }

    public void Update(Notification notification)
    {
        _context.Notifications.Update(notification);
    }
}

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    public UnitOfWork(
        AppDbContext context,
        IEmployeeRepository employees,
        IDepartmentRepository departments,
        IPositionRepository positions,
        IUserRepository users,
        ILeaveRepository leaves,
        IAttendanceRepository attendance,
        ISalaryRepository salary,
        IProjectRepository projects,
        IAuditLogRepository auditLogs,
        INotificationRepository notifications)
    {
        _context = context;
        Employees = employees;
        Departments = departments;
        Positions = positions;
        Users = users;
        Leaves = leaves;
        Attendance = attendance;
        Salary = salary;
        Projects = projects;
        AuditLogs = auditLogs;
        Notifications = notifications;
    }

    public IEmployeeRepository Employees { get; }
    public IDepartmentRepository Departments { get; }
    public IPositionRepository Positions { get; }
    public IUserRepository Users { get; }
    public ILeaveRepository Leaves { get; }
    public IAttendanceRepository Attendance { get; }
    public ISalaryRepository Salary { get; }
    public IProjectRepository Projects { get; }
    public IAuditLogRepository AuditLogs { get; }
    public INotificationRepository Notifications { get; }

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return await _context.SaveChangesAsync(ct);
    }
}
