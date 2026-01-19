using GenAiBot;
using GenAiBot.Services;

var builder = WebApplication.CreateBuilder(args);
Startup.ConfigureServices(builder);
var app = builder.Build();

//Build the document index on startup 
/*
var indexer = app.Services.GetRequiredService<IndexBuilder>();
await indexer.BuilderIndex(SourceData.LandmarkNames);
Console.WriteLine("Document index built successfully.");
*/

app.UseCors("FrontendCors");

// GET /search?query=...
app.MapGet("/search", async (string query, VectorSearchService vectorSearch) =>
{
	var results = await vectorSearch.FindTopKArticles(query, 3);
	return Results.Ok(results);
});

// GET /ask?question=...
app.MapGet("/ask", async (string question, RagQuestionService ragQuestionService) =>
{
	string answer = await ragQuestionService.AnswerQuestion(question);
	return Results.Ok(answer);
});

app.Run();