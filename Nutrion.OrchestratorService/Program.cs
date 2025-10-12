using Nutrion.OrchestratorService;
using Nutrion.Messaging;
using RabbitMQ.Client;

var builder = Host.CreateApplicationBuilder(args);


builder.Services.AddHttpClient();

//var rabbitMqConnection = builder.Configuration.GetConnectionString("RabbitMQ");

builder.Services.AddSingleton<IRabbitMqConnectionProvider, RabbitMqConnectionProvider>();
builder.Services.AddTransient<IMessageProducer, RabbitMqProducer>();
builder.Services.AddTransient<IMessageConsumer, RabbitMqConsumer>();


builder.Services.AddHostedService<Worker>();


var host = builder.Build();
host.Run();
