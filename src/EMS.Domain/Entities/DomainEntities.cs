using EMS.Domain.Enums;

namespace EMS.Domain.Entities;

public class Department
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? ManagerId { get; set; }
    public Employee? Manager { get; set; }
    public string? Location { get; set; }

    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}

public class Position
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}

public class Employee
{
    public int Id { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public int DepartmentId { get; set; }
    public Department Department { get; set; } = null!;
    public int PositionId { get; set; }
    public Position Position { get; set; } = null!;
    public int? ManagerId { get; set; }
    public Employee? Manager { get; set; }
    public DateTime JoiningDate { get; set; }
    public EmploymentType EmploymentType { get; set; }
    public EmployeeStatus Status { get; set; } = EmployeeStatus.Active;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<Employee> DirectReports { get; set; } = new List<Employee>();
    public SalaryStructure? SalaryStructure { get; set; }
    public ICollection<Payslip> Payslips { get; set; } = new List<Payslip>();
    public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
    public ICollection<Leave> Leaves { get; set; } = new List<Leave>();
    public ICollection<EmployeeProject> EmployeeProjects { get; set; } = new List<EmployeeProject>();
}

public class Attendance
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;
    public DateOnly Date { get; set; }
    public DateTime? CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public AttendanceStatus Status { get; set; }
}

public class LeaveType
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DefaultDaysPerYear { get; set; }

    public ICollection<Leave> Leaves { get; set; } = new List<Leave>();
}

public class Leave
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;
    public int LeaveTypeId { get; set; }
    public LeaveType LeaveType { get; set; } = null!;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string Reason { get; set; } = string.Empty;
    public LeaveStatus Status { get; set; } = LeaveStatus.Pending;
    public int? ApprovedBy { get; set; }
    public Employee? Approver { get; set; }
    public DateTime? ApprovedAt { get; set; }
}

public class SalaryStructure
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;
    public decimal BasicSalary { get; set; }
    public decimal HRA { get; set; }
    public decimal OtherAllowances { get; set; }
    public decimal PFDeduction { get; set; }
    public DateTime EffectiveFrom { get; set; }
}

public class Payslip
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;
    public int Month { get; set; }
    public int Year { get; set; }
    public decimal GrossSalary { get; set; }
    public decimal Deductions { get; set; }
    public decimal NetSalary { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

public class Project
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string Status { get; set; } = "Active";
    public int? ManagerId { get; set; }
    public Employee? Manager { get; set; }

    public ICollection<EmployeeProject> EmployeeProjects { get; set; } = new List<EmployeeProject>();
}

public class EmployeeProject
{
    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;
    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;
    public DateTime AllocatedFrom { get; set; }
    public DateTime? AllocatedTo { get; set; }
}

public class AuditLog
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public User? User { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string? OldValue { get; set; } // jsonb
    public string? NewValue { get; set; } // jsonb
    public string? IpAddress { get; set; }
    public string? CorrelationId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class Notification
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
