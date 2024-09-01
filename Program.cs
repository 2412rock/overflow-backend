using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using OverflowBackend.Services;
using OverflowBackend.Services.Implementantion;
using OverflowBackend.Services.Interface;
using System.Collections.Concurrent;
using System.Net;
using System.Net.WebSockets;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseKestrel(options =>
{
    options.Listen(IPAddress.Any, 4200, listenOptions =>
    {
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
builder.Services.AddTransient<IFriendService, FriendService>();
builder.Services.AddScoped<IScoreService, ScoreService>();
builder.Services.AddSingleton<IMatchMakingService, MatchMakingService>();
builder.Services.AddSingleton<IConnectionManager, ConnectionManager>();
builder.Services.AddSignalR();


var saPassword = Environment.GetEnvironmentVariable("SA_PASSWORD");
var env = builder.Environment.EnvironmentName;
string hostIp = env == "Development" ? "192.168.1.134" : "192.168.1.159";

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
app.UseRouting();
//app.MapControllers();
app.UseWebSockets();
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
    endpoints.MapHub<GameHub>("/gameHub");  // Map the SignalR hub
});
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

public class GameHub : Hub
{
    // A dictionary to store connections by user ID
    IConnectionManager _connectionManager;
    public GameHub(IConnectionManager connectionManager)
    {
        _connectionManager = connectionManager;
    }

    public override Task OnConnectedAsync()
    {
        var username = Context.GetHttpContext().Request.Query["username"];
        _connectionManager.UserConnections[username] = Context.ConnectionId;
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception exception)
    {
        var username = Context.GetHttpContext().Request.Query["username"];
        _connectionManager.UserConnections.TryRemove(username, out _);
        return base.OnDisconnectedAsync(exception);
    }

    public async Task SendGameInvitation(string receiverUsername, string senderUsername)
    {
        if (_connectionManager.UserConnections.TryGetValue(receiverUsername, out var connectionId))
        {
            await Clients.Client(connectionId).SendAsync("ReceiveGameInvitation", senderUsername);
        }
    }

    public async Task AcceptGameInvitation(string receiverUsername, string senderUsername)
    {
        if (_connectionManager.UserConnections.TryGetValue(receiverUsername, out var connectionId))
        {
            await Clients.Client(connectionId).SendAsync("AcceptGameInvitation", senderUsername);
        }
    }

    public async Task DeclineGameInvitation(string receiverUsername, string senderUsername)
    {
        if (_connectionManager.UserConnections.TryGetValue(receiverUsername, out var connectionId))
        {
            await Clients.Client(connectionId).SendAsync("DeclineGameInvitation", senderUsername);
        }
    }
}