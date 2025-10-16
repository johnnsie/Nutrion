using Microsoft.EntityFrameworkCore;
using Nutrion.GameServer.Worker;
using Nutrion.GameWorker.Database;
using Nutrion.Lib.Database;
using Nutrion.Lib.Database.Game.Entities;
using Nutrion.Lib.Database.Game.Persistence;
using Nutrion.Lib.GameLogic.Systems;
using Nutrion.Messaging;
using System;

var builder = Host.CreateApplicationBuilder(args);

var postgresConnection = builder.Configuration.GetConnectionString("Postgres");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(postgresConnection));

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
