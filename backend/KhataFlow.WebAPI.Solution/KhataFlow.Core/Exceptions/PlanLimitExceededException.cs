namespace KhataFlow.Core.Exceptions;

public class PlanLimitExceededException : DomainException
{
    public string LimitType { get; }

    public PlanLimitExceededException(string message, string limitType) : base(message)
        => LimitType = limitType;
}