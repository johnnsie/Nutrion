var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.Nutrion_Orchestrator>("orchestrator");

builder.Build().Run();
