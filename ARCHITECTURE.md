# EMS Architecture & System Design

## 1. High-Level Architecture

The Employee Management System follows **Clean Architecture** (Ports and Adapters / Layered Architecture) where the business logic does not depend on database implementations, web frameworks, or third-party libraries.

```mermaid
flowchart TD
    Client["React 19 Frontend\n(TypeScript + Tailwind + Axios)"] -->|HTTPS / REST + JWT| API["Presentation Layer\n(EMS.API)"]
    
    subgraph Core ["Application & Domain Core"]
        API -->|Invokes| App["Application Layer\n(EMS.Application)"]
        App -->|Defines & Uses| Domain["Domain Layer\n(EMS.Domain)"]
    end
    
    subgraph Persistence ["Infrastructure"]
        Infra["Infrastructure Layer\n(EMS.Infrastructure)"] -.->|Implements Interfaces| Domain
        Infra -.->|Implements Interfaces| App
        Infra -->|EF Core Npgsql| DB[("PostgreSQL Database\n(Tables + JSONB + Indexes)")]
    end
```

---

## 2. Low-Level Layer Responsibilities & Dependency Inversion

```mermaid
classDiagram
    direction TB
    
    class IEmployeeRepository {
        <<interface>>
        +GetByIdAsync(id)
        +GetPagedAsync(page, pageSize)
        +AddAsync(employee)
    }

    class IEmployeeService {
        <<interface>>
        +CreateAsync(dto, actingUserId)
        +UpdateAsync(id, dto, actingUserId)
        +GetByIdAsync(id)
    }

    class EmployeeService {
        -IUnitOfWork _uow
        -IAuditService _audit
        +CreateAsync(dto, actingUserId)
    }

    class EmployeeRepository {
        -AppDbContext _context
        +GetByIdAsync(id)
    }

    class EmployeesController {
        -IEmployeeService _employeeService
        +Create(dto)
        +GetById(id)
    }

    EmployeesController --> IEmployeeService : Calls
    EmployeeService ..|> IEmployeeService : Implements
    EmployeeService --> IEmployeeRepository : Calls via IUnitOfWork
    EmployeeRepository ..|> IEmployeeRepository : Implements
```

### Dependency Rules:
1. `EMS.Domain`: Zero dependencies on any other layer or third-party ORMs.
2. `EMS.Application`: References only `EMS.Domain`. Contains business use-cases, validators, exceptions, and DTO contracts.
3. `EMS.Infrastructure`: References `EMS.Domain` and `EMS.Application`. Implements EF Core `AppDbContext`, repository interfaces, JWT generators, and password hashers.
4. `EMS.API`: References `EMS.Application` and `EMS.Infrastructure` (solely for Dependency Injection registration in `Program.cs`).

---

## 3. Middleware Execution Pipeline Order (§6.4)

The pipeline is registered in strict execution order to guarantee error handling, tracing, and security:

```mermaid
sequenceDiagram
    autonumber
    actor Client
    participant CId as CorrelationIdMiddleware
    participant Exc as ExceptionHandlingMiddleware
    participant Cors as CorsMiddleware
    participant Log as RequestLoggingMiddleware
    participant AuthN as AuthenticationMiddleware
    participant AuthZ as AuthorizationMiddleware
    participant Ctrl as ControllerAction

    Client->>CId: HTTP Request
    Note over CId: Extracts/Generates X-Correlation-Id
    CId->>Exc: Next()
    Note over Exc: Wraps downstream execution in try-catch
    Exc->>Cors: Next()
    Cors->>Log: Next()
    Note over Log: Measures execution time and method/route
    Log->>AuthN: Next()
    Note over AuthN: Validates JWT Bearer signature & claims
    AuthN->>AuthZ: Next()
    Note over AuthZ: Evaluates RBAC & OwnResource handlers
    AuthZ->>Ctrl: Invoke Action
    Ctrl-->>AuthZ: ActionResult
    AuthZ-->>AuthN: Response
    AuthN-->>Log: Response
    Note over Log: Emits structured log event
    Log-->>Cors: Response
    Cors-->>Exc: Response
    Exc-->>CId: Response
    CId-->>Client: HTTP 200/201 JSON Envelope with X-Correlation-Id
```
