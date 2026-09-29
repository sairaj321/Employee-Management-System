# EMS Security & Authorization Specification

## 1. Authentication & JWT Lifecycle

```mermaid
sequenceDiagram
    autonumber
    actor User as User / Browser
    participant API as AuthController / AuthService
    participant DB as PostgreSQL Database
    
    User->>API: POST /api/auth/login { email, password }
    API->>DB: Query User with Roles & Permissions
    alt Invalid Password (Fail Count < 5)
        API->>DB: Increment FailedLoginCount & Log LOGIN_FAILED
        API-->>User: 401 Unauthorized
    else 5 Consecutive Failures
        API->>DB: Set IsLocked = true, LockoutEnd = Now + 15m
        API-->>User: 403 Forbidden (Account Locked)
    else Password Verified
        API->>DB: Reset FailedLoginCount & Set LastLoginAt
        API->>API: Generate Access Token (TTL: 15 min)
        API->>API: Generate Opaque Refresh Token (256-bit) + SHA-256 Hash
        API->>DB: Insert RefreshToken row
        API-->>User: 200 OK { accessToken, refreshToken, expiresIn: 900 }
    end
```

---

## 2. Refresh Token Rotation & Theft Detection

```mermaid
sequenceDiagram
    autonumber
    actor Client
    participant Auth as AuthService
    participant DB as RefreshTokens Table

    Client->>Auth: POST /api/auth/refresh { refreshToken }
    Auth->>Auth: Compute SHA-256(refreshToken)
    Auth->>DB: Query Token by Hash
    
    alt Token already revoked (Replay / Theft Detected)
        Note over Auth,DB: Malicious reuse detected!
        Auth->>DB: Revoke entire token family for this UserId
        Auth->>DB: Log TOKEN_REUSE_DETECTED
        Auth-->>Client: 401 Unauthorized (Session terminated)
    else Token expired
        Auth-->>Client: 401 Unauthorized (Token Expired)
    else Valid Token
        Auth->>DB: Mark current token RevokedAt = Now, ReplacedByTokenHash = newHash
        Auth->>DB: Insert new RefreshToken (TTL 7 days)
        Auth->>Auth: Issue new Access Token (15 min)
        Auth-->>Client: 200 OK { newAccessToken, newRefreshToken }
    end
```

---

## 3. RBAC & Horizontal Access Control Matrix

```mermaid
flowchart TD
    Request["Incoming API Request"] --> Handler["OwnResourceAuthorizationHandler"]
    
    Handler --> CheckAdmin{"Caller is Admin\nor has Wildcard (*)"}
    CheckAdmin -- Yes --> Allow["Grant Access (200 OK)"]
    
    CheckAdmin -- No --> CheckHR{"Caller is HR & Scope\nis Organization-wide"}
    CheckHR -- Yes --> Allow
    
    CheckHR -- No --> CheckOwn{"Target {id} equals\nCaller's EmployeeId"}
    CheckOwn -- Yes --> CheckOwnPerm{"Caller has\nPermission.Own"}
    CheckOwnPerm -- Yes --> Allow
    CheckOwnPerm -- No --> Deny["Deny Access (403 Forbidden)"]
    
    CheckOwn -- No --> CheckMgr{"Caller is Manager of\nTarget Employee (Team)"}
    CheckMgr -- Yes --> CheckTeamPerm{"Caller has\nPermission.Team"}
    CheckTeamPerm -- Yes --> Allow
    CheckTeamPerm -- No --> Deny
    CheckMgr -- No --> Deny
```

### Security Highlights:
- **Password Hashing**: PBKDF2 HMAC-SHA256, 100,000 iterations, 128-bit cryptographically secure per-user salt.
- **Data Protection**: Sensitive payloads (passwords, tokens, raw cryptographic keys) are excluded from Serilog output and database audit logs.
- **SQL Injection Prevention**: Parameterized queries and EF Core LINQ projections eliminate injection risks.
- **Cross-Site Scripting (XSS)**: Handled via React standard text escaping and JSON response serialization.
