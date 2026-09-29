# EMS Design Patterns Mapping & Implementation

Every pattern documented here has a direct, concrete implementation in the codebase solving a specific architectural requirement (§25.4, §25.11).

---

## 1. Pattern Implementation Table

| Design Pattern | Concrete Location in Code | Purpose & Problem Solved |
| :--- | :--- | :--- |
| **Repository Pattern** | `EMS.Domain.Interfaces.IEmployeeRepository`, `EMS.Infrastructure.Repositories.EmployeeRepository` | Decouples business use cases from EF Core persistence and query logic. |
| **Unit of Work Pattern** | `EMS.Domain.Interfaces.IUnitOfWork`, `EMS.Infrastructure.Repositories.UnitOfWork` | Coordinates atomic transactions across multiple repositories with `SaveChangesAsync()`. |
| **Service Layer Pattern** | `EMS.Application.Services.EmployeeService`, `AuthService`, `LeaveService`, `SalaryService` | Encapsulates all business rules, orchestration, conflict checking, and audit side-effects. |
| **Data Transfer Object (DTO)** | `EMS.Application.DTOs.*` (`CreateEmployeeDto`, `EmployeeDto`, `LoginResponseDto`) | Isolates database entity structure from API boundary contracts. |
| **Dependency Injection** | `EMS.API.Program.cs` | Binds all domain abstractions (`IEmployeeRepository`, `IAuthService`) to concrete implementations at runtime. |
| **Chain of Responsibility (Middleware)** | `EMS.API.Middleware.CorrelationIdMiddleware`, `ExceptionHandlingMiddleware`, `RequestLoggingMiddleware` | Passes incoming HTTP requests through a structured pipeline of logging, exception catching, and auth checks. |
| **Observer / Event Logging Pattern** | `EMS.Application.Services.AuditService`, `NotificationService` | Captures state changes (`EMPLOYEE_CREATED`, `SALARY_UPDATED`) and generates audit trails as side-effects. |

---

## 2. Code Evidence

### Repository & Unit of Work (`IUnitOfWork.cs` / `EmployeeService.cs`)
```csharp
// EmployeeService creates employee and logs audit atomically via Unit of Work
public async Task<EmployeeDto> CreateAsync(CreateEmployeeDto dto, int actingUserId, CancellationToken ct)
{
    if (await _uow.Employees.ExistsByEmailAsync(dto.Email, ct))
        throw new ConflictException($"Email '{dto.Email}' is already in use.");

    var employee = new Employee { ... };
    await _uow.Employees.AddAsync(employee, ct);
    await _uow.SaveChangesAsync(ct); // Atomic commit

    await _auditService.LogAsync(actingUserId, "EMPLOYEE_CREATED", nameof(Employee), employee.Id.ToString(), null, employee, ct: ct);
    await _uow.SaveChangesAsync(ct);
    
    return MapEmployeeDto(employee);
}
```
