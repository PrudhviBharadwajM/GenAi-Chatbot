using GenAiBot.Models;
using Microsoft.Data.Sqlite;

namespace GenAiBot.Services;

public class DocumentChunkStore
{
	private const string DbFile = "DocumentChunkStore.db";

	static DocumentChunkStore()
	{
		using SqliteConnection connection = new SqliteConnection($"Data Source={DbFile}");
		connection.Open();
		using SqliteCommand command = connection.CreateCommand();
		command.CommandText = @"
				CREATE TABLE IF NOT EXISTS Chunks (
					Id TEXT PRIMARY KEY,
					Title TEXT,
					Section TEXT,
					ChunkIndex INTEGER,
					Content TEXT,
					SourcePageUrl TEXT
				);";

		command.ExecuteNonQuery();
	}

	public List<DocumentChunk> GetDocumentsChunks(IEnumerable<string> ids)
	{
		List<string> idList = ids.Distinct().ToList() ?? [];
		if (idList.Count == 0) return [];

		using SqliteConnection connection = new SqliteConnection($"Data Source={DbFile}");
		connection.Open();
		using SqliteCommand cmd = connection.CreateCommand();

		List<string> parameterNames = new List<string>(idList.Count);
		for (int i = 0; i < idList.Count; i++)
		{
			string paramName = $"$p{i}";
			parameterNames.Add(paramName);
			cmd.Parameters.AddWithValue(paramName, idList[i]);
		}

		string orderByCase = "CASE Id " +
			string.Join(" ", idList.Select((id, i) => $"WHEN $p{i} THEN {i}")) +
			" END";

		cmd.CommandText = $@"
			SELECT Id, Title, Section, ChunkIndex, Content, SourcePageUrl
			FROM Chunks
			WHERE Id IN ({string.Join(", ", parameterNames)})
			ORDER BY {orderByCase};";

		var results = new List<DocumentChunk>();
		using var reader = cmd.ExecuteReader();
		while (reader.Read())
		{
			var doc = new DocumentChunk(
				Id: reader.GetString(0),
				Title: reader.GetString(1),
				Section: reader.GetString(2),
				ChunkIndex: reader.GetInt32(3),
				Content: reader.GetString(4),
				SourcePageUrl: reader.GetString(5)
			);
			results.Add(doc);
		}
		return results;
	}

	public void SaveDocuments(DocumentChunk chunk)
	{
		using var connection = new SqliteConnection($"Data Source={DbFile}");
		connection.Open();
		using var cmd = connection.CreateCommand();
		cmd.CommandText = @"
			INSERT OR REPLACE INTO Chunks (Id, Title, Section, ChunkIndex, Content, SourcePageUrl)
			VALUES ($id, $title, $section, $chunkIndex, $content, $sourcePageUrl);";
		cmd.Parameters.AddWithValue("$id", chunk.Id);
		cmd.Parameters.AddWithValue("$title", chunk.Title);
		cmd.Parameters.AddWithValue("$section", chunk.Section);
		cmd.Parameters.AddWithValue("$chunkIndex", chunk.ChunkIndex);
		cmd.Parameters.AddWithValue("$content", chunk.Content);
		cmd.Parameters.AddWithValue("$sourcePageUrl", chunk.SourcePageUrl);
		cmd.ExecuteNonQuery();
	}
}
