namespace NetworkAnalyzer.Models;

/// <summary>
/// Результат разбора URL и дополнительная информация (Ping, DNS, тип адреса).
/// </summary>
public class UrlParseResult
{
    public string Scheme { get; set; } = "";
    public string Host { get; set; } = "";
    public string Port { get; set; } = "";
    public string Path { get; set; } = "";
    public string Query { get; set; } = "";
    public string Fragment { get; set; } = "";

    public string PingStatus { get; set; } = "";
    public string DnsInfo { get; set; } = "";
    public string AddressType { get; set; } = "";
    public string ErrorMessage { get; set; } = "";
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
}
