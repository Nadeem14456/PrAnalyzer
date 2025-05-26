using Projects;

var builder = DistributedApplication.CreateBuilder(args);

// Add Redis for caching
var redis = builder.AddRedis("cache");

// Add API service
var apiService = builder.AddProject<PrAnalyzer_ApiService>("apiservice")
    .WithReference(redis);

// Add Web frontend
builder.AddProject<PrAnalyzer_Web>("webfrontend")
    .WithReference(apiService)
    .WithExternalHttpEndpoints();

builder.Build().Run();