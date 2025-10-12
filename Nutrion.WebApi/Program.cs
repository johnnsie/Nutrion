using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Nutrion.Lib.Database;
using Nutrion.Messaging;
using Nutrion.WebApi.Endpoints;
using Nutrion.WebApi.Services;
using RabbitMQ.Client;

var builder = WebApplication.CreateBuilder(args);


//var rabbitMqConnection = builder.Configuration.GetConnectionString("RabbitMQ");

builder.Services.AddSingleton<IRabbitMqConnectionProvider, RabbitMqConnectionProvider>();
builder.Services.AddTransient<IMessageProducer, RabbitMqProducer>();

var postgresConnection = builder.Configuration.GetConnectionString("Postgres");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(postgresConnection));

// ? Register services
builder.Services.AddHttpClient();
builder.Services.AddScoped<MealPlanService>();

// Register the producer
builder.Services.AddSingleton<IMessageProducer, RabbitMqProducer>();

builder.Services.AddEndpointsApiExplorer();



var app = builder.Build();


using (var scope = app.Services.CreateScope())
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


// ? Grouped endpoints
//var userGroup = app.MapGroup("/mealPlans").WithTags("MealPlans");

// NEW MAPPING ENDPOINTS
app.MapMealPlansEndpoints();


app.Run();
if (app.Environment.IsDevelopment())
{
    //app.MapOpenApi();
}

app.UseHttpsRedirection();

