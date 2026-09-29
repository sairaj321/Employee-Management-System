# Enterprise Employee Management System (EMS)

An industry-grade **Employee Management System (EMS)** designed with **Clean / Layered Architecture**, **ASP.NET Core 9 Web API**, **Entity Framework Core**, **PostgreSQL**, and **React 19 (TypeScript + Tailwind CSS)**.

---

## 🌟 Key Architecture & Highlights

- **Clean / Layered Architecture**: Strict dependency direction `API` → `Application` → `Domain` with `Infrastructure` implementing domain abstractions (DIP).
- **PostgreSQL Persistence**: Fully normalized relational schema, primary/foreign key constraints, database indexes, and `jsonb` storage for audit records.
- **Granular RBAC & Resource Policies**: Custom `PermissionAuthorizationHandler` and `OwnResourceAuthorizationHandler` preventing vertical & horizontal privilege escalation.
- **Robust Security**:
  - Short-lived JWT Access Tokens (15 min)
  - Hashed, rotating Refresh Tokens (7 days TTL) with token theft detection & token family revocation
  - PBKDF2 HMAC-SHA256 password hashing (100,000 iterations) with automatic lockout after 5 failed attempts
- **Audit & Structured Logging**: Serilog structured file/console logging with `X-Correlation-Id` propagation and database-backed `AuditLogs` tracking changes.
- **100% Passing Automated Tests**: xUnit unit tests and integration tests covering security, lockout, RBAC, leave validation, duplicate prevention, and payroll workflows.
- **Modern React Client**: 18+ responsive screens with role-based UI gating, protected routes, automated 401 token refresh interceptors, and printable payslip statements.

---

## 🚀 Quick Start (Local Development)

### Prerequisites
- [.NET 9 SDK](https://dotnet.microsoft.com/)
- [Node.js 20+ / npm](https://nodejs.org/)
- [PostgreSQL 15+](https://www.postgresql.org/) or [Docker](https://www.docker.com/)

### 1. Database Setup
```bash
# Start PostgreSQL via Docker (optional):
docker run --name ems-pg -e POSTGRES_PASSWORD=postgres -e POSTGRES_DB=ems_db -p 5432:5432 -d postgres:16-alpine
```

### 2. Run Backend API
```bash
cd src/EMS.API
dotnet run
```
- API will start at: `http://localhost:5000` (or `https://localhost:5001`)
- Swagger UI Documentation: `http://localhost:5000/swagger`
- Health Checks: `http://localhost:5000/health/ready`

### 3. Run Frontend (React + Vite)
```bash
cd client
npm install
npm run dev
```
- Frontend will open at: `http://localhost:5173`

---

## 🔑 Default Seed Accounts

| Role | Email | Password | Scope & Privileges |
| :--- | :--- | :--- | :--- |
| **Admin** | `admin@company.com` | `Admin@123` | Full wildcard access (`*`), User/Role management, Audit Logs |
| **HR Officer** | `hr@company.com` | `Hr@12345` | Employee CRUD, Department management, Attendance, Salary |
| **Manager** | `manager@company.com` | `Manager@123` | Team approvals, Team attendance, Project management |
| **Employee** | `employee@company.com` | `Employee@123` | Self profile, Check-in/out, Apply leave, View my payslips |

---

## 🧪 Automated Testing
```bash
dotnet test
```
All unit and integration tests run in under 1 second.

---

## 🐳 Docker Deployment
```bash
docker-compose up --build
```

---

## 📚 Documentation Deliverables

Detailed technical specifications and architectural documentation are available in the repository:

1. [Architecture & Layers](file:///C:/Users/sairajk/.gemini/antigravity/scratch/EMS/ARCHITECTURE.md)
2. [Database Design & Schema](file:///C:/Users/sairajk/.gemini/antigravity/scratch/EMS/DATABASE-DESIGN.md)
3. [API Documentation & Contracts](file:///C:/Users/sairajk/.gemini/antigravity/scratch/EMS/API-DOCUMENTATION.md)
4. [Security & Authorization Specification](file:///C:/Users/sairajk/.gemini/antigravity/scratch/EMS/SECURITY.md)
5. [Testing Strategy & Test Matrix](file:///C:/Users/sairajk/.gemini/antigravity/scratch/EMS/TESTING.md)
6. [Deployment & DevOps Guide](file:///C:/Users/sairajk/.gemini/antigravity/scratch/EMS/DEPLOYMENT.md)
7. [Design Patterns Implementation](file:///C:/Users/sairajk/.gemini/antigravity/scratch/EMS/DESIGN-PATTERNS.md)
8. [SOLID Principles in Code](file:///C:/Users/sairajk/.gemini/antigravity/scratch/EMS/SOLID-PRINCIPLES.md)
