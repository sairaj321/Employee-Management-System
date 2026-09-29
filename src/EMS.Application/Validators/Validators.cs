using EMS.Application.DTOs;
using FluentValidation;

namespace EMS.Application.Validators;

public class LoginRequestValidator : AbstractValidator<LoginRequestDto>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.");
    }
}

public class ChangePasswordValidator : AbstractValidator<ChangePasswordDto>
{
    public ChangePasswordValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("Current password is required.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("New password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .Matches("[A-Z]").WithMessage("Password must contain at least 1 uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least 1 lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least 1 number.");
    }
}

public class CreateEmployeeValidator : AbstractValidator<CreateEmployeeDto>
{
    public CreateEmployeeValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(100).WithMessage("First name cannot exceed 100 characters.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(100).WithMessage("Last name cannot exceed 100 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(150).WithMessage("Email cannot exceed 150 characters.");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Phone number is required.")
            .MaximumLength(20).WithMessage("Phone number cannot exceed 20 characters.");

        RuleFor(x => x.DepartmentId)
            .GreaterThan(0).WithMessage("Valid Department is required.");

        RuleFor(x => x.PositionId)
            .GreaterThan(0).WithMessage("Valid Position is required.");

        RuleFor(x => x.DateOfBirth)
            .LessThan(DateTime.UtcNow.AddYears(-16)).WithMessage("Employee must be at least 16 years old.");

        RuleFor(x => x.JoiningDate)
            .NotEmpty().WithMessage("Joining date is required.");
    }
}

public class UpdateEmployeeValidator : AbstractValidator<UpdateEmployeeDto>
{
    public UpdateEmployeeValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(100);

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(100);

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Phone number is required.")
            .MaximumLength(20);

        RuleFor(x => x.DepartmentId)
            .GreaterThan(0).WithMessage("Valid Department is required.");

        RuleFor(x => x.PositionId)
            .GreaterThan(0).WithMessage("Valid Position is required.");
    }
}

public class ApplyLeaveValidator : AbstractValidator<ApplyLeaveDto>
{
    public ApplyLeaveValidator()
    {
        RuleFor(x => x.LeaveTypeId)
            .GreaterThan(0).WithMessage("Valid leave type is required.");

        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("Start date is required.");

        RuleFor(x => x.EndDate)
            .NotEmpty().WithMessage("End date is required.")
            .GreaterThanOrEqualTo(x => x.StartDate).WithMessage("End date must be on or after start date.");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Reason for leave is required.")
            .MaximumLength(500).WithMessage("Reason cannot exceed 500 characters.");
    }
}

public class UpdateSalaryStructureValidator : AbstractValidator<UpdateSalaryStructureDto>
{
    public UpdateSalaryStructureValidator()
    {
        RuleFor(x => x.BasicSalary)
            .GreaterThanOrEqualTo(0).WithMessage("Basic salary cannot be negative.");

        RuleFor(x => x.HRA)
            .GreaterThanOrEqualTo(0).WithMessage("HRA cannot be negative.");

        RuleFor(x => x.OtherAllowances)
            .GreaterThanOrEqualTo(0).WithMessage("Other allowances cannot be negative.");

        RuleFor(x => x.PFDeduction)
            .GreaterThanOrEqualTo(0).WithMessage("PF Deduction cannot be negative.");
    }
}

public class CreateDepartmentValidator : AbstractValidator<CreateDepartmentDto>
{
    public CreateDepartmentValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Department name is required.")
            .MaximumLength(100);
    }
}

public class CreatePositionValidator : AbstractValidator<CreatePositionDto>
{
    public CreatePositionValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Position title is required.")
            .MaximumLength(100);
    }
}

public class CreateProjectValidator : AbstractValidator<CreateProjectDto>
{
    public CreateProjectValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Project name is required.")
            .MaximumLength(150);

        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("Start date is required.");
    }
}
