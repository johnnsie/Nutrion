using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Nutrion.Lib.Database;
using Nutrion.Messaging;
using Nutrion.WebApi.Endpoints;
using Nutrion.WebApi.Services;
using RabbitMQ.Client;
using System.ComponentModel.DataAnnotations;


public class OpenAIRequest
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public MessageStatus Status { get; set; } = MessageStatus.Init;

    public string Model { get; set; } = string.Empty;

    // JSON blob of the messages array
    public string MessagesJson { get; set; } = string.Empty;

    public string ReplyMessage { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

}


public enum MessageStatus
{
    Init,
    InProgress,
    Processed,
    Failed

}
public class MealPlanRequest
{
    public string Style { get; set; } = string.Empty;
    public int MealsPerDay { get; set; }
    public int NumPeople { get; set; }
}

public class CustomiseMealPlanRequest : MealPlanRequest
{
    public List<string> AdditionalFood { get; set; } = new();
    public List<string> RemoveFood { get; set; } = new();
    public string? OriginalMealPlanJson { get; set; } // Holds the initial full meal plan JSON
}

public class RecipePromptTemplate
{
    public string? Title { get; set; }
    public string? BaseTemplate { get; set; }
}



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

