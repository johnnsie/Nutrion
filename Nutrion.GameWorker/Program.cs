using Microsoft.EntityFrameworkCore;
using Nutrion.GameServer.Worker;
using Nutrion.GameWorker.Database;
using Nutrion.GameWorker.Persistence;
using Nutrion.GameWorker.Services;
using Nutrion.Lib.Database;
using Nutrion.Messaging;
using System;

var builder = Host.CreateApplicationBuilder(args);

var postgresConnection = builder.Configuration.GetConnectionString("Postgres");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(postgresConnection));

builder.Services.AddScoped<ITileRepository, TileRepository>();
builder.Services.AddSingleton<TileStateService>();
builder.Services.AddHostedService<TileWorker>();

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
