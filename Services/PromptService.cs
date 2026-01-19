namespace GenAiBot.Services;

public class PromptService
{
	static readonly Dictionary<string, string> _prompts = [];

	static PromptService()
	{
		var promptsDirectory = Path.Combine(AppContext.BaseDirectory, "Prompts");

		foreach(var promptName in new[] { "RagSystemPrompt" })
		{
			var promptText = File.ReadAllText(Path.Combine(promptsDirectory,promptName +".txt"));
			_prompts[promptName] = promptText;
		}
	}

	public string RagSystemPrompt => _prompts["RagSystemPrompt"];
}
