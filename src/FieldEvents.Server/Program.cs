using System.Text;
using FieldEvents.Server.Auth;
using FieldEvents.Server.Data;
using FieldEvents.Server.Hubs;
using FieldEvents.Server.Services;
using FieldEvents.Shared.Contracts;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddSignalR();
builder.Services.AddSingleton<ConnectionManager>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddSingleton<IPushNotificationChannel, WebPushNotificationChannel>();
builder.Services.AddSingleton<JwtTokenService>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };

        // Browsers cannot set custom headers on a WebSocket handshake, so SignalR's JS client
        // sends the token as a query string param instead - accept it here for the hub path only.
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken) && context.HttpContext.Request.Path.StartsWithSegments(HubRoutes.ClientsHubPath))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    })
    .AddScheme<AgentApiKeyOptions, AgentApiKeyAuthenticationHandler>(AgentApiKeyOptions.SchemeName, options =>
    {
        options.ExpectedKey = builder.Configuration["Agent:ApiKey"]!;
    });

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["http://localhost:4200"])
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<EventsHub>(HubRoutes.EventsHubPath);
app.MapHub<ClientsHub>(HubRoutes.ClientsHubPath);

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DbSeeder.SeedAsync(db);
}

app.Run();

public partial class Program; // exposed for WebApplicationFactory-based integration tests, if added later
