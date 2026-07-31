namespace KhataFlow.Core.Exceptions;

public class NotFoundException : Exception
{
    public string? ErrorCode { get; }

    public NotFoundException(string message, string? errorCode = null) : base(message)
        => ErrorCode = errorCode;
}