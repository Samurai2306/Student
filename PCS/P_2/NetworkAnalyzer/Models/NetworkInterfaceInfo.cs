namespace NetworkAnalyzer.Models;

/// <summary>
/// Модель для отображения информации о сетевом интерфейсе.
/// </summary>
public class NetworkInterfaceInfo
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string IpAddress { get; set; } = "—";
    public string SubnetMask { get; set; } = "—";
    public string MacAddress { get; set; } = "—";
    public string Status { get; set; } = "";
    public string Speed { get; set; } = "—";
    public string InterfaceType { get; set; } = "";

    /// <summary>
    /// Исходный объект NetworkInterface для привязки в списке (DisplayMemberPath = Name).
    /// </summary>
    public System.Net.NetworkInformation.NetworkInterface? Source { get; set; }
}
