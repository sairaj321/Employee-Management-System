using EMS.Domain.Entities;
using EMS.Domain.Enums;
using EMS.Domain.Interfaces;
using EMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EMS.Infrastructure.Repositories;

public class EmployeeRepository : IEmployeeRepository
{
    private readonly AppDbContext _context;

    public EmployeeRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Employee?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _context.Employees
            .Include(e => e.Department)
            .Include(e => e.Position)
            .Include(e => e.Manager)
            .Include(e => e.User)
            .Include(e => e.SalaryStructure)
            .FirstOrDefaultAsync(e => e.Id == id, ct);
    }

    public async Task<Employee?> GetByUserIdAsync(int userId, CancellationToken ct = default)
    {
        return await _context.Employees
            .Include(e => e.Department)
            .Include(e => e.Position)
            .Include(e => e.Manager)
            .Include(e => e.SalaryStructure)
            .FirstOrDefaultAsync(e => e.UserId == userId, ct);
    }

    public async Task<Employee?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        return await _context.Employees
            .Include(e => e.Department)
            .Include(e => e.Position)
            .FirstOrDefaultAsync(e => e.Email.ToLower() == email.ToLower(), ct);
    }

    public async Task<(IReadOnlyList<Employee> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, string? search, int? departmentId, CancellationToken ct = default)
    {
        var query = _context.Employees
            .Include(e => e.Department)
            .Include(e => e.Position)
            .Include(e => e.Manager)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(e =>
                e.FirstName.ToLower().Contains(term) ||
                e.LastName.ToLower().Contains(term) ||
                e.Email.ToLower().Contains(term) ||
                e.EmployeeCode.ToLower().Contains(term));
        }

        if (departmentId.HasValue && departmentId.Value > 0)
        {
            query = query.Where(e => e.DepartmentId == departmentId.Value);
        }

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderBy(e => e.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<Employee>> GetByManagerIdAsync(int managerId, CancellationToken ct = default)
    {
        return await _context.Employees
            .Include(e => e.Department)
            .Include(e => e.Position)
            .Where(e => e.ManagerId == managerId)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default)
    {
        return await _context.Employees.AnyAsync(e => e.Email.ToLower() == email.ToLower(), ct);
    }

    public async Task<bool> ExistsByCodeAsync(string code, CancellationToken ct = default)
    {
        return await _context.Employees.AnyAsync(e => e.EmployeeCode.ToLower() == code.ToLower(), ct);
    }

    public async Task<string> GetNextEmployeeCodeAsync(CancellationToken ct = default)
    {
        var count = await _context.Employees.CountAsync(ct);
        return $"EMP-{(count + 1):D4}";
    }

    public async Task AddAsync(Employee employee, CancellationToken ct = default)
    {
        await _context.Employees.AddAsync(employee, ct);
    }

    public void Update(Employee employee)
    {
        _context.Employees.Update(employee);
    }

    public void Remove(Employee employee)
    {
        _context.Employees.Remove(employee);
    }
}

public class DepartmentRepository : IDepartmentRepository
{
    private readonly AppDbContext _context;

    public DepartmentRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Department?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _context.Departments
            .Include(d => d.Manager)
            .Include(d => d.Employees)
            .FirstOrDefaultAsync(d => d.Id == id, ct);
    }

    public async Task<IReadOnlyList<Department>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.Departments
            .Include(d => d.Manager)
            .Include(d => d.Employees)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<bool> ExistsByNameAsync(string name, int? excludeId = null, CancellationToken ct = default)
    {
        return await _context.Departments
            .AnyAsync(d => d.Name.ToLower() == name.ToLower() && (!excludeId.HasValue || d.Id != excludeId.Value), ct);
    }

    public async Task AddAsync(Department department, CancellationToken ct = default)
    {
        await _context.Departments.AddAsync(department, ct);
    }

    public void Update(Department department)
    {
        _context.Departments.Update(department);
    }

    public void Remove(Department department)
    {
        _context.Departments.Remove(department);
    }
}

public class PositionRepository : IPositionRepository
{
    private readonly AppDbContext _context;

    public PositionRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Position?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _context.Positions
            .Include(p => p.Employees)
            .FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    public async Task<IReadOnlyList<Position>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.Positions.AsNoTracking().ToListAsync(ct);
    }

    public async Task<bool> ExistsByTitleAsync(string title, int? excludeId = null, CancellationToken ct = default)
    {
        return await _context.Positions
            .AnyAsync(p => p.Title.ToLower() == title.ToLower() && (!excludeId.HasValue || p.Id != excludeId.Value), ct);
    }

    public async Task AddAsync(Position position, CancellationToken ct = default)
    {
        await _context.Positions.AddAsync(position, ct);
    }

    public void Update(Position position)
    {
        _context.Positions.Update(position);
    }

    public void Remove(Position position)
    {
        _context.Positions.Remove(position);
    }
}

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _context.Users
            .Include(u => u.Employee)
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.Id == id, ct);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        return await _context.Users
            .Include(u => u.Employee)
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower(), ct);
    }

    public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.Users
            .Include(u => u.Employee)
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default)
    {
        return await _context.Users.AnyAsync(u => u.Email.ToLower() == email.ToLower(), ct);
    }

    public async Task AddAsync(User user, CancellationToken ct = default)
    {
        await _context.Users.AddAsync(user, ct);
    }

    public void Update(User user)
    {
        _context.Users.Update(user);
    }

    public async Task<RefreshToken?> GetRefreshTokenAsync(string tokenHash, CancellationToken ct = default)
    {
        return await _context.RefreshTokens
            .Include(rt => rt.User)
                .ThenInclude(u => u.Employee)
            .Include(rt => rt.User)
                .ThenInclude(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                        .ThenInclude(r => r.RolePermissions)
                            .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, ct);
    }

    public async Task AddRefreshTokenAsync(RefreshToken token, CancellationToken ct = default)
    {
        await _context.RefreshTokens.AddAsync(token, ct);
    }

    public async Task RevokeRefreshTokenFamilyAsync(int userId, CancellationToken ct = default)
    {
        var tokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null)
            .ToListAsync(ct);

        foreach (var token in tokens)
        {
            token.RevokedAt = DateTime.UtcNow;
        }
    }

    public async Task<IReadOnlyList<Role>> GetAllRolesAsync(CancellationToken ct = default)
    {
        return await _context.Roles
            .Include(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<Role?> GetRoleByIdAsync(int roleId, CancellationToken ct = default)
    {
        return await _context.Roles
            .Include(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(r => r.Id == roleId, ct);
    }

    public async Task<IReadOnlyList<Permission>> GetAllPermissionsAsync(CancellationToken ct = default)
    {
        return await _context.Permissions.AsNoTracking().ToListAsync(ct);
    }
}
