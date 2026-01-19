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


		builder.Services.AddSingleton<StringEmbeddingGenerator>(s => new OpenAI.Embeddings.EmbeddingClient(
			model : "text-embedding-3-small",
			apiKey : openAiKey).AsIEmbeddingGenerator());

		builder.Services.AddSingleton<IndexClient>(s => new PineconeClient(pineconeKey).Index("landmark-chunks"));

		builder.Services.AddLogging(logging => logging.AddConsole().SetMinimumLevel(LogLevel.Information));

		builder.Services.AddSingleton<ILoggerFactory>(s => LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information)));

		builder.Services.AddSingleton<IChatClient>(s =>
		{
			var loggerFactory = s.GetRequiredService<ILoggerFactory>();
			var client = new OpenAI.Chat.ChatClient(
				model: "gpt-5-mini",
				apiKey: openAiKey).AsIChatClient();

			return new ChatClientBuilder(client).UseLogging(loggerFactory).UseFunctionInvocation(loggerFactory, c => { c.IncludeDetailedErrors = true; }).Build(s);
		});

		builder.Services.AddTransient<ChatOptions>(s => new ChatOptions { });

		builder.Services.AddSingleton<RagQuestionService>();
		builder.Services.AddSingleton<PromptService>();
		builder.Services.AddSingleton<VectorSearchService>();
		builder.Services.AddSingleton<DocumentChunkStore>();
		builder.Services.AddSingleton<WikipediaClient>();
		builder.Services.AddSingleton<IndexBuilder>();
		builder.Services.AddSingleton<ArticleSplitter>();
	}
}
