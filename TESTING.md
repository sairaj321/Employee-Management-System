# EMS Testing Strategy & Test Matrix

## 1. Test Matrix & Coverage (§11, §16, §25.8)

| Area / Module | Test Scenario | Verified Behavior | Test Type |
| :--- | :--- | :--- | :--- |
| **Auth** | Valid Login | Issues Access + Refresh token, resets `FailedLoginCount` to 0 | Unit & Integration |
| **Auth** | Invalid Password | Increments `FailedLoginCount`, throws `401 Unauthorized` | Unit & Integration |
| **Auth** | Account Lockout | 5 consecutive bad attempts sets `IsLocked = true` for 15 min | Unit |
| **Auth** | Refresh Token Rotation | Successfully issues rotated refresh token and revokes old row | Unit |
| **Auth** | Reused / Stolen Token | Re-using revoked token triggers family revocation (theft detection) | Unit |
| **Employee** | Duplicate Email | Throws `409 ConflictException` ("Email already in use") | Unit |
| **Employee** | Not Found Update | Updating non-existent ID throws `404 NotFoundException` | Unit |
| **Attendance** | Duplicate Check-in | Second check-in on the same date throws `409 ConflictException` | Unit |
| **Leave** | Overlapping Date Range | Overlapping approved/pending leaves throws `409 ConflictException` | Unit |
| **Leave** | EndDate < StartDate | Invalid date range throws `400 ValidationException` | Unit |
| **RBAC** | Permission Claim Match | `PermissionAuthorizationHandler` succeeds on matching permission | Unit |
| **RBAC** | Missing Permission Claim | `PermissionAuthorizationHandler` denies authorization | Unit |
| **E2E Workflow** | End-to-End Chain | Login Admin → Create Employee → Fetch Salary → Generate Payslip | Integration |

---

## 2. Running Automated Tests

Run the full xUnit test suite from the repository root:

```bash
dotnet test
```

### Example Test Run Output:
```text
Starting test execution, please wait...
A total of 1 test files matched the specified pattern.

Passed!  - Failed: 0, Passed: 16, Skipped: 0, Total: 16, Duration: 1 s - EMS.Tests.dll (net9.0)
```
