using Microsoft.EntityFrameworkCore;
using PiFunctions.Models;

namespace PiFunctions.Data;

public class PiCalculusDbContext : DbContext
{
    public PiCalculusDbContext(DbContextOptions<PiCalculusDbContext> options) 
        : base(options)
    {
    }

    public DbSet<SessionState> SessionStates { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Add an index to ClientId so your Node-RED lookups are blazing fast
        modelBuilder.Entity<SessionState>()
            .HasIndex(s => s.ClientId)
            .IsUnique();
    }
}