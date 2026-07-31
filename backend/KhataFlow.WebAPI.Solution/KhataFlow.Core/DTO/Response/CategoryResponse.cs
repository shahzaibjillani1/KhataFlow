using KhataFlow.Core.Domain.Entities;

namespace KhataFlow.Core.DTO.Response;

public record CategoryResponse(Guid Id, string CategoryName, string? CategoryNameUr);