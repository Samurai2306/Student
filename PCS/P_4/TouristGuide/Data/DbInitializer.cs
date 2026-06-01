using TouristGuide.Models;

namespace TouristGuide.Data;

public static class DbInitializer
{
    private static readonly Dictionary<string, string> CityImageByName = new()
    {
        ["Москва"] = "/images/cities/moscow.jpg",
        ["Санкт-Петербург"] = "/images/cities/saint-petersburg.jpg",
        ["Казань"] = "/images/cities/kazan.jpg",
        ["Сочи"] = "/images/cities/sochi.jpg",
        ["Новосибирск"] = "/images/cities/novosibirsk.jpg"
    };

    public static void Initialize(TouristGuideContext context)
    {
        SchemaUpgrader.Upgrade(context);

        if (!context.Cities.Any())
        {
            context.Cities.AddRange(GetInitialCities());
            context.SaveChanges();
        }
        else if (!context.Cities.Any(c => c.Name == "Сочи"))
        {
            context.Cities.AddRange(GetAdditionalCities());
            context.SaveChanges();
        }

        SyncCityImageUrls(context);
        GeoData.ApplyToDatabase(context);
    }

    private static void SyncCityImageUrls(TouristGuideContext context)
    {
        var updated = false;

        foreach (var city in context.Cities)
        {
            if (CityImageByName.TryGetValue(city.Name, out var localUrl) && city.ImageUrl != localUrl)
            {
                city.ImageUrl = localUrl;
                updated = true;
            }
        }

        if (updated)
        {
            context.SaveChanges();
        }
    }

    private static IEnumerable<City> GetInitialCities()
    {
        return GetCoreCities();
    }

    private static IEnumerable<City> GetAdditionalCities()
    {
        return GetExtendedCities();
    }

    private static List<City> GetCoreCities()
    {
        var cities = new List<City>
        {
            CreateMoscow(),
            CreateSaintPetersburg(),
            CreateKazan()
        };

        cities.AddRange(GetExtendedCities());
        return cities;
    }

    private static City CreateMoscow() => new()
    {
        Name = "Москва",
        Region = "Центральный федеральный округ",
        Population = 13_104_177,
        ShortDescription = "Столица России, крупнейший экономический и культурный центр страны.",
        History = "Первое упоминание Москвы относится к 1147 году. Город стал центром объединения русских земель, " +
                  "столицей Российского государства и сохранил статус столицы Российской Федерации. " +
                  "Здесь сосредоточены Кремль, исторические кварталы и современные деловые районы.",
        CoatOfArmsUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/1/16/Coat_of_arms_of_Moscow.svg/120px-Coat_of_arms_of_Moscow.svg.png",
        ImageUrl = CityImageByName["Москва"],
        Attractions =
        [
            new Attraction
            {
                Name = "Красная площадь",
                ShortDescription = "Главная площадь страны и символ российской истории.",
                History = "Красная площадь сформировалась в конце XV века. На ней расположены Кремль, " +
                          "Собор Василия Блаженного, ГУМ и Мавзолей. Площадь включена в список Всемирного наследия ЮНЕСКО.",
                ImageUrl = "https://images.unsplash.com/photo-1596484552834-086a7e8470f5?w=800&q=80",
                OpeningHours = "Круглосуточно (отдельные объекты — по расписанию)",
                EntryFee = null
            },
            new Attraction
            {
                Name = "Третьяковская галерея",
                ShortDescription = "Крупнейший музей русского изобразительного искусства.",
                History = "Коллекция основана Павлом Третьяковым в XIX веке. В галерее представлены работы " +
                          "Репина, Врубеля, Кандинского и других мастеров.",
                ImageUrl = "https://images.unsplash.com/photo-1566127995068-859a1c4a4c5?w=800&q=80",
                OpeningHours = "Вт–Вс 10:00–18:00, чт до 21:00",
                EntryFee = 500
            },
            new Attraction
            {
                Name = "ВДНХ",
                ShortDescription = "Выставочный комплекс с павильонами, фонтанами и парком.",
                History = "Всероссийский выставочный центр — одна из главных городских площадок для выставок, " +
                          "концертов и прогулок. Знаменит фонтаном «Дружба народов».",
                ImageUrl = "https://images.unsplash.com/photo-1520106212296-d8962ba902ae?w=800&q=80",
                OpeningHours = "Ежедневно 10:00–22:00",
                EntryFee = null
            }
        ]
    };

    private static City CreateSaintPetersburg() => new()
    {
        Name = "Санкт-Петербург",
        Region = "Северо-Западный федеральный округ",
        Population = 5_601_911,
        ShortDescription = "Культурная столица России на берегах Невы и Финского залива.",
        History = "Город основан Петром I в 1703 году как «окно в Европу». Архитектура XVIII–XIX веков, " +
                  "разводные мосты и музеи делают Петербург одним из главных туристических направлений.",
        CoatOfArmsUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/e/e7/Coat_of_Arms_of_Saint_Petersburg.svg/120px-Coat_of_Arms_of_Saint_Petersburg.svg.png",
        ImageUrl = CityImageByName["Санкт-Петербург"],
        Attractions =
        [
            new Attraction
            {
                Name = "Эрмитаж",
                ShortDescription = "Один из крупнейших художественных музеев мира.",
                History = "Музейный комплекс размещается в Зимнем дворце и примыкающих зданиях. " +
                          "Коллекция насчитывает миллионы экспонатов — от античности до современности.",
                ImageUrl = "https://images.unsplash.com/photo-1555881400-74d7acaacd8b?w=800&q=80",
                OpeningHours = "Вт–Вс 10:30–18:00, ср и пт до 21:00",
                EntryFee = 700
            },
            new Attraction
            {
                Name = "Петропавловская крепость",
                ShortDescription = "Историческое ядро города на Заячьем острове.",
                History = "Заложена 27 мая 1703 года — эта дата считается днём основания Петербурга. " +
                          "На территории крепости находятся собор, музеи и стрелка Васильевского острова рядом.",
                ImageUrl = "https://images.unsplash.com/photo-1523906834658-6e24ef2386f9?w=800&q=80",
                OpeningHours = "Ежедневно 10:00–20:00",
                EntryFee = 750
            }
        ]
    };

    private static City CreateKazan() => new()
    {
        Name = "Казань",
        Region = "Приволжский федеральный округ",
        Population = 1_308_660,
        ShortDescription = "Столица Татарстана, город на стыке европейской и тюркской культур.",
        History = "Казань — один из древнейших городов России. Казанский Кремль, мечети и православные храмы " +
                  "отражают многовековую историю региона.",
        CoatOfArmsUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/5/5a/Coat_of_Arms_of_Kazan.svg/120px-Coat_of_Arms_of_Kazan.svg.png",
        ImageUrl = CityImageByName["Казань"],
        Attractions =
        [
            new Attraction
            {
                Name = "Казанский Кремль",
                ShortDescription = "Объект Всемирного наследия ЮНЕСКО в центре города.",
                History = "Комплекс включает мечеть Кул-Шариф, Благовещенский собор и башню Сююмбике. " +
                          "Кремль — символ единства культур Татарстана.",
                ImageUrl = "https://images.unsplash.com/photo-1609137144813-9d421563cb64?w=800&q=80",
                OpeningHours = "Ежедневно 8:00–22:00",
                EntryFee = null
            },
            new Attraction
            {
                Name = "Улица Баумана",
                ShortDescription = "Пешеходная улица с кафе, сувенирами и исторической застройкой.",
                History = "Одна из старейших улиц Казани, популярная у туристов и местных жителей. " +
                          "Здесь проходят фестивали и городские мероприятия.",
                ImageUrl = "https://images.unsplash.com/photo-1449824913935-59a10b8d2000?w=800&q=80",
                OpeningHours = "Круглосуточно",
                EntryFee = null
            }
        ]
    };

    private static List<City> GetExtendedCities() =>
    [
        new()
        {
            Name = "Сочи",
            Region = "Южный федеральный округ",
            Population = 466_078,
            ShortDescription = "Главный черноморский курорт России с субтропическим климатом.",
            History = "Сочи известен с XIX века как климатический и морской курорт. Город принимал Зимнюю " +
                      "Олимпиаду 2014 года, здесь сосредоточены пляжи, горные курорты Красной Поляны и парки.",
            CoatOfArmsUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/1/1a/Coat_of_Arms_of_Sochi.svg/120px-Coat_of_Arms_of_Sochi.svg.png",
            ImageUrl = CityImageByName["Сочи"],
            Attractions =
            [
                new Attraction
                {
                    Name = "Олимпийский парк",
                    ShortDescription = "Спортивный комплекс и набережная у Чёрного моря.",
                    History = "Построен к Олимпиаде-2014. На территории — стадион «Фишт», музей олимпийских объектов " +
                              "и прогулочная зона с фонтанами.",
                    ImageUrl = "https://images.unsplash.com/photo-1469854523086-cc02fe5d8800?w=800&q=80",
                    OpeningHours = "Ежедневно 9:00–23:00",
                    EntryFee = null
                },
                new Attraction
                {
                    Name = "Дендрарий",
                    ShortDescription = "Ботанический сад с канатной дорогой и видами на море.",
                    History = "Один из старейших dendrarium страны. Коллекция включает субтропические растения " +
                              "и тематические аллеи на склонах гор.",
                    ImageUrl = "https://images.unsplash.com/photo-1441974231531-c6227db76b6e?w=800&q=80",
                    OpeningHours = "Ежедневно 8:00–21:00",
                    EntryFee = 350
                }
            ]
        },
        new()
        {
            Name = "Новосибирск",
            Region = "Сибирский федеральный округ",
            Population = 1_633_595,
            ShortDescription = "Крупнейший город Сибири и научный центр за Уралом.",
            History = "Основан в 1893 году как поселок у моста через Обь. Новосибирск вырос вокруг Транссибирской " +
                      "магистрали и Академгородка, сохранив динамичный деловой и культурный облик.",
            CoatOfArmsUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/3/3a/Coat_of_Arms_of_Novosibirsk.svg/120px-Coat_of_Arms_of_Novosibirsk.svg.png",
            ImageUrl = CityImageByName["Новосибирск"],
            Attractions =
            [
                new Attraction
                {
                    Name = "Театр оперы и балета",
                    ShortDescription = "Крупнейший театральный комплекс Сибири на главной площади.",
                    History = "Открыт в 1945 году. Здание — один из символов города, здесь проходят оперные " +
                              "и балетные постановки федерального уровня.",
                    ImageUrl = "https://images.unsplash.com/photo-1503095396549-807759245b35?w=800&q=80",
                    OpeningHours = "Касса: ежедневно 10:00–19:00",
                    EntryFee = 900
                },
                new Attraction
                {
                    Name = "Метро «Сибирская»",
                    ShortDescription = "Станция с уникальным оформлением и мозаиками.",
                    History = "Одна из станций Новосибирского метрополитена — самого восточного метро в России. " +
                              "Интерьер отражает сибирскую тематику и природные мотивы.",
                    ImageUrl = "https://images.unsplash.com/photo-1515169067865-5387ec356754?w=800&q=80",
                    OpeningHours = "Ежедневно 6:00–0:00",
                    EntryFee = 35
                }
            ]
        }
    ];
}
