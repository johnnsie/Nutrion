using MessagePack;
using MessagePack.Resolvers;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Nutrion.GameLib.Database;
using Nutrion.GameServer;
using Nutrion.GameServer.RabbitMQ;
using Nutrion.GameServer.SignalR;
using Nutrion.Lib.Database;
using Nutrion.Lib.Database.Hydration;
using Nutrion.Messaging;
using System.Collections.Concurrent;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.SetMinimumLevel(LogLevel.Information);

builder.Services.AddSingleton<IRabbitMqConnectionProvider, RabbitMqConnectionProvider>();
builder.Services.AddTransient<IMessageProducer, RabbitMqProducer>();
builder.Services.AddTransient<IMessageConsumer, RabbitMqConsumer>();

// Add SignalR and enable MessagePack
builder.Services.AddSignalR(options => options.EnableDetailedErrors = true)
    .AddJsonProtocol(options =>
    {
        options.PayloadSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

builder.Services.AddScoped<GameHubNotifier>();

/*
    .AddMessagePackProtocol(options =>
    {
        options.SerializerOptions = MessagePackSerializerOptions.Standard
           // .WithSecurity(MessagePackSecurity.UntrustedData)
            .WithResolver(ContractlessStandardResolver.Instance)
            .WithCompression(MessagePackCompression.Lz4BlockArray);
.
    });
*/

// ✅ register the allocator so DI can resolve it
builder.Services.AddHostedService<GameEventConsumer>();
builder.Services.AddHostedService<SignalRShutdownService>();

builder.Logging.AddConsole().SetMinimumLevel(LogLevel.Debug);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

// Map the interface abstraction to the EF context
builder.Services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());


// builder.Services.AddScoped<ITileReadRepository, TileReadRepository>();
builder.Services.AddScoped(typeof(IReadRepository<>), typeof(ReadRepository<>)); // ✅ Read repo

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .SetIsOriginAllowed(_ => true); // allow all origins
    });

});
var app = builder.Build();

app.MapGet("/", () => "SignalR server running...");

// --- GameHub definition ---
app.MapHub<GameHub>("/hub/game");

// Enable CORS for local testing
app.UseCors(b =>
    b.AllowAnyHeader()
     .AllowAnyMethod()
     .AllowCredentials()
     .SetIsOriginAllowed(_ => true));

app.Run();

