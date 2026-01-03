using Pinecone;
using Microsoft.Extensions.AI;

namespace GenAiBot.Services;

public class IndexBuilder(StringEmbeddingGenerator embeddingGenerator,IndexClient pineconeIndex, WikipediaClient wikipediaClient, DocumentStore documentStore)
{
	public async Task BuildDocumentIndexAsync(string[] pageTitles)
	{
		foreach (string landmark in pageTitles)
		{
			var wikiPage = await wikipediaClient.GetWikipediaPageForTitle(landmark);
			var embedding = await embeddingGenerator.GenerateAsync([wikiPage.Content], new EmbeddingGenerationOptions { Dimensions = 512 });
			var vectorArray = embedding[0].Vector.ToArray();

			var pineconeVector = new Vector
			{
				Id = wikiPage.Id,
				Values = vectorArray,
				Metadata = new Metadata
				{
					{ "title", wikiPage.Title },
				}
			};

			await pineconeIndex.UpsertAsync(new UpsertRequest { Vectors = [pineconeVector] });

			// Save the document to Sqlite.
			documentStore.SaveDocuments(wikiPage);
		}

	}
}