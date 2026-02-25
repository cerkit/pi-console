using Microsoft.EntityFrameworkCore;
using Pi.Shared.Models; // For your shared Pi Calculus models
using PiFunctions.Data;
using PiFunctions.Models;

var builder = WebApplication.CreateBuilder(args);

// Register PostgreSQL
builder.Services.AddDbContext<PiCalculusDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// --- YOUR "SERVERLESS" FUNCTIONS ---

// Function 1: Node-RED calls this to save a handshake state
app.MapPost("/api/state", async (SessionState state, PiCalculusDbContext db) =>
{
    // 1. Check if this client already has a session in the DB
    var existingSession = await db.SessionStates
        .FirstOrDefaultAsync(s => s.ClientId == state.ClientId);

    if (existingSession is not null)
    {
        // 2. UPDATE: The client exists, just update their active properties
        existingSession.Status = state.Status;
        existingSession.ActiveChannel = state.ActiveChannel;
        existingSession.CurrentUiState = state.CurrentUiState;
        existingSession.LastUpdatedAt = DateTimeOffset.UtcNow;
    }
    else
    {
        // 3. INSERT: Brand new client
        db.SessionStates.Add(state);
    }

    // 4. Save changes (EF Core handles whether it's an UPDATE or INSERT)
    await db.SaveChangesAsync();
    
    return Results.Ok(state);
});

// Function 2: Node-RED calls this to retrieve the latest state
app.MapGet("/api/state/{clientId}", async (string clientId, PiCalculusDbContext db) =>
{
    var state = await db.SessionStates.FirstOrDefaultAsync(s => s.ClientId == clientId);
    return state is not null ? Results.Ok(state) : Results.NotFound();
});

app.Run();