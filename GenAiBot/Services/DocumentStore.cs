using GenAiBot.Models;
using Microsoft.Data.Sqlite;

namespace GenAiBot.Services;

public class DocumentStore
{
	private const string DbFile = "DocumentStore.db";

	static DocumentStore()
	{
		using SqliteConnection connection = new SqliteConnection($"Data Source={DbFile}");
		connection.Open();
		using SqliteCommand command = connection.CreateCommand();
		command.CommandText = @"
				CREATE TABLE IF NOT EXISTS Documents (
					Id TEXT PRIMARY KEY,
					Title TEXT,
					Content TEXT,
					PageUrl TEXT
				);";

		command.ExecuteNonQuery();
	}

	public List<Document> GetDocuments(IEnumerable<string> ids)
	{
		List<string> idList = ids.Distinct().ToList() ?? [];
		if (idList.Count == 0) return [];

		using SqliteConnection connection = new SqliteConnection($"Data Source={DbFile}");
		using SqliteCommand cmd = connection.CreateCommand();

		List<string> parameterNames = new List<string>(idList.Count);
		for (int i = 0; i < idList.Count; i++)
		{
			string paramName = $"p" + i;
			parameterNames.Add(paramName);
			cmd.Parameters.AddWithValue(paramName, idList[i]);
		}

		string orderByCase = string.Join(" ", idList.Select((id, index) => $"WHEN $p{index} THEN {index}")) + "END";

		cmd.CommandText = $@"
			SELECT Id, Title, Content, PageUrl
			FROM Documents
			WHERE Id IN ({string.Join(", ", parameterNames)})
			ORDER BY {orderByCase};";

		var results = new List<Document>();
		using var reader = cmd.ExecuteReader();
		while (reader.Read())
		{
			var doc = new Document(
				Id: reader.GetString(0),
				Title: reader.GetString(1),
				Content: reader.GetString(2),
				PageUrl: reader.GetString(3)
			);
			results.Add(doc);
		}
		return results;
	}

	public void SaveDocuments(Document document)
	{
		using var connection = new SqliteConnection($"Data Source={DbFile}");
		connection.Open();
		using var cmd = connection.CreateCommand();
		cmd.CommandText = @"
			INSERT OR REPLACE INTO Documents (Id, Title, Content, PageUrl)
			VALUES ($id, $title, $content, $pageUrl);";
		cmd.Parameters.AddWithValue("$id", document.Id);
		cmd.Parameters.AddWithValue("$title", document.Title);
		cmd.Parameters.AddWithValue("$content", document.Content);
		cmd.Parameters.AddWithValue("$pageUrl", document.PageUrl);
		cmd.ExecuteNonQuery();
	}
}
