using Microsoft.AspNetCore.Mvc;
using Nutrion.Lib.Database;
using Nutrion.WebApi.Services;

namespace Nutrion.WebApi.Endpoints
{
    public static class MealPlansEndpoints
    {

        public static RouteGroupBuilder MapMealPlansEndpoints(this IEndpointRouteBuilder routes)
        {
            var group = routes.MapGroup("/nutrion").WithTags("nutrion");

            group.MapPost("/openai-mealplan", GenerateOpenAIMealPlanAsync);
            group.MapPost("/customise-openai-mealplan", CustomiseOpenAIMealPlanAsync);

            return group;
        }

        // ✅ Long handler logic in its own method
        private static async Task<IResult> GenerateOpenAIMealPlanAsync(MealPlanService service, [FromBody] MealPlanRequest request)
        {

            var template = await service.GetTemplateForStyleAsync(request.Style);
            if (template == null)
                return Results.BadRequest($"No template found for style: {request.Style}");

            var mealPlanRequest = new MealPlanRequest
            {
                Style = request.Style,
                MealsPerDay = request.MealsPerDay,
                NumPeople = request.NumPeople
            };

            try
            {
                var openaiResult = await service.AnalyzeAsync(mealPlanRequest, template);
                //logger.LogInformation($"Received Prompt: {openaiResult}", openaiResult);
                return Results.Ok(new { result = openaiResult });
            }
            catch (HttpRequestException ex)
            {
                return Results.BadRequest($"Failed to contact OpenAI backend: {ex.Message}");
            }

        }

        private static async Task<IResult> CustomiseOpenAIMealPlanAsync(MealPlanService service, [FromBody] CustomiseMealPlanRequest request)
        {

            // Load the 'customise' template from MealPlanTemplates.json
            RecipePromptTemplate template = await service.GetTemplateForStyleAsync("customise");
            if (template is null)
                return Results.BadRequest($"No template found for style: {request.Style}");

            var mealPlanRequest = new MealPlanRequest
            {
                Style = request.Style,
                MealsPerDay = request.MealsPerDay,
                NumPeople = request.NumPeople
            };

            try
            {
                var openaiResult = await service.CustomiseMealPlanAsync(
                    request.AdditionalFood,
                    request.RemoveFood,
                    request.OriginalMealPlanJson,
                    mealPlanRequest,
                    template
                );
                //logger.LogInformation($"Received Prompt: {openaiResult}", openaiResult);
                return Results.Ok(new { result = openaiResult });

            }
            catch (HttpRequestException ex)
            {
                return Results.BadRequest($"Failed to contact OpenAI backend: {ex.Message}");
            }

        }
    }

}


