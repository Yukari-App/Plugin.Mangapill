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

    public Task<IReadOnlyList<Comic>> SearchAsync(
        string query,
        IReadOnlyDictionary<string, IReadOnlyList<string>> filters,
        int page = 1,
        CancellationToken ct = default
    )
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyList<Comic>> GetTrendingAsync(
        IReadOnlyDictionary<string, IReadOnlyList<string>> filters,
        int page = 1,
        CancellationToken ct = default
    )
    {
        throw new NotImplementedException();
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
}
