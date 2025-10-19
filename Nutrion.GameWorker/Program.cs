using Microsoft.EntityFrameworkCore;
using Nutrion.GameLib.Database;
using Nutrion.GameLib.Database.EntityRepository;
using Nutrion.GameLib.Database.Init;
using Nutrion.GameServer.Worker;
using Nutrion.Lib.Database;
using Nutrion.Lib.Database.Hydration;
using Nutrion.Lib.Database.Persistence;
using Nutrion.Lib.GameLogic.Systems;
using Nutrion.Messaging;
using Nutrion.Worker.Tile;
using System;

var builder = Host.CreateApplicationBuilder(args);

var postgresConnection = builder.Configuration.GetConnectionString("Postgres");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

// Register interface mapping
builder.Services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());


builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<EntityRepository>();

// register game logic systems
builder.Services.AddScoped<PlayerSystem>();
builder.Services.AddScoped<TileSystem>();

builder.Services.AddHostedService<TileWorker>();
builder.Services.AddHostedService<PlayerWorker>();
//builder.Services.AddHostedService<BuildWorker>();

builder.Services.AddScoped<IDatabaseMigrator, DatabaseMigrator>();
builder.Services.AddHostedService<DatabaseMigrationHostedService>();


builder.Services.AddSingleton<IRabbitMqConnectionProvider, RabbitMqConnectionProvider>();
builder.Services.AddTransient<IMessageProducer, RabbitMqProducer>();
builder.Services.AddTransient<IMessageConsumer, RabbitMqConsumer>();

var host = builder.Build();

using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    for (int i = 0; i < 5; i++)
    {
        try
        {
            await db.Database.MigrateAsync();
            Console.WriteLine("? Database migration succeeded");

            break;
        }
        catch (EndOfStreamException)
        {
            Console.WriteLine("Postgres not ready yet, retrying...");
            await Task.Delay(10000);
        }
    }
}

host.Run();
