namespace DefaultNamespace;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    public IGenericRepository<Listing> Listings { get; }
    public IGenericRepository<Order> Orders { get; }
    public IGenericRepository<Review> Reviews { get; }

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
        Listings = new GenericRepository<Listing>(_context);
        Orders = new GenericRepository<Order>(_context);
        Reviews = new GenericRepository<Review>(_context);
    }

    public async Task<int> CompleteAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
