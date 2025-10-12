using Nutrion.Orchestrator;
using Microsoft.Extensions.Logging;

Console.WriteLine("OTEL_EXPORTER_OTLP_ENDPOINT = " +
    Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT") ?? "null");


var builder = Host.CreateApplicationBuilder(args);

// 🔹 Add this to confirm logging works
builder.Logging.SetMinimumLevel(LogLevel.Information);
var logger = LoggerFactory.Create(lb =>
{
    lb.AddConsole();
    lb.SetMinimumLevel(LogLevel.Information);
}).CreateLogger("Program");

logger.LogInformation("✅ Worker Program.cs starting at {Time}", DateTimeOffset.Now);


builder.Services.AddHostedService<DebugWorker>();

var host = builder.Build();

var logddddger = host.Services.GetRequiredService<ILoggerFactory>()
    .CreateLogger("Program");

logddddger.LogInformation("✅ Host built successfully, starting run loop.");



host.Run();
