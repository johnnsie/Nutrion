using Microsoft.EntityFrameworkCore;
using Nutrion.Lib.Database;
using Nutrion.Messaging;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace Nutrion.WebApi.Services
{
    public class MealPlanService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMessageProducer _producerService;
        private readonly ILogger<MealPlanService> _logger;
        private readonly AppDbContext _db;   // 👈 add this

        public MealPlanService(IHttpClientFactory httpClientFactory, IMessageProducer producerService, ILogger<MealPlanService> logger, AppDbContext db)
        {
            _httpClientFactory = httpClientFactory;
            _producerService = producerService;
            _logger = logger;
            _db = db;
        }

        public async Task<string> AnalyzeAsync(MealPlanRequest req, RecipePromptTemplate template)
        {

            //Console.WriteLine($"Analyzing meal plan request: {req.Style}, {req.MealsPerDay} meals/day, {req.NumPeople} people");

            using (var transaction = await _db.Database.BeginTransactionAsync())
            {
                try
                {
                    var prompt = (template.BaseTemplate ?? string.Empty)
                        .Replace("{style}", req.Style ?? "");

                    var openaiRequest = new
                    {
                        model = "gpt-3.5-turbo",
                        messages = new[] {
                            new { role = "system", content = "You are a helpful chef." },
                            new { role = "user", content = prompt }
                        }
                    };

                    OpenAIRequest dbRequest = new OpenAIRequest
                    {
                        Model = openaiRequest.model,
                        MessagesJson = JsonSerializer.Serialize(openaiRequest.messages),
                        CreatedAt = DateTime.UtcNow
                    };

                    _db.OutboxMessage.Add(new OutboxMessage
                    {
                        Topic = "openAI_request",
                        Payload = JsonSerializer.Serialize(openaiRequest.messages),
                        OccurredOn = DateTime.UtcNow
                    });
                    await _db.SaveChangesAsync();

                    // Save to DB
                    _db.OpenAIRequest.Add(dbRequest);
                    await _db.SaveChangesAsync();

                    // Publish to message queue
                    await _producerService.PublishAsync("openAI_request", openaiRequest);

                    // Commit transaction if everything succeeds
                    await transaction.CommitAsync();
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Failed to handle OpenAI request transactionally.");
                    throw;
                }
            }

            /* 
            // Debug: Output received parameters
            var debugParamPath = Path.Combine(AppContext.BaseDirectory, "Services", "openai_debug_params.json");
            var debugObj = new
            {
                MealPlanRequest = req,
                RecipePromptTemplate = template,
                Prompt = prompt
            };
            try
            {
                var debugJson = System.Text.Json.JsonSerializer.Serialize(debugObj, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                await System.IO.File.WriteAllTextAsync(debugParamPath, debugJson);
            }
            catch { 
            //ignore debug write errors 
            }
            */

            return null;
        }

        public async Task<string> CustomiseMealPlanAsync(
            List<string> additionalFood,
            List<string> removeFood,
            string? originalMealPlanJson,
            MealPlanRequest mealPlanRequest,
            RecipePromptTemplate template)
        {
            var prompt = (template.BaseTemplate ?? string.Empty)
                .Replace("{originalmealplan}", originalMealPlanJson ?? "")
                .Replace("{additionalfood}", additionalFood != null ? string.Join(", ", additionalFood) : string.Empty)
                .Replace("{removedfood}", removeFood != null ? string.Join(", ", removeFood) : string.Empty);

            // Debug: Output received parameters
            var debugParamPath = Path.Combine(AppContext.BaseDirectory, "Services", "openai_debug_customise_params.json");
            var debugObj = new
            {
                AdditionalFood = additionalFood,
                RemoveFood = removeFood,
                OriginalMealPlanJson = originalMealPlanJson,
                MealPlanRequest = mealPlanRequest,
                RecipePromptTemplate = template,
                Prompt = prompt
            };
            try
            {
                var debugJson = System.Text.Json.JsonSerializer.Serialize(debugObj, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                await System.IO.File.WriteAllTextAsync(debugParamPath, debugJson);
            }
            catch { /* ignore debug write errors */ }

            var openaiRequest = new
            {
                model = "gpt-3.5-turbo",
                messages = new[] {
                    new { role = "system", content = "You are a helpful chef." },
                    new { role = "user", content = prompt }
                }
            };

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "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");
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            return null;

            
        }

        public async Task<RecipePromptTemplate?> GetTemplateForStyleAsync(string style)
        {
            var templatePath = Path.Combine(AppContext.BaseDirectory, "Services", "MealPlanTemplates.json");
            if (!System.IO.File.Exists(templatePath))
                return null;

            var json = await System.IO.File.ReadAllTextAsync(templatePath);
            var doc = System.Text.Json.JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty(style, out var templateElement))
                return null;

            return new RecipePromptTemplate
            {
                Title = templateElement.TryGetProperty("title", out var titleProp) ? titleProp.GetString() : null,
                BaseTemplate = templateElement.TryGetProperty("template", out var templateProp) ? templateProp.GetString() : null
            };
        }
    }
}

