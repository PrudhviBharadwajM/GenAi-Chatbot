using GenAiBot;
using GenAiBot.Services;

var builder = WebApplication.CreateBuilder(args);
Startup.ConfigureServices(builder);
var app = builder.Build();

var indexer = app.Services.GetRequiredService<IndexBuilder>();
await indexer.BuildDocumentIndexAsync(SourceData.LandmarkNames);
Console.WriteLine("Document index built successfully.");