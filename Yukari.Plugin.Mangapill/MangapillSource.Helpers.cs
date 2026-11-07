using System.Net;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Yukari.Core.Models;

namespace Yukari.Plugin.Mangapill;

public partial class MangapillSource
{
    private static readonly IReadOnlyList<Filter> _filters =
    [
        new Filter(
            Key: "type",
            DisplayName: "Type",
            Options:
            [
                new FilterOption("", "All", true),
                new FilterOption("manga", "Manga", false),
                new FilterOption("novel", "Novel", false),
                new FilterOption("one-shot", "One Shot", false),
                new FilterOption("doujinshi", "Doujinshi", false),
                new FilterOption("manhua", "Manhua", false),
                new FilterOption("oel", "OEL", false),
            ],
            AllowMultiple: false
        ),
        new Filter(
            Key: "status",
            DisplayName: "Status",
            Options:
            [
                new FilterOption("", "All", true),
                new FilterOption("publishing", "Ongoing", false),
                new FilterOption("finished", "Completed", false),
                new FilterOption("on hiatus", "Hiatus", false),
                new FilterOption("discontinued", "Cancelled", false),
            ],
            AllowMultiple: false
        ),
        new Filter(
            Key: "genre",
            DisplayName: "Genres",
            Options:
            [
                new FilterOption("Action", "Action", false),
                new FilterOption("Adventure", "Adventure", false),
                new FilterOption("Cars", "Cars", false),
                new FilterOption("Comedy", "Comedy", false),
                new FilterOption("Dementia", "Dementia", false),
                new FilterOption("Demons", "Demons", false),
                new FilterOption("Doujinshi", "Doujinshi", false),
                new FilterOption("Drama", "Drama", false),
                new FilterOption("Ecchi", "Ecchi", false),
                new FilterOption("Fantasy", "Fantasy", false),
                new FilterOption("Game", "Game", false),
                new FilterOption("Gender Bender", "Gender Bender", false),
                new FilterOption("Harem", "Harem", false),
                new FilterOption("Historical", "Historical", false),
                new FilterOption("Horror", "Horror", false),
                new FilterOption("Isekai", "Isekai", false),
                new FilterOption("Josei", "Josei", false),
                new FilterOption("Kids", "Kids", false),
                new FilterOption("Magic", "Magic", false),
                new FilterOption("Martial Arts", "Martial Arts", false),
                new FilterOption("Mecha", "Mecha", false),
                new FilterOption("Military", "Military", false),
                new FilterOption("Music", "Music", false),
                new FilterOption("Mystery", "Mystery", false),
                new FilterOption("Parody", "Parody", false),
                new FilterOption("Police", "Police", false),
                new FilterOption("Psychological", "Psychological", false),
                new FilterOption("Romance", "Romance", false),
                new FilterOption("Samurai", "Samurai", false),
                new FilterOption("School", "School", false),
                new FilterOption("Sci-Fi", "Sci-Fi", false),
                new FilterOption("Seinen", "Seinen", false),
                new FilterOption("Shoujo", "Shoujo", false),
                new FilterOption("Shoujo Ai", "Shoujo Ai", false),
                new FilterOption("Shounen", "Shounen", false),
                new FilterOption("Shounen Ai", "Shounen Ai", false),
                new FilterOption("Slice of Life", "Slice of Life", false),
                new FilterOption("Space", "Space", false),
                new FilterOption("Sports", "Sports", false),
                new FilterOption("Super Power", "Super Power", false),
                new FilterOption("Supernatural", "Supernatural", false),
                new FilterOption("Thriller", "Thriller", false),
                new FilterOption("Tragedy", "Tragedy", false),
                new FilterOption("Vampire", "Vampire", false),
                new FilterOption("Yaoi", "Yaoi", false),
                new FilterOption("Yuri", "Yuri", false),
            ],
            AllowMultiple: true
        ),
    ];

    private static readonly IReadOnlyDictionary<string, string> _languages = new Dictionary<
        string,
        string
    >
    {
        { "en", "English" },
    };

    private IReadOnlyList<Comic> ParseComicListAsync(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var items = doc.DocumentNode.SelectNodes("//div[contains(@class, 'grid')]/div");

        if (items is not { Count: > 0 })
            return Array.Empty<Comic>();

        var comics = new List<Comic>();
        foreach (var item in items)
        {
            var linkNode = item.SelectSingleNode(".//a[contains(@href, '/manga/')]");
            if (linkNode == null)
                continue;

            string href = linkNode.GetAttributeValue("href", "");
            string? id = ExtractIdFromUrl(href);
            if (string.IsNullOrEmpty(id))
                continue;

            var titleNode = item.SelectSingleNode(".//div[contains(@class, 'font-black')]");
            string title = titleNode?.InnerText.Trim() ?? "Unknown Title";

            var imgNode = linkNode.SelectSingleNode(".//img");
            string? coverUrl = imgNode?.GetAttributeValue("data-src", null!);
            if (!string.IsNullOrEmpty(coverUrl))
                coverUrl = coverUrl.Split("?").FirstOrDefault();

            comics.Add(
                new Comic(
                    Id: id,
                    ComicUrl: null,
                    Title: title,
                    Author: null,
                    Description: null,
                    Tags: [],
                    Year: null,
                    CoverImageUrl: coverUrl,
                    Langs: []
                )
            );
        }

        return comics;
    }

    private async Task<string?> GetHTMLAsync(string url, CancellationToken ct = default)
    {
        using var response = await _httpClient!.SendAsync(
            new HttpRequestMessage(HttpMethod.Get, url),
            ct
        );

        if (response.StatusCode == HttpStatusCode.TooManyRequests)
            throw new HttpRequestException(
                "Mangapill Rate Limit Exceeded. Try again later.",
                null,
                HttpStatusCode.TooManyRequests
            );

        if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.NotFound)
            return default;

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync(ct);
    }

    private static ComicStatus GetComicStatus(string? status) =>
        status switch
        {
            "publishing" => ComicStatus.Ongoing,
            "finished" => ComicStatus.Completed,
            "on hiatus" => ComicStatus.Hiatus,
            "discontinued" => ComicStatus.Cancelled,
            _ => ComicStatus.Unknown,
        };

    private static string? ExtractIdFromUrl(string? url)
    {
        if (string.IsNullOrEmpty(url))
            return null;
        var match = Regex.Match(url, @"/manga/(\d+)/");
        return match.Success ? match.Groups[1].Value : null;
    }

    private static string? ExtractChapterIdFromUrl(string? url)
    {
        if (string.IsNullOrEmpty(url))
            return null;
        var match = Regex.Match(url, @"/chapters/([^/]+)");
        return match.Success ? match.Groups[1].Value : null;
    }

    private static string? ExtractChapterNumber(string title)
    {
        var match = Regex.Match(title, @"Chapter\s+(\d+(\.\d+)?)");
        return match.Success ? match.Groups[1].Value : null;
    }

    private static string ToQueryString(Dictionary<string, string[]> source) =>
        string.Join(
            "&",
            source.SelectMany(kvp =>
                kvp.Value.Select(v => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(v)}")
            )
        );
}
