using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using OverflowBackend.Services;
using OverflowBackend.Services.Implementantion;
using OverflowBackend.Services.Interface;
using System.Net;
using System.Net.WebSockets;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseKestrel(options =>
{
    options.Listen(IPAddress.Any, 4500 /*listenOptions => //IPAddress.Parse("192.168.1.125")
    {

            // local
            listenOptions.UseHttps("C:/Users/Adi/Desktop/dev certs overflow/certificate.pfx", "password");
        

    }*/);
    options.Listen(IPAddress.Any, 4200, listenOptions =>
    {
        /*try
        {
            // docker
            listenOptions.UseHttps("/app/backendcertificate.pfx"); //Environment.GetEnvironmentVariable("PFX_PASS")
        }
        catch
        {
            // local
            listenOptions.UseHttps("C:/Users/Adi/Desktop/certs/backendcertificate.pfx");
        }*/

    });
});

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalHost",
        builder =>
        {
            builder.WithOrigins("http://localhost")
                   .AllowAnyHeader()
                   .AllowAnyMethod().AllowCredentials();
        });
    options.AddPolicy("AllowAnyOrigin",
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyHeader()
                   .AllowAnyMethod();
        });

    options.AddPolicy("AllowOVerflowOrigin",
        builder =>
        {
            builder.WithOrigins("https://overflowapp.xyz")
                   .AllowAnyHeader()
                   .AllowAnyMethod().AllowCredentials();
        });

});

builder.Services.AddTransient<IAuthService, AuthService>();
builder.Services.AddTransient<IPasswordHashService, PasswordHashService>();
builder.Services.AddTransient<IRedisService, RedisService>();
builder.Services.AddScoped<IScoreService, ScoreService>();
builder.Services.AddSingleton<IMatchMakingService, MatchMakingService>();

var saPassword = Environment.GetEnvironmentVariable("SA_PASSWORD");
var env = builder.Environment.EnvironmentName;
string hostIp = env == "Development" ? "192.168.1.125" : "192.168.1.159";

Console.WriteLine($"Connecting to DB IP {hostIp}");
builder.Services.AddDbContext<OverflowDbContext>(options =>
    options.UseSqlServer($"Server={hostIp},1433;Database=OverflowDB;User Id=sa;Password={saPassword};TrustServerCertificate=True"));
var app = builder.Build();

app.UseCors("AllowAnyOrigin");
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
app.UseWebSockets();
app.Use(async (context, next) =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
        using (var scope = app.Services.CreateScope())
        {
            var scoreService = scope.ServiceProvider.GetRequiredService<IScoreService>();
            
            await WebSocketHandler.HandleWebSocketRequest(webSocket, context, scoreService);
        }
    }
    else
    {
        await next();
    }
});

app.Run();
