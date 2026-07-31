using KhataFlow.Core.Domain.Common;

namespace KhataFlow.Core.Domain.Entities;

public class Category : BaseEntity
{
    public string CategoryName { get; set; } = string.Empty;
    public string? CategoryNameUr { get; set; } 
    public string? Description { get; set; }
    public string? DescriptionUr { get; set; } 

    public Guid BusinessId { get; set; }
    public Business Business { get; set; } = null!;

    public ICollection<Product> Products { get; set; } = new List<Product>();
}