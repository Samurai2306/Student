using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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

                // Все IP-адреса интерфейса (IPv4/IPv6)
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

                // DNS‑серверы
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

                // Статистика интерфейса
                var stats = ni.GetIPStatistics();
                lines.Add("Статистика трафика (всего с момента запуска системы):");
                lines.Add($"  Отправлено байт: {stats.BytesSent}");
                lines.Add($"  Получено байт: {stats.BytesReceived}");
                lines.Add($"  Отправлено пакетов: {stats.UnicastPacketsSent}");
                lines.Add($"  Получено пакетов: {stats.UnicastPacketsReceived}");
            }
            catch
            {
                // Игнорируем ошибки получения расширенной информации
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

        // Попытка разобрать как URI (добавляем схему при необходимости)
        string toParse = input;
        if (!toParse.Contains("://") && !toParse.StartsWith("//"))
        {
            if (toParse.Contains(".") || input.Contains(":"))
                toParse = "https://" + toParse;
            else
                toParse = "https://" + toParse;
        }

        try
        {
            var uri = new Uri(toParse);
            result.Scheme = uri.Scheme;
            result.Host = uri.Host;
            result.Port = uri.IsDefaultPort ? "(по умолчанию)" : uri.Port.ToString();
            result.Path = string.IsNullOrEmpty(uri.AbsolutePath) ? "/" : uri.AbsolutePath;
            result.Query = uri.Query.TrimStart('?');
            result.Fragment = uri.Fragment.TrimStart('#');

            string hostForPing = uri.Host;
            if (!string.IsNullOrEmpty(hostForPing))
            {
                result.PingStatus = GetPingStatus(hostForPing);
                result.DnsInfo = GetDnsInfo(hostForPing);
                result.AddressType = GetAddressTypeFromHost(hostForPing);
            }
        }
        catch (UriFormatException ex)
        {
            result.ErrorMessage = "Неверный формат URL: " + ex.Message;
            // Пробуем как хост
            result.PingStatus = GetPingStatus(input);
            result.DnsInfo = GetDnsInfo(input);
            result.AddressType = GetAddressTypeFromHost(input);
        }
        catch (Exception ex)
        {
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    private static string GetPingStatus(string host)
    {
        try
        {
            using var ping = new Ping();
            var reply = ping.Send(host, 3000);
            if (reply.Status == IPStatus.Success)
            {
                var ttl = reply.Options?.Ttl;
                var ttlPart = ttl.HasValue ? $", TTL: {ttl}" : string.Empty;
                return $"Доступен (время ответа: {reply.RoundtripTime} мс{ttlPart})";
            }
            return $"Не доступен: {reply.Status}";
        }
        catch (Exception ex)
        {
            return "Ошибка: " + ex.Message;
        }
    }

    private static string GetDnsInfo(string host)
    {
        try
        {
            var entry = Dns.GetHostEntry(host);
            if (entry.AddressList.Length == 0)
                return "—";

            var ips = entry.AddressList.Select(a => a.ToString()).ToArray();
            var reverseName = !string.IsNullOrWhiteSpace(entry.HostName) ? entry.HostName : null;

            var parts = new List<string> { "IP: " + string.Join(", ", ips) };
            if (reverseName != null)
                parts.Add("обратное имя: " + reverseName);

            return string.Join("; ", parts);
        }
        catch (Exception ex)
        {
            return "Ошибка: " + ex.Message;
        }
    }

    private static string GetAddressTypeFromHost(string host)
    {
        try
        {
            var entry = Dns.GetHostEntry(host);
            if (entry.AddressList.Length == 0)
                return "—";
            var first = entry.AddressList.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork)
                        ?? entry.AddressList.FirstOrDefault();
            if (first == null)
                return "—";
            return GetAddressType(first);
        }
        catch
        {
            if (IPAddress.TryParse(host, out var ip))
                return GetAddressType(ip);
            return "—";
        }
    }

    private static string GetAddressType(IPAddress ip)
    {
        if (IPAddress.IsLoopback(ip))
            return "Loopback (локальный контур)";
        var s = ip.ToString();
        if (s.StartsWith("192.168.") || s.StartsWith("10.") || s.StartsWith("172.16.") || s.StartsWith("172.17.") ||
            s.StartsWith("172.18.") || s.StartsWith("172.19.") || s.StartsWith("172.2") || s.StartsWith("172.30.") || s.StartsWith("172.31."))
            return "Локальный (частный)";
        if (ip.AddressFamily == AddressFamily.InterNetworkV6 && s.StartsWith("fe80"))
            return "Локальный IPv6 (link-local)";
        return "Публичный";
    }

    private void ShowUrlResult(UrlParseResult r)
    {
        var lines = new List<string>();
        if (r.HasError)
            lines.Add("Ошибка: " + r.ErrorMessage);
        lines.Add("Схема (протокол): " + r.Scheme);
        lines.Add("Хост: " + r.Host);
        lines.Add("Порт: " + r.Port);
        lines.Add("Путь: " + r.Path);
        lines.Add("Параметры запроса: " + (string.IsNullOrEmpty(r.Query) ? "—" : r.Query));
        lines.Add("Фрагмент: " + (string.IsNullOrEmpty(r.Fragment) ? "—" : r.Fragment));
        lines.Add("");
        lines.Add("Ping: " + r.PingStatus);
        lines.Add("DNS (IP): " + r.DnsInfo);
        lines.Add("Тип адреса: " + r.AddressType);
        UrlResultsText.Text = string.Join("\n", lines);
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
