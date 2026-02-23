using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

// JWT (dev setup)
var jwtSecret = builder.Configuration["JWT_SECRET"] ?? "dev_only_change_me_please_32_chars_min";
var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key,
            ValidateLifetime = true
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthentication();
app.UseAuthorization();

// ---- AUTH ----
app.MapPost("/api/auth/login", (LoginRequest req) =>
{
    // TODO: replace with real user check
    if (req.Email != "test1@example.com" || req.Password != "Password123!")
        return Results.Unauthorized();

    var token = Jwt.MakeToken(jwtSecret);
    return Results.Ok(new { token });
});

// ---- TASKS ----
app.MapPost("/tasks", [Authorize] (CreateTaskRequest req) =>
{
    // TODO: replace with DB insert
    return Results.Created($"/tasks/{Guid.NewGuid()}", new
    {
        id = Guid.NewGuid(),
        text = req.Text,
        done = false
    });
});

app.Run();

record LoginRequest(string Email, string Password);
record CreateTaskRequest(string Text);

static class Jwt
{
    public static string MakeToken(string secret)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
            claims: [],
            expires: DateTime.UtcNow.AddHours(6),
            signingCredentials: creds
        );

        return new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);
    }
}