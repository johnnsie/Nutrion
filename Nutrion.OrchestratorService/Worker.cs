using Nutrion.Lib.Database;
using Nutrion.Messaging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;

namespace Nutrion.OrchestratorService;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMessageProducer _producer;
    private readonly IMessageConsumer _consumer;

    public Worker(
        ILogger<Worker> logger,
        IHttpClientFactory httpClientFactory,
        IMessageProducer producerService,
        IMessageConsumer consumerService
        )
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _producer = producerService;
         _consumer = consumerService;

        _logger.LogInformation("Worker constructed");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("ExecuteAsync started");
            await _consumer.StartConsumingAsync("openAI_request", HandleMessageAsync, stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ExecuteAsync crashed");
            throw;
        }

    }


    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await _consumer.DisposeAsync();
        await base.StopAsync(cancellationToken);
    }

    private async Task HandleMessageAsync(string routingKey, ReadOnlyMemory<byte> body, CancellationToken ct)
    {
        try
        {
            var json = System.Text.Encoding.UTF8.GetString(body.Span);
            _logger.LogInformation("Processing message: {Message}", json);

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", "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");

            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Await the HTTP POST call
            using var response = await client.PostAsync(
                "https://api.openai.com/v1/chat/completions",
                content
            );

            if (response.IsSuccessStatusCode)
            {
                var raw = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("OpenAI response OK, length {Length}", raw.Length);

                var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff");
                var debugPath = Path.Combine(AppContext.BaseDirectory, $"openai_debug_result_{timestamp}.json");

                await File.WriteAllTextAsync(debugPath, raw);

                OpenAIRequest openRequest = new OpenAIRequest
                {
                    Status = MessageStatus.Processed,
                    ReplyMessage = raw,
                    MessagesJson = JsonSerializer.Serialize(json)
                };

                await _producer.PublishAsync("openAI_reply", openRequest);
            }
            else
            {
                _logger.LogWarning("OpenAI API failed: {Status}", response.StatusCode);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Message processing was canceled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message");
        }
    }

}
