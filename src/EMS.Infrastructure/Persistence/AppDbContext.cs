using EMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Position> Positions => Set<Position>();

    public DbSet<Attendance> Attendances => Set<Attendance>();
    public DbSet<LeaveType> LeaveTypes => Set<LeaveType>();
    public DbSet<Leave> Leaves => Set<Leave>();

    public DbSet<SalaryStructure> SalaryStructures => Set<SalaryStructure>();
    public DbSet<Payslip> Payslips => Set<Payslip>();

    public DbSet<Project> Projects => Set<Project>();
    public DbSet<EmployeeProject> EmployeeProjects => Set<EmployeeProject>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);
        builder.HasIndex(u => u.Email).IsUnique();
        builder.Property(u => u.Email).HasMaxLength(150).IsRequired();
        builder.Property(u => u.PasswordHash).IsRequired();
        builder.Property(u => u.PasswordSalt).IsRequired();
    }
}

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles");
        builder.HasKey(r => r.Id);
        builder.HasIndex(r => r.Name).IsUnique();
        builder.Property(r => r.Name).HasMaxLength(100).IsRequired();
    }
}

public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("Permissions");
        builder.HasKey(p => p.Id);
        builder.HasIndex(p => p.Code).IsUnique();
        builder.Property(p => p.Code).HasMaxLength(100).IsRequired();
    }
}

public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("UserRoles");
        builder.HasKey(ur => new { ur.UserId, ur.RoleId });
        builder.HasOne(ur => ur.User)
            .WithMany(u => u.UserRoles)
            .HasForeignKey(ur => ur.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(ur => ur.Role)
            .WithMany(r => r.UserRoles)
            .HasForeignKey(ur => ur.RoleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("RolePermissions");
        builder.HasKey(rp => new { rp.RoleId, rp.PermissionId });
        builder.HasOne(rp => rp.Role)
            .WithMany(r => r.RolePermissions)
            .HasForeignKey(rp => rp.RoleId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(rp => rp.Permission)
            .WithMany(p => p.RolePermissions)
            .HasForeignKey(rp => rp.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");
        builder.HasKey(rt => rt.Id);
        builder.HasIndex(rt => rt.TokenHash);
        builder.HasOne(rt => rt.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("Departments");
        builder.HasKey(d => d.Id);
        builder.HasIndex(d => d.Name).IsUnique();
        builder.Property(d => d.Name).HasMaxLength(100).IsRequired();
        builder.Property(d => d.Location).HasMaxLength(150);

        builder.HasOne(d => d.Manager)
            .WithMany()
            .HasForeignKey(d => d.ManagerId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class PositionConfiguration : IEntityTypeConfiguration<Position>
{
    public void Configure(EntityTypeBuilder<Position> builder)
    {
        builder.ToTable("Positions");
        builder.HasKey(p => p.Id);
        builder.HasIndex(p => p.Title).IsUnique();
        builder.Property(p => p.Title).HasMaxLength(100).IsRequired();
    }
}

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("Employees");
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.EmployeeCode).IsUnique().HasDatabaseName("ix_employee_code");
        builder.HasIndex(e => e.Email).IsUnique().HasDatabaseName("ix_employee_email");
        builder.HasIndex(e => e.DepartmentId).HasDatabaseName("ix_employee_department");
        builder.HasIndex(e => e.ManagerId).HasDatabaseName("ix_employee_manager");

        builder.Property(e => e.EmployeeCode).HasMaxLength(20).IsRequired();
        builder.Property(e => e.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(e => e.LastName).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Email).HasMaxLength(150).IsRequired();
        builder.Property(e => e.Phone).HasMaxLength(20).IsRequired();
        builder.Property(e => e.Gender).HasMaxLength(10);

        builder.HasOne(e => e.Department)
            .WithMany(d => d.Employees)
            .HasForeignKey(e => e.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Position)
            .WithMany(p => p.Employees)
            .HasForeignKey(e => e.PositionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Manager)
            .WithMany(m => m.DirectReports)
            .HasForeignKey(e => e.ManagerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.User)
            .WithOne(u => u.Employee)
            .HasForeignKey<Employee>(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class AttendanceConfiguration : IEntityTypeConfiguration<Attendance>
{
    public void Configure(EntityTypeBuilder<Attendance> builder)
    {
        builder.ToTable("Attendances");
        builder.HasKey(a => a.Id);
        builder.HasIndex(a => new { a.EmployeeId, a.Date }).IsUnique().HasDatabaseName("ix_attendance_emp_date");
        builder.HasIndex(a => a.Date).HasDatabaseName("ix_attendance_date");

        builder.HasOne(a => a.Employee)
            .WithMany(e => e.Attendances)
            .HasForeignKey(a => a.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class LeaveConfiguration : IEntityTypeConfiguration<Leave>
{
    public void Configure(EntityTypeBuilder<Leave> builder)
    {
        builder.ToTable("Leaves");
        builder.HasKey(l => l.Id);
        builder.HasIndex(l => l.EmployeeId).HasDatabaseName("ix_leave_employee");
        builder.HasIndex(l => l.Status).HasDatabaseName("ix_leave_status");

        builder.HasOne(l => l.Employee)
            .WithMany(e => e.Leaves)
            .HasForeignKey(l => l.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(l => l.LeaveType)
            .WithMany(lt => lt.Leaves)
            .HasForeignKey(l => l.LeaveTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.Approver)
            .WithMany()
            .HasForeignKey(l => l.ApprovedBy)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class LeaveTypeConfiguration : IEntityTypeConfiguration<LeaveType>
{
    public void Configure(EntityTypeBuilder<LeaveType> builder)
    {
        builder.ToTable("LeaveTypes");
        builder.HasKey(lt => lt.Id);
        builder.Property(lt => lt.Name).HasMaxLength(100).IsRequired();
    }
}

public class SalaryStructureConfiguration : IEntityTypeConfiguration<SalaryStructure>
{
    public void Configure(EntityTypeBuilder<SalaryStructure> builder)
    {
        builder.ToTable("SalaryStructures");
        builder.HasKey(s => s.Id);
        builder.HasIndex(s => s.EmployeeId).IsUnique();

        builder.HasOne(s => s.Employee)
            .WithOne(e => e.SalaryStructure)
            .HasForeignKey<SalaryStructure>(s => s.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class PayslipConfiguration : IEntityTypeConfiguration<Payslip>
{
    public void Configure(EntityTypeBuilder<Payslip> builder)
    {
        builder.ToTable("Payslips");
        builder.HasKey(p => p.Id);
        builder.HasIndex(p => new { p.EmployeeId, p.Month, p.Year }).IsUnique().HasDatabaseName("ix_payslip_emp_month_yr");

        builder.HasOne(p => p.Employee)
            .WithMany(e => e.Payslips)
            .HasForeignKey(p => p.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("Projects");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Name).HasMaxLength(150).IsRequired();

        builder.HasOne(p => p.Manager)
            .WithMany()
            .HasForeignKey(p => p.ManagerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class EmployeeProjectConfiguration : IEntityTypeConfiguration<EmployeeProject>
{
    public void Configure(EntityTypeBuilder<EmployeeProject> builder)
    {
        builder.ToTable("EmployeeProjects");
        builder.HasKey(ep => new { ep.EmployeeId, ep.ProjectId });

        builder.HasOne(ep => ep.Employee)
            .WithMany(e => e.EmployeeProjects)
            .HasForeignKey(ep => ep.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ep => ep.Project)
            .WithMany(p => p.EmployeeProjects)
            .HasForeignKey(ep => ep.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");
        builder.HasKey(a => a.Id);
        builder.HasIndex(a => a.UserId).HasDatabaseName("ix_auditlog_user");
        builder.HasIndex(a => a.Timestamp).HasDatabaseName("ix_auditlog_timestamp");
        builder.HasIndex(a => a.CorrelationId).HasDatabaseName("ix_auditlog_correlation");

        builder.HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");
        builder.HasKey(n => n.Id);
        builder.HasIndex(n => n.UserId);

        builder.HasOne(n => n.User)
            .WithMany()
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
