using Microsoft.EntityFrameworkCore;
using Nutrion.Lib.Database;
using Nutrion.Lib.Database.Game.Hydration;
using Nutrion.Lib.Database.Game.Persistence;
using Nutrion.Lib.GameLogic.Engine;
using Nutrion.Lib.GameLogic.Systems;

var builder = Host.CreateApplicationBuilder(args);

var postgresConnection = builder.Configuration.GetConnectionString("Postgres");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(postgresConnection));

// Repository registrations
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

// game systems
builder.Services.AddScoped<ResourceSystem>();
builder.Services.AddScoped<PlayerSystem>();

builder.Services.AddHostedService<GameTickService>();


var host = builder.Build();
host.Run();
