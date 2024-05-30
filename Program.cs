using MongoDB.Driver;
using player_service_net_test4.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Add MongoDB
string mongoConnectionString = Environment.GetEnvironmentVariable("MONGO_CONNECTION_STRING") ?? "mongodb://localhost:27025";
MongoClient mongoClient = new MongoClient(mongoConnectionString);
builder.Services.AddSingleton(mongoClient);

// Add SignalR for using websockets
builder.Services.AddSignalR();

var app = builder.Build();

// Configure the HTTP request pipeline
// app.UseHttpsRedirection();
// app.UseAuthorization();
app.MapControllers();

// Add SignalR PlayerHub
app.MapHub<PlayerHub>("/playerHub");

app.Run();
