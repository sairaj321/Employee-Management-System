namespace EMS.Domain.Enums;

public enum EmployeeStatus
{
    Active = 1,
    Inactive = 2,
    Terminated = 3
}

public enum LeaveStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3,
    Cancelled = 4
}

public enum AttendanceStatus
{
    Present = 1,
    Absent = 2,
    Late = 3,
    HalfDay = 4
}

public enum EmploymentType
{
    Permanent = 1,
    Contract = 2,
    Intern = 3
}
