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
}
