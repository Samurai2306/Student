namespace LibraryManagement.Models;

/// <summary>
/// Сущность «Книга»: книга со связями с автором и жанром.
/// </summary>
public class Book
{
    public int Id { get; set; }

    /// <summary>Обязательное. Макс. длина 200.</summary>
    public string Title { get; set; } = string.Empty;

    public int AuthorId { get; set; }
    /// <summary>Навигационное свойство: автор книги (многие-к-одному).</summary>
    public virtual Author Author { get; set; } = null!;

    public int? PublishYear { get; set; }

    /// <summary>Макс. длина 20.</summary>
    public string? ISBN { get; set; }

    public int GenreId { get; set; }
    /// <summary>Навигационное свойство: жанр книги (многие-к-одному).</summary>
    public virtual Genre Genre { get; set; } = null!;

    public int QuantityInStock { get; set; }
}
