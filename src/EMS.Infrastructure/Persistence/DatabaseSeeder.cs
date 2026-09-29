using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using EMS.Domain.Enums;
using EMS.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EMS.Infrastructure.Persistence;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(AppDbContext context, IPasswordHasher hasher, ILogger logger)
    {
        if (await context.Roles.AnyAsync())
        {
            return; // Already seeded
        }

        logger.LogInformation("Seeding initial EMS data: Roles, Permissions, Admin, Sample Data...");

        // 1. Roles
        var adminRole = new Role { Name = "Admin", Description = "System Administrator with full access" };
        var hrRole = new Role { Name = "HR", Description = "Human Resources Officer" };
        var managerRole = new Role { Name = "Manager", Description = "Department or Team Manager" };
        var employeeRole = new Role { Name = "Employee", Description = "Standard Employee" };

        context.Roles.AddRange(adminRole, hrRole, managerRole, employeeRole);
        await context.SaveChangesAsync();

        // 2. Permissions
        var permissionsList = new List<Permission>
        {
            new() { Code = "*", Description = "Full system access" },
            new() { Code = "Employee.Create", Description = "Create employee" },
            new() { Code = "Employee.Read", Description = "View all employees" },
            new() { Code = "Employee.Update", Description = "Update any employee" },
            new() { Code = "Employee.Delete", Description = "Deactivate/delete employee" },
            new() { Code = "Employee.Read.Own", Description = "View own employee record" },
            new() { Code = "Employee.Update.Own", Description = "Update own employee record" },
            new() { Code = "Employee.Read.Team", Description = "View team members" },

            new() { Code = "Department.Create", Description = "Create department" },
            new() { Code = "Department.Read", Description = "View departments" },
            new() { Code = "Department.Update", Description = "Update department" },
            new() { Code = "Department.Delete", Description = "Delete department" },

            new() { Code = "Position.Create", Description = "Create position" },
            new() { Code = "Position.Read", Description = "View positions" },
            new() { Code = "Position.Update", Description = "Update position" },
            new() { Code = "Position.Delete", Description = "Delete position" },

            new() { Code = "Attendance.Read", Description = "View all attendances" },
            new() { Code = "Attendance.Update", Description = "Update attendances" },
            new() { Code = "Attendance.Read.Own", Description = "View own attendance" },
            new() { Code = "Attendance.CheckIn.Own", Description = "Check-in and check-out for self" },
            new() { Code = "Attendance.Read.Team", Description = "View team attendances" },

            new() { Code = "Leave.Read", Description = "View all leaves" },
            new() { Code = "Leave.Create.Own", Description = "Apply for leave" },
            new() { Code = "Leave.Read.Own", Description = "View own leaves" },
            new() { Code = "Leave.Approve", Description = "Approve any leave" },
            new() { Code = "Leave.Approve.Team", Description = "Approve team leave" },
            new() { Code = "Leave.Read.Team", Description = "View team leaves" },

            new() { Code = "Salary.Read", Description = "View all salaries and payslips" },
            new() { Code = "Salary.Update", Description = "Manage salaries and generate payslips" },
            new() { Code = "Salary.Read.Own", Description = "View own salary and payslip" },

            new() { Code = "Project.Create", Description = "Create project" },
            new() { Code = "Project.Read", Description = "View all projects" },
            new() { Code = "Project.Update", Description = "Update project" },
            new() { Code = "Project.Assign", Description = "Assign employees to project" },
            new() { Code = "Project.Assign.Team", Description = "Assign team to project" },

            new() { Code = "User.Manage", Description = "Manage user accounts" },
            new() { Code = "Role.Manage", Description = "Manage roles and permissions" },
            new() { Code = "AuditLog.Read", Description = "View system audit logs" },
            new() { Code = "Reports.Read", Description = "View reports" }
        };

        context.Permissions.AddRange(permissionsList);
        await context.SaveChangesAsync();

        // 3. Map Role Permissions
        // Admin: all
        foreach (var perm in permissionsList)
        {
            context.RolePermissions.Add(new RolePermission { RoleId = adminRole.Id, PermissionId = perm.Id });
        }

        // HR
        var hrCodes = new HashSet<string>
        {
            "Employee.Create", "Employee.Read", "Employee.Update", "Department.Read",
            "Position.Read", "Attendance.Read", "Attendance.Update", "Leave.Read", "Leave.Approve",
            "Salary.Read", "Salary.Update", "Project.Read", "Reports.Read", "AuditLog.Read"
        };
        foreach (var perm in permissionsList.Where(p => hrCodes.Contains(p.Code)))
        {
            context.RolePermissions.Add(new RolePermission { RoleId = hrRole.Id, PermissionId = perm.Id });
        }

        // Manager
        var mgrCodes = new HashSet<string>
        {
            "Employee.Read.Team", "Attendance.Read.Team", "Leave.Approve.Team", "Leave.Read.Team","Employee.Read.Team",
            "Project.Assign.Team", "Project.Read", "Department.Read", "Position.Read",
            "Employee.Read.Own", "Employee.Update.Own", "Attendance.Read.Own", "Attendance.CheckIn.Own", "Leave.Create.Own", "Leave.Read.Own", "Salary.Read.Own", "Reports.Read"
        };
        foreach (var perm in permissionsList.Where(p => mgrCodes.Contains(p.Code)))
        {
            context.RolePermissions.Add(new RolePermission { RoleId = managerRole.Id, PermissionId = perm.Id });
        }

        // Employee
        var empCodes = new HashSet<string>
        {
            "Employee.Read.Own", "Employee.Update.Own", "Attendance.Read.Own", "Attendance.CheckIn.Own",
            "Leave.Create.Own", "Leave.Read.Own", "Salary.Read.Own", "Department.Read", "Position.Read", "Project.Read"
        };
        foreach (var perm in permissionsList.Where(p => empCodes.Contains(p.Code)))
        {
            context.RolePermissions.Add(new RolePermission { RoleId = employeeRole.Id, PermissionId = perm.Id });
        }
        await context.SaveChangesAsync();

        // 4. Leave Types
        var leaveTypes = new List<LeaveType>
        {
            new() { Name = "Annual Leave", DefaultDaysPerYear = 18 },
            new() { Name = "Sick Leave", DefaultDaysPerYear = 12 },
            new() { Name = "Casual Leave", DefaultDaysPerYear = 6 },
            new() { Name = "Maternity / Paternity Leave", DefaultDaysPerYear = 90 }
        };
        context.LeaveTypes.AddRange(leaveTypes);

        // 5. Departments
        var deptEng = new Department { Name = "Engineering", Location = "Building A - Floor 3" };
        var deptHr = new Department { Name = "Human Resources", Location = "Building B - Floor 1" };
        var deptFin = new Department { Name = "Finance", Location = "Building B - Floor 2" };
        var deptOps = new Department { Name = "Operations", Location = "Building A - Floor 1" };
        context.Departments.AddRange(deptEng, deptHr, deptFin, deptOps);

        // 6. Positions
        var posAdmin = new Position { Title = "System Administrator", Description = "IT Administration and Infrastructure" };
        var posHrLead = new Position { Title = "HR Lead", Description = "Lead HR Manager" };
        var posEngMgr = new Position { Title = "Engineering Manager", Description = "Leads software engineering teams" };
        var posSrDev = new Position { Title = "Senior Full Stack Engineer", Description = "Core product developer" };
        var posDev = new Position { Title = "Software Engineer", Description = "Application developer" };
        context.Positions.AddRange(posAdmin, posHrLead, posEngMgr, posSrDev, posDev);
        await context.SaveChangesAsync();

        // 7. Users & Employees
        // Admin
        var (adminHash, adminSalt) = hasher.HashPassword("Admin@123");
        var adminUser = new User
        {
            Email = "admin@company.com",
            PasswordHash = adminHash,
            PasswordSalt = adminSalt,
            CreatedAt = DateTime.UtcNow
        };
        context.Users.Add(adminUser);
        await context.SaveChangesAsync();
        context.UserRoles.Add(new UserRole { UserId = adminUser.Id, RoleId = adminRole.Id });

        var adminEmp = new Employee
        {
            EmployeeCode = "EMP-0001",
            FirstName = "Super",
            LastName = "Admin",
            Email = "admin@company.com",
            Phone = "+1-555-0101",
            DateOfBirth = new DateTime(1988, 1, 15, 0, 0, 0, DateTimeKind.Utc),
            Gender = "Male",
            DepartmentId = deptEng.Id,
            PositionId = posAdmin.Id,
            JoiningDate = new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EmploymentType = EmploymentType.Permanent,
            Status = EmployeeStatus.Active,
            UserId = adminUser.Id,
            CreatedAt = DateTime.UtcNow
        };
        context.Employees.Add(adminEmp);

        // HR User
        var (hrHash, hrSalt) = hasher.HashPassword("Hr@12345");
        var hrUser = new User
        {
            Email = "hr@company.com",
            PasswordHash = hrHash,
            PasswordSalt = hrSalt,
            CreatedAt = DateTime.UtcNow
        };
        context.Users.Add(hrUser);
        await context.SaveChangesAsync();
        context.UserRoles.Add(new UserRole { UserId = hrUser.Id, RoleId = hrRole.Id });

        var hrEmp = new Employee
        {
            EmployeeCode = "EMP-0002",
            FirstName = "Sarah",
            LastName = "Jenkins",
            Email = "hr@company.com",
            Phone = "+1-555-0102",
            DateOfBirth = new DateTime(1991, 5, 20, 0, 0, 0, DateTimeKind.Utc),
            Gender = "Female",
            DepartmentId = deptHr.Id,
            PositionId = posHrLead.Id,
            JoiningDate = new DateTime(2022, 3, 15, 0, 0, 0, DateTimeKind.Utc),
            EmploymentType = EmploymentType.Permanent,
            Status = EmployeeStatus.Active,
            UserId = hrUser.Id,
            CreatedAt = DateTime.UtcNow
        };
        context.Employees.Add(hrEmp);

        // Manager User
        var (mgrHash, mgrSalt) = hasher.HashPassword("Manager@123");
        var mgrUser = new User
        {
            Email = "manager@company.com",
            PasswordHash = mgrHash,
            PasswordSalt = mgrSalt,
            CreatedAt = DateTime.UtcNow
        };
        context.Users.Add(mgrUser);
        await context.SaveChangesAsync();
        context.UserRoles.Add(new UserRole { UserId = mgrUser.Id, RoleId = managerRole.Id });

        var mgrEmp = new Employee
        {
            EmployeeCode = "EMP-0003",
            FirstName = "David",
            LastName = "Miller",
            Email = "manager@company.com",
            Phone = "+1-555-0103",
            DateOfBirth = new DateTime(1986, 11, 10, 0, 0, 0, DateTimeKind.Utc),
            Gender = "Male",
            DepartmentId = deptEng.Id,
            PositionId = posEngMgr.Id,
            JoiningDate = new DateTime(2022, 2, 1, 0, 0, 0, DateTimeKind.Utc),
            EmploymentType = EmploymentType.Permanent,
            Status = EmployeeStatus.Active,
            UserId = mgrUser.Id,
            CreatedAt = DateTime.UtcNow
        };
        context.Employees.Add(mgrEmp);
        await context.SaveChangesAsync();

        deptEng.ManagerId = mgrEmp.Id;
        deptHr.ManagerId = hrEmp.Id;

        // Employee User
        var (empHash, empSalt) = hasher.HashPassword("Employee@123");
        var empUser = new User
        {
            Email = "employee@company.com",
            PasswordHash = empHash,
            PasswordSalt = empSalt,
            CreatedAt = DateTime.UtcNow
        };
        context.Users.Add(empUser);
        await context.SaveChangesAsync();
        context.UserRoles.Add(new UserRole { UserId = empUser.Id, RoleId = employeeRole.Id });

        var stdEmp = new Employee
        {
            EmployeeCode = "EMP-0004",
            FirstName = "Alex",
            LastName = "Rivera",
            Email = "employee@company.com",
            Phone = "+1-555-0104",
            DateOfBirth = new DateTime(1995, 8, 25, 0, 0, 0, DateTimeKind.Utc),
            Gender = "Male",
            DepartmentId = deptEng.Id,
            PositionId = posDev.Id,
            ManagerId = mgrEmp.Id,
            JoiningDate = new DateTime(2023, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            EmploymentType = EmploymentType.Permanent,
            Status = EmployeeStatus.Active,
            UserId = empUser.Id,
            CreatedAt = DateTime.UtcNow
        };
        context.Employees.Add(stdEmp);
        await context.SaveChangesAsync();

        // 8. Salary Structures
        context.SalaryStructures.AddRange(
            new SalaryStructure { EmployeeId = adminEmp.Id, BasicSalary = 9000, HRA = 3000, OtherAllowances = 1000, PFDeduction = 600, EffectiveFrom = DateTime.UtcNow.AddMonths(-6) },
            new SalaryStructure { EmployeeId = hrEmp.Id, BasicSalary = 6500, HRA = 2000, OtherAllowances = 800, PFDeduction = 450, EffectiveFrom = DateTime.UtcNow.AddMonths(-6) },
            new SalaryStructure { EmployeeId = mgrEmp.Id, BasicSalary = 8000, HRA = 2500, OtherAllowances = 900, PFDeduction = 550, EffectiveFrom = DateTime.UtcNow.AddMonths(-6) },
            new SalaryStructure { EmployeeId = stdEmp.Id, BasicSalary = 5000, HRA = 1500, OtherAllowances = 500, PFDeduction = 350, EffectiveFrom = DateTime.UtcNow.AddMonths(-6) }
        );

        // 9. Sample Projects
        var proj1 = new Project
        {
            Name = "Enterprise Cloud Portal",
            Description = "Next-generation customer self-service cloud infrastructure portal",
            StartDate = DateTime.UtcNow.AddMonths(-3),
            Status = "Active",
            ManagerId = mgrEmp.Id
        };
        var proj2 = new Project
        {
            Name = "Mobile HR & Payroll Sync",
            Description = "Automated cross-region payroll synchronization service",
            StartDate = DateTime.UtcNow.AddMonths(-1),
            Status = "Active",
            ManagerId = mgrEmp.Id
        };
        context.Projects.AddRange(proj1, proj2);
        await context.SaveChangesAsync();

        context.EmployeeProjects.AddRange(
            new EmployeeProject { EmployeeId = stdEmp.Id, ProjectId = proj1.Id, AllocatedFrom = DateTime.UtcNow.AddMonths(-3) },
            new EmployeeProject { EmployeeId = mgrEmp.Id, ProjectId = proj1.Id, AllocatedFrom = DateTime.UtcNow.AddMonths(-3) },
            new EmployeeProject { EmployeeId = stdEmp.Id, ProjectId = proj2.Id, AllocatedFrom = DateTime.UtcNow.AddMonths(-1) }
        );

        // 10. Sample Attendance
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        context.Attendances.AddRange(
            new Attendance { EmployeeId = stdEmp.Id, Date = today.AddDays(-1), CheckInTime = DateTime.UtcNow.AddDays(-1).Date.AddHours(9), CheckOutTime = DateTime.UtcNow.AddDays(-1).Date.AddHours(17).AddMinutes(30), Status = AttendanceStatus.Present },
            new Attendance { EmployeeId = mgrEmp.Id, Date = today.AddDays(-1), CheckInTime = DateTime.UtcNow.AddDays(-1).Date.AddHours(8).AddMinutes(50), CheckOutTime = DateTime.UtcNow.AddDays(-1).Date.AddHours(18), Status = AttendanceStatus.Present }
        );

        // 11. Sample Notifications
        context.Notifications.Add(new Notification
        {
            UserId = empUser.Id,
            Type = "SYSTEM",
            Title = "Welcome to EMS",
            Message = "Your employee profile has been configured successfully. Check your assigned projects and attendance records.",
            CreatedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync();
        logger.LogInformation("EMS Initial seed completed successfully.");
    }
}
