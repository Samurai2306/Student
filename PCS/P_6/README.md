# Практическое занятие №6 — «Управление товарами интернет-магазина»

Клиент-серверное приложение на **Blazor WebAssembly** + **ASP.NET Core Host** + **Entity Framework Core** + SQLite.

## Соответствие заданию

| Требование | Проект / реализация |
|------------|---------------------|
| **BlazorClient** (WebAssembly) | `InternetShop.Client` — UI, формы, темы |
| **BlazorServer** (ASP.NET Core Host) | `InternetShop.Server` — API + хост WASM |
| GET `/api/products` | `ProductsController.Get` |
| POST `/api/products` | `ProductsController.Post` |
| PUT `/api/products` | `ProductsController.Put` (товар с `Id` в теле) |
| DELETE `/api/products` | `ProductsController.Delete?id=` |
| EF Core + БД | SQLite `shop.db` |
| Светлая / тёмная тема | переключатель в шапке, `localStorage` |

## Запуск

```powershell
cd .\PCS\P_6\InternetShop.Server
dotnet run
```

Откройте **http://localhost:5280** — браузер откроется автоматически (Development).

Один процесс: сервер отдаёт и **Blazor WASM**, и **REST API**.

## Функции клиента

- каталог товаров (карточки);
- добавление товара (`/products/add`);
- редактирование (`/products/edit/{id}`);
- удаление с **подтверждением** (модальное окно);
- переключатель **светлая / тёмная** тема.

## Структура

```text
P_6/
├── InternetShop.sln
├── InternetShop.Server/          # BlazorServer — хост + API
│   ├── Controllers/ProductsController.cs
│   ├── Data/
│   ├── Models/Product.cs
│   └── Program.cs
└── InternetShop.Client/         # BlazorClient — WebAssembly
    ├── Pages/Products.razor
    ├── Pages/ProductEdit.razor
    ├── Services/
    └── wwwroot/js/theme.js
```

## Модель товара

- `Id`, `Name`, `Description`, `Price`, `Stock`, `Category`

## Начальные данные

5 товаров (электроника, аудио, бытовая техника и др.) создаются при первом запуске.

Пересоздать БД:

```powershell
Remove-Item .\shop.db -ErrorAction SilentlyContinue
dotnet run
```

## Сборка

```powershell
dotnet build .\PCS\P_6\InternetShop.sln
```

## Проверка API

```powershell
curl http://localhost:5280/api/products
```
