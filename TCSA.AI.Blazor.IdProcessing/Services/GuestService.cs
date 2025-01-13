using Microsoft.EntityFrameworkCore;
using TCSA.AI.Blazor.IdProcessing.Data;

public interface IGuestsService
{
    Task<List<Guest>> GetGuestsAsync();
    Task AddGuestAsync(Guest guest);
}

public class GuestsService : IGuestsService
{
    private readonly GuestsContext _context;

    public GuestsService(GuestsContext context)
    {
        _context = context;
    }

    public async Task<List<Guest>> GetGuestsAsync()
    {
        return await _context.Guests.ToListAsync();
    }

    public async Task AddGuestAsync(Guest guest)
    {
        _context.Guests.Add(guest);
        await _context.SaveChangesAsync();
    }
}
