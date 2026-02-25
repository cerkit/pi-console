using Microsoft.EntityFrameworkCore;
using Pi.Shared.Models; // For your shared Pi Calculus models

var builder = WebApplication.CreateBuilder(args);

// Register PostgreSQL
builder.Services.AddDbContext<PiCalculusDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// --- YOUR "SERVERLESS" FUNCTIONS ---

// Function 1: Node-RED calls this to save a handshake state
app.MapPost("/api/state", async (SessionStatus state, PiCalculusDbContext db) =>
{
    db.SessionStates.Add(state);
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