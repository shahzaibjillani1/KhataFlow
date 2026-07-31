namespace KhataFlow.Core.DTO.Request;

public record PaginationRequest(int PageNumber = 1, int PageSize = 20);