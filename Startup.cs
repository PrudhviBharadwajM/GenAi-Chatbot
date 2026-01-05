using GenAiBot.Services;
using Microsoft.Extensions.AI;
using Pinecone;

namespace GenAiBot;

static class Startup
{
	public static void ConfigureServices(WebApplicationBuilder builder)
	{
		string openAiKey = Utils.RequireEnv(builder, "OPENAI_API_KEY");
		string pineconeKey = Utils.RequireEnv(builder, "PINECONE_API_KEY");

		//Add cors for the frontend app
		builder.Services.AddCors(options =>
		{
			options.AddPolicy("FrontendCors", policy => policy.WithOrigins("http://localhost:3000").AllowAnyHeader().AllowAnyMethod());
		});

		builder.Services.AddSingleton<VectorSearchService>();

		builder.Services.AddSingleton<StringEmbeddingGenerator>(s => new OpenAI.Embeddings.EmbeddingClient(
			model : "text-embedding-3-small",
			apiKey : openAiKey).AsIEmbeddingGenerator());

		builder.Services.AddSingleton<IndexClient>(s => new PineconeClient(pineconeKey).Index("landmark-chunks"));

		builder.Services.AddSingleton<DocumentChunkStore>();
		builder.Services.AddSingleton<WikipediaClient>();
		builder.Services.AddSingleton<IndexBuilder>();
		builder.Services.AddSingleton<ArticleSplitter>();
	}
}
