using System.Net;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Yukari.Core.Models;
using Yukari.Core.Sources;
using Yukari.Core.Sources.Http;

namespace Yukari.Plugin.Mangapill;

[ComicSourceMetadata(
    "Mangapill",
    "1.2.0+core2.6.0",
    "https://github.com/Yukari-App/Plugin.Mangapill/releases",
    "https://mangapill.com/static/favicon/favicon-32x32.png",
    "Warning: Filters don't work when search box is empty (showing new mangas)."
)]
public partial class MangapillSource : IComicSource, IRequiresHttpClient
{
    private const string SourceLanguage = "en";
    private const string BaseUrl = "https://mangapill.com";

    private ISharedHttpClient? _httpClient;

    public IReadOnlyList<Filter> Filters => _filters;
    public IReadOnlyDictionary<string, string> Languages => _languages;

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

        return ParseComicListAsync(html);
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

        return ParseComicListAsync(html);
    }

    public async Task<Comic?> GetDetailsAsync(string comicId, CancellationToken ct = default)
    {
        string detailsUrl = $"{BaseUrl}/manga/{comicId}";

        var html = await GetHTMLAsync(detailsUrl, ct);
        if (html == null)
            return null;

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var titleNode = doc.DocumentNode.SelectSingleNode("//h1[contains(@class, 'font-bold')]");
        string title = titleNode?.InnerText.Trim() ?? "Unknown Title";

        var descNode = doc.DocumentNode.SelectSingleNode(
            "//p[contains(@class, 'text-sm text--secondary')]"
        );
        string? description = descNode?.InnerText.Trim();

        var imgNode = doc.DocumentNode.SelectSingleNode(
            "//div[contains(@class, 'flex-shrink-0')]//img"
        );
        string? coverUrl = imgNode?.GetAttributeValue("data-src", null!);
        if (!string.IsNullOrEmpty(coverUrl))
            coverUrl = coverUrl.Split('?').FirstOrDefault();

        var statusNode = doc.DocumentNode.SelectSingleNode(
            "//label[text()='Status']/following-sibling::div"
        );
        string? statusStr = statusNode?.InnerText.Trim();

        var yearNode = doc.DocumentNode.SelectSingleNode(
            "//label[text()='Year']/following-sibling::div"
        );
        int? year = null;
        if (yearNode != null && int.TryParse(yearNode.InnerText.Trim(), out int y))
            year = y;

        var genreNodes = doc.DocumentNode.SelectNodes(
            "//label[text()='Genres']/following-sibling::a"
        );
        string[] tags =
            genreNodes?.Select(g => g.InnerText.Trim()).ToArray() ?? Array.Empty<string>();

        return new Comic(
            Id: comicId,
            ComicUrl: detailsUrl,
            Title: title,
            Author: null,
            Description: description,
            Tags: tags,
            Year: year,
            CoverImageUrl: coverUrl,
            Langs: [SourceLanguage],
            Status: GetComicStatus(statusStr)
        );
    }

    public async Task<IReadOnlyList<Chapter>> GetAllChaptersAsync(
        string comicId,
        string language,
        CancellationToken ct = default
    )
    {
        string chaptersUrl = $"{BaseUrl}/manga/{comicId}";

        var html = await GetHTMLAsync(chaptersUrl, ct);
        if (html == null)
            return Array.Empty<Chapter>();

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var chapterLinks = doc.DocumentNode.SelectNodes("//div[@id='chapters']//a");
        if (chapterLinks is not { Count: > 0 })
            return Array.Empty<Chapter>();

        var chapters = new List<Chapter>();

        // Mangapill returns chapters in descending order (newest first).
        // Reverse to get chronological order (oldest first).
        foreach (var link in chapterLinks.Reverse())
        {
            string href = link.GetAttributeValue("href", "");
            string? chapterId = ExtractChapterIdFromUrl(href);
            if (string.IsNullOrEmpty(chapterId))
                continue;

            string title = link.InnerText.Trim();

            string? groupName = null;
            if (title.StartsWith("Group "))
            {
                var parts = title.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                    groupName = parts[0] + " " + parts[1];
            }

            chapters.Add(
                new Chapter(
                    Id: chapterId,
                    Title: null,
                    Number: ExtractChapterNumber(title),
                    Volume: null,
                    Language: language,
                    Groups: string.IsNullOrEmpty(groupName) ? Array.Empty<string>() : [groupName],
                    LastUpdate: null,
                    Pages: null
                )
            );
        }

        return chapters;
    }

    public async Task<IReadOnlyList<ChapterPage>> GetChapterPagesAsync(
        string comicId,
        string chapterId,
        CancellationToken ct = default
    )
    {
        string pagesUrl = $"{BaseUrl}/chapters/{chapterId}";

        var html = await GetHTMLAsync(pagesUrl, ct);
        if (html == null)
            return Array.Empty<ChapterPage>();

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var imgNodes = doc.DocumentNode.SelectNodes("//img[contains(@class, 'js-page')]");
        if (imgNodes is not { Count: > 0 })
            return Array.Empty<ChapterPage>();

        var pages = new List<ChapterPage>(imgNodes.Count);
        for (int i = 0; i < imgNodes.Count; i++)
        {
            string imageUrl = imgNodes[i].GetAttributeValue("data-src", "");
            if (string.IsNullOrEmpty(imageUrl))
                continue;

            pages.Add(new ChapterPage(Number: i + 1, ImageUrl: imageUrl));
        }

        return pages;
    }

    public async Task<byte[]?> GetImageBytesAsync(string imageUrl, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(imageUrl))
            return null;

        using var request = new HttpRequestMessage(HttpMethod.Get, imageUrl);
        request.Headers.Add("Referer", BaseUrl);

        using var response = await _httpClient!.SendAsync(request, ct);

        if (response.StatusCode == HttpStatusCode.TooManyRequests)
            throw new HttpRequestException(
                "Mangapill Rate Limit Exceeded. Try again later.",
                null,
                HttpStatusCode.TooManyRequests
            );

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadAsByteArrayAsync(ct);
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}
