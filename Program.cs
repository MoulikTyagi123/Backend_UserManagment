using Microsoft.EntityFrameworkCore;
using UserManagementAPI.Data;
using UserManagementAPI.Middleware;
using UserManagementAPI.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure middleware pipeline (order matters!)
// Error handling middleware should be first
app.UseMiddleware<ErrorHandlingMiddleware>();

// Authentication middleware
app.UseMiddleware<AuthenticationMiddleware>();

// Logging middleware
app.UseMiddleware<LoggingMiddleware>();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");

// Root endpoint - serve a friendly HTML status page so browsers render it instead of showing raw JSON
app.MapGet("/", async (HttpContext ctx) =>
{
    var html = @"<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>User Management API</title>
    <style>
        * { margin: 0; padding: 0; box-sizing: border-box; }
        body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Arial, sans-serif; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); min-height: 100vh; display: flex; align-items: center; justify-content: center; }
        .container { background: white; padding: 3rem; border-radius: 10px; box-shadow: 0 20px 60px rgba(0,0,0,0.3); max-width: 600px; text-align: center; }
        h1 { color: #333; margin-bottom: 0.5rem; font-size: 2.5rem; }
        .badge { display: inline-block; background: #28a745; color: white; padding: 0.5rem 1rem; border-radius: 20px; font-size: 0.9rem; margin: 1rem 0; }
        .version { color: #666; font-size: 0.95rem; margin-bottom: 1.5rem; }
        .endpoints { text-align: left; background: #f8f9fa; padding: 2rem; border-radius: 8px; margin: 2rem 0; }
        .endpoints h2 { color: #333; margin-bottom: 1rem; font-size: 1.2rem; }
        .endpoints ul { list-style: none; }
        .endpoints li { margin: 0.8rem 0; }
        .endpoints a { color: #667eea; text-decoration: none; font-weight: 500; padding: 0.5rem 1rem; display: inline-block; background: white; border-radius: 5px; transition: all 0.3s; }
        .endpoints a:hover { background: #667eea; color: white; transform: translateX(5px); }
        .footer { color: #999; font-size: 0.9rem; margin-top: 2rem; }
    </style>
</head>
<body>
    <div class='container'>
        <h1>🚀 User Management API</h1>
        <div class='version'>Version 1.0</div>
        <div class='badge'>✓ Running</div>
        
        <div class='endpoints'>
            <h2>Available Endpoints</h2>
            <ul>
                <li><a href='/swagger'>📚 Swagger UI</a> - Interactive API documentation</li>
                <li><a href='/api/users'>👥 /api/users</a> - Get all users (GET)</li>
                <li><a href='/api/users/1'>👤 /api/users/{id}</a> - Get user by ID (GET)</li>
            </ul>
        </div>
        
        <p class='footer'>Built with ASP.NET Core 8.0 & Entity Framework Core</p>
    </div>
</body>
</html>";

    ctx.Response.ContentType = "text/html; charset=utf-8";
    ctx.Response.ContentLength = System.Text.Encoding.UTF8.GetByteCount(html);
    await ctx.Response.WriteAsync(html);
})
.WithName("GetRoot")
.WithOpenApi();

// Serve a tiny inline SVG as favicon so browsers don't repeatedly request and log 404
app.MapGet("/favicon.ico", async (HttpContext ctx) =>
{
    var svg = "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"64\" height=\"64\" viewBox=\"0 0 64 64\"><rect width=\"100%\" height=\"100%\" fill=\"#0366d6\"/><text x=\"50%\" y=\"55%\" font-size=\"36\" font-family=\"Segoe UI,Arial\" fill=\"#fff\" text-anchor=\"middle\">U</text></svg>";

    ctx.Response.ContentType = "image/svg+xml";
    await ctx.Response.WriteAsync(svg);
})
.WithName("Favicon");

// CRUD Endpoints for Users
// GET: Get all users
app.MapGet("/api/users", async (ApplicationDbContext db) =>
{
    var users = await db.Users.ToListAsync();
    return Results.Ok(users);
})
.WithName("GetAllUsers")
.WithOpenApi();

// GET: Get user by ID
app.MapGet("/api/users/{id}", async (int id, ApplicationDbContext db) =>
{
    var user = await db.Users.FindAsync(id);
    if (user == null)
    {
        return Results.NotFound(new { error = $"User with ID {id} not found." });
    }
    return Results.Ok(user);
})
.WithName("GetUserById")
.WithOpenApi();

// POST: Create a new user
app.MapPost("/api/users", async (User user, ApplicationDbContext db) =>
{
    // Validate user input
    if (string.IsNullOrWhiteSpace(user.FirstName) || string.IsNullOrWhiteSpace(user.LastName))
    {
        return Results.BadRequest(new { error = "FirstName and LastName are required." });
    }

    if (string.IsNullOrWhiteSpace(user.Email))
    {
        return Results.BadRequest(new { error = "Email is required." });
    }

    // Check if email is valid format
    if (!IsValidEmail(user.Email))
    {
        return Results.BadRequest(new { error = "Email format is invalid." });
    }

    // Check for duplicate email
    var existingUser = await db.Users.FirstOrDefaultAsync(u => u.Email == user.Email);
    if (existingUser != null)
    {
        return Results.BadRequest(new { error = "Email is already in use." });
    }

    user.CreatedAt = DateTime.UtcNow;
    db.Users.Add(user);
    await db.SaveChangesAsync();

    return Results.Created($"/api/users/{user.Id}", user);
})
.WithName("CreateUser")
.WithOpenApi();

// PUT: Update an existing user
app.MapPut("/api/users/{id}", async (int id, User updatedUser, ApplicationDbContext db) =>
{
    var user = await db.Users.FindAsync(id);
    if (user == null)
    {
        return Results.NotFound(new { error = $"User with ID {id} not found." });
    }

    // Validate input
    if (string.IsNullOrWhiteSpace(updatedUser.FirstName) || string.IsNullOrWhiteSpace(updatedUser.LastName))
    {
        return Results.BadRequest(new { error = "FirstName and LastName are required." });
    }

    if (string.IsNullOrWhiteSpace(updatedUser.Email) || !IsValidEmail(updatedUser.Email))
    {
        return Results.BadRequest(new { error = "Email is required and must be valid." });
    }

    // Check for duplicate email (excluding current user)
    var emailExists = await db.Users.AnyAsync(u => u.Email == updatedUser.Email && u.Id != id);
    if (emailExists)
    {
        return Results.BadRequest(new { error = "Email is already in use." });
    }

    // Update user properties
    user.FirstName = updatedUser.FirstName;
    user.LastName = updatedUser.LastName;
    user.Email = updatedUser.Email;
    user.PhoneNumber = updatedUser.PhoneNumber;
    user.Department = updatedUser.Department;
    user.UpdatedAt = DateTime.UtcNow;

    db.Users.Update(user);
    await db.SaveChangesAsync();

    return Results.Ok(user);
})
.WithName("UpdateUser")
.WithOpenApi();

// DELETE: Remove a user
app.MapDelete("/api/users/{id}", async (int id, ApplicationDbContext db) =>
{
    var user = await db.Users.FindAsync(id);
    if (user == null)
    {
        return Results.NotFound(new { error = $"User with ID {id} not found." });
    }

    db.Users.Remove(user);
    await db.SaveChangesAsync();

    return Results.Ok(new { message = "User deleted successfully." });
})
.WithName("DeleteUser")
.WithOpenApi();

app.Run();

// Helper function to validate email
static bool IsValidEmail(string email)
{
    try
    {
        var addr = new System.Net.Mail.MailAddress(email);
        return addr.Address == email;
    }
    catch
    {
        return false;
    }
}
