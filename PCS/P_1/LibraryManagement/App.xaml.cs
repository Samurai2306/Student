using System.Windows;
using System.Windows.Threading;
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

        // Перехват необработанных исключений — приложение не закрывается молча
        DispatcherUnhandledException += App_DispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

        try
        {
            InitializeDatabase();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "Ошибка при инициализации базы данных:\n\n" + ex.Message + "\n\nПриложение запустится с пустой базой.",
                "Ошибка запуска",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show(
            "Произошла ошибка:\n\n" + e.Exception.Message + "\n\n" + e.Exception.StackTrace,
            "Ошибка",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        e.Handled = true; // Не завершать приложение
    }

    private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            MessageBox.Show("Критическая ошибка:\n\n" + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
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
