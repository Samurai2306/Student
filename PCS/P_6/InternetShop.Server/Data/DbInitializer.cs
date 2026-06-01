using InternetShop.Server.Models;

namespace InternetShop.Server.Data;

public static class DbInitializer
{
    public static void Initialize(ShopDbContext context)
    {
        context.Database.EnsureCreated();

        if (context.Products.Any())
        {
            return;
        }

        context.Products.AddRange(
            new Product
            {
                Name = "Ноутбук Pro 15",
                Description = "15.6\", 16 ГБ RAM, SSD 512 ГБ",
                Price = 89_990,
                Stock = 12,
                Category = "Электроника"
            },
            new Product
            {
                Name = "Беспроводные наушники",
                Description = "Шумоподавление, 30 ч автономности",
                Price = 7_490,
                Stock = 45,
                Category = "Аудио"
            },
            new Product
            {
                Name = "Кофемашина Compact",
                Description = "Капсульная, 19 bar",
                Price = 24_500,
                Stock = 8,
                Category = "Бытовая техника"
            },
            new Product
            {
                Name = "Рюкзак Urban",
                Description = "Водоотталкивающий, отделение для ноутбука 16\"",
                Price = 3_290,
                Stock = 30,
                Category = "Аксессуары"
            },
            new Product
            {
                Name = "Умные часы Fit",
                Description = "Пульс, GPS, уведомления",
                Price = 12_990,
                Stock = 20,
                Category = "Электроника"
            });

        context.SaveChanges();
    }
}
