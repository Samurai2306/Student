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
                lines.Add("─── ICMP (Ping) ───");
                AppendPingReport(lines, host);
                lines.Add("");
                lines.Add("─── DNS и адреса ───");
                AppendDnsReport(lines, host);
                lines.Add("");
                lines.Add("─── Проверка TCP ───");
                AppendTcpProbeReport(lines, uri);
            }
        }
        catch (UriFormatException ex)
        {
            result.ErrorMessage = "Неверный формат URL: " + ex.Message;
            lines.Add("Ошибка разбора URI: " + ex.Message);
            lines.Add("");
            lines.Add("─── Сеть по введённой строке (как по имени хоста) ───");
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
        lines.Add("═══ Разбор URI ═══");
        lines.Add("Ввод пользователя: " + userInput);
        if (!string.Equals(userInput, stringUsedForParse, StringComparison.Ordinal))
            lines.Add("Добавлена схема для разбора: " + stringUsedForParse);
        lines.Add("AbsoluteUri (канонический вид): " + uri.AbsoluteUri);
        lines.Add("IsAbsoluteUri: " + uri.IsAbsoluteUri);
        lines.Add("Схема (протокол): " + uri.Scheme);
        lines.Add("Тип имени хоста (HostNameType): " + DescribeHostNameType(uri.HostNameType));
        lines.Add("Хост (Host): " + uri.Host);
        if (uri.HostNameType == UriHostNameType.Dns)
        {
            lines.Add("Хост в Unicode (IdnHost): " + uri.IdnHost);
            lines.Add("DnsSafeHost (ASCII / punycode): " + uri.DnsSafeHost);
        }

        lines.Add(DescribeUserInfoLine(uri));
        lines.Add("Authority: " + (string.IsNullOrEmpty(uri.Authority) ? "пусто" : uri.Authority));

        var portDesc = uri.Port < 0
            ? "не задан"
            : uri.IsDefaultPort
                ? $"{uri.Port} (стандартный порт для схемы «{uri.Scheme}»)"
                : $"{uri.Port} (явно указан в URL)";
        lines.Add("Порт (число): " + uri.Port);
        lines.Add("Порт (описание): " + portDesc);
        lines.Add("IsDefaultPort: " + uri.IsDefaultPort);

        var path = string.IsNullOrEmpty(uri.AbsolutePath) ? "/" : uri.AbsolutePath;
        lines.Add("AbsolutePath: " + path);
        lines.Add("LocalPath: " + uri.LocalPath);
        lines.Add("PathAndQuery: " + uri.PathAndQuery);

        if (uri.Segments.Length > 0)
        {
            lines.Add("Сегменты пути (Segments):");
            foreach (var seg in uri.Segments)
            {
                var note = seg.EndsWith('/') ? "каталог" : "ресурс";
                lines.Add("  • " + seg.TrimEnd('\r', '\n') + "  (" + note + ")");
            }
        }
    }

    private static string DescribeHostNameType(UriHostNameType t) => t switch
    {
        UriHostNameType.Basic => "Basic (нестандартное имя)",
        UriHostNameType.Dns => "Dns (доменное имя)",
        UriHostNameType.IPv4 => "IPv4",
        UriHostNameType.IPv6 => "IPv6",
        UriHostNameType.Unknown => "Unknown",
        _ => t.ToString()
    };

    private static string DescribeUserInfoLine(Uri uri)
    {
        var ui = uri.UserInfo;
        if (string.IsNullOrEmpty(ui))
            return "UserInfo (логин:пароль): не указаны";
        var colon = ui.IndexOf(':');
        if (colon < 0)
            return "UserInfo: пользователь «" + Uri.UnescapeDataString(ui) + "» (без пароля в URI)";
        var user = Uri.UnescapeDataString(ui[..colon]);
        return "UserInfo: пользователь «" + user + "», пароль в URI присутствует (не отображается)";
    }

    private static void AppendQueryReport(List<string> lines, Uri uri)
    {
        lines.Add("");
        lines.Add("─── Строка запроса (query) ───");
        var raw = uri.Query;
        if (string.IsNullOrEmpty(raw) || raw == "?")
        {
            lines.Add("Не указана: в URL нет символа ? с параметрами.");
            return;
        }

        lines.Add("Сырой фрагмент Query: " + raw);
        var body = raw.StartsWith('?') ? raw[1..] : raw;
        AppendParsedQueryPairs(lines, body);
    }

    private static void AppendParsedQueryPairs(List<string> lines, string queryBody)
    {
        if (string.IsNullOrEmpty(queryBody))
        {
            lines.Add("После ? строка пуста.");
            return;
        }

        lines.Add("Пары параметр → значение (декодирование %XX и + как пробел):");
        var pairs = queryBody.Split('&', StringSplitOptions.RemoveEmptyEntries);
        foreach (var pair in pairs)
        {
            var eq = pair.IndexOf('=');
            if (eq < 0)
            {
                lines.Add("  • " + DecodeQueryComponent(pair) + " → (флаг без значения)");
                continue;
            }

            var key = DecodeQueryComponent(pair[..eq]);
            var val = DecodeQueryComponent(pair[(eq + 1)..]);
            lines.Add("  • " + key + " → " + val);
        }
    }

    private static string DecodeQueryComponent(string s) =>
        Uri.UnescapeDataString(s.Replace('+', ' '));

    private static void AppendFragmentReport(List<string> lines, Uri uri)
    {
        lines.Add("");
        lines.Add("─── Фрагмент (fragment) ───");
        var f = uri.Fragment;
        if (string.IsNullOrEmpty(f) || f == "#")
        {
            lines.Add("Не указан: в URL нет части после символа #.");
            return;
        }

        lines.Add("Сырой фрагмент: " + f);
        lines.Add("Без символа #: " + f.TrimStart('#'));
    }

    private static void AppendPingReport(List<string> lines, string host)
    {
        try
        {
            using var ping = new Ping();
            var reply = ping.Send(host, 3000);
            lines.Add("Статус: " + reply.Status);
            if (reply.Status == IPStatus.Success)
            {
                lines.Add("Доступен по ICMP.");
                lines.Add("Время отклика (RoundtripTime): " + reply.RoundtripTime + " мс");
                if (reply.Address != null)
                    lines.Add("Ответивший адрес: " + reply.Address + " (" + reply.Address.AddressFamily + ")");
                if (reply.Options != null)
                {
                    lines.Add("TTL: " + reply.Options.Ttl);
                    lines.Add("DontFragment: " + reply.Options.DontFragment);
                }

                if (reply.Buffer is { Length: > 0 })
                    lines.Add("Размер полученного буфера: " + reply.Buffer.Length + " байт");
            }
            else
                lines.Add("Узел не ответил на ping в ожидаемом виде (часто блокируется файрволом).");
        }
        catch (Exception ex)
        {
            lines.Add("Ошибка ping: " + ex.Message);
        }
    }

    private static void AppendDnsReport(List<string> lines, string host)
    {
        try
        {
            var entry = Dns.GetHostEntry(host);
            lines.Add("Каноническое имя (HostName): " + entry.HostName);

            var aliases = entry.Aliases?.Where(a => !string.IsNullOrWhiteSpace(a)).ToArray() ?? Array.Empty<string>();
            if (aliases.Length > 0)
            {
                lines.Add("Псевдонимы (Aliases):");
                foreach (var a in aliases)
                    lines.Add("  • " + a);
            }
            else
                lines.Add("Псевдонимы (Aliases): нет");

            if (entry.AddressList.Length == 0)
            {
                lines.Add("Список IP-адресов пуст.");
                return;
            }

            var v4 = entry.AddressList.Where(a => a.AddressFamily == AddressFamily.InterNetwork).ToArray();
            var v6 = entry.AddressList.Where(a => a.AddressFamily == AddressFamily.InterNetworkV6).ToArray();
            lines.Add("Всего записей AddressList: " + entry.AddressList.Length + " (IPv4: " + v4.Length + ", IPv6: " + v6.Length + ")");

            foreach (var ip in entry.AddressList)
            {
                lines.Add("  • " + ip + "  [" + ip.AddressFamily + "]  — классификация: " + GetAddressType(ip));
                if (ip.AddressFamily == AddressFamily.InterNetworkV6 && ip.ScopeId != 0)
                    lines.Add("      ScopeId (зона интерфейса): " + ip.ScopeId);
            }
        }
        catch (Exception ex)
        {
            lines.Add("Ошибка DNS: " + ex.Message);
            if (IPAddress.TryParse(host, out var ipOnly))
            {
                lines.Add("Ввод распознан как IP без DNS-имени.");
                lines.Add("  • " + ipOnly + " — классификация: " + GetAddressType(ipOnly));
            }
        }
    }

    private static void AppendTcpProbeReport(List<string> lines, Uri uri)
    {
        var scheme = uri.Scheme.ToLowerInvariant();
        if (scheme is not ("http" or "https" or "ftp" or "ws" or "wss"))
        {
            lines.Add("Для схемы «" + uri.Scheme + "» проверка TCP не выполняется (не типовой клиентский порт).");
            return;
        }

        if (uri.Port < 0)
        {
            lines.Add("Порт не определён — TCP не проверялся.");
            return;
        }

        var host = uri.Host;
        if (string.IsNullOrEmpty(host))
        {
            lines.Add("Нет хоста — TCP не проверялся.");
            return;
        }

        try
        {
            using var client = new TcpClient();
            var connectTask = client.ConnectAsync(host, uri.Port);
            if (!connectTask.Wait(2500))
            {
                lines.Add("TCP " + host + ":" + uri.Port + " — таймаут 2500 мс (порт может быть закрыт или фильтруется).");
                return;
            }

            if (client.Connected)
            {
                var ep = (IPEndPoint)client.Client.RemoteEndPoint!;
                lines.Add("TCP-соединение с " + ep.Address + ":" + ep.Port + " установлено успешно.");
            }
            else
                lines.Add("TCP: соединение не установлено.");
        }
        catch (Exception ex)
        {
            lines.Add("TCP " + host + ":" + uri.Port + " — ошибка: " + ex.Message);
        }
    }

    private static string GetAddressType(IPAddress ip)
    {
        if (IPAddress.IsLoopback(ip))
            return "Loopback (локальный контур)";
        var s = ip.ToString();
        if (s.StartsWith("192.168.", StringComparison.Ordinal) || s.StartsWith("10.", StringComparison.Ordinal) ||
            s.StartsWith("172.16.", StringComparison.Ordinal) || s.StartsWith("172.17.", StringComparison.Ordinal) ||
            s.StartsWith("172.18.", StringComparison.Ordinal) || s.StartsWith("172.19.", StringComparison.Ordinal) ||
            IsPrivate172(s) ||
            s.StartsWith("172.30.", StringComparison.Ordinal) || s.StartsWith("172.31.", StringComparison.Ordinal))
            return "Локальный (частный IPv4)";
        if (ip.AddressFamily == AddressFamily.InterNetworkV6)
        {
            if (s.StartsWith("fe80", StringComparison.OrdinalIgnoreCase) || ip.IsIPv6LinkLocal)
                return "Локальный IPv6 (link-local)";
            if (ip.IsIPv6SiteLocal)
                return "IPv6 site-local (устар.)";
        }

        return "Публичный / глобальный маршрутизируемый";
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
                var gap = new Paragraph { Margin = new Thickness(0, 8, 0, 0) };
                gap.Inlines.Add(new LineBreak());
                UrlResultsDoc.Blocks.Add(gap);
                continue;
            }

            var run = new Run(line);
            var para = new Paragraph(run) { KeepTogether = true, LineHeight = 20 };

            if (line.Contains("═══", StringComparison.Ordinal))
            {
                run.FontWeight = FontWeights.SemiBold;
                run.FontSize = 14;
                run.Foreground = headerBrush;
                para.Margin = new Thickness(0, 16, 0, 8);
            }
            else if (line.StartsWith("───", StringComparison.Ordinal))
            {
                run.FontWeight = FontWeights.SemiBold;
                run.Foreground = subHeaderBrush;
                para.Margin = new Thickness(0, 12, 0, 6);
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
