# SOLID Principles in EMS Implementation

Every SOLID principle is demonstrated through real code structure and project dependencies (§25.3, §25.11).

---

## 1. Single Responsibility Principle (SRP)
- **Controllers** (`EmployeesController`, `SalaryController`): Only handle HTTP routing, model binding, and status code envelope dispatching.
- **Services** (`EmployeeService`, `LeaveService`): Exclusively own business invariants, domain validation, and use-case workflow coordination.
- **Repositories** (`EmployeeRepository`, `LeaveRepository`): Exclusively own database querying, filtering, and persistence mechanics.
- **Validators** (`CreateEmployeeValidator`, `ApplyLeaveValidator`): Exclusively own field constraint validations.

---

## 2. Open/Closed Principle (OCP)
- **Policy Handlers** (`PermissionAuthorizationHandler`, `OwnResourceAuthorizationHandler`): The authorization system is extended by registering new `IAuthorizationRequirement` policies without modifying core authentication or controller logic.
- **Audit Logging** (`IAuditService`): Supports plugging in new sink backends (e.g. Seq, Elastic, CloudWatch) behind the `IAuditService` abstraction.

---

## 3. Liskov Substitution Principle (LSP)
- Implementations of abstractions (`EmployeeRepository` for `IEmployeeRepository`, `JwtTokenGenerator` for `ITokenService`, `PasswordHasher` for `IPasswordHasher`) can be swapped seamlessly in `Program.cs` or test fixtures (`CustomWebApplicationFactory`) without altering consumer behavior or breaking contracts.

---

## 4. Interface Segregation Principle (ISP)
- Rather than one monolithic data interface, interfaces are separated into cohesive contracts:
  - `IEmployeeRepository`
  - `IAttendanceRepository`
  - `ILeaveRepository`
  - `ISalaryRepository`
  - `IAuditLogRepository`
- Services only depend on the specific repository abstractions they need.

---

## 5. Dependency Inversion Principle (DIP)
- High-level business orchestrations in `EMS.Application` **never depend on concrete database or infrastructure types** in `EMS.Infrastructure`.
- All service constructors receive interfaces (`IUnitOfWork`, `IPasswordHasher`, `ITokenService`, `IAuditService`).
- In `EMS.API/Program.cs`, the IoC container wires concrete implementations:
```csharp
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<ITokenService, JwtTokenGenerator>();
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
```
