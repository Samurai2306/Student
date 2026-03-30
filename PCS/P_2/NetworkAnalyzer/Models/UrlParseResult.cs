namespace NetworkAnalyzer.Models;


/// Результат разбора URL и сетевой проверки (многострочный отчёт).

public class UrlParseResult
{
    public string ErrorMessage { get; set; } = "";
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    /// <summary>Все строки для вывода в блок «Результаты анализа».</summary>
    public List<string> ReportLines { get; set; } = new();
}
