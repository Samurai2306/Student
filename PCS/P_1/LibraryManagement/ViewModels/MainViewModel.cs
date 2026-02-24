using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using LibraryManagement.Data;
using LibraryManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.ViewModels;

/// <summary>
/// ViewModel главного окна: список книг с фильтрами по жанру/автору и командами Добавить/Изменить/Удалить.
/// Логика фильтра: при смене фильтра по жанру или автору таблица показывает только подходящие книги;
/// значение «Все» означает отсутствие фильтра по этому критерию.
/// </summary>
public class MainViewModel : INotifyPropertyChanged
{
    private Book? _selectedBook;
    private Genre? _selectedGenreFilter;
    private Author? _selectedAuthorFilter;
    private readonly ObservableCollection<Book> _books = new();
    private readonly ObservableCollection<Author> _authors = new();
    private readonly ObservableCollection<Genre> _genres = new();
    private readonly ObservableCollection<Genre?> _genreFilterOptions = new();
    private readonly ObservableCollection<Author?> _authorFilterOptions = new();
    private ICollectionView? _booksView;
    private Action? _refreshGridCallback;
    private Action? _addBookCallback;
    private Action<Book>? _editBookCallback;

    public MainViewModel()
    {
        BooksView = CollectionViewSource.GetDefaultView(_books);
        BooksView.Filter = FilterBook;
        AddBookCommand = new RelayCommand(_ => _addBookCallback?.Invoke());
        EditBookCommand = new RelayCommand(_ => { if (SelectedBook != null) _editBookCallback?.Invoke(SelectedBook); }, _ => SelectedBook != null);
        DeleteBookCommand = new RelayCommand(_ => OnDeleteBook(), _ => SelectedBook != null);
    }

    /// <summary>Задаёт обработчики для Добавить/Изменить: вид открывает диалоги и затем обновляет таблицу.</summary>
    public void SetAddEditCallbacks(Action addBook, Action<Book> editBook, Action refresh)
    {
        _addBookCallback = addBook;
        _editBookCallback = editBook;
        _refreshGridCallback = refresh;
    }

    /// <summary>Условие фильтра: показывать книгу только если она совпадает по выбранному жанру и автору (или фильтр «Все»).</summary>
    private bool FilterBook(object obj)
    {
        if (obj is not Book book) return false;
        if (_selectedGenreFilter != null && book.GenreId != _selectedGenreFilter.Id) return false;
        if (_selectedAuthorFilter != null && book.AuthorId != _selectedAuthorFilter.Id) return false;
        return true;
    }

    public ICollectionView BooksView
    {
        get => _booksView!;
        private set
        {
            _booksView = value;
            OnPropertyChanged(nameof(BooksView));
        }
    }

    public ObservableCollection<Author> Authors => _authors;
    public ObservableCollection<Genre> Genres => _genres;
    /// <summary>Жанры для фильтра: null (Все) плюс каждый жанр.</summary>
    public ObservableCollection<Genre?> GenreFilterOptions => _genreFilterOptions;
    /// <summary>Авторы для фильтра: null (Все) плюс каждый автор.</summary>
    public ObservableCollection<Author?> AuthorFilterOptions => _authorFilterOptions;

    public Book? SelectedBook
    {
        get => _selectedBook;
        set
        {
            _selectedBook = value;
            OnPropertyChanged(nameof(SelectedBook));
            CommandManager.InvalidateRequerySuggested();
        }
    }

    /// <summary>Если задан — в таблице только книги этого жанра; null означает «Все».</summary>
    public Genre? SelectedGenreFilter
    {
        get => _selectedGenreFilter;
        set
        {
            _selectedGenreFilter = value;
            OnPropertyChanged(nameof(SelectedGenreFilter));
            RefreshFilter();
        }
    }

    /// <summary>Если задан — в таблице только книги этого автора; null означает «Все».</summary>
    public Author? SelectedAuthorFilter
    {
        get => _selectedAuthorFilter;
        set
        {
            _selectedAuthorFilter = value;
            OnPropertyChanged(nameof(SelectedAuthorFilter));
            RefreshFilter();
        }
    }

    private void RefreshFilter()
    {
        _booksView?.Refresh();
    }

    public ICommand AddBookCommand { get; }
    public ICommand EditBookCommand { get; }
    public ICommand DeleteBookCommand { get; }

    /// <summary>Вызывается видом для загрузки данных из базы.</summary>
    public void LoadData()
    {
        using var context = DbContextFactory.Create();
        var books = context.Books
            .Include(b => b.Author)
            .Include(b => b.Genre)
            .OrderBy(b => b.Title)
            .ToList();
        _books.Clear();
        foreach (var b in books) _books.Add(b);

        var authors = context.Authors.OrderBy(a => a.LastName).ToList();
        _authors.Clear();
        foreach (var a in authors) _authors.Add(a);

        var genres = context.Genres.OrderBy(g => g.Name).ToList();
        _genres.Clear();
        foreach (var g in genres) _genres.Add(g);

        _genreFilterOptions.Clear();
        _genreFilterOptions.Add(null);
        foreach (var g in genres) _genreFilterOptions.Add(g);
        _authorFilterOptions.Clear();
        _authorFilterOptions.Add(null);
        foreach (var a in authors) _authorFilterOptions.Add(a);

        RefreshFilter();
    }

    private void OnDeleteBook()
    {
        if (SelectedBook == null) return;
        var result = MessageBox.Show(
            $"Удалить книгу «{SelectedBook.Title}»?",
            "Подтверждение удаления",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        if (result != MessageBoxResult.Yes) return;

        try
        {
            using var context = DbContextFactory.Create();
            var book = context.Books.Find(SelectedBook.Id);
            if (book != null)
            {
                context.Books.Remove(book);
                context.SaveChanges();
            }
            _refreshGridCallback?.Invoke();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при удалении книги: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
