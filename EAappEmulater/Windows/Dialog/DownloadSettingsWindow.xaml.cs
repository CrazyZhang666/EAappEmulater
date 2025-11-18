using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Nodes;
using EAappEmulater.Helper;
using EAappEmulater.Core;
using EAappEmulater.Models;

namespace EAappEmulater.Windows;

public partial class DownloadSettingsWindow
{
    private readonly HttpClient _httpClient = new();
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

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

    public DownloadSettingsWindow()
    {
        InitializeComponent();
    }

    private async void DownloadSettingsWindow_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is DownloadSettingsModel model && !string.IsNullOrWhiteSpace(model.OfferId))
        {
            try
            {
                var vars = new { offerId = model.OfferId, cdnOverride = (string)null };
                var data = await ApiRequestAsync(QUERY_DOWNLOAD_URL, vars, true);
                var jit = data?["jitUrl"];
                if (jit != null)
                {
                    var sizeStr = jit["archiveSize"]?.ToString();
                    if (long.TryParse(sizeStr, out long bytes) && bytes > 0)
                    {
                        model.GameSizeBytes = bytes;
                    }
                    model.JitDownloadUrl = jit["url"]?.ToString();
                }
                else
                {
                    LoggerHelper.Warn($"JIT download info missing for offerId={model.OfferId}");
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.Error($"DownloadSettingsWindow load size error: {ex.Message}");
            }
        }
    }

    private const string GraphQlEndpoint = "https://service-aggregation-layer.juno.ea.com/graphql";
    private async Task<JsonNode?> ApiRequestAsync(string query, object variables, bool needsAuth)
    {
        var payload = new { query, variables };
        string json = JsonSerializer.Serialize(payload, JsonOptions);
        using var req = new HttpRequestMessage(HttpMethod.Post, GraphQlEndpoint);
        req.Headers.TryAddWithoutValidation("User-Agent", "EAApp/PC/13.463.0.5976");
        req.Headers.TryAddWithoutValidation("x-client-id", "EAX-JUNO-CLIENT");
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (needsAuth && Account.AccessToken is string token && !string.IsNullOrWhiteSpace(token))
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        req.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        var res = await _httpClient.SendAsync(req);
        string resp = await res.Content.ReadAsStringAsync();
        if (!res.IsSuccessStatusCode)
            throw new System.Exception($"HTTP {(int)res.StatusCode}: {resp}");
        return JsonNode.Parse(resp)?["data"];
    }
}

public class DownloadSettingsModel : INotifyPropertyChanged
{
    public DownloadSettingsModel()
    {
        _startDownloadCommand = new RelayCommand(StartDownload, CanStart);
    }

    public string GameName { get; set; }
    private string _offerId;
    public string OfferId { get => _offerId; set { _offerId = value; OnPropertyChanged(); _startDownloadCommand?.NotifyCanExecuteChanged(); } }

    private long _gameSizeBytes;
    public long GameSizeBytes
    {
        get => _gameSizeBytes;
        set { _gameSizeBytes = value; OnPropertyChanged(); OnPropertyChanged(nameof(GameSizeDisplay)); ValidateSpace(); _startDownloadCommand?.NotifyCanExecuteChanged(); }
    }
    public string GameSizeDisplay => _gameSizeBytes > 0 ? FormatBytes(GameSizeBytes) : "--";

    public string JitDownloadUrl { get => _jitDownloadUrl; set { _jitDownloadUrl = value; OnPropertyChanged(); } }
    private string _jitDownloadUrl = string.Empty;

    public ObservableCollection<string> InstallLanguages { get; } = new(new[] { "Auto", "简体中文", "English" });
    private string _selectedLanguage = "Auto";
    public string SelectedLanguage { get => _selectedLanguage; set { _selectedLanguage = value; OnPropertyChanged(); } }

    private string _installDirectory = string.Empty;
    public string InstallDirectory { get => _installDirectory; set { _installDirectory = value; OnPropertyChanged(); UpdateFreeSpace(); _startDownloadCommand?.NotifyCanExecuteChanged(); } }

    private string _freeSpaceDisplay = "Unknown";
    public string FreeSpaceDisplay { get => _freeSpaceDisplay; set { _freeSpaceDisplay = value; OnPropertyChanged(); ValidateSpace(); _startDownloadCommand?.NotifyCanExecuteChanged(); } }

    private string _validationText = string.Empty;
    public string ValidationText { get => _validationText; set { _validationText = value; OnPropertyChanged(); _startDownloadCommand?.NotifyCanExecuteChanged(); } }

    private Brush _validationBrush = Brushes.Black;
    public Brush ValidationBrush { get => _validationBrush; set { _validationBrush = value; OnPropertyChanged(); } }

    private readonly IRelayCommand _startDownloadCommand;
    public IRelayCommand StartDownloadCommand => _startDownloadCommand;

    public IRelayCommand BrowseFolderCommand => new RelayCommand(BrowseFolder);
    public IRelayCommand CloseCommand => new RelayCommand(() =>
    {
        Application.Current.Windows
            .OfType<DownloadSettingsWindow>()
            .FirstOrDefault(w => ReferenceEquals(w.DataContext, this))?.Close();
    });

    private bool CanStart()
    {
        return !string.IsNullOrWhiteSpace(OfferId)
            && GameSizeBytes > 0
            && Directory.Exists(InstallDirectory)
            && string.IsNullOrWhiteSpace(ValidationText);
    }

    private void StartDownload()
    {
        var task = new DownloadTaskModel
        {
            OfferId = OfferId,
            GameName = GameName,
            InstallDir = InstallDirectory,
            InstallLanguage = SelectedLanguage,
            JitUrl = JitDownloadUrl,
            TotalBytes = GameSizeBytes
        };
        DownloadHelper.Enqueue(task, autoStart: true);
        Application.Current.Windows.OfType<DownloadSettingsWindow>().FirstOrDefault(w => ReferenceEquals(w.DataContext, this))?.Close();
    }

    private void BrowseFolder()
    {
        using var dlg = new System.Windows.Forms.FolderBrowserDialog();
        var result = dlg.ShowDialog();
        if (result == System.Windows.Forms.DialogResult.OK)
        {
            InstallDirectory = dlg.SelectedPath;
        }
    }

    private void UpdateFreeSpace()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(InstallDirectory) || !Directory.Exists(InstallDirectory))
            {
                FreeSpaceDisplay = "Unknown";
                return;
            }
            var root = Path.GetPathRoot(InstallDirectory);
            if (string.IsNullOrWhiteSpace(root)) { FreeSpaceDisplay = "Unknown"; return; }
            var drive = DriveInfo.GetDrives().FirstOrDefault(d => d.IsReady && string.Equals(d.RootDirectory.FullName, root, StringComparison.OrdinalIgnoreCase));
            if (drive == null) { FreeSpaceDisplay = "Unknown"; return; }
            FreeSpaceDisplay = FormatBytes(drive.AvailableFreeSpace);
        }
        catch { FreeSpaceDisplay = "Unknown"; }
    }

    private void ValidateSpace()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(InstallDirectory) || !Directory.Exists(InstallDirectory)) { ValidationText = string.Empty; return; }
            double free = ParseBytes(FreeSpaceDisplay);
            double need = GameSizeBytes * 2.0; // require 200% of game size
            if (GameSizeBytes > 0 && free > 0 && free < need)
            {
                ValidationText = $"Not enough space. Need at least {FormatBytes((long)need)}";
                ValidationBrush = Brushes.OrangeRed;
            }
            else { ValidationText = string.Empty; }
        }
        catch { ValidationText = string.Empty; }
    }

    public static string FormatBytes(long bytes)
    {
        double v = bytes; string unit = "B";
        if (v >= 1024) { v /= 1024; unit = "KB"; }
        if (v >= 1024) { v /= 1024; unit = "MB"; }
        if (v >= 1024) { v /= 1024; unit = "GB"; }
        if (v >= 1024) { v /= 1024; unit = "TB"; }
        return $"{v:0.00} {unit}";
    }

    private static double ParseBytes(string display)
    {
        if (string.IsNullOrWhiteSpace(display)) return 0;
        var parts = display.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2) return 0;
        if (!double.TryParse(parts[0], out var v)) return 0;
        var unit = parts[1].ToUpperInvariant();
        return unit switch { "TB" => v * 1024 * 1024 * 1024 * 1024, "GB" => v * 1024 * 1024 * 1024, "MB" => v * 1024 * 1024, "KB" => v * 1024, _ => v };
    }

    public event PropertyChangedEventHandler PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
