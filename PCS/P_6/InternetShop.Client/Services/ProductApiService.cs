using System.Net.Http.Json;
using InternetShop.Client.Models;

namespace InternetShop.Client.Services;

public sealed class ProductApiService
{
    private readonly HttpClient _http;

    public ProductApiService(HttpClient http)
    {
        _http = http;
    }

    public async Task<IReadOnlyList<Product>> GetAllAsync()
    {
        var items = await _http.GetFromJsonAsync<List<Product>>("api/products");
        return items ?? [];
    }

    public async Task<Product?> GetByIdAsync(int id)
    {
        var items = await GetAllAsync();
        return items.FirstOrDefault(p => p.Id == id);
    }

    public async Task<Product?> CreateAsync(Product product)
    {
        var response = await _http.PostAsJsonAsync("api/products", product);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<Product>();
    }

    public async Task<bool> UpdateAsync(Product product)
    {
        var response = await _http.PutAsJsonAsync("api/products", product);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var response = await _http.DeleteAsync($"api/products?id={id}");
        return response.IsSuccessStatusCode;
    }
}
