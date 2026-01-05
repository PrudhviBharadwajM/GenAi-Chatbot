using Pinecone;
using Microsoft.Extensions.AI;
using System.Collections.Immutable;

namespace GenAiBot.Services;

public class IndexBuilder(StringEmbeddingGenerator embeddingGenerator, IndexClient pineconeIndex, WikipediaClient wikipediaClient, DocumentChunkStore chunkStore, ArticleSplitter splitter)
{
	public async Task BuilderIndex(string[] pageTitles)
	{
		foreach (string pageTitle in pageTitles)
		{
			var page = await wikipediaClient.GetWikipediaPageForTitle(pageTitle, full: true);
			var sections = wikipediaClient.SplitIntoSections(page.Content);
			var chunks = sections.SelectMany(section => 
				splitter.Chunk(page.Title, section.Content, page.PageUrl, section.Title)
			).Take(25)
			 .ToImmutableList();

			var stringsToEmbed = chunks.Select(c => $"{c.Title} > {c.Section}\n\n{c.Content}");

			var embeddings = await embeddingGenerator.GenerateAsync(stringsToEmbed, new EmbeddingGenerationOptions { Dimensions = 512 });

			var vectors = chunks.Select((chunk, Index) => new Vector
			{
				Id = chunk.Id,
				Values = embeddings[Index].Vector.ToArray(),
				Metadata = new Metadata
				{
					{ "title", chunk.Title },
					{ "section", chunk.Section },
					{ "chunk_index", chunk.ChunkIndex },
				}
			});

			await pineconeIndex.UpsertAsync(new UpsertRequest
			{
				Vectors = vectors
			});

			// Save chunks to content store
			foreach (var chunk in chunks)
			{
				chunkStore.SaveDocuments(chunk);
			}
		}
	}
}