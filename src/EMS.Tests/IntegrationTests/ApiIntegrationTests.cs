using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using EMS.Application.DTOs;
using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using EMS.Domain.Enums;
using EMS.Domain.Interfaces;
using EMS.Infrastructure.Persistence;
using EMS.Infrastructure.Security;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EMS.Tests.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptors = services.Where(d =>
                d.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                d.ServiceType == typeof(DbContextOptions) ||
                d.ServiceType == typeof(AppDbContext) ||
                (d.ServiceType.FullName != null && d.ServiceType.FullName.Contains("AppDbContext")) ||
                (d.ImplementationType != null && d.ImplementationType.FullName != null && d.ImplementationType.FullName.Contains("Npgsql")) ||
                (d.ServiceType.FullName != null && d.ServiceType.FullName.Contains("Npgsql"))).ToList();

            foreach (var d in descriptors)
            {
                services.Remove(d);
            }

            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseInMemoryDatabase(_dbName);
            });

            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            db.Database.EnsureCreated();
            DatabaseSeeder.SeedAsync(db, hasher, new Microsoft.Extensions.Logging.Abstractions.NullLogger<Program>()).GetAwaiter().GetResult();
        });
    }
}

public class AuthAndControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AuthAndControllerIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_ValidAdmin_Returns200AndTokenEnvelope()
    {
        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto
        {
            Email = "admin@company.com",
            Password = "Admin@123"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponseDto>>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeTrue();
        envelope.Data.Should().NotBeNull();
        envelope.Data!.AccessToken.Should().NotBeNullOrWhiteSpace();
        envelope.Data.User.Email.Should().Be("admin@company.com");
    }

    [Fact]
    public async Task Login_InvalidPassword_Returns401()
    {
        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto
        {
            Email = "admin@company.com",
            Password = "IncorrectPassword"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<object>>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        envelope!.Success.Should().BeFalse();
        envelope.Message.Should().Contain("Invalid email or password");
    }

    [Fact]
    public async Task GetEmployees_WithoutToken_Returns401()
    {
        // Act
        var response = await _client.GetAsync("/api/employees");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task FullWorkflow_LoginAdmin_CreateEmployee_GetSalary()
    {
        // 1. Login as Admin
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto
        {
            Email = "admin@company.com",
            Password = "Admin@123"
        });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var loginData = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<LoginResponseDto>>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        var token = loginData!.Data!.AccessToken;

        // 2. Setup HttpClient with Bearer token
        var authedClient = _factory.CreateClient();
        authedClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // 3. Create a new employee
        var createEmpDto = new CreateEmployeeDto
        {
            FirstName = "Integration",
            LastName = "TestUser",
            Email = $"test.user.{Guid.NewGuid():N}@company.com",
            Phone = "+1-555-9999",
            DateOfBirth = new DateTime(1996, 4, 15),
            DepartmentId = 1,
            PositionId = 1,
            JoiningDate = DateTime.UtcNow.Date,
            EmploymentType = EmploymentType.Permanent
        };

        var createResponse = await authedClient.PostAsJsonAsync("/api/employees", createEmpDto);
        if (createResponse.StatusCode != HttpStatusCode.Created)
        {
            var content = await createResponse.Content.ReadAsStringAsync();
            throw new Exception($"CreateEmployee failed with status {createResponse.StatusCode}: {content}");
        }
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdEnvelope = await createResponse.Content.ReadFromJsonAsync<ApiResponse<EmployeeDto>>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        createdEnvelope!.Success.Should().BeTrue();
        var empId = createdEnvelope.Data!.Id;

        // 4. Get Created Employee
        var getEmpResponse = await authedClient.GetAsync($"/api/employees/{empId}");
        getEmpResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // 5. Get Salary Structure
        var salaryResponse = await authedClient.GetAsync($"/api/employees/{empId}/salary");
        salaryResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // 6. Generate Payslip
        var generatePayslipResponse = await authedClient.PostAsync($"/api/employees/{empId}/payslips/generate?month=9&year=2026", null);
        generatePayslipResponse.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task StandardEmployee_Attendance_ReturnsOnlyOwnRecords_AndPreventsTampering()
    {
        // 1. Login as standard Employee (Alex Rivera - EMP-0004)
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto
        {
            Email = "employee@company.com",
            Password = "Employee@123"
        });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var loginData = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<LoginResponseDto>>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        var token = loginData!.Data!.AccessToken;
        var myEmpId = loginData.Data.User.EmployeeId;
        myEmpId.Should().NotBeNull();

        var empClient = _factory.CreateClient();
        empClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // 2. Query general attendance history - should return ONLY own records
        var attResponse = await empClient.GetAsync("/api/attendance");
        attResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var attData = await attResponse.Content.ReadFromJsonAsync<ApiResponse<List<AttendanceDto>>>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        attData!.Success.Should().BeTrue();
        attData.Data.Should().NotBeNull();
        attData.Data!.Should().NotBeEmpty();
        attData.Data.Should().OnlyContain(a => a.EmployeeId == myEmpId!.Value);

        // 3. Attempting to query another employee's attendance by passing ?employeeId=3
        var spoofResponse = await empClient.GetAsync("/api/attendance?employeeId=3");
        if (spoofResponse.StatusCode == HttpStatusCode.OK)
        {
            var spoofData = await spoofResponse.Content.ReadFromJsonAsync<ApiResponse<List<AttendanceDto>>>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            spoofData!.Data.Should().OnlyContain(a => a.EmployeeId == myEmpId!.Value);
        }
        else
        {
            spoofResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        // 4. Attempting check-in with another employee's ID in body
        var checkInResponse = await empClient.PostAsJsonAsync("/api/attendance/check-in", new CheckInRequestDto { EmployeeId = 1 });
        checkInResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var checkInData = await checkInResponse.Content.ReadFromJsonAsync<ApiResponse<AttendanceDto>>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        checkInData!.Data!.EmployeeId.Should().Be(myEmpId!.Value); // Must enforce caller's employee ID, not 1
    }

    [Fact]
    public async Task StandardEmployee_Leaves_ReturnsOnlyOwnRecords_AndPreventsTampering()
    {
        // 1. Login as standard Employee
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto
        {
            Email = "employee@company.com",
            Password = "Employee@123"
        });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var loginData = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<LoginResponseDto>>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        var token = loginData!.Data!.AccessToken;
        var myEmpId = loginData.Data.User.EmployeeId;

        var empClient = _factory.CreateClient();
        empClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // 2. Apply for leave with spoofed EmployeeId=1 (Admin) in request body
        var applyResponse = await empClient.PostAsJsonAsync("/api/leaves", new ApplyLeaveDto
        {
            EmployeeId = 1,
            LeaveTypeId = 1,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(11)),
            Reason = "Personal work"
        });
        applyResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var applyData = await applyResponse.Content.ReadFromJsonAsync<ApiResponse<LeaveDto>>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        applyData!.Data!.EmployeeId.Should().Be(myEmpId!.Value); // Server must enforce own EmployeeId

        // 3. Query leaves - should ONLY return own leave applications
        var leavesResponse = await empClient.GetAsync("/api/leaves");
        leavesResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var leavesData = await leavesResponse.Content.ReadFromJsonAsync<ApiResponse<List<LeaveDto>>>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        leavesData!.Success.Should().BeTrue();
        leavesData.Data.Should().NotBeNull();
        leavesData.Data.Should().OnlyContain(l => l.EmployeeId == myEmpId!.Value);
    }

    [Fact]
    public async Task AdminAndHR_CanViewAllAttendanceAndLeaves()
    {
        // 1. Login as Admin
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto
        {
            Email = "admin@company.com",
            Password = "Admin@123"
        });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var loginData = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<LoginResponseDto>>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        var token = loginData!.Data!.AccessToken;

        var adminClient = _factory.CreateClient();
        adminClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // 2. Query attendance history as Admin - contains all employees
        var attResponse = await adminClient.GetAsync("/api/attendance");
        attResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var attData = await attResponse.Content.ReadFromJsonAsync<ApiResponse<List<AttendanceDto>>>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        attData!.Data!.Select(a => a.EmployeeId).Distinct().Count().Should().BeGreaterThan(1);
    }

    [Fact]
    public async Task LeaveApproval_ReportingManagerAndHR_CanApprove_EmployeeCannot()
    {
        // 1. Login as standard Employee (Alex Rivera, manager is David Miller)
        var empLogin = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto
        {
            Email = "employee@company.com",
            Password = "Employee@123"
        });
        var empToken = (await empLogin.Content.ReadFromJsonAsync<ApiResponse<LoginResponseDto>>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true }))!.Data!.AccessToken;
        var empClient = _factory.CreateClient();
        empClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", empToken);

        // Apply for leave
        var applyResponse = await empClient.PostAsJsonAsync("/api/leaves", new ApplyLeaveDto
        {
            LeaveTypeId = 1,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(20)),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(22)),
            Reason = "Vacation Trip"
        });
        applyResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdLeave = (await applyResponse.Content.ReadFromJsonAsync<ApiResponse<LeaveDto>>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true }))!.Data!;

        // 2. Employee attempts to approve own leave -> 403 Forbidden
        var empApproveAttempt = await empClient.PutAsJsonAsync($"/api/leaves/{createdLeave.Id}/approve", new ApproveLeaveDto { Approved = true });
        empApproveAttempt.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // 3. Login as Manager (David Miller, Alex's reporting manager)
        var mgrLogin = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto
        {
            Email = "manager@company.com",
            Password = "Manager@123"
        });
        var mgrToken = (await mgrLogin.Content.ReadFromJsonAsync<ApiResponse<LoginResponseDto>>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true }))!.Data!.AccessToken;
        var mgrClient = _factory.CreateClient();
        mgrClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", mgrToken);

        // Manager approves the team member's leave -> 200 OK
        var mgrApprove = await mgrClient.PutAsJsonAsync($"/api/leaves/{createdLeave.Id}/approve", new ApproveLeaveDto { Approved = true, Comments = "Approved by Manager" });
        mgrApprove.StatusCode.Should().Be(HttpStatusCode.OK);
        var mgrApproveData = (await mgrApprove.Content.ReadFromJsonAsync<ApiResponse<LeaveDto>>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true }))!.Data!;
        mgrApproveData.Status.Should().Be(LeaveStatus.Approved);

        // 4. Employee applies for another leave for HR approval test
        var apply2Response = await empClient.PostAsJsonAsync("/api/leaves", new ApplyLeaveDto
        {
            LeaveTypeId = 2,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(31)),
            Reason = "Medical Leave"
        });
        var leave2 = (await apply2Response.Content.ReadFromJsonAsync<ApiResponse<LeaveDto>>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true }))!.Data!;

        // 5. Login as HR (Sarah Jenkins)
        var hrLogin = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto
        {
            Email = "hr@company.com",
            Password = "Hr@12345"
        });
        var hrToken = (await hrLogin.Content.ReadFromJsonAsync<ApiResponse<LoginResponseDto>>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true }))!.Data!.AccessToken;
        var hrClient = _factory.CreateClient();
        hrClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", hrToken);

        // HR approves leave -> 200 OK
        var hrApprove = await hrClient.PutAsJsonAsync($"/api/leaves/{leave2.Id}/approve", new ApproveLeaveDto { Approved = true, Comments = "Approved by HR" });
        hrApprove.StatusCode.Should().Be(HttpStatusCode.OK);
        var hrApproveData = (await hrApprove.Content.ReadFromJsonAsync<ApiResponse<LeaveDto>>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true }))!.Data!;
        hrApproveData.Status.Should().Be(LeaveStatus.Approved);
    }

    [Fact]
    public async Task LeaveBalancesAndPendingApprovals_Workflow()
    {
        // 1. Employee checks initial leave balances
        var empLogin = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto
        {
            Email = "employee@company.com",
            Password = "Employee@123"
        });
        var empToken = (await empLogin.Content.ReadFromJsonAsync<ApiResponse<LoginResponseDto>>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true }))!.Data!.AccessToken;
        var empClient = _factory.CreateClient();
        empClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", empToken);

        var balanceResponse = await empClient.GetAsync("/api/leaves/balances");
        balanceResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var balances = (await balanceResponse.Content.ReadFromJsonAsync<ApiResponse<List<LeaveBalanceDto>>>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true }))!.Data!;
        balances.Should().NotBeEmpty();
        var annualLeave = balances.First(b => b.LeaveTypeName == "Annual Leave");
        annualLeave.TotalAllocatedDays.Should().Be(18);

        // 2. Employee applies for 3 days of Annual Leave
        var applyResponse = await empClient.PostAsJsonAsync("/api/leaves", new ApplyLeaveDto
        {
            LeaveTypeId = annualLeave.LeaveTypeId,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(40)),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(42)),
            Reason = "Vacation"
        });
        applyResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var appliedLeave = (await applyResponse.Content.ReadFromJsonAsync<ApiResponse<LeaveDto>>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true }))!.Data!;
        appliedLeave.DaysCount.Should().Be(3);
        appliedLeave.AvailableDaysRemaining.Should().Be(annualLeave.AvailableDays - 3);

        // 3. Manager logs in and checks pending approvals - should see the pending leave
        var mgrLogin = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto
        {
            Email = "manager@company.com",
            Password = "Manager@123"
        });
        var mgrToken = (await mgrLogin.Content.ReadFromJsonAsync<ApiResponse<LoginResponseDto>>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true }))!.Data!.AccessToken;
        var mgrClient = _factory.CreateClient();
        mgrClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", mgrToken);

        var pendingResponse = await mgrClient.GetAsync("/api/leaves/pending-approvals");
        pendingResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var pendingLeaves = (await pendingResponse.Content.ReadFromJsonAsync<ApiResponse<List<LeaveDto>>>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true }))!.Data!;
        pendingLeaves.Should().Contain(l => l.Id == appliedLeave.Id);

        // 4. Manager approves the leave
        var approveResponse = await mgrClient.PutAsJsonAsync($"/api/leaves/{appliedLeave.Id}/approve", new ApproveLeaveDto { Approved = true });
        approveResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // 5. Manager checks pending approvals again - approved leave MUST NOT appear
        var pendingAfterResponse = await mgrClient.GetAsync("/api/leaves/pending-approvals");
        pendingAfterResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var pendingAfter = (await pendingAfterResponse.Content.ReadFromJsonAsync<ApiResponse<List<LeaveDto>>>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true }))!.Data!;
        pendingAfter.Should().NotContain(l => l.Id == appliedLeave.Id);
    }
}
