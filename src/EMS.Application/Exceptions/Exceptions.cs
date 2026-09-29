namespace EMS.Application.Exceptions;

public class ValidationException : Exception
{
    public IDictionary<string, string[]> Errors { get; }

    public ValidationException(IDictionary<string, string[]> errors) 
        : base("One or more validation failures have occurred.")
    {
        Errors = errors;
    }

    public ValidationException(string field, string message) 
        : base("Validation failed.")
    {
        Errors = new Dictionary<string, string[]>
        {
            { field, new[] { message } }
        };
    }
}

public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }

    public NotFoundException(string name, object key) 
        : base($"Entity \"{name}\" ({key}) was not found.")
    {
    }
}

public class ConflictException : Exception
{
    public ConflictException(string message) : base(message)
    {
    }
}

public class ForbiddenException : Exception
{
    public ForbiddenException(string message = "You do not have permission to access this resource.") : base(message)
    {
    }
}

public class UnauthorizedException : Exception
{
    public UnauthorizedException(string message = "Authentication failed.") : base(message)
    {
    }
}
