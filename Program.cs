using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using OverflowBackend.Services;
using OverflowBackend.Services.Implementantion;
using OverflowBackend.Services.Interface;
using System.Net;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseKestrel(options =>
{
    options.Listen(IPAddress.Any, 4500);
    options.Listen(IPAddress.Any, 4200, listenOptions =>
    {
        try
        {
            // docker
            listenOptions.UseHttps("/app/backendcertificate.pfx"); //Environment.GetEnvironmentVariable("PFX_PASS")
        }
        catch
        {
            // local
            listenOptions.UseHttps("C:/Users/Adi/Desktop/certs/backendcertificate.pfx");
        }

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

    options.AddPolicy("AllowDorelOrigin",
        builder =>
        {
            builder.WithOrigins("https://dorelapp.xyz")
                   .AllowAnyHeader()
                   .AllowAnyMethod().AllowCredentials();
        });

});

builder.Services.AddTransient<IAuthService, AuthService>();
var saPassword = Environment.GetEnvironmentVariable("SA_PASSWORD");
string hostIp = "";
try
{
    IPAddress[] addresses = Dns.GetHostAddresses("host.docker.internal");
    if (addresses.Length > 0)
    {
        // we are running in docker
        hostIp = addresses[0].ToString();
    }
}
catch
{

    if (String.IsNullOrEmpty(hostIp))
    {
        hostIp = "10.132.0.2";
    }
}

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

app.Run();
