using System.Windows;
using LibraryManagement.Data;
using LibraryManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement;

/// <summary>
/// Диалог добавления новой книги или редактирования существующей.
/// Загружает авторов и жанры из БД; сохраняет книгу и устанавливает DialogResult = true при успехе.
/// </summary>
public partial class AddEditBookWindow : Window
{
    private readonly Book? _existingBook;
    private List<Author> _authors = new();
    private List<Genre> _genres = new();

    /// <param name="book">null — режим добавления; существующая книга — режим редактирования.</param>
    public AddEditBookWindow(Book? book)
    {
        InitializeComponent();
        _existingBook = book;
        Title = book == null ? "Добавить книгу" : "Изменить книгу";
        LoadAuthorsAndGenres();
        if (book != null)
            LoadBook(book);
    }

    private void LoadAuthorsAndGenres()
    {
        using var context = DbContextFactory.Create();
        _authors = context.Authors.OrderBy(a => a.LastName).ToList();
        _genres = context.Genres.OrderBy(g => g.Name).ToList();
        AuthorCombo.ItemsSource = _authors;
        GenreCombo.ItemsSource = _genres;
    }

    private void LoadBook(Book book)
    {
        TitleBox.Text = book.Title;
        PublishYearBox.Text = book.PublishYear?.ToString() ?? "";
        IsbnBox.Text = book.ISBN ?? "";
        QuantityBox.Text = book.QuantityInStock.ToString();
        AuthorCombo.SelectedItem = _authors.FirstOrDefault(a => a.Id == book.AuthorId);
        GenreCombo.SelectedItem = _genres.FirstOrDefault(g => g.Id == book.GenreId);
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TitleBox.Text))
        {
            MessageBox.Show("Укажите название книги.", "Проверка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (AuthorCombo.SelectedItem is not Author author)
        {
            MessageBox.Show("Выберите автора.", "Проверка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (GenreCombo.SelectedItem is not Genre genre)
        {
            MessageBox.Show("Выберите жанр.", "Проверка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (!int.TryParse(QuantityBox.Text, out var quantity) || quantity < 0)
        {
            MessageBox.Show("Количество должно быть неотрицательным числом.", "Проверка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        int? publishYear = null;
        if (!string.IsNullOrWhiteSpace(PublishYearBox.Text) && int.TryParse(PublishYearBox.Text, out var year))
            publishYear = year;

        try
        {
            using var context = DbContextFactory.Create();
            if (_existingBook != null)
            {
                var book = context.Books.Include(b => b.Author).Include(b => b.Genre).FirstOrDefault(b => b.Id == _existingBook.Id);
                if (book != null)
                {
                    book.Title = TitleBox.Text.Trim();
                    book.AuthorId = author.Id;
                    book.GenreId = genre.Id;
                    book.PublishYear = publishYear;
                    book.ISBN = string.IsNullOrWhiteSpace(IsbnBox.Text) ? null : IsbnBox.Text.Trim();
                    book.QuantityInStock = quantity;
                    context.SaveChanges();
                }
            }
            else
            {
                context.Books.Add(new Book
                {
                    Title = TitleBox.Text.Trim(),
                    AuthorId = author.Id,
                    GenreId = genre.Id,
                    PublishYear = publishYear,
                    ISBN = string.IsNullOrWhiteSpace(IsbnBox.Text) ? null : IsbnBox.Text.Trim(),
                    QuantityInStock = quantity
                });
                context.SaveChanges();
            }
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при сохранении книги: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
