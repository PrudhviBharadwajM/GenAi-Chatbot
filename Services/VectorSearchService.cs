using GenAiBot.Models;
using Pinecone;

namespace GenAiBot.Services;

public class VectorSearchService(StringEmbeddingGenerator embeddingGenerator, IndexClient pineConeIndex, DocumentChunkStore contentStore)
{
	public async Task<List<DocumentChunk>> FindTopKArticles(string query, int k)
	{
		if (string.IsNullOrWhiteSpace(query))
		{
			return [];
		}

		var embedding = await embeddingGenerator.GenerateAsync([query], new EmbeddingGenerationOptions { Dimensions = 512 });

		var vector = embedding[0].Vector.ToArray();

		var response = await pineConeIndex.QueryAsync(new QueryRequest
		{
			TopK = (uint)k,
			IncludeMetadata = true,
			Vector = vector
		});

		var matches = (response.Matches ?? []).ToList();

		if (matches.Count == 0)
		{
			return [];
		}
		var ids = matches.Select(m => m.Id!).Where(id => !string.IsNullOrWhiteSpace(id)).ToList();

		var articles = contentStore.GetDocumentsChunks(ids);
		var scorebyId = matches.Where(m => m.Id is not null).ToDictionary(m => m.Id!, m => m.Score);

		var sortedArticles = articles.OrderByDescending(a => scorebyId.GetValueOrDefault(a.Id, 0f))
									 .Take(k)
									 .ToList();
		return sortedArticles;
	}
}
