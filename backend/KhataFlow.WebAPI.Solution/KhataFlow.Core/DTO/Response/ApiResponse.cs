namespace KhataFlow.Core.DTO.Response;

public class ApiResponse<T>
{
    public string Message { get; set; } = string.Empty;
    public bool Result { get; set; }
    public T Data { get; set; } = default!;
}
