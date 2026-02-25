using Microsoft.EntityFrameworkCore;

namespace PiFunctions.Data;

public class PiCalculusDbContext : DbContext
{
    public PiCalculusDbContext(DbContextOptions<PiCalculusDbContext> options) 
        : base(options)
    {
    }

    // TODO: Add your DbSets here as you build out your persistent models.
    // For example, logging session handshakes or saving UI states:
    // public DbSet<SessionState> SessionStates { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // PostgreSQL specific configurations can go here.
        // For example, if you want to use Postgres' native JSONB columns for your payloads:
        // modelBuilder.Entity<SessionState>()
        //     .Property(b => b.Payload)
        //     .HasColumnType("jsonb");
    }
}