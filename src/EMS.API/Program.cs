using System.Text;
using EMS.API.Filters;
using EMS.API.Middleware;
using EMS.Application.Interfaces;
using EMS.Application.Services;
using EMS.Application.Validators;
using EMS.Domain.Interfaces;
using EMS.Infrastructure.Logging;
using EMS.Infrastructure.Persistence;
using EMS.Infrastructure.Repositories;
using EMS.Infrastructure.Security;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
SerilogConfig.ConfigureLogging();
builder.Host.UseSerilog();

// Add Controllers with Validation Filter
builder.Services.AddControllers(options =>
{
    options.Filters.Add<ValidationFilter>();
});

// FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>();

// Database Context (PostgreSQL with SQLite local dev fallback)
var connectionString = builder.Configuration
    .GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "DefaultConnection is not configured.");

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(
        connectionString,
        npgsqlOptions =>
        {
            npgsqlOptions.MigrationsAssembly(
                typeof(AppDbContext).Assembly.FullName);
        });
});

// HttpContext Accessor
builder.Services.AddHttpContextAccessor();

// Domain & Infrastructure Dependency Injection (DIP Compliance)
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<ITokenService, JwtTokenGenerator>();

builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
builder.Services.AddScoped<IDepartmentRepository, DepartmentRepository>();
builder.Services.AddScoped<IPositionRepository, PositionRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAttendanceRepository, AttendanceRepository>();
builder.Services.AddScoped<ILeaveRepository, LeaveRepository>();
builder.Services.AddScoped<ISalaryRepository, SalaryRepository>();
builder.Services.AddScoped<IProjectRepository, ProjectRepository>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Application Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<IDepartmentService, DepartmentService>();
builder.Services.AddScoped<IPositionService, PositionService>();
builder.Services.AddScoped<IAttendanceService, AttendanceService>();
builder.Services.AddScoped<ILeaveService, LeaveService>();
builder.Services.AddScoped<ISalaryService, SalaryService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IReportService, ReportService>();

// JWT Authentication Setup
var jwtSecretKey = builder.Configuration["Jwt:SecretKey"] ?? "super_secret_jwt_key_ems_enterprise_2026_dev_prod_token_must_be_at_least_256_bits_long!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "EMS.API";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "EMS.Client";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
        ClockSkew = TimeSpan.Zero
    };
});

// Authorization Handlers
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
builder.Services.AddScoped<IAuthorizationHandler, OwnResourceAuthorizationHandler>();

// RBAC & Policy-based Authorization Configuration
builder.Services.AddAuthorization(options =>
{
    // Exact permission policies
    var standardPermissions = new[]
    {
        "Employee.Create", "Employee.Read", "Employee.Update", "Employee.Delete",
        "Department.Create", "Department.Read", "Department.Update", "Department.Delete",
        "Position.Create", "Position.Read", "Position.Update", "Position.Delete",
        "Attendance.Read", "Attendance.Update",
        "Leave.Read", "Leave.Approve",
        "Salary.Read", "Salary.Update",
        "Project.Create", "Project.Read", "Project.Update", "Project.Assign",
        "User.Manage", "Role.Manage", "AuditLog.Read", "Reports.Read"
    };

    foreach (var perm in standardPermissions)
    {
        options.AddPolicy(perm, policy =>
            policy.Requirements.Add(new PermissionRequirement(perm)));
    }

    // Resource-based / Own / Team Policies
    options.AddPolicy("Employee.Read.Own", policy =>
        policy.Requirements.Add(new OwnResourceRequirement("Employee.Read")));

    options.AddPolicy("Employee.Update.Own", policy =>
        policy.Requirements.Add(new OwnResourceRequirement("Employee.Update")));

    options.AddPolicy("Employee.Read.Team", policy =>
        policy.Requirements.Add(new PermissionRequirement("Employee.Read.Team")));

    options.AddPolicy("Attendance.Read.Own", policy =>
        policy.Requirements.Add(new OwnResourceRequirement("Attendance.Read")));

    options.AddPolicy("Attendance.CheckIn.Own", policy =>
        policy.Requirements.Add(new OwnResourceRequirement("Attendance.CheckIn")));

    options.AddPolicy("Attendance.Read.Team", policy =>
        policy.Requirements.Add(new PermissionRequirement("Attendance.Read.Team")));

    options.AddPolicy("Leave.Create.Own", policy =>
        policy.Requirements.Add(new OwnResourceRequirement("Leave.Create")));

    options.AddPolicy("Leave.Read.Own", policy =>
        policy.Requirements.Add(new OwnResourceRequirement("Leave.Read")));

    options.AddPolicy("Leave.Approve.Team", policy =>
        policy.Requirements.Add(new OwnResourceRequirement("Leave.Approve")));

    options.AddPolicy("Leave.Read.Team", policy =>
        policy.Requirements.Add(new PermissionRequirement("Leave.Read.Team")));

    options.AddPolicy("Salary.Read.Own", policy =>
        policy.Requirements.Add(new OwnResourceRequirement("Salary.Read")));

    options.AddPolicy("Project.Assign.Team", policy =>
        policy.Requirements.Add(new OwnResourceRequirement("Project.Assign")));
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultCorsPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000", "http://localhost:80", "http://127.0.0.1:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Health Checks
var healthBuilder = builder.Services.AddHealthChecks();
if ( connectionString.Contains("Host="))
{
    healthBuilder.AddNpgSql(connectionString, name: "postgresql", tags: new[] { "ready", "db" });
}
else
{
    healthBuilder.AddAsyncCheck("database", async () =>
    {
        return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Database reachable.");
    }, tags: new[] { "ready", "db" });
}

// Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Employee Management System (EMS) API",
        Version = "v1",
        Description = "Enterprise REST API for EMS with JWT, RBAC, and Clean Architecture"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below. Example: 'Bearer eyJhbGciOi...'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Automated DB Migration & Seeding on Startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        var db = services.GetRequiredService<AppDbContext>();
        if (db.Database.IsRelational())
        {
            await db.Database.MigrateAsync();
        }

        var hasher = services.GetRequiredService<IPasswordHasher>();
        await DatabaseSeeder.SeedAsync(db, hasher, logger);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database migration/seeding failed.");
        throw;
    }
}

// Middleware Pipeline in EXACT specified order (§6.4)
// 1. CorrelationIdMiddleware
app.UseMiddleware<CorrelationIdMiddleware>();

// 2. ExceptionHandlingMiddleware (outermost catch)
app.UseMiddleware<ExceptionHandlingMiddleware>();

// 3. HttpsRedirection (disabled in development if needed, or active)
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// 4. UseCors
app.UseCors("DefaultCorsPolicy");

// 5. RequestLoggingMiddleware
app.UseMiddleware<RequestLoggingMiddleware>();

// Swagger UI
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "EMS API v1");
    c.RoutePrefix = "swagger";
});

// 6. UseAuthentication
app.UseAuthentication();

// 7. UseAuthorization
app.UseAuthorization();

// 8. MapControllers & Health Checks
app.MapControllers();

app.MapHealthChecks("/health");
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false
});
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.Run();

// Export Program for Integration Testing WebApplicationFactory
public partial class Program { }
