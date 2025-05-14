namespace DefaultNamespace;

public class Review : BaseEntity
{
    public Guid ListingId { get; set; }
    public Guid UserId { get; set; }
    public int Rating { get; set; }
    public string Comment { get; set; }
}