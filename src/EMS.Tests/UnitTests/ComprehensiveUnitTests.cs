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

    [Fact]
    public async Task Leave_Approve_ByReportingManager_Succeeds()
    {
        // Arrange
        var managerUser = new User { Id = 10, Email = "mgr@company.com" };
        var managerEmp = new Employee { Id = 2, UserId = 10, FirstName = "Boss", LastName = "Man" };
        var empUser = new User { Id = 20, Email = "emp@company.com" };
        var emp = new Employee { Id = 5, UserId = 20, FirstName = "John", LastName = "Doe", ManagerId = 2 };
        var leave = new Leave { Id = 100, EmployeeId = 5, Employee = emp, Status = LeaveStatus.Pending, StartDate = new DateOnly(2026, 10, 1), EndDate = new DateOnly(2026, 10, 5) };

        _uowMock.Setup(u => u.Leaves.GetByIdAsync(100, It.IsAny<CancellationToken>())).ReturnsAsync(leave);
        _uowMock.Setup(u => u.Employees.GetByUserIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(managerEmp);
        _uowMock.Setup(u => u.Users.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(managerUser);

        // Act
        var result = await _leaveService.ApproveAsync(100, new ApproveLeaveDto { Approved = true }, 10);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(LeaveStatus.Approved);
        leave.ApprovedBy.Should().Be(2);
        _uowMock.Verify(u => u.Leaves.Update(leave), Times.Once);
    }

    [Fact]
    public async Task Leave_Approve_ByHR_Succeeds()
    {
        // Arrange
        var hrRole = new Role { Id = 2, Name = "HR" };
        var hrUser = new User
        {
            Id = 30,
            Email = "hr@company.com",
            UserRoles = new List<UserRole> { new() { RoleId = 2, Role = hrRole } }
        };
        var hrEmp = new Employee { Id = 3, UserId = 30, FirstName = "HR", LastName = "Admin" };
        var emp = new Employee { Id = 5, UserId = 20, FirstName = "John", LastName = "Doe", ManagerId = 99 };
        var leave = new Leave { Id = 101, EmployeeId = 5, Employee = emp, Status = LeaveStatus.Pending };

        _uowMock.Setup(u => u.Leaves.GetByIdAsync(101, It.IsAny<CancellationToken>())).ReturnsAsync(leave);
        _uowMock.Setup(u => u.Employees.GetByUserIdAsync(30, It.IsAny<CancellationToken>())).ReturnsAsync(hrEmp);
        _uowMock.Setup(u => u.Users.GetByIdAsync(30, It.IsAny<CancellationToken>())).ReturnsAsync(hrUser);

        // Act
        var result = await _leaveService.ApproveAsync(101, new ApproveLeaveDto { Approved = true }, 30);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(LeaveStatus.Approved);
        leave.ApprovedBy.Should().Be(3);
    }

    [Fact]
    public async Task Leave_Approve_ByUnrelatedManager_ThrowsForbiddenException()
    {
        // Arrange
        var managerEmp = new Employee { Id = 9, UserId = 90 };
        var managerUser = new User { Id = 90, UserRoles = new List<UserRole>() };
        var emp = new Employee { Id = 5, UserId = 20, ManagerId = 2 }; // Manager is 2, not 9
        var leave = new Leave { Id = 102, EmployeeId = 5, Employee = emp, Status = LeaveStatus.Pending };

        _uowMock.Setup(u => u.Leaves.GetByIdAsync(102, It.IsAny<CancellationToken>())).ReturnsAsync(leave);
        _uowMock.Setup(u => u.Employees.GetByUserIdAsync(90, It.IsAny<CancellationToken>())).ReturnsAsync(managerEmp);
        _uowMock.Setup(u => u.Users.GetByIdAsync(90, It.IsAny<CancellationToken>())).ReturnsAsync(managerUser);

        // Act & Assert
        var act = () => _leaveService.ApproveAsync(102, new ApproveLeaveDto { Approved = true }, 90);
        await act.Should().ThrowAsync<ForbiddenException>().WithMessage("*Only the employee's reporting manager or HR*");
    }

    [Fact]
    public async Task Leave_Approve_SelfApproval_ThrowsForbiddenException()
    {
        // Arrange
        var managerEmp = new Employee { Id = 2, UserId = 10, ManagerId = 1 };
        var managerUser = new User
        {
            Id = 10,
            UserRoles = new List<UserRole> { new() { Role = new Role { Name = "Manager" } } }
        };
        var leave = new Leave { Id = 103, EmployeeId = 2, Employee = managerEmp, Status = LeaveStatus.Pending };

        _uowMock.Setup(u => u.Leaves.GetByIdAsync(103, It.IsAny<CancellationToken>())).ReturnsAsync(leave);
        _uowMock.Setup(u => u.Employees.GetByUserIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(managerEmp);
        _uowMock.Setup(u => u.Users.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(managerUser);

        // Act & Assert
        var act = () => _leaveService.ApproveAsync(103, new ApproveLeaveDto { Approved = true }, 10);
        await act.Should().ThrowAsync<ForbiddenException>().WithMessage("*Employees cannot approve or reject their own leave requests*");
    }

    [Fact]
    public async Task Leave_Approve_CancelledLeave_ThrowsConflictException()
    {
        // Arrange
        var hrUser = new User { Id = 30, UserRoles = new List<UserRole> { new() { Role = new Role { Name = "HR" } } } };
        var hrEmp = new Employee { Id = 3, UserId = 30 };
        var leave = new Leave { Id = 104, EmployeeId = 5, Status = LeaveStatus.Cancelled };

        _uowMock.Setup(u => u.Leaves.GetByIdAsync(104, It.IsAny<CancellationToken>())).ReturnsAsync(leave);

        // Act & Assert
        var act = () => _leaveService.ApproveAsync(104, new ApproveLeaveDto { Approved = true }, 30);
        await act.Should().ThrowAsync<ConflictException>().WithMessage("*Cannot approve or reject a cancelled leave request*");
    }

    [Fact]
    public async Task Leave_Apply_ExceedingAvailableQuota_ThrowsConflictException()
    {
        // Arrange
        var leaveType = new LeaveType { Id = 1, Name = "Casual Leave", DefaultDaysPerYear = 6 };
        var emp = new Employee { Id = 5, UserId = 20, FirstName = "John", LastName = "Doe" };
        var existingApprovedLeave = new Leave
        {
            Id = 50,
            EmployeeId = 5,
            LeaveTypeId = 1,
            StartDate = new DateOnly(2026, 3, 1),
            EndDate = new DateOnly(2026, 3, 4), // 4 days
            Status = LeaveStatus.Approved
        };

        _uowMock.Setup(u => u.Leaves.GetLeaveTypeByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(leaveType);
        _uowMock.Setup(u => u.Leaves.HasOverlappingLeaveAsync(5, It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), null, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _uowMock.Setup(u => u.Leaves.GetLeavesAsync(5, null, null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Leave> { existingApprovedLeave });

        var dto = new ApplyLeaveDto
        {
            EmployeeId = 5,
            LeaveTypeId = 1,
            StartDate = new DateOnly(2026, 6, 1),
            EndDate = new DateOnly(2026, 6, 4), // 4 days requested, but only 2 available (6 - 4)
            Reason = "Trip"
        };

        // Act & Assert
        var act = () => _leaveService.ApplyAsync(dto, 20);
        await act.Should().ThrowAsync<ConflictException>().WithMessage("*Insufficient leave balance for Casual Leave. Available: 2 days, Requested: 4 days*");
    }

    [Fact]
    public async Task Leave_GetLeaveBalances_CalculatesAllocatedUsedAndAvailableCorrectly()
    {
        // Arrange
        var emp = new Employee { Id = 5, UserId = 20, FirstName = "Alex", LastName = "Rivera" };
        var leaveTypes = new List<LeaveType>
        {
            new() { Id = 1, Name = "Annual Leave", DefaultDaysPerYear = 18 },
            new() { Id = 2, Name = "Sick Leave", DefaultDaysPerYear = 12 }
        };
        var leaves = new List<Leave>
        {
            new() { EmployeeId = 5, LeaveTypeId = 1, StartDate = new DateOnly(2026, 1, 10), EndDate = new DateOnly(2026, 1, 14), Status = LeaveStatus.Approved }, // 5 days approved
            new() { EmployeeId = 5, LeaveTypeId = 1, StartDate = new DateOnly(2026, 4, 1), EndDate = new DateOnly(2026, 4, 2), Status = LeaveStatus.Pending } // 2 days pending
        };

        _uowMock.Setup(u => u.Employees.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(emp);
        _uowMock.Setup(u => u.Leaves.GetAllLeaveTypesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(leaveTypes);
        _uowMock.Setup(u => u.Leaves.GetLeavesAsync(5, null, null, null, null, It.IsAny<CancellationToken>())).ReturnsAsync(leaves);

        // Act
        var balances = await _leaveService.GetLeaveBalancesAsync(5, 2026);

        // Assert
        balances.Should().HaveCount(2);
        var annual = balances.First(b => b.LeaveTypeId == 1);
        annual.TotalAllocatedDays.Should().Be(18);
        annual.UsedDays.Should().Be(5);
        annual.PendingDays.Should().Be(2);
        annual.AvailableDays.Should().Be(11); // 18 - 5 - 2

        var sick = balances.First(b => b.LeaveTypeId == 2);
        sick.TotalAllocatedDays.Should().Be(12);
        sick.UsedDays.Should().Be(0);
        sick.PendingDays.Should().Be(0);
        sick.AvailableDays.Should().Be(12);
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
