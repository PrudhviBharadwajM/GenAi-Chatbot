using GenAiBot.Models;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace GenAiBot.Services;

public partial class WikipediaClient
{
	private static readonly HttpClient WikipediaHttpClient = new();

	static WikipediaClient()
	{
		WikipediaHttpClient.DefaultRequestHeaders.Clear();
		WikipediaHttpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("GenAIBot", "1.0"));
		WikipediaHttpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("(contact: prudhvi.m1995@gmail.com)"));
	}

	private static readonly JsonSerializerOptions JsonSerializerOptions = new()
	{
		PropertyNameCaseInsensitive = true
	};

	private sealed class WikiApiResponse
	{
		[JsonPropertyName("query")]
		public WikiQuery? Query { get; set; }
	}

	private sealed class WikiQuery
	{
		[JsonPropertyName("pages")]
		public List<WikiPage> Pages { get; set; } = new();
	}

	private sealed class WikiPage
	{
		[JsonPropertyName("pageid")]
		public long? PageId { get; set; }

		[JsonPropertyName("title")]
		public string? Title { get; set; }

		[JsonPropertyName("extract")]
		public string? Extract { get; set; }

		[JsonPropertyName("missing")]
		public bool? Missing { get; set; }
	}

	static string GetWikipediaPageUrl(string pageTitle, bool full)
	{
		var urlBuilder = new UriBuilder("https://en.wikipedia.org/w/api.php");
		var queryString = new Dictionary<string, string>
		{
			["action"] = "query",
			["prop"] = "extracts",
			["format"] = "json",
			["formatversion"] = "2",
			["redirects"] = "1",
			["explaintext"] = "1",
			// Keep wiki-style headings like "== History =="
			["exsectionformat"] = "wiki",
			["titles"] = pageTitle
		};

		if (!full)
			queryString["exintro"] = "1";

		urlBuilder.Query = string.Join("&", queryString.Select(kvp => $"{WebUtility.UrlEncode(kvp.Key)}={WebUtility.UrlEncode(kvp.Value)}"));
		return urlBuilder.ToString();
	}

	static async Task<Document> GetWikipediaPage(string url)
	{
		using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
		using HttpResponseMessage response = await WikipediaHttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
		response.EnsureSuccessStatusCode();

		string json = await response.Content.ReadAsStringAsync();
		WikiApiResponse apiResponse = JsonSerializer.Deserialize<WikiApiResponse>(json, JsonSerializerOptions)
			?? throw new InvalidOperationException("Failed to de-serialize the api response");

		WikiPage? firstPage = apiResponse.Query?.Pages?.FirstOrDefault();

		if (firstPage is null || firstPage.Missing is true)
			throw new Exception($"Could not find wikipedia page for {url}");

		if (string.IsNullOrWhiteSpace(firstPage.Title) || string.IsNullOrWhiteSpace(firstPage.Extract))
			throw new Exception($"Empty wikipedia page returned for {url}");

		string title = firstPage.Title!;
		string content = firstPage.Extract!.Trim();

		string id = Utils.ToUrlSafeId(title);

		string pageUrl = $"https://en.wikipedia.org/wiki/{WebUtility.UrlEncode(title.Replace(' ', '_'))}";

		return new Document(
			Id: id,
			Title: title,
			Content: content,
			PageUrl: pageUrl
		);
	}

	public async Task<Document> GetWikipediaPageForTitle(string pageTitle, bool full = false)
	{
		string url = GetWikipediaPageUrl(pageTitle, full);
		return await GetWikipediaPage(url);
	}

	[GeneratedRegex(@"^\s*=+\s*(.+?)\s*=+\s*", RegexOptions.Multiline | RegexOptions.Compiled)]
	private static partial Regex HeaderRegex();

	public IEnumerable<(string Title, string Content)> SplitIntoSections(string articleText)
	{
		var matches = HeaderRegex().Matches(articleText);

		if (matches.Count == 0)
		{
			yield return ("Introduction", articleText[..]);
			yield break;
		}

		if (matches[0].Index > 0)
		{
			yield return ("Introduction", articleText[..matches[0].Index]);
		}

		for (int i = 0; i < matches.Count; i++)
		{
			var m = matches[i];
			string sectionName = m.Groups[1].Value.Trim();
			
			if(sectionName is "See also" or "References" or "External links" or "Notes")
				continue;

			int bodyStart = m.Index + m.Length;
			int bodyEnd = (i < matches.Count - 1) ? matches[i + 1].Index : articleText.Length;
			int length = bodyEnd - bodyStart;
			var content = articleText.Substring(bodyStart, length).Trim();
			yield return (sectionName, content);
		}

	}

}
