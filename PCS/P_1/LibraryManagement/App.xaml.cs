using System.Windows;
using LibraryManagement.Data;
using LibraryManagement.Models;

namespace LibraryManagement;

/// <summary>
/// Точка входа приложения. Создаёт базу при необходимости и при первом запуске заполняет тестовыми данными.
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        InitializeDatabase();
    }

    /// <summary>
    /// Создаёт базу данных (EnsureCreated для простоты; в продакшене лучше использовать миграции).
    /// Заполняет тестовыми авторами, жанрами и книгами, если база пуста.
    /// </summary>
    private static void InitializeDatabase()
    {
        using var context = DbContextFactory.Create();
        context.Database.EnsureCreated();

        if (context.Books.Any())
            return;

        // Тестовые данные при первом запуске
        var author1 = new Author
        {
            FirstName = "Jane",
            LastName = "Austen",
            BirthDate = new DateTime(1775, 12, 16),
            Country = "United Kingdom"
        };
        var author2 = new Author
        {
            FirstName = "George",
            LastName = "Orwell",
            BirthDate = new DateTime(1903, 6, 25),
            Country = "United Kingdom"
        };
        context.Authors.AddRange(author1, author2);

        var genreFiction = new Genre { Name = "Fiction", Description = "Fictional works" };
        var genreDystopian = new Genre { Name = "Dystopian", Description = "Dystopian fiction" };
        context.Genres.AddRange(genreFiction, genreDystopian);
        context.SaveChanges();

        context.Books.AddRange(
            new Book
            {
                Title = "Pride and Prejudice",
                Author = author1,
                Genre = genreFiction,
                PublishYear = 1813,
                ISBN = "978-0-14-143951-8",
                QuantityInStock = 5
            },
            new Book
            {
                Title = "1984",
                Author = author2,
                Genre = genreDystopian,
                PublishYear = 1949,
                ISBN = "978-0-452-28423-4",
                QuantityInStock = 3
            });
        context.SaveChanges();
    }
}
