using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using HttpRequestMonitor.Models;
using HttpRequestMonitor.Services;

namespace HttpRequestMonitor.ViewModels;

public sealed class MainViewModel : ViewModelBase, IDisposable
{
    private const int LoadChartWindowMinutes = 20;

    private readonly HttpClient _httpClient = new();
    private readonly HttpServer _httpServer = new();
    private readonly SemaphoreSlim _logFileLock = new(1, 1);
    private readonly string _logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs.txt");
    private string _port = "8080";
    private string _serverStatus = "Сервер остановлен";
    private bool _isServerRunning;
    private string _clientUrl = "http://localhost:8080/";
    private string _selectedMethod = "GET";
    private string _jsonBody = "{\r\n  \"message\": \"Hello from WPF\"\r\n}";
    private string _clientResponse = string.Empty;
    private string _selectedMethodFilter = "Все";
    private string _selectedStatusFilter = "Все";
    private string _logText = string.Empty;
    private int _totalGetRequests;
    private int _totalPostRequests;
    private double _averageProcessingTimeMs;
    private PlotModel _loadPlotModel;
    private bool _isDisposed;

    public MainViewModel()
    {
        _httpServer.RequestLogged += AddLogAsync;

        MethodOptions = new ObservableCollection<string> { "GET", "POST" };
        MethodFilters = new ObservableCollection<string> { "Все", "GET", "POST" };
        StatusFilters = new ObservableCollection<string> { "Все", "2xx", "3xx", "4xx", "5xx" };
        Logs = new ObservableCollection<LogEntry>();
        FilteredLogs = new ObservableCollection<LogEntry>();
        _loadPlotModel = CreatePlotModel();

        StartServerCommand = new RelayCommand(_ => StartServer(), _ => !IsServerRunning);
        StopServerCommand = new RelayCommand(_ => StopServer(), _ => IsServerRunning);
        SendRequestCommand = new RelayCommand(_ => SendRequestAsync());
        SaveLogsCommand = new RelayCommand(_ => SaveLogsAsync());
        DownloadLogsCommand = new RelayCommand(_ => DownloadLogsAsync());
    }

    public ObservableCollection<string> MethodOptions { get; }

    public ObservableCollection<string> MethodFilters { get; }

    public ObservableCollection<string> StatusFilters { get; }

    public ObservableCollection<LogEntry> Logs { get; }

    public ObservableCollection<LogEntry> FilteredLogs { get; }

    public ICommand StartServerCommand { get; }

    public ICommand StopServerCommand { get; }

    public ICommand SendRequestCommand { get; }

    public ICommand SaveLogsCommand { get; }

    public ICommand DownloadLogsCommand { get; }

    public string Port
    {
        get => _port;
        set => SetProperty(ref _port, value);
    }

    public string ServerStatus
    {
        get => _serverStatus;
        set => SetProperty(ref _serverStatus, value);
    }

    public bool IsServerRunning
    {
        get => _isServerRunning;
        private set
        {
            if (SetProperty(ref _isServerRunning, value))
            {
                RaiseServerCommandStates();
            }
        }
    }

    public string ClientUrl
    {
        get => _clientUrl;
        set => SetProperty(ref _clientUrl, value);
    }

    public string SelectedMethod
    {
        get => _selectedMethod;
        set => SetProperty(ref _selectedMethod, value);
    }

    public string JsonBody
    {
        get => _jsonBody;
        set => SetProperty(ref _jsonBody, value);
    }

    public string ClientResponse
    {
        get => _clientResponse;
        set => SetProperty(ref _clientResponse, value);
    }

    public string SelectedMethodFilter
    {
        get => _selectedMethodFilter;
        set
        {
            if (SetProperty(ref _selectedMethodFilter, value))
            {
                ApplyFilters();
            }
        }
    }

    public string SelectedStatusFilter
    {
        get => _selectedStatusFilter;
        set
        {
            if (SetProperty(ref _selectedStatusFilter, value))
            {
                ApplyFilters();
            }
        }
    }

    public string LogText
    {
        get => _logText;
        set => SetProperty(ref _logText, value);
    }

    public int TotalGetRequests
    {
        get => _totalGetRequests;
        private set => SetProperty(ref _totalGetRequests, value);
    }

    public int TotalPostRequests
    {
        get => _totalPostRequests;
        private set => SetProperty(ref _totalPostRequests, value);
    }

    public double AverageProcessingTimeMs
    {
        get => _averageProcessingTimeMs;
        private set => SetProperty(ref _averageProcessingTimeMs, value);
    }

    public PlotModel LoadPlotModel
    {
        get => _loadPlotModel;
        private set => SetProperty(ref _loadPlotModel, value);
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _httpServer.RequestLogged -= AddLogAsync;
        _httpServer.Dispose();
        _httpClient.Dispose();
        _logFileLock.Dispose();
    }

    public async Task ShutdownAsync()
    {
        StopServer();
        await SaveLogsAsync();
    }

    private void StartServer()
    {
        if (!int.TryParse(Port, out var port) || port is < 1 or > 65535)
        {
            ServerStatus = "Введите корректный порт от 1 до 65535.";
            return;
        }

        try
        {
            _httpServer.Start(port);
            IsServerRunning = true;
            ServerStatus = $"Сервер запущен: http://localhost:{port}/";
        }
        catch (HttpListenerException ex)
        {
            ServerStatus = $"Не удалось запустить сервер: {ex.Message}";
        }
        catch (InvalidOperationException ex)
        {
            ServerStatus = $"Не удалось запустить сервер: {ex.Message}";
        }
    }

    private void StopServer()
    {
        _httpServer.Stop();
        IsServerRunning = false;
        ServerStatus = "Сервер остановлен";
    }

    private async Task SendRequestAsync()
    {
        if (!Uri.TryCreate(ClientUrl, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            ClientResponse = "Введите корректный HTTP/HTTPS URL.";
            return;
        }

        if (SelectedMethod == "POST" && !IsValidJson(JsonBody))
        {
            ClientResponse = "JSON-тело POST-запроса некорректно.";
            return;
        }

        using var request = new HttpRequestMessage(new HttpMethod(SelectedMethod), uri);
        if (SelectedMethod == "POST")
        {
            request.Content = new StringContent(JsonBody, Encoding.UTF8, "application/json");
        }

        var stopwatch = Stopwatch.StartNew();
        var requestBody = SelectedMethod == "POST" ? JsonBody : string.Empty;
        var responseBody = string.Empty;
        var statusCode = 0;

        try
        {
            using var response = await _httpClient.SendAsync(request);
            statusCode = (int)response.StatusCode;
            responseBody = await response.Content.ReadAsStringAsync();
            ClientResponse = responseBody;
        }
        catch (Exception ex)
        {
            responseBody = ex.Message;
            ClientResponse = $"Ошибка запроса: {ex.Message}";
        }
        finally
        {
            stopwatch.Stop();
            await AddLogAsync(new LogEntry
            {
                Timestamp = DateTime.Now,
                Source = "Client",
                Method = SelectedMethod,
                Url = uri.ToString(),
                StatusCode = statusCode,
                ProcessingTimeMs = stopwatch.Elapsed.TotalMilliseconds,
                Headers = FormatRequestHeaders(request),
                RequestBody = requestBody,
                ResponseBody = responseBody
            });
        }
    }

    private async Task AddLogAsync(LogEntry entry)
    {
        if (_isDisposed)
        {
            return;
        }

        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            if (_isDisposed)
            {
                return;
            }

            Logs.Add(entry);
            if (MatchesFilters(entry))
            {
                FilteredLogs.Add(entry);
            }

            LogText += entry.DisplayText + Environment.NewLine + Environment.NewLine;
            UpdateStats();
            UpdatePlot();
        });

        await AppendLogToFileAsync(entry);
    }

    private void ApplyFilters()
    {
        FilteredLogs.Clear();
        foreach (var log in Logs.Where(MatchesFilters))
        {
            FilteredLogs.Add(log);
        }
    }

    private bool MatchesFilters(LogEntry entry)
    {
        var methodMatches = SelectedMethodFilter == "Все"
            || entry.Method.Equals(SelectedMethodFilter, StringComparison.OrdinalIgnoreCase);

        var statusMatches = SelectedStatusFilter == "Все"
            || SelectedStatusFilter switch
            {
                "2xx" => entry.StatusCode is >= 200 and <= 299,
                "3xx" => entry.StatusCode is >= 300 and <= 399,
                "4xx" => entry.StatusCode is >= 400 and <= 499,
                "5xx" => entry.StatusCode is >= 500 and <= 599,
                _ => true
            };

        return methodMatches && statusMatches;
    }

    private void UpdateStats()
    {
        TotalGetRequests = Logs.Count(log => log.Method.Equals("GET", StringComparison.OrdinalIgnoreCase));
        TotalPostRequests = Logs.Count(log => log.Method.Equals("POST", StringComparison.OrdinalIgnoreCase));
        AverageProcessingTimeMs = Logs.Count == 0 ? 0 : Logs.Average(log => log.ProcessingTimeMs);
    }

    private void UpdatePlot()
    {
        var endMinute = TruncateToMinute(DateTime.Now);
        var startMinute = endMinute.AddMinutes(-(LoadChartWindowMinutes - 1));

        var countsByMinute = Logs
            .GroupBy(log => TruncateToMinute(log.Timestamp))
            .ToDictionary(group => group.Key, group => group.Count());

        var labels = new List<string>(LoadChartWindowMinutes);
        var maxCount = 0;

        for (var minute = startMinute; minute <= endMinute; minute = minute.AddMinutes(1))
        {
            labels.Add(minute.ToString("HH:mm"));
            var count = countsByMinute.TryGetValue(minute, out var value) ? value : 0;
            maxCount = Math.Max(maxCount, count);
        }

        var categoryAxis = LoadPlotModel.Axes.OfType<CategoryAxis>().First();
        categoryAxis.Labels.Clear();
        foreach (var label in labels)
        {
            categoryAxis.Labels.Add(label);
        }

        categoryAxis.Minimum = -0.5;
        categoryAxis.Maximum = labels.Count - 0.5;

        var valueAxis = LoadPlotModel.Axes.OfType<LinearAxis>().First(axis => axis.Position == AxisPosition.Left);
        valueAxis.Minimum = 0;
        valueAxis.Maximum = Math.Max(5, maxCount + 1);
        valueAxis.MajorStep = valueAxis.Maximum <= 10 ? 1 : Math.Ceiling(valueAxis.Maximum / 5);
        valueAxis.MinorStep = valueAxis.MajorStep <= 1 ? 1 : valueAxis.MajorStep / 2;

        var series = new BarSeries
        {
            Title = "Запросы / мин",
            FillColor = OxyColor.FromRgb(66, 133, 244),
            StrokeColor = OxyColor.FromRgb(41, 98, 209),
            StrokeThickness = 1,
            LabelFormatString = "{0}",
            LabelPlacement = LabelPlacement.Inside,
            LabelColor = OxyColors.White
        };

        for (var i = 0; i < labels.Count; i++)
        {
            var minute = startMinute.AddMinutes(i);
            var count = countsByMinute.TryGetValue(minute, out var value) ? value : 0;
            series.Items.Add(new BarItem { Value = count });
        }

        LoadPlotModel.Series.Clear();
        LoadPlotModel.Series.Add(series);
        LoadPlotModel.InvalidatePlot(true);
    }

    private static DateTime TruncateToMinute(DateTime value) =>
        new(value.Year, value.Month, value.Day, value.Hour, value.Minute, 0);

    private async Task AppendLogToFileAsync(LogEntry entry)
    {
        if (_isDisposed)
        {
            return;
        }

        try
        {
            await _logFileLock.WaitAsync();
        }
        catch (ObjectDisposedException)
        {
            return;
        }

        try
        {
            await File.AppendAllTextAsync(_logFilePath, FormatLogForFile(entry) + Environment.NewLine);
        }
        finally
        {
            try
            {
                _logFileLock.Release();
            }
            catch (ObjectDisposedException)
            {
            }
        }
    }

    private async Task SaveLogsAsync()
    {
        if (_isDisposed)
        {
            return;
        }

        await _logFileLock.WaitAsync();
        try
        {
            var lines = Logs.Select(FormatLogForFile);
            await File.WriteAllLinesAsync(_logFilePath, lines);
            ServerStatus = $"Логи сохранены: {_logFilePath}";
        }
        finally
        {
            try
            {
                _logFileLock.Release();
            }
            catch (ObjectDisposedException)
            {
            }
        }
    }

    private async Task DownloadLogsAsync()
    {
        if (_isDisposed)
        {
            return;
        }

        if (Logs.Count == 0)
        {
            ServerStatus = "Нет записей для скачивания.";
            return;
        }

        var downloadsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Downloads");
        Directory.CreateDirectory(downloadsPath);

        var exportPath = Path.Combine(
            downloadsPath,
            $"http-logs-{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt");

        List<string> lines;
        await _logFileLock.WaitAsync();
        try
        {
            lines = Logs.Select(FormatLogForFile).ToList();
            await File.WriteAllLinesAsync(exportPath, lines);
        }
        catch (Exception ex)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
                ServerStatus = $"Не удалось создать файл логов: {ex.Message}");
            return;
        }
        finally
        {
            try
            {
                _logFileLock.Release();
            }
            catch (ObjectDisposedException)
            {
            }
        }

        await Application.Current.Dispatcher.InvokeAsync(() =>
            ServerStatus = $"Логи скачаны: {exportPath}");

        OpenExportedLogFile(exportPath);
    }

    private static void OpenExportedLogFile(string path)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true
            });
        }
        catch
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"/select,\"{path}\"",
                    UseShellExecute = true
                });
            }
            catch
            {
            }
        }
    }

    private void RaiseServerCommandStates()
    {
        if (StartServerCommand is RelayCommand startCommand)
        {
            startCommand.RaiseCanExecuteChanged();
        }

        if (StopServerCommand is RelayCommand stopCommand)
        {
            stopCommand.RaiseCanExecuteChanged();
        }
    }

    private static PlotModel CreatePlotModel()
    {
        var plotModel = new PlotModel
        {
            Title = "Пиковая нагрузка по минутам",
            PlotMargins = new OxyThickness(52, 4, 16, 56),
            IsLegendVisible = false
        };

        plotModel.Axes.Add(new CategoryAxis
        {
            Position = AxisPosition.Bottom,
            Title = "Время (последние 20 мин)",
            Angle = -35,
            MajorStep = 1,
            GapWidth = 0.25,
            IsZoomEnabled = false,
            IsPanEnabled = false
        });

        plotModel.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Left,
            Title = "Запросов в минуту",
            Minimum = 0,
            Maximum = 5,
            MajorStep = 1,
            MinorStep = 1,
            IsZoomEnabled = false,
            IsPanEnabled = false
        });

        return plotModel;
    }

    private static bool IsValidJson(string json)
    {
        try
        {
            using var _ = JsonDocument.Parse(json);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static string FormatRequestHeaders(HttpRequestMessage request)
    {
        var builder = new StringBuilder();
        foreach (var header in request.Headers)
        {
            builder.Append(header.Key).Append(": ").AppendLine(string.Join(", ", header.Value));
        }

        if (request.Content is not null)
        {
            foreach (var header in request.Content.Headers)
            {
                builder.Append(header.Key).Append(": ").AppendLine(string.Join(", ", header.Value));
            }
        }

        return builder.ToString().TrimEnd();
    }

    private static string FormatLogForFile(LogEntry entry)
    {
        return string.Join(" | ", new[]
        {
            entry.Timestamp.ToString("O"),
            entry.Source,
            entry.Method,
            entry.Url,
            entry.StatusCode.ToString(),
            $"{entry.ProcessingTimeMs:F1} ms",
            $"Headers: {entry.Headers.Replace(Environment.NewLine, "; ")}",
            $"RequestBody: {entry.RequestBody}",
            $"ResponseBody: {entry.ResponseBody}"
        });
    }
}
