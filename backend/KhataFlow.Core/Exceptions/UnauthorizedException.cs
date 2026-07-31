namespace KhataFlow.Core.Exceptions;

public class UnauthorizedException : Exception
{
    public string? ErrorCode { get; }

    public UnauthorizedException(string message, string? errorCode = null) : base(message)
        => ErrorCode = errorCode;
}