namespace DefaultNamespace;

public class User : BaseEntity
{
    public string Name { get; set; }
    public string Email { get; set; }
    public ICollection<Listing> Listings { get; set; }
}
