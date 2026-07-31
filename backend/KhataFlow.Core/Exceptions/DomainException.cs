namespace KhataFlow.Core.Exceptions;

public class DomainException : Exception
{
    public bool IsResourceKey { get; }
    public string? ResourceKey { get; }
    public object?[] Args { get; }

    public DomainException(string message) : base(message)
    {
        IsResourceKey = false;
        ResourceKey = null;
        Args = Array.Empty<object?>();
    }

    private DomainException(string resourceKey, object?[] args, bool _)
        : base(resourceKey)
    {
        IsResourceKey = true;
        ResourceKey = resourceKey;
        Args = args;
    }

    public static DomainException ForResource(string resourceKey, params object?[] args) =>
        new(resourceKey, args, true);
}