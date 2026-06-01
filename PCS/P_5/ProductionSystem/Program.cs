using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using ProductionSystem.Data;
using ProductionSystem.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<ProductionContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Data Source=production.db"));
builder.Services.AddScoped<ProductionService>();
builder.Services.AddHostedService<WorkOrderProgressBackgroundService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    DbInitializer.Initialize(scope.ServiceProvider.GetRequiredService<ProductionContext>());
}

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.Lifetime.ApplicationStarted.Register(() =>
    {
        var url = app.Urls.FirstOrDefault(u => u.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
                  ?? app.Urls.FirstOrDefault();
        if (!string.IsNullOrEmpty(url))
        {
            OpenBrowser(url);
        }
    });
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllers();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

static void OpenBrowser(string url)
{
    try
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = url,
            UseShellExecute = true
        });
    }
    catch
    {
        // Браузер не удалось открыть — приложение продолжает работать.
    }
}
