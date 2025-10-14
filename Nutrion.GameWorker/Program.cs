using Microsoft.EntityFrameworkCore;
using Nutrion.GameServer.Worker;
using Nutrion.GameWorker.Database;
using Nutrion.GameWorker.Services;
using Nutrion.Lib.Database;
using Nutrion.Lib.Database.Game.Entities;
using Nutrion.Lib.Database.Game.Persistence;
using Nutrion.Messaging;
using System;

var builder = Host.CreateApplicationBuilder(args);

var postgresConnection = builder.Configuration.GetConnectionString("Postgres");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(postgresConnection));

builder.Services.AddScoped<IRepository<Tile>, Repository<Tile>>();
builder.Services.AddScoped<IRepository<Player>, Repository<Player>>();
builder.Services.AddScoped<EntityRepository>(); 

builder.Services.AddSingleton<TileStateService>();
builder.Services.AddHostedService<TileWorker>();
builder.Services.AddHostedService<PlayerWorker>();


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
