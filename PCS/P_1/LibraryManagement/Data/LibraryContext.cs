using LibraryManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Data;

/// <summary>
/// DbContext EF Core для базы данных библиотеки.
/// Вся конфигурация сущностей выполняется через Fluent API в OnModelCreating.
/// </summary>
public class LibraryContext : DbContext
{
    public DbSet<Book> Books => Set<Book>();
    public DbSet<Author> Authors => Set<Author>();
    public DbSet<Genre> Genres => Set<Genre>();

    public LibraryContext(DbContextOptions<LibraryContext> options) : base(options) { }

    /// <summary>
    /// Конфигурация Fluent API: первичные ключи, обязательные поля, макс. длины и связи.
    /// Каскадное удаление для Автора и Жанра установлено в Restrict: удаление автора или жанра
    /// завершится ошибкой, если на него ссылается хотя бы одна книга (сохраняется целостность данных).
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ----- Автор -----
        modelBuilder.Entity<Author>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Country).HasMaxLength(100);
        });

        // ----- Жанр -----
        modelBuilder.Entity<Genre>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
        });

        // ----- Книга -----
        modelBuilder.Entity<Book>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.ISBN).HasMaxLength(20);

            // Книга -> Автор (многие-к-одному). Restrict: нельзя удалить автора, если на него ссылается книга.
            entity.HasOne(b => b.Author)
                .WithMany(a => a.Books)
                .HasForeignKey(b => b.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Книга -> Жанр (многие-к-одному). Restrict: нельзя удалить жанр, если на него ссылается книга.
            entity.HasOne(b => b.Genre)
                .WithMany(g => g.Books)
                .HasForeignKey(b => b.GenreId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
