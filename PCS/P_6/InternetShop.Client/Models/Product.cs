using System.ComponentModel.DataAnnotations;

namespace InternetShop.Client.Models;

public class Product
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Укажите название")]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [Range(0.01, 999_999_999, ErrorMessage = "Цена должна быть больше 0")]
    public decimal Price { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Остаток не может быть отрицательным")]
    public int Stock { get; set; }

    [Required(ErrorMessage = "Укажите категорию")]
    [MaxLength(100)]
    public string Category { get; set; } = string.Empty;
}
