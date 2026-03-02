using System.IO;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Data;

/// <summary>
/// Создаёт экземпляры LibraryContext с SQLite. Используется приложением для доступа к данным.
/// </summary>
public static class DbContextFactory
{
    /// <summary>Путь к БД рядом с исполняемым файлом — иначе при разном текущем каталоге приложение может падать или создавать лишние файлы.</summary>
    private static string ConnectionString => "Data Source=" + Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "library.db");

    /// <summary>Создаёт новый экземпляр контекста на каждый вызов. Вызывающий код должен вызывать Dispose (например, через using).</summary>
    public static LibraryContext Create()
    {
        var options = new DbContextOptionsBuilder<LibraryContext>()
            .UseSqlite(ConnectionString)
            .Options;
        return new LibraryContext(options);
    }
}
