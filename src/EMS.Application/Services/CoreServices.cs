using EMS.Application.DTOs;
using EMS.Application.Exceptions;
using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using EMS.Domain.Enums;
using EMS.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace EMS.Application.Services;

public class EmployeeService : IEmployeeService
{
    private readonly IUnitOfWork _uow;
    private readonly IPasswordHasher _hasher;
    private readonly IAuditService _auditService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<EmployeeService> _logger;

    public EmployeeService(
        IUnitOfWork uow,
        IPasswordHasher hasher,
        IAuditService auditService,
        INotificationService notificationService,
        ILogger<EmployeeService> logger)
    {
        _uow = uow;
        _hasher = hasher;
        _auditService = auditService;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<EmployeeDto> CreateAsync(CreateEmployeeDto dto, int actingUserId, CancellationToken ct = default)
    {
        if (await _uow.Employees.ExistsByEmailAsync(dto.Email, ct))
        {
            throw new ConflictException($"Email '{dto.Email}' is already in use.");
        }

        var department = await _uow.Departments.GetByIdAsync(dto.DepartmentId, ct);
        if (department == null)
            throw new NotFoundException(nameof(Department), dto.DepartmentId);

        var position = await _uow.Positions.GetByIdAsync(dto.PositionId, ct);
        if (position == null)
            throw new NotFoundException(nameof(Position), dto.PositionId);

        // Create User account
        var defaultPassword = !string.IsNullOrWhiteSpace(dto.InitialPassword) ? dto.InitialPassword : "Employee@123";
        var (hash, salt) = _hasher.HashPassword(defaultPassword);

        var user = new User
        {
            Email = dto.Email.Trim().ToLower(),
            PasswordHash = hash,
            PasswordSalt = salt,
            CreatedAt = DateTime.UtcNow
        };
        await _uow.Users.AddAsync(user, ct);
        await _uow.SaveChangesAsync(ct);

        // Assign Role (default Employee role or selected)
        var roles = await _uow.Users.GetAllRolesAsync(ct);
        var targetRoleIds = dto.RoleIds != null && dto.RoleIds.Any()
            ? dto.RoleIds
            : new List<int> { roles.FirstOrDefault(r => r.Name == "Employee")?.Id ?? 4 };

        foreach (var roleId in targetRoleIds)
        {
            user.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = roleId });
        }

        var code = await _uow.Employees.GetNextEmployeeCodeAsync(ct);
        var employee = new Employee
        {
            EmployeeCode = code,
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName.Trim(),
            Email = dto.Email.Trim().ToLower(),
            Phone = dto.Phone.Trim(),
            DateOfBirth = DateTime.SpecifyKind(dto.DateOfBirth, DateTimeKind.Utc),
            Gender = dto.Gender,
            DepartmentId = dto.DepartmentId,
            PositionId = dto.PositionId,
            ManagerId = dto.ManagerId,
            JoiningDate = DateTime.SpecifyKind(dto.JoiningDate, DateTimeKind.Utc),
            EmploymentType = dto.EmploymentType,
            Status = EmployeeStatus.Active,
            UserId = user.Id,
            CreatedAt = DateTime.UtcNow
        };

        await _uow.Employees.AddAsync(employee, ct);
        await _uow.SaveChangesAsync(ct);

        // Initial default salary structure
        var salary = new SalaryStructure
        {
            EmployeeId = employee.Id,
            BasicSalary = 5000,
            HRA = 1500,
            OtherAllowances = 500,
            PFDeduction = 350,
            EffectiveFrom = employee.JoiningDate
        };
        await _uow.Salary.AddStructureAsync(salary, ct);
        await _uow.SaveChangesAsync(ct);

        // Audit Log
        await _auditService.LogAsync(actingUserId, "EMPLOYEE_CREATED", nameof(Employee), employee.Id.ToString(), null, employee, ct: ct);
        await _uow.SaveChangesAsync(ct);

        // In-app notification to new employee
        await _notificationService.NotifyAsync(user.Id, "EMPLOYEE_CREATED", "Welcome to EMS", "Your employee account has been created successfully.", ct);

        _logger.LogInformation("EMPLOYEE_CREATED. EmployeeId: {EmployeeId}, CreatedBy: {UserId}", employee.Id, actingUserId);

        return await GetByIdAsync(employee.Id, ct);
    }

    public async Task<EmployeeDto> UpdateAsync(int id, UpdateEmployeeDto dto, int actingUserId, CancellationToken ct = default)
    {
        var employee = await _uow.Employees.GetByIdAsync(id, ct);
        if (employee == null)
            throw new NotFoundException(nameof(Employee), id);

        var oldState = new
        {
            employee.FirstName, employee.LastName, employee.Phone, employee.DepartmentId, employee.PositionId, employee.ManagerId, employee.Status
        };

        employee.FirstName = dto.FirstName.Trim();
        employee.LastName = dto.LastName.Trim();
        employee.Phone = dto.Phone.Trim();
        employee.DateOfBirth = DateTime.SpecifyKind(dto.DateOfBirth, DateTimeKind.Utc);
        employee.Gender = dto.Gender;
        employee.DepartmentId = dto.DepartmentId;
        employee.PositionId = dto.PositionId;
        employee.ManagerId = dto.ManagerId;
        employee.EmploymentType = dto.EmploymentType;
        employee.Status = dto.Status;
        employee.UpdatedAt = DateTime.UtcNow;

        _uow.Employees.Update(employee);
        await _uow.SaveChangesAsync(ct);

        await _auditService.LogAsync(actingUserId, "EMPLOYEE_UPDATED", nameof(Employee), id.ToString(), oldState, employee, ct: ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("EMPLOYEE_UPDATED. EmployeeId: {EmployeeId}, UpdatedBy: {UserId}", id, actingUserId);

        return await GetByIdAsync(id, ct);
    }

    public async Task DeactivateAsync(int id, int actingUserId, CancellationToken ct = default)
    {
        var employee = await _uow.Employees.GetByIdAsync(id, ct);
        if (employee == null)
            throw new NotFoundException(nameof(Employee), id);

        employee.Status = EmployeeStatus.Terminated;
        employee.UpdatedAt = DateTime.UtcNow;

        if (employee.User != null)
        {
            employee.User.IsLocked = true;
            _uow.Users.Update(employee.User);
        }

        _uow.Employees.Update(employee);
        await _uow.SaveChangesAsync(ct);

        await _auditService.LogAsync(actingUserId, "EMPLOYEE_DEACTIVATED", nameof(Employee), id.ToString(), null, new { Status = "Terminated" }, ct: ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("EMPLOYEE_DEACTIVATED. EmployeeId: {EmployeeId}, DeactivatedBy: {UserId}", id, actingUserId);
    }

    public async Task<EmployeeDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var e = await _uow.Employees.GetByIdAsync(id, ct);
        if (e == null)
            throw new NotFoundException(nameof(Employee), id);

        return MapEmployeeDto(e);
    }

    public async Task<EmployeeDto?> GetByUserIdAsync(int userId, CancellationToken ct = default)
    {
        var e = await _uow.Employees.GetByUserIdAsync(userId, ct);
        return e != null ? MapEmployeeDto(e) : null;
    }

    public async Task<PagedResult<EmployeeDto>> GetPagedAsync(EmployeeQueryDto query, CancellationToken ct = default)
    {
        var (items, totalCount) = await _uow.Employees.GetPagedAsync(query.Page, query.PageSize, query.Search, query.DepartmentId, ct);
        return new PagedResult<EmployeeDto>
        {
            Items = items.Select(MapEmployeeDto).ToList(),
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }

    public async Task<IReadOnlyList<EmployeeDto>> GetTeamMembersAsync(int managerEmployeeId, CancellationToken ct = default)
    {
        var items = await _uow.Employees.GetByManagerIdAsync(managerEmployeeId, ct);
        return items.Select(MapEmployeeDto).ToList();
    }

    private static EmployeeDto MapEmployeeDto(Employee e)
    {
        return new EmployeeDto
        {
            Id = e.Id,
            EmployeeCode = e.EmployeeCode,
            FirstName = e.FirstName,
            LastName = e.LastName,
            Email = e.Email,
            Phone = e.Phone,
            DateOfBirth = e.DateOfBirth,
            Gender = e.Gender,
            DepartmentId = e.DepartmentId,
            DepartmentName = e.Department?.Name,
            PositionId = e.PositionId,
            PositionTitle = e.Position?.Title,
            ManagerId = e.ManagerId,
            ManagerName = e.Manager != null ? $"{e.Manager.FirstName} {e.Manager.LastName}" : null,
            JoiningDate = e.JoiningDate,
            EmploymentType = e.EmploymentType,
            Status = e.Status,
            UserId = e.UserId,
            CreatedAt = e.CreatedAt,
            UpdatedAt = e.UpdatedAt
        };
    }
}

public class DepartmentService : IDepartmentService
{
    private readonly IUnitOfWork _uow;
    private readonly IAuditService _auditService;
    private readonly ILogger<DepartmentService> _logger;

    public DepartmentService(IUnitOfWork uow, IAuditService auditService, ILogger<DepartmentService> logger)
    {
        _uow = uow;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<DepartmentDto> CreateAsync(CreateDepartmentDto dto, int actingUserId, CancellationToken ct = default)
    {
        if (await _uow.Departments.ExistsByNameAsync(dto.Name, ct: ct))
            throw new ConflictException($"Department '{dto.Name}' already exists.");

        var dept = new Department
        {
            Name = dto.Name.Trim(),
            Location = dto.Location?.Trim(),
            ManagerId = dto.ManagerId
        };

        await _uow.Departments.AddAsync(dept, ct);
        await _uow.SaveChangesAsync(ct);

        await _auditService.LogAsync(actingUserId, "DEPARTMENT_CREATED", nameof(Department), dept.Id.ToString(), null, dept, ct: ct);
        await _uow.SaveChangesAsync(ct);

        return await GetByIdAsync(dept.Id, ct);
    }

    public async Task<DepartmentDto> UpdateAsync(int id, UpdateDepartmentDto dto, int actingUserId, CancellationToken ct = default)
    {
        var dept = await _uow.Departments.GetByIdAsync(id, ct);
        if (dept == null)
            throw new NotFoundException(nameof(Department), id);

        if (await _uow.Departments.ExistsByNameAsync(dto.Name, excludeId: id, ct: ct))
            throw new ConflictException($"Department '{dto.Name}' already exists.");

        var old = new { dept.Name, dept.Location, dept.ManagerId };
        dept.Name = dto.Name.Trim();
        dept.Location = dto.Location?.Trim();
        dept.ManagerId = dto.ManagerId;

        _uow.Departments.Update(dept);
        await _uow.SaveChangesAsync(ct);

        await _auditService.LogAsync(actingUserId, "DEPARTMENT_UPDATED", nameof(Department), id.ToString(), old, dept, ct: ct);
        await _uow.SaveChangesAsync(ct);

        return await GetByIdAsync(id, ct);
    }

    public async Task DeleteAsync(int id, int actingUserId, CancellationToken ct = default)
    {
        var dept = await _uow.Departments.GetByIdAsync(id, ct);
        if (dept == null)
            throw new NotFoundException(nameof(Department), id);

        if (dept.Employees.Any())
            throw new ConflictException("Cannot delete department that contains active employees. Reassign employees first.");

        _uow.Departments.Remove(dept);
        await _uow.SaveChangesAsync(ct);

        await _auditService.LogAsync(actingUserId, "DEPARTMENT_DELETED", nameof(Department), id.ToString(), dept, null, ct: ct);
        await _uow.SaveChangesAsync(ct);
    }

    public async Task<DepartmentDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var dept = await _uow.Departments.GetByIdAsync(id, ct);
        if (dept == null)
            throw new NotFoundException(nameof(Department), id);

        return new DepartmentDto
        {
            Id = dept.Id,
            Name = dept.Name,
            Location = dept.Location,
            ManagerId = dept.ManagerId,
            ManagerName = dept.Manager != null ? $"{dept.Manager.FirstName} {dept.Manager.LastName}" : null,
            EmployeeCount = dept.Employees.Count
        };
    }

    public async Task<IReadOnlyList<DepartmentDto>> GetAllAsync(CancellationToken ct = default)
    {
        var depts = await _uow.Departments.GetAllAsync(ct);
        return depts.Select(d => new DepartmentDto
        {
            Id = d.Id,
            Name = d.Name,
            Location = d.Location,
            ManagerId = d.ManagerId,
            ManagerName = d.Manager != null ? $"{d.Manager.FirstName} {d.Manager.LastName}" : null,
            EmployeeCount = d.Employees.Count
        }).ToList();
    }
}

public class PositionService : IPositionService
{
    private readonly IUnitOfWork _uow;
    private readonly IAuditService _auditService;

    public PositionService(IUnitOfWork uow, IAuditService auditService)
    {
        _uow = uow;
        _auditService = auditService;
    }

    public async Task<PositionDto> CreateAsync(CreatePositionDto dto, int actingUserId, CancellationToken ct = default)
    {
        if (await _uow.Positions.ExistsByTitleAsync(dto.Title, ct: ct))
            throw new ConflictException($"Position '{dto.Title}' already exists.");

        var pos = new Position
        {
            Title = dto.Title.Trim(),
            Description = dto.Description?.Trim()
        };

        await _uow.Positions.AddAsync(pos, ct);
        await _uow.SaveChangesAsync(ct);

        await _auditService.LogAsync(actingUserId, "POSITION_CREATED", nameof(Position), pos.Id.ToString(), null, pos, ct: ct);
        await _uow.SaveChangesAsync(ct);

        return new PositionDto { Id = pos.Id, Title = pos.Title, Description = pos.Description, EmployeeCount = 0 };
    }

    public async Task<PositionDto> UpdateAsync(int id, UpdatePositionDto dto, int actingUserId, CancellationToken ct = default)
    {
        var pos = await _uow.Positions.GetByIdAsync(id, ct);
        if (pos == null)
            throw new NotFoundException(nameof(Position), id);

        if (await _uow.Positions.ExistsByTitleAsync(dto.Title, excludeId: id, ct: ct))
            throw new ConflictException($"Position '{dto.Title}' already exists.");

        var old = new { pos.Title, pos.Description };
        pos.Title = dto.Title.Trim();
        pos.Description = dto.Description?.Trim();

        _uow.Positions.Update(pos);
        await _uow.SaveChangesAsync(ct);

        await _auditService.LogAsync(actingUserId, "POSITION_UPDATED", nameof(Position), id.ToString(), old, pos, ct: ct);
        await _uow.SaveChangesAsync(ct);

        return new PositionDto { Id = pos.Id, Title = pos.Title, Description = pos.Description, EmployeeCount = pos.Employees.Count };
    }

    public async Task DeleteAsync(int id, int actingUserId, CancellationToken ct = default)
    {
        var pos = await _uow.Positions.GetByIdAsync(id, ct);
        if (pos == null)
            throw new NotFoundException(nameof(Position), id);

        if (pos.Employees.Any())
            throw new ConflictException("Cannot delete position assigned to employees.");

        _uow.Positions.Remove(pos);
        await _uow.SaveChangesAsync(ct);

        await _auditService.LogAsync(actingUserId, "POSITION_DELETED", nameof(Position), id.ToString(), pos, null, ct: ct);
        await _uow.SaveChangesAsync(ct);
    }

    public async Task<PositionDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var pos = await _uow.Positions.GetByIdAsync(id, ct);
        if (pos == null)
            throw new NotFoundException(nameof(Position), id);

        return new PositionDto
        {
            Id = pos.Id,
            Title = pos.Title,
            Description = pos.Description,
            EmployeeCount = pos.Employees.Count
        };
    }

    public async Task<IReadOnlyList<PositionDto>> GetAllAsync(CancellationToken ct = default)
    {
        var positions = await _uow.Positions.GetAllAsync(ct);
        return positions.Select(p => new PositionDto
        {
            Id = p.Id,
            Title = p.Title,
            Description = p.Description,
            EmployeeCount = p.Employees.Count
        }).ToList();
    }
}
