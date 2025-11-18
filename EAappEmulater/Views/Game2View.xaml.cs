using CommunityToolkit.Mvvm.Input;
using EAappEmulater.Core;
using EAappEmulater.Enums;
using EAappEmulater.Helper;
using EAappEmulater.Models;
using EAappEmulater.Windows;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows; // for Window related types
using System.Threading.Tasks;

namespace EAappEmulater.Views;

public partial class Game2View : UserControl
{
    public ObservableCollection<GameMenuInfo> ObsCol_GameMenuInfos { get; } = new();
    public ObservableCollection<MyOwnedGameInfo> ObsCol_OwnedGames { get; } = new();

    public IRelayCommand RunGameCommand { get; }
    public IRelayCommand SetGameOptionCommand { get; }
    public IRelayCommand<MyOwnedGameInfo> OpenDownloadSettingsCommand { get; }

    private readonly HttpClient _httpClient = new();
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private bool _isViewLoaded = false;

    public Game2View()
    {
        InitializeComponent();

        RunGameCommand = new RelayCommand<GameType>(RunGame);
        SetGameOptionCommand = new RelayCommand<GameType>(SetGameOption);
        OpenDownloadSettingsCommand = new RelayCommand<MyOwnedGameInfo>(OpenDownloadSettings);

        ToDoList();
        Loaded += Game2View_Loaded;
        Unloaded += Game2View_Unloaded;
    }

    private void Game2View_Loaded(object sender, RoutedEventArgs e)
    {
        Loaded -= Game2View_Loaded;
        _isViewLoaded = true;
        _ = LoadOwnedGamesAsync();
    }

    private void Game2View_Unloaded(object sender, RoutedEventArgs e)
    {
        // 切换视图时不立即清空我的游戏数据，保留直到下一次成功刷新替换
        _isViewLoaded = false;
    }

    // Expose manual refresh
    public async Task RefreshOwnedGamesAsync() => await LoadOwnedGamesAsync();

    private void ToDoList()
    {
        foreach (var item in Base.GameInfoDb)
        {
            ObsCol_GameMenuInfos.Add(new()
            {
                GameType = item.Value.GameType,
                Image = item.Value.Image,
                IsInstalled = item.Value.IsInstalled,
                RunGameCommand = RunGameCommand,
                SetGameOptionCommand = SetGameOptionCommand
            });
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
  }
}";

    private async Task<long> FetchGameSizeAsync(string offerId)
    {
        try
        {
            var vars = new { offerId, cdnOverride = (string)null };
            var data = await ApiRequestAsync(QUERY_DOWNLOAD_URL, vars, true);
            var jit = data?["jitUrl"];
            var sizeStr = jit?["archiveSize"]?.ToString();
            if (long.TryParse(sizeStr, out var bytes)) return bytes;
        }
        catch (Exception ex)
        {
            LoggerHelper.Warn("FetchGameSizeAsync error: " + ex.Message);
        }
        return 0;
    }

    private async void OpenDownloadSettings(MyOwnedGameInfo info)
    {
        if (info == null || string.IsNullOrWhiteSpace(info.OfferId)) return;
        long size = await FetchGameSizeAsync(info.OfferId);
        var model = new DownloadSettingsModel
        {
            GameName = info.Name,
            OfferId = info.OfferId,
            GameSizeBytes = size
        };
        var win = new DownloadSettingsWindow
        {
            Owner = MainWindow.MainWinInstance,
            DataContext = model
        };
        MainWindow.MainWinInstance.IsShowMaskLayer = true;
        win.ShowDialog();
        MainWindow.MainWinInstance.IsShowMaskLayer = false;
    }

    // 直接复用 DownloadView 的查询文本（多行，无转义）
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

    private async Task LoadOwnedGamesAsync()
    {
        try
        {
            // 记录加载前的滚动位置，避免加载时跳转
            double offset = ScrollViewerRoot?.VerticalOffset ?? 0;

            string next = "0";
            string locale = Globals.Language switch { "zh-CN" => "zh-hans", _ => "en" };

            var tempList = new List<MyOwnedGameInfo>();

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
                    tempList.Add(new MyOwnedGameInfo
                    {
                        OfferId = offerId,
                        Name = product["name"]?.ToString(),
                        TrialType = product["trialDetails"]?["trialType"]?.ToString(),
                        GameType = product["baseItem"]?["gameType"]?.ToString()
                    });
                }
                if (string.IsNullOrEmpty(next)) break;
            }

            // 仅当 API 返回至少一个条目时才更新 UI，防止因请求失败清空已有数据
            if (tempList.Count >= 1)
            {
                ObsCol_OwnedGames.Clear();
                foreach (var g in tempList) ObsCol_OwnedGames.Add(g);
                // 恢复滚动位置
                ScrollViewerRoot?.ScrollToVerticalOffset(offset);
            }
        }
        catch (Exception ex)
        {
            LoggerHelper.Error("LOAD OWNED GAMES ERROR:" + ex.Message);
            // 发生错误也确保停留在顶部
            ScrollViewerRoot?.ScrollToVerticalOffset(0);
        }
    }

    private const string GraphQlEndpoint = "https://service-aggregation-layer.juno.ea.com/graphql";
    private async Task<JsonNode?> ApiRequestAsync(string query, object variables, bool needsAuth)
    {
        // 复用 DownloadView 的核心请求
        var payload = new { query, variables };
        string json = JsonSerializer.Serialize(payload, JsonOptions);
        using var req = new HttpRequestMessage(HttpMethod.Post, GraphQlEndpoint);
        req.Headers.TryAddWithoutValidation("User-Agent", "EAApp/PC/13.463.0.5976");
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

    private void RunGame(GameType gameType) => Game.RunGame(gameType);

    private void SetGameOption(GameType gameType)
    {
        var advancedWindow = new AdvancedWindow(gameType) { Owner = MainWindow.MainWinInstance };
        MainWindow.MainWinInstance.IsShowMaskLayer = true;
        advancedWindow.ShowDialog();
        MainWindow.MainWinInstance.IsShowMaskLayer = false;
    }
}

public class MyOwnedGameInfo
{
    public string OfferId { get; set; }
    public string Name { get; set; }
    public string TrialType { get; set; }
    public string GameType { get; set; }
}
