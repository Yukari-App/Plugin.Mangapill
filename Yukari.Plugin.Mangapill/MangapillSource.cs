using System.Net;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Yukari.Core.Models;
using Yukari.Core.Sources;

namespace Yukari.Plugin.Mangapill;

[ComicSourceMetadata(
    "Mangapill",
    "0.1.0-Alpha+core2.4.0",
    "https://mangapill.com/static/favicon/favicon-32x32.png",
    "Access a vast array of manga from Mangapill."
)]
public class MangapillSource : IComicSource, IRequiresHttpClient
{
    private const string SourceLanguage = "en";

    private static IReadOnlyList<Filter>? _filters;
    private static IReadOnlyDictionary<string, string>? _languages;

    public IReadOnlyList<Filter> Filters =>
        _filters ??= [
            new Filter(
                Key: "type",
                DisplayName: "Type",
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
                [
                    new FilterOption("", "All", true),
                    new FilterOption("publishing", "Ongoing", false),
                    new FilterOption("finished", "Completed", false),
                    new FilterOption("on+hiatus", "Hiatus", false),
                    new FilterOption("discontinued", "Cancelled", false),
                ],
                AllowMultiple: false
            ),
            new Filter(
                Key: "genre",
                DisplayName: "Genres",
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

    public IReadOnlyDictionary<string, string> Languages =>
        _languages ??= new Dictionary<string, string> { { "en", "English" } };

    private const string BaseUrl = "https://mangapill.com";

    private ISharedHttpClient? _httpClient;

    public void SetHttpClient(ISharedHttpClient httpClient) => _httpClient = httpClient;

    public async Task<IReadOnlyList<Comic>> SearchAsync(
        string query,
        IReadOnlyDictionary<string, IReadOnlyList<string>> filters,
        int page = 1,
        CancellationToken ct = default
    )
    {
        var queryParams = new Dictionary<string, string[]>
        {
            ["q"] = [query],
            ["page"] = [page.ToString()],
        };

        foreach (var kvp in filters)
            queryParams[kvp.Key] = kvp.Value.ToArray();

        string searchUrl = $"{BaseUrl}/search?{ToQueryString(queryParams)}";

        var html = await GetHTMLAsync(searchUrl, ct);
        if (html == null)
            return Array.Empty<Comic>();

        return await ParseComicListAsync(html);
    }

    public async Task<IReadOnlyList<Comic>> GetTrendingAsync(
        IReadOnlyDictionary<string, IReadOnlyList<string>> filters,
        int page = 1,
        CancellationToken ct = default
    )
    {
        if (page > 1)
            return Array.Empty<Comic>();

        string searchUrl = $"{BaseUrl}/mangas/new";

        var html = await GetHTMLAsync(searchUrl, ct);
        if (html == null)
            return Array.Empty<Comic>();

        return await ParseComicListAsync(html);
    }

    public Task<Comic?> GetDetailsAsync(string comicId, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyList<Chapter>> GetAllChaptersAsync(
        string comicId,
        string language,
        CancellationToken ct = default
    )
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyList<ChapterPage>> GetChapterPagesAsync(
        string comicId,
        string chapterId,
        CancellationToken ct = default
    )
    {
        throw new NotImplementedException();
    }

    public Task<byte[]?> GetImageBytesAsync(string imageUrl, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }

    private async Task<IReadOnlyList<Comic>> ParseComicListAsync(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var items =
            doc.DocumentNode.SelectNodes(
                "//div[contains(@class, 'grid')]//div[contains(@class, 'relative')]//.."
            ) ?? doc.DocumentNode.SelectNodes("//div[contains(@class, 'grid')]/div");

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
            coverUrl = coverUrl?.Split("?").FirstOrDefault();

            comics.Add(
                new Comic(
                    Id: id,
                    ComicUrl: null,
                    Slug: null,
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

    private static string? ExtractIdFromUrl(string? url)
    {
        if (string.IsNullOrEmpty(url))
            return null;
        var match = Regex.Match(url, @"/manga/(\d+)/");
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
