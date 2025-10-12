using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Linq;
using Nutrion.Lib.Database;

var style = "breakfast";
var mealsPerDay = 1;
var numPeople = 1;

var request = new MealPlanRequest
{
    Style = style,
    MealsPerDay = mealsPerDay,
    NumPeople = numPeople
};

using var handler = new HttpClientHandler { AllowAutoRedirect = false };
using var client = new HttpClient(handler);
//client.BaseAddress = new Uri("http://localhost:5041/");
client.BaseAddress = new Uri("https://localhost:7215/");

async Task CallOpenAIMealPlan(HttpClient client, MealPlanRequest request)
{
    var response = await client.PostAsJsonAsync("/nutrion/openai-mealplan", request);
    if (response.IsSuccessStatusCode)
    {
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var result = doc.RootElement.GetProperty("result").GetString();
        Console.WriteLine("API Result (raw):");
        Console.WriteLine(result);
        try { await File.WriteAllTextAsync("ApiResultRaw.json", result ?? ""); }
        catch (Exception ex) { Console.WriteLine($"Failed to write ApiResultRaw.json: {ex.Message}"); }
    }
    else
    {
        Console.WriteLine($"API call failed: {response.StatusCode}");
    }
}

// Main loop
while (true)
{
    Console.WriteLine("Select API to call:");
    Console.WriteLine("1. OpenAI Meal Plan");
    Console.WriteLine("0. Exit");
    Console.Write("Enter choice: ");
    var choice = Console.ReadLine();
    if (choice == "1")
    {
        await CallOpenAIMealPlan(client, request);
    }
    else if (choice == "0")
    {
        break;
    }
    else
    {
        Console.WriteLine("Invalid choice. Try again.");
    }
    Console.WriteLine();
}

Console.WriteLine("Press Enter to exit...");
Console.ReadLine();
