namespace KhataFlow.Core.Exceptions;

public class BusinessNotFoundException : Exception
{
    public string? ErrorCode { get; }

    public BusinessNotFoundException(string message, string? errorCode = null) : base(message)
        => ErrorCode = errorCode;
}