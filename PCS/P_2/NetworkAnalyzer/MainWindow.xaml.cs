using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using NetworkAnalyzer.Models;

namespace NetworkAnalyzer;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<NetworkInterfaceInfo> _interfaces = new();
    private readonly ObservableCollection<string> _urlHistory = new();
    private const string HistoryFileName = "url_history.txt";

    public MainWindow()
    {
        InitializeComponent();
        InterfacesListBox.ItemsSource = _interfaces;
        UrlHistoryListBox.ItemsSource = _urlHistory;
        LoadNetworkInterfaces();
        LoadUrlHistory();
    }

    private void LoadNetworkInterfaces()
    {
        _interfaces.Clear();
        try
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces();
            foreach (var ni in interfaces)
            {
                var info = BuildInterfaceInfo(ni);
                _interfaces.Add(info);
            }
        }
        catch (Exception ex)
        {
            _interfaces.Add(new NetworkInterfaceInfo
            {
                Name = "Ошибка",
                Description = ex.Message
            });
        }
    }

    private static NetworkInterfaceInfo BuildInterfaceInfo(NetworkInterface ni)
    {
        string ip = "—", mask = "—";
        var ipProps = ni.GetIPProperties();
        foreach (UnicastIPAddressInformation addr in ipProps.UnicastAddresses)
        {
            if (addr.Address.AddressFamily != AddressFamily.InterNetwork)
                continue;
            ip = addr.Address.ToString();
            mask = addr.IPv4Mask?.ToString() ?? "—";
            break;
        }

        var macBytes = ni.GetPhysicalAddress().GetAddressBytes();
        string mac = macBytes.Length == 0
            ? "—"
            : string.Join(":", macBytes.Select(b => b.ToString("X2")));

        string speed = ni.Speed > 0
            ? (ni.Speed / 1_000_000) + " Мбит/с"
            : "—";

        return new NetworkInterfaceInfo
        {
            Name = ni.Name,
            Description = ni.Description,
            IpAddress = ip,
            SubnetMask = mask,
            MacAddress = mac,
            Status = ni.OperationalStatus.ToString(),
            Speed = speed,
            InterfaceType = ni.NetworkInterfaceType.ToString(),
            Source = ni
        };
    }

    private void InterfacesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (InterfacesListBox.SelectedItem is not NetworkInterfaceInfo info)
        {
            InterfaceDetailsText.Text = "";
            return;
        }

        var lines = new List<string>
        {
            $"Имя: {info.Name}",
            $"Описание: {info.Description}",
            $"Тип: {info.InterfaceType}",
            $"Состояние: {info.Status}",
            $"Скорость: {info.Speed}",
            $"MAC-адрес: {info.MacAddress}",
            $"IP-адрес (основной): {info.IpAddress}",
            $"Маска подсети: {info.SubnetMask}"
        };

        if (info.Source is NetworkInterface ni)
        {
            try
            {
                var ipProps = ni.GetIPProperties();

                // Все IP-адреса интерфейса
                var allIps = ipProps.UnicastAddresses
                    .Select(a => $"{a.Address} ({a.Address.AddressFamily})")
                    .ToArray();
                if (allIps.Length > 0)
                {
                    lines.Add("");
                    lines.Add("Все IP-адреса:");
                    foreach (var addr in allIps)
                        lines.Add("  • " + addr);
                }

                // Шлюзы
                var gateways = ipProps.GatewayAddresses
                    .Select(g => g.Address.ToString())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToArray();
                if (gateways.Length > 0)
                {
                    lines.Add("");
                    lines.Add("Шлюзы (Gateway):");
                    foreach (var g in gateways)
                        lines.Add("  • " + g);
                }

                // DNS‑серваки
                var dnsServers = ipProps.DnsAddresses
                    .Select(d => d.ToString())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToArray();
                if (dnsServers.Length > 0)
                {
                    lines.Add("");
                    lines.Add("DNS‑серверы:");
                    foreach (var d in dnsServers)
                        lines.Add("  • " + d);
                }

                // MTU
                lines.Add("");
                lines.Add($"MTU: {ipProps.GetIPv4Properties()?.Mtu.ToString() ?? "—"}");

                var stats = ni.GetIPStatistics();
                lines.Add("Статистика трафика (всего с момента запуска системы):");
                lines.Add($"  Отправлено байт: {stats.BytesSent}");
                lines.Add($"  Получено байт: {stats.BytesReceived}");
                lines.Add($"  Отправлено пакетов: {stats.UnicastPacketsSent}");
                lines.Add($"  Получено пакетов: {stats.UnicastPacketsReceived}");
            }
            catch
            {
                // Игнорируем ошибки
            }
        }

        InterfaceDetailsText.Text = string.Join("\n", lines);
    }

    private void UrlTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            AnalyzeUrl();
    }

    private void AnalyzeUrlButton_Click(object sender, RoutedEventArgs e) => AnalyzeUrl();

    private void AnalyzeUrl()
    {
        string input = UrlTextBox.Text?.Trim() ?? "";
        if (string.IsNullOrEmpty(input))
        {
            MessageBox.Show("Введите URL или адрес для анализа.", "Ввод", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var result = ParseAndAnalyzeUrl(input);
        ShowUrlResult(result);
        AddToHistory(input);
    }

    private static UrlParseResult ParseAndAnalyzeUrl(string input)
    {
        var result = new UrlParseResult();
        var lines = new List<string>();

        string toParse = input;
        if (!toParse.Contains("://", StringComparison.Ordinal) && !toParse.StartsWith("//", StringComparison.Ordinal))
            toParse = "https://" + toParse;

        try
        {
            var uri = new Uri(toParse);
            AppendUriReport(lines, uri, input, toParse);
            AppendQueryReport(lines, uri);
            AppendFragmentReport(lines, uri);

            var host = uri.Host;
            if (!string.IsNullOrEmpty(host))
            {
                lines.Add("");
                lines.Add("── Ping ──");
                AppendPingReport(lines, host);
                lines.Add("");
                lines.Add("── DNS ──");
                AppendDnsReport(lines, host);
                lines.Add("");
                lines.Add("── TCP ──");
                AppendTcpProbeReport(lines, uri);
            }
        }
        catch (UriFormatException ex)
        {
            result.ErrorMessage = "Неверный формат URL: " + ex.Message;
            lines.Add("Ошибка разбора URI: " + ex.Message);
            lines.Add("");
            lines.Add("── Сеть (ввод как хост) ──");
            AppendPingReport(lines, input);
            lines.Add("");
            AppendDnsReport(lines, input);
        }
        catch (Exception ex)
        {
            result.ErrorMessage = ex.Message;
            lines.Add("Ошибка: " + ex.Message);
        }

        result.ReportLines = lines;
        return result;
    }

    private static void AppendUriReport(List<string> lines, Uri uri, string userInput, string stringUsedForParse)
    {
        lines.Add("── URI ──");
        if (!string.Equals(userInput, stringUsedForParse, StringComparison.Ordinal))
            lines.Add("Ввод: " + userInput + " → разбор: " + stringUsedForParse);
        else
            lines.Add("Ввод: " + userInput);

        lines.Add("URL: " + uri.AbsoluteUri);
        lines.Add("Схема: " + uri.Scheme + ", хост: " + uri.Host + " (" + ShortHostKind(uri.HostNameType) + ")");
        if (uri.HostNameType == UriHostNameType.Dns &&
            !string.Equals(uri.IdnHost, uri.DnsSafeHost, StringComparison.Ordinal))
            lines.Add("Punycode: " + uri.DnsSafeHost);

        lines.Add(ShortUserInfo(uri));

        var portLine = uri.Port < 0
            ? "Порт: не задан"
            : uri.IsDefaultPort
                ? "Порт: " + uri.Port + " (стандарт для " + uri.Scheme + ")"
                : "Порт: " + uri.Port + " (в строке URL)";
        lines.Add(portLine);

        var path = string.IsNullOrEmpty(uri.AbsolutePath) ? "/" : uri.AbsolutePath;
        lines.Add("Путь: " + path);
        var queryBody = uri.Query.TrimStart('?');
        if (queryBody.Length > 0)
            lines.Add("Путь + query: " + uri.PathAndQuery);
    }

    private static string ShortHostKind(UriHostNameType t) => t switch
    {
        UriHostNameType.Dns => "домен",
        UriHostNameType.IPv4 => "IPv4",
        UriHostNameType.IPv6 => "IPv6",
        UriHostNameType.Basic => "другое",
        _ => "?"
    };

    private static string ShortUserInfo(Uri uri)
    {
        var ui = uri.UserInfo;
        if (string.IsNullOrEmpty(ui))
            return "Логин в URL: нет";
        var colon = ui.IndexOf(':');
        if (colon < 0)
            return "Логин в URL: " + Uri.UnescapeDataString(ui);
        return "Логин в URL: " + Uri.UnescapeDataString(ui[..colon]) + " (пароль скрыт)";
    }

    private static void AppendQueryReport(List<string> lines, Uri uri)
    {
        var raw = uri.Query;
        if (string.IsNullOrEmpty(raw) || raw == "?")
            return;

        var body = raw.StartsWith('?') ? raw[1..] : raw;
        if (string.IsNullOrEmpty(body))
            return;

        lines.Add("");
        lines.Add("── Query ──");
        var parts = new List<string>();
        foreach (var pair in body.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var eq = pair.IndexOf('=');
            if (eq < 0)
                parts.Add(DecodeQueryComponent(pair));
            else
                parts.Add(DecodeQueryComponent(pair[..eq]) + "=" + DecodeQueryComponent(pair[(eq + 1)..]));
        }

        lines.Add(string.Join("; ", parts));
    }

    private static string DecodeQueryComponent(string s) =>
        Uri.UnescapeDataString(s.Replace('+', ' '));

    private static void AppendFragmentReport(List<string> lines, Uri uri)
    {
        var f = uri.Fragment;
        if (string.IsNullOrEmpty(f) || f == "#")
            return;

        lines.Add("");
        lines.Add("── Фрагмент ──");
        lines.Add(f.TrimStart('#'));
    }

    private static void AppendPingReport(List<string> lines, string host)
    {
        try
        {
            using var ping = new Ping();
            var reply = ping.Send(host, 3000);
            if (reply.Status == IPStatus.Success)
            {
                var ip = reply.Address?.ToString() ?? "?";
                var ttl = reply.Options?.Ttl.ToString() ?? "?";
                lines.Add("Есть ответ: " + reply.RoundtripTime + " мс, TTL " + ttl + ", с " + ip);
            }
            else
                lines.Add("Нет ответа: " + reply.Status + " (часто ICMP режется)");
        }
        catch (Exception ex)
        {
            lines.Add("Ping: " + ex.Message);
        }
    }

    private static void AppendDnsReport(List<string> lines, string host)
    {
        try
        {
            var entry = Dns.GetHostEntry(host);
            lines.Add("Имя: " + entry.HostName);

            var aliases = entry.Aliases?.Where(a => !string.IsNullOrWhiteSpace(a)).ToArray() ?? Array.Empty<string>();
            if (aliases.Length > 0)
                lines.Add("Псевдонимы: " + string.Join(", ", aliases));

            if (entry.AddressList.Length == 0)
            {
                lines.Add("IP: нет");
                return;
            }

            var summary = string.Join(", ",
                entry.AddressList.Select(ip => ip + " (" + ShortAddressKind(ip) + ")"));
            lines.Add("IP: " + summary);
        }
        catch (Exception ex)
        {
            lines.Add("DNS: " + ex.Message);
            if (IPAddress.TryParse(host, out var ipOnly))
                lines.Add("Это IP: " + ipOnly + " (" + ShortAddressKind(ipOnly) + ")");
        }
    }

    private static void AppendTcpProbeReport(List<string> lines, Uri uri)
    {
        var scheme = uri.Scheme.ToLowerInvariant();
        if (scheme is not ("http" or "https" or "ftp" or "ws" or "wss"))
        {
            lines.Add("Для «" + uri.Scheme + "» проверка TCP не делается");
            return;
        }

        if (uri.Port < 0 || string.IsNullOrEmpty(uri.Host))
        {
            lines.Add("TCP: пропущено");
            return;
        }

        var host = uri.Host;
        try
        {
            using var client = new TcpClient();
            var connectTask = client.ConnectAsync(host, uri.Port);
            if (!connectTask.Wait(2500))
            {
                lines.Add("TCP " + uri.Port + ": таймаут (закрыт или фильтр)");
                return;
            }

            lines.Add(client.Connected ? "TCP " + uri.Port + ": ок" : "TCP: нет соединения");
        }
        catch (Exception ex)
        {
            lines.Add("TCP: " + ex.Message);
        }
    }

    private static string ShortAddressKind(IPAddress ip)
    {
        if (IPAddress.IsLoopback(ip))
            return "loopback";
        var s = ip.ToString();
        if (s.StartsWith("192.168.", StringComparison.Ordinal) || s.StartsWith("10.", StringComparison.Ordinal) ||
            s.StartsWith("172.16.", StringComparison.Ordinal) || s.StartsWith("172.17.", StringComparison.Ordinal) ||
            s.StartsWith("172.18.", StringComparison.Ordinal) || s.StartsWith("172.19.", StringComparison.Ordinal) ||
            IsPrivate172(s) ||
            s.StartsWith("172.30.", StringComparison.Ordinal) || s.StartsWith("172.31.", StringComparison.Ordinal))
            return "частный";
        if (ip.AddressFamily == AddressFamily.InterNetworkV6 &&
            (s.StartsWith("fe80", StringComparison.OrdinalIgnoreCase) || ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal))
            return "локальный IPv6";

        return "публичный";
    }

    private static bool IsPrivate172(string s)
    {
        if (!s.StartsWith("172.", StringComparison.Ordinal) || s.Length < 7)
            return false;
        var rest = s.AsSpan(4);
        var dot = rest.IndexOf('.');
        if (dot <= 0)
            return false;
        if (!int.TryParse(rest[..dot], out var second))
            return false;
        return second is >= 16 and <= 31;
    }

    private void ShowUrlResult(UrlParseResult r)
    {
        var lines = new List<string>();
        if (r.HasError && r.ReportLines.Count == 0)
            lines.Add("Ошибка: " + r.ErrorMessage);
        else
            lines.AddRange(r.ReportLines);

        UrlResultsDoc.Blocks.Clear();
        var headerBrush = new SolidColorBrush(Color.FromRgb(0x1a, 0x5f, 0x9c));
        var subHeaderBrush = new SolidColorBrush(Color.FromRgb(0x2d, 0x37, 0x4a));
        var bulletBrush = new SolidColorBrush(Color.FromRgb(0x4a, 0x55, 0x66));

        foreach (var raw in lines)
        {
            var line = raw ?? "";
            if (string.IsNullOrWhiteSpace(line))
            {
                var gap = new Paragraph { Margin = new Thickness(0, 4, 0, 0) };
                gap.Inlines.Add(new LineBreak());
                UrlResultsDoc.Blocks.Add(gap);
                continue;
            }

            var run = new Run(line);
            var para = new Paragraph(run) { KeepTogether = true, LineHeight = 19 };

            var trimmed = line.Trim();
            if (trimmed.Length >= 6 && trimmed.StartsWith("──", StringComparison.Ordinal) &&
                trimmed.EndsWith("──", StringComparison.Ordinal))
            {
                run.FontWeight = FontWeights.SemiBold;
                run.Foreground = trimmed.Contains("URI", StringComparison.Ordinal) ? headerBrush : subHeaderBrush;
                run.FontSize = trimmed.Contains("URI", StringComparison.Ordinal) ? 13.5 : 13;
                para.Margin = new Thickness(0, 10, 0, 4);
            }
            else if (line.StartsWith("  •", StringComparison.Ordinal) || line.StartsWith("    ", StringComparison.Ordinal))
            {
                run.Foreground = bulletBrush;
                para.Margin = new Thickness(14, 0, 0, 3);
                para.TextIndent = 0;
            }
            else
                para.Margin = new Thickness(0, 0, 0, 5);

            UrlResultsDoc.Blocks.Add(para);
        }
    }

    private void AddToHistory(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return;
        if (_urlHistory.Contains(url))
            _urlHistory.Remove(url);
        _urlHistory.Insert(0, url);
        SaveUrlHistory();
    }

    private void UrlHistoryListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (UrlHistoryListBox.SelectedItem is string s)
            UrlTextBox.Text = s;
    }

    private void LoadUrlHistory()
    {
        try
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, HistoryFileName);
            if (!File.Exists(path))
                return;
            foreach (var line in File.ReadAllLines(path))
            {
                var t = line.Trim();
                if (!string.IsNullOrEmpty(t) && !_urlHistory.Contains(t))
                    _urlHistory.Add(t);
            }
        }
        catch { /* игнор */ }
    }

    private void SaveUrlHistory()
    {
        try
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, HistoryFileName);
            File.WriteAllLines(path, _urlHistory.Take(100));
        }
        catch { /* игнор */ }
    }
}
