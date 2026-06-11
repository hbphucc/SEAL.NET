namespace SEAL.NET.DTOs.Category
{
    /// <summary>Flat category read model (includes EventId) used by the categories endpoints.</summary>
    public class CategoryDetailDto
    {
        public Guid CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Guid EventId { get; set; }
    }
}
