using Microsoft.AspNetCore.SignalR;
using MongoDB.Driver;
using W3ChampionsPlayerService.Hubs;
using W3ChampionsPlayerService.Authentication;
using W3ChampionsPlayerService.Services;
using W3ChampionsPlayerService.Friends;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Add MongoDB
string mongoConnectionString = Environment.GetEnvironmentVariable("MONGO_CONNECTION_STRING") ?? "mongodb://localhost:27025";
MongoClient mongoClient = new MongoClient(mongoConnectionString);
builder.Services.AddSingleton(mongoClient);

builder.Services.AddTransient<AuthenticationService, AuthenticationService>();
builder.Services.AddTransient<W3CAuthenticationService, W3CAuthenticationService>();
builder.Services.AddTransient<WebsiteBackendService, WebsiteBackendService>();
builder.Services.AddSingleton<ConnectionMapping>();
builder.Services.AddTransient<FriendRepository>();
builder.Services.AddSingleton<FriendRequestCache, FriendRequestCache>();

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
