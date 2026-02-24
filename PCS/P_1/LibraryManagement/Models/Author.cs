namespace LibraryManagement.Models;

/// <summary>
/// Сущность «Автор»: представляет автора книги.
/// </summary>
public class Author
{
    public int Id { get; set; }

    /// <summary>Обязательное. Макс. длина 100.</summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>Обязательное. Макс. длина 100.</summary>
    public string LastName { get; set; } = string.Empty;

    public DateTime? BirthDate { get; set; }

    /// <summary>Макс. длина 100.</summary>
    public string? Country { get; set; }

    /// <summary>Навигационное свойство: книги этого автора.</summary>
    public virtual ICollection<Book> Books { get; set; } = new List<Book>();

    /// <summary>Полное имя для отображения (напр. в таблице и выпадающем списке).</summary>
    public string FullName => $"{FirstName} {LastName}".Trim();
}
