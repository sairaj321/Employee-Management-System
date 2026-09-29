# EMS REST API Documentation & Contracts

All endpoints return a standardized envelope structure.

```json
{
  "success": true,
  "message": "Operation completed successfully.",
  "data": { ... },
  "errors": [],
  "traceId": "494104e488f34f4a96dd7fb0fd417591"
}
```

---

## 1. Authentication Endpoints

| Method | Route | Body | Policy / Scope | Description |
| :--- | :--- | :--- | :--- | :--- |
| `POST` | `/api/auth/login` | `{ email, password }` | Anonymous | Authenticates and returns JWT Access & Refresh token |
| `POST` | `/api/auth/refresh` | `{ refreshToken }` | Anonymous | Rotates refresh token & issues new access token |
| `POST` | `/api/auth/logout` | `{ refreshToken }` | `[Authorize]` | Revokes refresh token |
| `POST` | `/api/auth/change-password` | `{ currentPassword, newPassword }` | `[Authorize]` | Updates PBKDF2 hash & revokes active sessions |
| `GET` | `/api/auth/me` | — | `[Authorize]` | Returns current user profile, roles, and permissions |
| `GET` | `/api/auth/users` | — | `User.Manage` | Lists all users with lockout status |
| `GET` | `/api/auth/roles` | — | `[Authorize]` | Lists available roles |
| `GET` | `/api/auth/permissions` | — | `Role.Manage` | Lists all registered system permissions |

---

## 2. Employees Module

| Method | Route | Request Body | Policy / Scope | Status Codes |
| :--- | :--- | :--- | :--- | :--- |
| `GET` | `/api/employees?page=1&pageSize=20&search=&departmentId=` | — | `Employee.Read` | `200` |
| `GET` | `/api/employees/{id}` | — | `Employee.Read.Own` | `200`, `403`, `404` |
| `GET` | `/api/employees/me` | — | `[Authorize]` | `200`, `404` |
| `GET` | `/api/employees/team` | — | `Employee.Read.Team` | `200`, `403` |
| `POST` | `/api/employees` | `CreateEmployeeDto` | `Employee.Create` | `201`, `400`, `409` |
| `PUT` | `/api/employees/{id}` | `UpdateEmployeeDto` | `Employee.Update` | `200`, `400`, `404` |
| `DELETE` | `/api/employees/{id}` | — | `Employee.Delete` | `204`, `404` (Soft Delete) |

---

## 3. Time, Attendance & Leaves

| Method | Route | Body | Policy / Scope | Description |
| :--- | :--- | :--- | :--- | :--- |
| `POST` | `/api/attendance/check-in` | `{ employeeId? }` | `Attendance.CheckIn.Own` | Records check-in; throws `409` on duplicate |
| `POST` | `/api/attendance/check-out` | `{ employeeId? }` | `Attendance.CheckIn.Own` | Records check-out |
| `GET` | `/api/attendance/today` | — | `Attendance.Read.Own` | Returns today's active check-in record |
| `GET` | `/api/attendance` | Query params | `Attendance.Read.Own` | Attendance log history |
| `POST` | `/api/leaves` | `ApplyLeaveDto` | `Leave.Create.Own` | Submits leave request; throws `409` on date overlap |
| `GET` | `/api/leaves` | Query params | `Leave.Read.Own` | Retrieves leaves |
| `PUT` | `/api/leaves/{id}/approve` | `ApproveLeaveDto` | `Leave.Approve.Team` | Approves leave application |
| `PUT` | `/api/leaves/{id}/reject` | `ApproveLeaveDto` | `Leave.Approve.Team` | Rejects leave application |
| `POST` | `/api/leaves/{id}/cancel` | — | `Leave.Create.Own` | Cancels pending leave application |
| `GET` | `/api/leaves/types` | — | `[Authorize]` | Returns active leave categories |

---

## 4. Compensation & Salary

| Method | Route | Policy / Scope | Description |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/employees/{id}/salary` | `Salary.Read.Own` | Retrieves salary structure |
| `PUT` | `/api/employees/{id}/salary` | `Salary.Update` | Configures salary components (HR/Admin only) |
| `POST` | `/api/employees/{id}/payslips/generate?month=&year=` | `Salary.Update` | Calculates and generates monthly payslip |
| `GET` | `/api/employees/{id}/payslips?year=` | `Salary.Read.Own` | Lists payslips for employee |
| `GET` | `/api/payslips/{id}` | `[Authorize]` | Detailed statement for payslip invoice |

---

## 5. Security & Audit Logs

| Method | Route | Policy | Description |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/audit-logs?page=1&pageSize=20&entityName=&userId=` | `AuditLog.Read` | Returns paged audit logs with JSONB diffs |
| `GET` | `/api/notifications?unreadOnly=` | `[Authorize]` | Lists user in-app notifications |
| `GET` | `/api/notifications/unread-count` | `[Authorize]` | Returns total unread notification count |
| `PUT` | `/api/notifications/{id}/read` | `[Authorize]` | Marks notification as read |

---

## 6. Projections & Reports

| Method | Route | Policy | Output Model |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/reports/employees-by-department` | `Department.Read` | Headcount breakdown |
| `GET` | `/api/reports/attendance-summary` | `Attendance.Read.Own` | Daily Present/Absent/Late metrics |
| `GET` | `/api/reports/leave-utilization` | `Leave.Read.Own` | Days utilized per employee |
| `GET` | `/api/reports/payroll-summary?month=&year=` | `Salary.Update` | Total gross, tax deductions, net payout |
| `GET` | `/api/reports/project-allocation` | `Project.Read` | Team members allocated per project |
