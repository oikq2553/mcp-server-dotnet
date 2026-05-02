using System.Text.Json;
using Shared.Models;

namespace McpServer.Services;

public class ApiClient
{
    private readonly HttpClient _httpClient;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ApiClient(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("ApiClient");
    }

    public async Task<List<TodoItem>> GetAllTodosAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/todos");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<TodoItem>>(content, JsonOptions) ?? new List<TodoItem>();
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Failed to get all todos: {ex.Message}", ex);
        }
    }

    public async Task<TodoItem?> GetTodoByIdAsync(int id)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/todos/{id}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<TodoItem>(content, JsonOptions);
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Failed to get todo by id {id}: {ex.Message}", ex);
        }
    }

    public async Task<TodoItem> CreateTodoAsync(CreateTodoRequest request)
    {
        try
        {
            var json = JsonSerializer.Serialize(request, JsonOptions);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("/api/todos", content);
            response.EnsureSuccessStatusCode();
            var responseContent = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<TodoItem>(responseContent, JsonOptions)
                ?? throw new Exception("Failed to deserialize created todo response");
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Failed to create todo: {ex.Message}", ex);
        }
    }

    public async Task<TodoItem?> UpdateTodoAsync(int id, UpdateTodoRequest request)
    {
        try
        {
            var json = JsonSerializer.Serialize(request, JsonOptions);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"/api/todos/{id}", content);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
            response.EnsureSuccessStatusCode();
            var responseContent = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<TodoItem>(responseContent, JsonOptions);
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Failed to update todo {id}: {ex.Message}", ex);
        }
    }

    public async Task<bool> DeleteTodoAsync(int id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"/api/todos/{id}");
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Failed to delete todo {id}: {ex.Message}", ex);
        }
    }

    public async Task<TodoStats> GetStatsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/todos/stats");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<TodoStats>(content, JsonOptions)
                ?? throw new Exception("Failed to deserialize stats response");
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Failed to get todo stats: {ex.Message}", ex);
        }
    }
}
