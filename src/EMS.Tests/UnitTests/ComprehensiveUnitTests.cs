using System.Security.Claims;
using EMS.Application.DTOs;
using EMS.Application.Exceptions;
using EMS.Application.Interfaces;
using EMS.Application.Services;
using EMS.Domain.Entities;
using EMS.Domain.Enums;
using EMS.Domain.Interfaces;
using EMS.Infrastructure.Security;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace EMS.Tests.UnitTests;

public class AuthServiceTests
{
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<IPasswordHasher> _hasherMock;
    private readonly Mock<ITokenService> _tokenMock;
    private readonly Mock<IAuditService> _auditMock;
    private readonly Mock<ILogger<AuthService>> _loggerMock;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _uowMock = new Mock<IUnitOfWork>();
        _hasherMock = new Mock<IPasswordHasher>();
        _tokenMock = new Mock<ITokenService>();
        _auditMock = new Mock<IAuditService>();
        _loggerMock = new Mock<ILogger<AuthService>>();

        _authService = new AuthService(_uowMock.Object, _hasherMock.Object, _tokenMock.Object, _auditMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsTokensAndResetsFailedAttempts()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Email = "user@company.com",
            PasswordHash = "hashed",
            PasswordSalt = "salted",
            FailedLoginCount = 2,
            IsLocked = false
        };

        _uowMock.Setup(u => u.Users.GetByEmailAsync("user@company.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _hasherMock.Setup(h => h.VerifyPassword("Password123", "hashed", "salted"))
            .Returns(true);
        _tokenMock.Setup(t => t.GenerateAccessToken(user, It.IsAny<Employee>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>()))
            .Returns(("access-token", 900));
        _tokenMock.Setup(t => t.GenerateRefreshToken()).Returns("raw-refresh-token");
        _tokenMock.Setup(t => t.HashToken("raw-refresh-token")).Returns("hashed-refresh-token");

        // Act
        var result = await _authService.LoginAsync(new LoginRequestDto { Email = "user@company.com", Password = "Password123" }, "127.0.0.1");

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be("access-token");
        result.RefreshToken.Should().Be("raw-refresh-token");
        user.FailedLoginCount.Should().Be(0);
        _uowMock.Verify(u => u.Users.AddRefreshTokenAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Login_InvalidPassword_IncrementsFailedCountAndThrowsUnauthorized()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Email = "user@company.com",
            PasswordHash = "hashed",
            PasswordSalt = "salted",
            FailedLoginCount = 0
        };

        _uowMock.Setup(u => u.Users.GetByEmailAsync("user@company.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _hasherMock.Setup(h => h.VerifyPassword("WrongPass", "hashed", "salted"))
            .Returns(false);

        // Act & Assert
        var act = () => _authService.LoginAsync(new LoginRequestDto { Email = "user@company.com", Password = "WrongPass" }, "127.0.0.1");
        await act.Should().ThrowAsync<UnauthorizedException>();
        user.FailedLoginCount.Should().Be(1);
    }

    [Fact]
    public async Task Login_FiveFailedAttempts_LocksAccount()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Email = "user@company.com",
            PasswordHash = "hashed",
            PasswordSalt = "salted",
            FailedLoginCount = 4
        };

        _uowMock.Setup(u => u.Users.GetByEmailAsync("user@company.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _hasherMock.Setup(h => h.VerifyPassword("WrongPass", "hashed", "salted"))
            .Returns(false);

        // Act & Assert
        var act = () => _authService.LoginAsync(new LoginRequestDto { Email = "user@company.com", Password = "WrongPass" }, "127.0.0.1");
        await act.Should().ThrowAsync<UnauthorizedException>();
        user.FailedLoginCount.Should().Be(5);
        user.IsLocked.Should().BeTrue();
        user.LockoutEnd.Should().NotBeNull();
    }

    [Fact]
    public async Task RefreshToken_RevokedTokenReused_TriggersTheftDetectionAndRevokesEntireFamily()
    {
        // Arrange
        var revokedToken = new RefreshToken
        {
            Id = 10,
            UserId = 5,
            TokenHash = "hash123",
            RevokedAt = DateTime.UtcNow.AddMinutes(-5),
            ExpiresAt = DateTime.UtcNow.AddDays(2)
        };

        _tokenMock.Setup(t => t.HashToken("stolen-token")).Returns("hash123");
        _uowMock.Setup(u => u.Users.GetRefreshTokenAsync("hash123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(revokedToken);

        // Act & Assert
        var act = () => _authService.RefreshTokenAsync("stolen-token", "192.168.1.1");
        await act.Should().ThrowAsync<UnauthorizedException>().WithMessage("*Compromised session detected*");

        _uowMock.Verify(u => u.Users.RevokeRefreshTokenFamilyAsync(5, It.IsAny<CancellationToken>()), Times.Once);
    }
}

public class EmployeeServiceTests
{
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<IPasswordHasher> _hasherMock;
    private readonly Mock<IAuditService> _auditMock;
    private readonly Mock<INotificationService> _notifMock;
    private readonly Mock<ILogger<EmployeeService>> _loggerMock;
    private readonly EmployeeService _service;

    public EmployeeServiceTests()
    {
        _uowMock = new Mock<IUnitOfWork>();
        _hasherMock = new Mock<IPasswordHasher>();
        _auditMock = new Mock<IAuditService>();
        _notifMock = new Mock<INotificationService>();
        _loggerMock = new Mock<ILogger<EmployeeService>>();

        _service = new EmployeeService(_uowMock.Object, _hasherMock.Object, _auditMock.Object, _notifMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Create_DuplicateEmail_ThrowsConflictException()
    {
        // Arrange
        _uowMock.Setup(u => u.Employees.ExistsByEmailAsync("test@company.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var dto = new CreateEmployeeDto { Email = "test@company.com", FirstName = "John", LastName = "Doe" };

        // Act & Assert
        var act = () => _service.CreateAsync(dto, 1);
        await act.Should().ThrowAsync<ConflictException>().WithMessage("*already in use*");
    }

    [Fact]
    public async Task Update_NonExistentEmployee_ThrowsNotFoundException()
    {
        // Arrange
        _uowMock.Setup(u => u.Employees.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employee?)null);

        var dto = new UpdateEmployeeDto { FirstName = "John", LastName = "Doe", Phone = "123" };

        // Act & Assert
        var act = () => _service.UpdateAsync(999, dto, 1);
        await act.Should().ThrowAsync<NotFoundException>();
    }
}

public class AttendanceAndLeaveServiceTests
{
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<IAuditService> _auditMock;
    private readonly Mock<INotificationService> _notifMock;
    private readonly AttendanceService _attendanceService;
    private readonly LeaveService _leaveService;

    public AttendanceAndLeaveServiceTests()
    {
        _uowMock = new Mock<IUnitOfWork>();
        _auditMock = new Mock<IAuditService>();
        _notifMock = new Mock<INotificationService>();

        _attendanceService = new AttendanceService(_uowMock.Object, _auditMock.Object, new Mock<ILogger<AttendanceService>>().Object);
        _leaveService = new LeaveService(_uowMock.Object, _auditMock.Object, _notifMock.Object, new Mock<ILogger<LeaveService>>().Object);
    }

    [Fact]
    public async Task Attendance_DuplicateCheckInSameDay_ThrowsConflictException()
    {
        // Arrange
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var existing = new Attendance
        {
            EmployeeId = 1,
            Date = today,
            CheckInTime = DateTime.UtcNow.AddHours(-2),
            Status = AttendanceStatus.Present
        };

        _uowMock.Setup(u => u.Attendance.GetTodayAttendanceAsync(1, today, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        // Act & Assert
        var act = () => _attendanceService.CheckInAsync(1, 1);
        await act.Should().ThrowAsync<ConflictException>().WithMessage("Already checked in for today.");
    }

    [Fact]
    public async Task Leave_OverlappingLeave_ThrowsConflictException()
    {
        // Arrange
        var startDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5));
        var endDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10));

        _uowMock.Setup(u => u.Leaves.HasOverlappingLeaveAsync(1, startDate, endDate, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var dto = new ApplyLeaveDto
        {
            EmployeeId = 1,
            LeaveTypeId = 1,
            StartDate = startDate,
            EndDate = endDate,
            Reason = "Vacation"
        };

        // Act & Assert
        var act = () => _leaveService.ApplyAsync(dto, 1);
        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Leave_EndDateBeforeStartDate_ThrowsValidationException()
    {
        // Arrange
        var startDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10));
        var endDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5));

        var dto = new ApplyLeaveDto
        {
            EmployeeId = 1,
            LeaveTypeId = 1,
            StartDate = startDate,
            EndDate = endDate,
            Reason = "Invalid Dates"
        };

        // Act & Assert
        var act = () => _leaveService.ApplyAsync(dto, 1);
        await act.Should().ThrowAsync<ValidationException>();
    }
}

public class AuthorizationHandlerTests
{
    [Fact]
    public async Task PermissionAuthorizationHandler_WhenUserHasPermission_Succeeds()
    {
        // Arrange
        var requirement = new PermissionRequirement("Employee.Create");
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("permissions", "Employee.Create"),
            new Claim("permissions", "Employee.Read")
        }, "TestAuth"));

        var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);
        var handler = new PermissionAuthorizationHandler();

        // Act
        await handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task PermissionAuthorizationHandler_WhenUserLacksPermission_DoesNotSucceed()
    {
        // Arrange
        var requirement = new PermissionRequirement("Employee.Delete");
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("permissions", "Employee.Read")
        }, "TestAuth"));

        var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);
        var handler = new PermissionAuthorizationHandler();

        // Act
        await handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeFalse();
    }
}
