using GenAiBot.Models;
using Microsoft.Extensions.AI;
using Pinecone;

namespace GenAiBot.Services;

public class VectorSearchServiceWithHyde(StringEmbeddingGenerator embeddingGenerator, 
										 DocumentChunkStore contentStore, 
										 IndexClient pineconeIndex,
										 IChatClient chatClient, 
										 ChatOptions chatOptions, 
										 PromptService promptService)
{
	private async Task<string?> GenerateHypothesisAsync(string question)
	{
		string systemText = "You create concise, factual reference passages.";
		string userText = promptService.HydePrompt.Replace("{question}", question); 

		var messages = new List<ChatMessage>{
			new ChatMessage(ChatRole.System, systemText),
			new ChatMessage(ChatRole.User, userText)
		};

		var response = await chatClient.GetResponseAsync(messages, chatOptions);
		string text = response.Text.Trim();
		if (string.IsNullOrWhiteSpace(response.Text)) return null;
		return text.Length > 1500 ? text[..1500] : text;
	}

	public async Task<List<DocumentChunk>> FindTopKChunks(string query, int k)
	{
		if(string.IsNullOrWhiteSpace(query))
		{
			return [];
		}

		var hypothesis = await GenerateHypothesisAsync(query);

		var textToEmbed = hypothesis ?? query;

		var embeddings = await embeddingGenerator.GenerateAsync(
																new[] { textToEmbed },
																new EmbeddingGenerationOptions { Dimensions = 512 }
																);

		var vector = embeddings[0].Vector.ToArray();

		var response = await pineconeIndex.QueryAsync(new Pinecone.QueryRequest
		{
			Vector = vector,
			TopK = (uint)k,
			IncludeMetadata = true
		});

		var matches = (response.Matches ?? []).ToList();

		if(matches.Count == 0)
			return [];

		IEnumerable<string> ids = matches.Select(m => m.Id!).Where(id => !string.IsNullOrEmpty(id));

		List<DocumentChunk> articles = contentStore.GetDocumentsChunks(ids);

		var scoreById = matches.Where(m => m.Id is not null).ToDictionary(m => m.Id!, m => m.Score);

		List<DocumentChunk> ordered = articles.OrderByDescending(a => scoreById.GetValueOrDefault(a.Id, 0f))
								.Take(k)
								.ToList();
		
		return ordered;
	}
}
