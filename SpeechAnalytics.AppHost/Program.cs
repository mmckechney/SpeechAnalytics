using Aspire.Hosting;
using Aspire.Hosting.Azure;

var builder = DistributedApplication.CreateBuilder(args);

// Application services
var askInsights = builder.AddProject("askinsights", "../AskInsightsService/AskInsightsService.csproj");

var transcription = builder.AddProject("transcription", "../TranscriptionService/TranscriptionService.csproj");

builder.Build().Run();
