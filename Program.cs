using Microsoft.AspNetCore.SignalR;
using MongoDB.Driver;
using player_service_net_test4.Hubs;
using player_service_net_test4.Authentication;
using player_service_net_test4.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Add MongoDB
string mongoConnectionString = Environment.GetEnvironmentVariable("MONGO_CONNECTION_STRING") ?? "mongodb://localhost:27025";
MongoClient mongoClient = new MongoClient(mongoConnectionString);
builder.Services.AddSingleton(mongoClient);

builder.Services.AddTransient<IAuthenticationService, AuthenticationService>();
builder.Services.AddTransient<IW3CAuthenticationService, W3CAuthenticationService>();
builder.Services.AddTransient<IWebsiteBackendRepository, WebsiteBackendRepository>();
builder.Services.AddSingleton<ConnectionMapping>();

// Add SignalR for using websockets
builder.Services.AddSignalR();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Configure the HTTP request pipeline
// app.UseHttpsRedirection();
app.UseRouting();

app.UseCors(builder =>
    builder
        .AllowAnyHeader()
        .AllowAnyMethod()
        .SetIsOriginAllowed(_ => true)
        .AllowCredentials());

// app.UseAuthorization();
app.MapControllers();

// Add SignalR PlayerHub
app.MapHub<PlayerHub>("/playerHub");

app.Run();
