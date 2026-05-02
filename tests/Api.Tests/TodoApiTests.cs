using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Shared.Models;
using Xunit;

namespace Api.Tests;

public class TodoApiTests : IDisposable
{
    private readonly TestServer _server;
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public TodoApiTests()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddOpenApi();

        var app = builder.Build();

        var todos = new List<TodoItem>();
        var nextId = 1;

        app.MapGet("/api/todos", () => todos);

        app.MapGet("/api/todos/{id:int}", (int id) =>
        {
            var todo = todos.FirstOrDefault(t => t.Id == id);
            return todo is not null ? Results.Ok(todo) : Results.NotFound();
        });

        app.MapPost("/api/todos", (CreateTodoRequest request) =>
        {
            var todo = new TodoItem
            {
                Id = nextId++,
                Title = request.Title,
                Description = request.Description,
                IsCompleted = false,
                CreatedAt = DateTime.UtcNow,
                CompletedAt = null
            };
            todos.Add(todo);
            return Results.Created($"/api/todos/{todo.Id}", todo);
        });

        app.MapPut("/api/todos/{id:int}", (int id, UpdateTodoRequest request) =>
        {
            var todo = todos.FirstOrDefault(t => t.Id == id);
            if (todo is null) return Results.NotFound();

            if (request.Title is not null) todo.Title = request.Title;
            if (request.Description is not null) todo.Description = request.Description;
            if (request.IsCompleted.HasValue)
            {
                todo.IsCompleted = request.IsCompleted.Value;
                todo.CompletedAt = request.IsCompleted.Value ? DateTime.UtcNow : null;
            }
            return Results.Ok(todo);
        });

        app.MapDelete("/api/todos/{id:int}", (int id) =>
        {
            var todo = todos.FirstOrDefault(t => t.Id == id);
            if (todo is null) return Results.NotFound();
            todos.Remove(todo);
            return Results.NoContent();
        });

        app.MapGet("/api/todos/stats", () =>
        {
            var completed = todos.Count(t => t.IsCompleted);
            return Results.Ok(new TodoStats
            {
                CompletedCount = completed,
                PendingCount = todos.Count - completed,
                TotalCount = todos.Count
            });
        });

        app.StartAsync().GetAwaiter().GetResult();
        _server = app.Services.GetRequiredService<IServer>() as TestServer ?? throw new InvalidOperationException("TestServer not available");
        _client = _server.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _server.Dispose();
    }

    private async Task<TodoItem> CreateTodoAsync(string title, string? description = null)
    {
        var response = await _client.PostAsJsonAsync("/api/todos", new CreateTodoRequest
        {
            Title = title,
            Description = description
        }, JsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TodoItem>(JsonOptions))!;
    }

    [Fact]
    public async Task CreateTodo_ReturnsCreatedTodo()
    {
        var request = new CreateTodoRequest { Title = "Test Todo", Description = "Test Desc" };

        var response = await _client.PostAsJsonAsync("/api/todos", request, JsonOptions);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var todo = await response.Content.ReadFromJsonAsync<TodoItem>(JsonOptions);
        Assert.NotNull(todo);
        Assert.Equal("Test Todo", todo.Title);
        Assert.Equal("Test Desc", todo.Description);
        Assert.False(todo.IsCompleted);
        Assert.True(todo.Id > 0);
        Assert.NotNull(response.Headers.Location);
        Assert.EndsWith($"/api/todos/{todo.Id}", response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task GetAllTodos_ReturnsList()
    {
        var todo1 = await CreateTodoAsync("First");
        var todo2 = await CreateTodoAsync("Second");

        var response = await _client.GetAsync("/api/todos");

        response.EnsureSuccessStatusCode();
        var list = await response.Content.ReadFromJsonAsync<List<TodoItem>>(JsonOptions);
        Assert.NotNull(list);
        Assert.True(list.Count >= 2);
        Assert.Contains(list, t => t.Title == "First");
        Assert.Contains(list, t => t.Title == "Second");
    }

    [Fact]
    public async Task GetById_ReturnsTodo_WhenFound()
    {
        var created = await CreateTodoAsync("Find Me", "Description");

        var response = await _client.GetAsync($"/api/todos/{created.Id}");

        response.EnsureSuccessStatusCode();
        var todo = await response.Content.ReadFromJsonAsync<TodoItem>(JsonOptions);
        Assert.NotNull(todo);
        Assert.Equal(created.Id, todo.Id);
        Assert.Equal("Find Me", todo.Title);
    }

    [Fact]
    public async Task GetById_Returns404_WhenNotFound()
    {
        var response = await _client.GetAsync("/api/todos/99999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateTodo_ReturnsUpdatedTodo()
    {
        var created = await CreateTodoAsync("Original", "Orig Desc");

        var updateRequest = new UpdateTodoRequest { Title = "Updated", Description = "Updated Desc", IsCompleted = true };
        var response = await _client.PutAsJsonAsync($"/api/todos/{created.Id}", updateRequest, JsonOptions);

        response.EnsureSuccessStatusCode();
        var updated = await response.Content.ReadFromJsonAsync<TodoItem>(JsonOptions);
        Assert.NotNull(updated);
        Assert.Equal(created.Id, updated.Id);
        Assert.Equal("Updated", updated.Title);
        Assert.Equal("Updated Desc", updated.Description);
        Assert.True(updated.IsCompleted);
        Assert.NotNull(updated.CompletedAt);
    }

    [Fact]
    public async Task UpdateTodo_Returns404_WhenNotFound()
    {
        var updateRequest = new UpdateTodoRequest { Title = "Updated" };
        var response = await _client.PutAsJsonAsync("/api/todos/99999", updateRequest, JsonOptions);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteTodo_Returns204_WhenFound()
    {
        var created = await CreateTodoAsync("To Delete");

        var response = await _client.DeleteAsync($"/api/todos/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var getResponse = await _client.GetAsync($"/api/todos/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteTodo_Returns404_WhenNotFound()
    {
        var response = await _client.DeleteAsync("/api/todos/99999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Stats_ReturnsCorrectCounts()
    {
        var pending = await CreateTodoAsync("Pending 1");
        var completed = await CreateTodoAsync("Completed 1");
        await _client.PutAsJsonAsync($"/api/todos/{completed.Id}", new UpdateTodoRequest { IsCompleted = true }, JsonOptions);

        var response = await _client.GetAsync("/api/todos/stats");

        response.EnsureSuccessStatusCode();
        var stats = await response.Content.ReadFromJsonAsync<TodoStats>(JsonOptions);
        Assert.NotNull(stats);
        Assert.Equal(1, stats.CompletedCount);
        Assert.True(stats.PendingCount >= 1);
        Assert.True(stats.TotalCount >= 2);
        Assert.Equal(stats.TotalCount, stats.CompletedCount + stats.PendingCount);
    }
}
