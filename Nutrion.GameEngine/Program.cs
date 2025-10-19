using Microsoft.EntityFrameworkCore;
using Nutrion.GameLib.Database;
using Nutrion.GameLib.Database.EntityRepository;
using Nutrion.Lib.Database;
using Nutrion.Lib.Database.Hydration;
using Nutrion.Lib.Database.Persistence;
using Nutrion.Lib.GameLogic.Engine;
using Nutrion.Lib.GameLogic.Systems;

var builder = Host.CreateApplicationBuilder(args);

var postgresConnection = builder.Configuration.GetConnectionString("Postgres");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

// 👇 This line is CRUCIAL
builder.Services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());


// Repository registrations
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

// game systems
builder.Services.AddScoped<ResourceSystem>();
builder.Services.AddScoped<PlayerSystem>();
builder.Services.AddScoped<EntityRepository>();

builder.Services.AddHostedService<GameTickService>();


var host = builder.Build();
host.Run();
