using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Data;

/// <summary>
/// Создаёт экземпляры LibraryContext с SQLite. Используется приложением для доступа к данным.
/// </summary>
public static class DbContextFactory
{
    private const string ConnectionString = "Data Source=library.db";

    private static readonly Lazy<LibraryContext> LazyContext = new(() =>
    {
        var options = new DbContextOptionsBuilder<LibraryContext>()
            .UseSqlite(ConnectionString)
            .Options;
        return new LibraryContext(options);
    });

    /// <summary>Возвращает общий экземпляр контекста. В продакшене лучше создавать новый экземпляр на операцию.</summary>
    public static LibraryContext Create() => LazyContext.Value;
}
