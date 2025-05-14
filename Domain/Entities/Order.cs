namespace DefaultNamespace;

public class Order : BaseEntity
{
    public Guid UserId { get; set; }
    public List<Guid> ListingIds { get; set; }
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
}
