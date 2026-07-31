namespace KhataFlow.Core.Exceptions;

public class AIServiceUnavailableException : Exception
{
    public string? ErrorCode { get; }

    public AIServiceUnavailableException(string message, string? errorCode = null) : base(message)
        => ErrorCode = errorCode;
}