namespace LibraryManagement.Models;

/// <summary>
/// Сущность «Жанр»: представляет жанр/категорию книги.
/// </summary>
public class Genre
{
    public int Id { get; set; }

    /// <summary>Обязательное. Макс. длина 100.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Необязательное описание. Макс. длина 500.</summary>
    public string? Description { get; set; }

    /// <summary>Навигационное свойство: книги в этом жанре.</summary>
    public virtual ICollection<Book> Books { get; set; } = new List<Book>();
}
