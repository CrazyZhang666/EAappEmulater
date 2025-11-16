using EAappEmulater.Core;
using EAappEmulater.Helper;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;

namespace EAappEmulater.Views;

public partial class DownloadView : UserControl, INotifyPropertyChanged
{
    private readonly HttpClient _httpClient = new();

    private static readonly JsonSerializerOptions JsonOptions =
        new JsonSerializerOptions(JsonSerializerDefaults.Web);

    public ObservableCollection<GameItem> Games { get; } = new();

    private GameItem _selectedGame;
    public GameItem SelectedGame
    {
        get => _selectedGame;
        set
        {
            _selectedGame = value;
            OnPropertyChanged();
            DownloadUrl = "";
            DownloadSize = "";
            if (value != null)
                _ = DownloadSelectedAsync();
        }
    }

    private string _downloadUrl = "";
    public string DownloadUrl
    {
        get => _downloadUrl;
        set { _downloadUrl = value; OnPropertyChanged(); }
    }

    private string _downloadSize = "";
    public string DownloadSize
    {
        get => _downloadSize;
        set { _downloadSize = value; OnPropertyChanged(); }
    }

    public DownloadView()
    {
        InitializeComponent();
        DataContext = this;
        Loaded += DownloadView_Loaded;
    }

    private async void DownloadView_Loaded(object sender, RoutedEventArgs e)
    {
        Loaded -= DownloadView_Loaded;
        await LoadGamesAsync();
    }

    private const string QUERY_OWNED_GAMES = @"
query getPreloadedOwnedGames(
  $next: String,
  $locale: Locale,
  $limit: Int,
  $type: [GameProductType!]!,
  $entitlementEnabled: Boolean,
  $storefronts: [UserGameProductStorefront!],
  $ownershipMethods: [OwnershipMethod!],
  $processorArchitectures: [ProcessorArchitecture!],
  $platforms: [GamePlatform!]!
) {
  me {
    ownedGameProducts(
      storefronts: $storefronts,
      locale: $locale,
      paging: {limit: $limit, next: $next},
      productFound: true,
      orderBy: {field: NAME, direction: ASC},
      ownershipMethod: $ownershipMethods,
      processorArchitectures: $processorArchitectures,
      type: $type,
      downloadableOnly: false,
      entitlementEnabled: $entitlementEnabled,
      platforms: $platforms
    ) {
      next
      items {
        originOfferId
        product {
          name
          downloadable
          trialDetails { trialType }
          baseItem { gameType }
        }
      }
    }
  }
}";

    private async Task LoadGamesAsync()
    {
        try
        {
            Games.Clear();
            string next = "0";
            string locale = "";
            switch (Globals.Language)
            {
                case "zh-CN":
                    locale = "zh-hans";
                    break;
                default:
                    locale = "en";
                    break;
            }

            while (true)
            {
                var variables = new
                {
                    locale,
                    limit = 100,
                    next,
                    type = new[]
                    {
                        "DIGITAL_FULL_GAME","PACKAGED_FULL_GAME",
                        "DIGITAL_EXTRA_CONTENT","PACKAGED_EXTRA_CONTENT"
                    },
                    entitlementEnabled = true,
                    storefronts = new[] { "EA", "STEAM", "EPIC" },
                    ownershipMethods = new[]
                    {
                        "UNKNOWN","ASSOCIATION","PURCHASE","REDEMPTION",
                        "GIFT_RECEIPT","ENTITLEMENT_GRANT","DIRECT_ENTITLEMENT",
                        "PRE_ORDER_PURCHASE","VAULT","XGP_VAULT","STEAM",
                        "STEAM_VAULT","STEAM_SUBSCRIPTION","EPIC",
                        "EPIC_VAULT","EPIC_SUBSCRIPTION"
                    },
                    platforms = new[] { "PC" },
                    processorArchitectures = Array.Empty<string>()
                };

                var data = await ApiRequestAsync(QUERY_OWNED_GAMES, variables, true);
                var owned = data?["me"]?["ownedGameProducts"];
                var items = owned?["items"]?.AsArray();
                next = owned?["next"]?.ToString();

                if (items == null) break;

                foreach (var item in items)
                {
                    var offerId = item?["originOfferId"]?.ToString();
                    if (offerId == null) continue;

                    var product = item["product"];
                    if (product == null) continue;

                    bool downloadable = product["downloadable"]?.GetValue<bool>() ?? false;
                    if (!downloadable) continue;

                    Games.Add(new GameItem
                    {
                        OfferId = offerId,
                        Name = product["name"]?.ToString(),
                        Downloadable = true,
                        TrialType = product["trialDetails"]?["trialType"]?.ToString(),
                        GameType = product["baseItem"]?["gameType"]?.ToString()
                    });
                }

                if (string.IsNullOrEmpty(next)) break;
            }

            if (Games.Count > 0)
                SelectedGame = Games[0];
        }
        catch (Exception ex)
        {
            LoggerHelper.Error("LOAD GAMELIST ERROR:"+ ex.Message + "");
        }
    }

    private const string QUERY_DOWNLOAD_URL = @"
query JitUrlRequest(
  $offerId: String!,
  $cdnOverride: String
){
  jitUrl: downloadUrl(offerId: $offerId, cdnOverride: $cdnOverride) {
    url
    archiveSize
    syncUrl
    syncArchiveSize
  }
}";

    private async Task DownloadSelectedAsync()
    {
        try
        {
            var vars = new
            {
                offerId = SelectedGame?.OfferId,
                cdnOverride = (string)null
            };

            var data = await ApiRequestAsync(QUERY_DOWNLOAD_URL, vars, true);
            var jit = data?["jitUrl"];

            if (jit == null)
            {
                DownloadUrl = "";
                DownloadSize = "";
                return;
            }

            DownloadUrl = jit["url"]?.ToString() ?? "";

            var sizeStr = jit["archiveSize"]?.ToString();
            if (long.TryParse(sizeStr, out long bytes))
            {
                double gb = bytes / 1024d / 1024d / 1024d;
                DownloadSize = $"{gb:0.00} GB";
            }
            else
            {
                DownloadSize = "";
            }
        }
        catch (Exception ex)
        {
            LoggerHelper.Error("GET Downloadlink ERROR:" + ex.Message + "");
        }
    }

    private const string GraphQlEndpoint = "https://service-aggregation-layer.juno.ea.com/graphql";

    private async Task<JsonNode?> ApiRequestAsync(string query, object variables, bool needsAuth)
    {
        var payload = new { query, variables };
        string json = JsonSerializer.Serialize(payload, JsonOptions);

        using var req = new HttpRequestMessage(HttpMethod.Post, GraphQlEndpoint);
        req.Headers.TryAddWithoutValidation("User-Agent", $"EAApp/PC/13.463.0.5976");
        req.Headers.TryAddWithoutValidation("x-client-id", "EAX-JUNO-CLIENT");
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        if (needsAuth)
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Account.AccessToken);

        req.Content = new StringContent(json, Encoding.UTF8, "application/json");

        var res = await _httpClient.SendAsync(req);
        string resp = await res.Content.ReadAsStringAsync();

        if (!res.IsSuccessStatusCode)
            throw new Exception($"HTTP {(int)res.StatusCode}: {resp}");

        return JsonNode.Parse(resp)?["data"];
    }

    public event PropertyChangedEventHandler PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public class GameItem
{
    public string OfferId { get; set; }
    public string Name { get; set; }
    public bool Downloadable { get; set; }
    public string TrialType { get; set; }
    public string GameType { get; set; }
}
