using EMS.Application.DTOs;
using EMS.Application.Exceptions;
using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace EMS.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _uow;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IAuditService _auditService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUnitOfWork uow,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IAuditService auditService,
        ILogger<AuthService> logger)
    {
        _uow = uow;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request, string? ipAddress, CancellationToken ct = default)
    {
        var user = await _uow.Users.GetByEmailAsync(request.Email, ct);
        if (user == null)
        {
            _logger.LogWarning("LOGIN_FAILED. Email not found: {Email}, IP: {IpAddress}", request.Email, ipAddress);
            await _auditService.LogAsync(null, "LOGIN_FAILED", nameof(User), request.Email, null, new { reason = "UserNotFound" }, ipAddress: ipAddress, ct: ct);
            await _uow.SaveChangesAsync(ct);
            throw new UnauthorizedException("Invalid email or password.");
        }

        if (user.IsLocked)
        {
            if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow)
            {
                _logger.LogWarning("ACCOUNT_LOCKED. Locked user attempted login: {Email}", user.Email);
                throw new ForbiddenException($"Account is locked due to multiple failed attempts. Please try again after {user.LockoutEnd.Value:t} UTC.");
            }

            // Lockout period expired
            user.IsLocked = false;
            user.FailedLoginCount = 0;
            user.LockoutEnd = null;
        }

        var isPasswordValid = _passwordHasher.VerifyPassword(request.Password, user.PasswordHash, user.PasswordSalt);
        if (!isPasswordValid)
        {
            user.FailedLoginCount++;
            if (user.FailedLoginCount >= 5)
            {
                user.IsLocked = true;
                user.LockoutEnd = DateTime.UtcNow.AddMinutes(15);
                _logger.LogWarning("ACCOUNT_LOCKED. User {Email} reached max failed login attempts", user.Email);
                await _auditService.LogAsync(user.Id, "ACCOUNT_LOCKED", nameof(User), user.Id.ToString(), null, new { FailedAttempts = user.FailedLoginCount }, ipAddress: ipAddress, ct: ct);
            }
            else
            {
                _logger.LogWarning("LOGIN_FAILED. Incorrect password for user: {Email}, count: {Count}", user.Email, user.FailedLoginCount);
                await _auditService.LogAsync(user.Id, "LOGIN_FAILED", nameof(User), user.Id.ToString(), null, new { FailedAttempts = user.FailedLoginCount }, ipAddress: ipAddress, ct: ct);
            }

            _uow.Users.Update(user);
            await _uow.SaveChangesAsync(ct);
            throw new UnauthorizedException("Invalid email or password.");
        }

        // Reset failed logins
        user.FailedLoginCount = 0;
        user.IsLocked = false;
        user.LockoutEnd = null;
        user.LastLoginAt = DateTime.UtcNow;
        _uow.Users.Update(user);

        // Load roles & permissions
        var roles = user.UserRoles.Select(ur => ur.Role.Name).Distinct().ToList();
        var permissions = user.UserRoles
            .SelectMany(ur => ur.Role.RolePermissions)
            .Select(rp => rp.Permission.Code)
            .Distinct()
            .ToList();

        // Generate tokens
        var (accessToken, expiresIn) = _tokenService.GenerateAccessToken(user, user.Employee, roles, permissions);
        var rawRefreshToken = _tokenService.GenerateRefreshToken();
        var hashedRefreshToken = _tokenService.HashToken(rawRefreshToken);

        var refreshTokenEntity = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = hashedRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };

        await _uow.Users.AddRefreshTokenAsync(refreshTokenEntity, ct);
        await _uow.SaveChangesAsync(ct);

        await _auditService.LogAsync(user.Id, "LOGIN_SUCCESS", nameof(User), user.Id.ToString(), null, null, ipAddress: ipAddress, ct: ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("LOGIN_SUCCESS. User {Email}, EmployeeId: {EmployeeId}", user.Email, user.Employee?.Id);

        return new LoginResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = rawRefreshToken,
            ExpiresIn = expiresIn,
            User = MapUserDto(user, roles, permissions)
        };
    }

    public async Task<LoginResponseDto> RefreshTokenAsync(string rawRefreshToken, string? ipAddress, CancellationToken ct = default)
    {
        var tokenHash = _tokenService.HashToken(rawRefreshToken);
        var storedToken = await _uow.Users.GetRefreshTokenAsync(tokenHash, ct);

        if (storedToken == null)
        {
            _logger.LogWarning("TOKEN_REFRESH_FAILED. Non-existent refresh token");
            throw new UnauthorizedException("Invalid refresh token.");
        }

        // Compromised / Reused token detection
        if (storedToken.IsRevoked)
        {
            _logger.LogWarning("TOKEN_REUSE_DETECTED! Revoking token family for user {UserId}", storedToken.UserId);
            await _uow.Users.RevokeRefreshTokenFamilyAsync(storedToken.UserId, ct);
            await _auditService.LogAsync(storedToken.UserId, "TOKEN_REUSE_DETECTED", nameof(RefreshToken), storedToken.Id.ToString(), null, new { ipAddress }, ipAddress: ipAddress, ct: ct);
            await _uow.SaveChangesAsync(ct);
            throw new UnauthorizedException("Compromised session detected. All active tokens for this account have been revoked.");
        }

        if (storedToken.IsExpired)
        {
            _logger.LogWarning("TOKEN_EXPIRED for user {UserId}", storedToken.UserId);
            throw new UnauthorizedException("Refresh token has expired. Please log in again.");
        }

        // Rotate token
        var newRawRefreshToken = _tokenService.GenerateRefreshToken();
        var newHashedRefreshToken = _tokenService.HashToken(newRawRefreshToken);

        storedToken.RevokedAt = DateTime.UtcNow;
        storedToken.ReplacedByTokenHash = newHashedRefreshToken;

        var newRefreshTokenEntity = new RefreshToken
        {
            UserId = storedToken.UserId,
            TokenHash = newHashedRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };

        await _uow.Users.AddRefreshTokenAsync(newRefreshTokenEntity, ct);

        var user = storedToken.User;
        var roles = user.UserRoles.Select(ur => ur.Role.Name).Distinct().ToList();
        var permissions = user.UserRoles
            .SelectMany(ur => ur.Role.RolePermissions)
            .Select(rp => rp.Permission.Code)
            .Distinct()
            .ToList();

        var (newAccessToken, expiresIn) = _tokenService.GenerateAccessToken(user, user.Employee, roles, permissions);

        await _auditService.LogAsync(user.Id, "TOKEN_REFRESHED", nameof(RefreshToken), newRefreshTokenEntity.Id.ToString(), null, null, ipAddress: ipAddress, ct: ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("TOKEN_REFRESHED. User: {UserId}", user.Id);

        return new LoginResponseDto
        {
            AccessToken = newAccessToken,
            RefreshToken = newRawRefreshToken,
            ExpiresIn = expiresIn,
            User = MapUserDto(user, roles, permissions)
        };
    }

    public async Task LogoutAsync(string rawRefreshToken, int actingUserId, CancellationToken ct = default)
    {
        var tokenHash = _tokenService.HashToken(rawRefreshToken);
        var storedToken = await _uow.Users.GetRefreshTokenAsync(tokenHash, ct);
        if (storedToken != null && !storedToken.IsRevoked)
        {
            storedToken.RevokedAt = DateTime.UtcNow;
            await _auditService.LogAsync(actingUserId, "LOGOUT", nameof(RefreshToken), storedToken.Id.ToString(), null, null, ct: ct);
            await _uow.SaveChangesAsync(ct);
            _logger.LogInformation("LOGOUT. User {UserId}", actingUserId);
        }
    }

    public async Task ChangePasswordAsync(int userId, ChangePasswordDto dto, CancellationToken ct = default)
    {
        var user = await _uow.Users.GetByIdAsync(userId, ct);
        if (user == null)
            throw new NotFoundException(nameof(User), userId);

        if (!_passwordHasher.VerifyPassword(dto.CurrentPassword, user.PasswordHash, user.PasswordSalt))
            throw new ValidationException("CurrentPassword", "Current password is incorrect.");

        var (newHash, newSalt) = _passwordHasher.HashPassword(dto.NewPassword);
        user.PasswordHash = newHash;
        user.PasswordSalt = newSalt;
        _uow.Users.Update(user);

        // Invalidate current refresh tokens upon password change
        await _uow.Users.RevokeRefreshTokenFamilyAsync(userId, ct);
        await _auditService.LogAsync(userId, "PASSWORD_CHANGED", nameof(User), userId.ToString(), null, null, ct: ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("PASSWORD_CHANGED. User {UserId}", userId);
    }

    public async Task<UserDto> GetCurrentUserAsync(int userId, CancellationToken ct = default)
    {
        var user = await _uow.Users.GetByIdAsync(userId, ct);
        if (user == null)
            throw new NotFoundException(nameof(User), userId);

        var roles = user.UserRoles.Select(ur => ur.Role.Name).Distinct().ToList();
        var permissions = user.UserRoles
            .SelectMany(ur => ur.Role.RolePermissions)
            .Select(rp => rp.Permission.Code)
            .Distinct()
            .ToList();

        return MapUserDto(user, roles, permissions);
    }

    public async Task<IReadOnlyList<UserDto>> GetAllUsersAsync(CancellationToken ct = default)
    {
        var users = await _uow.Users.GetAllAsync(ct);
        return users.Select(u =>
        {
            var roles = u.UserRoles.Select(ur => ur.Role.Name).Distinct().ToList();
            return MapUserDto(u, roles, new List<string>());
        }).ToList();
    }

    public async Task<IReadOnlyList<RoleDto>> GetAllRolesAsync(CancellationToken ct = default)
    {
        var roles = await _uow.Users.GetAllRolesAsync(ct);
        return roles.Select(r => new RoleDto
        {
            Id = r.Id,
            Name = r.Name,
            Description = r.Description,
            Permissions = r.RolePermissions.Select(rp => new PermissionDto
            {
                Id = rp.Permission.Id,
                Code = rp.Permission.Code,
                Description = rp.Permission.Description
            }).ToList()
        }).ToList();
    }

    public async Task<IReadOnlyList<PermissionDto>> GetAllPermissionsAsync(CancellationToken ct = default)
    {
        var permissions = await _uow.Users.GetAllPermissionsAsync(ct);
        return permissions.Select(p => new PermissionDto
        {
            Id = p.Id,
            Code = p.Code,
            Description = p.Description
        }).ToList();
    }

    private static UserDto MapUserDto(User user, List<string> roles, List<string> permissions)
    {
        return new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            IsLocked = user.IsLocked,
            LastLoginAt = user.LastLoginAt,
            CreatedAt = user.CreatedAt,
            EmployeeId = user.Employee?.Id,
            FullName = user.Employee != null ? $"{user.Employee.FirstName} {user.Employee.LastName}" : "System User",
            Roles = roles,
            Permissions = permissions
        };
    }
}
