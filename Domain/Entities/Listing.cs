namespace DefaultNamespace;

public class Listing : BaseEntity
{
    public string Title { get; set; }
    public string Description { get; set; }
    public decimal Price { get; set; }
    public string ImageUrl { get; set; }
    public Guid SellerId { get; set; }
}
