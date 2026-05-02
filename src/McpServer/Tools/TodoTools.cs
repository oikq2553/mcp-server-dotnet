using System.ComponentModel;
using System.Text.Json;
using McpDotNet.Server;
using McpServer.Services;
using Shared.Models;

namespace McpServer.Tools;

[McpToolType]
public class TodoTools
{
    public static ApiClient ApiClient { get; set; } = null!;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    [McpTool("get_all_todos")]
    [Description("Retrieves all todo items from the API")]
    public static async Task<string> GetAllTodosAsync()
    {
        try
        {
            var todos = await ApiClient.GetAllTodosAsync();
            return JsonSerializer.Serialize(todos, JsonOptions);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message }, JsonOptions);
        }
    }

    [McpTool("get_todo_by_id")]
    [Description("Retrieves a single todo item by its ID")]
    public static async Task<string> GetTodoByIdAsync(
        [Description("The unique identifier of the todo item")] [McpParameter("The unique identifier of the todo item", Required = true)] int id)
    {
        try
        {
            var todo = await ApiClient.GetTodoByIdAsync(id);
            if (todo == null)
            {
                return JsonSerializer.Serialize(new { error = $"Todo with id {id} not found" }, JsonOptions);
            }
            return JsonSerializer.Serialize(todo, JsonOptions);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message }, JsonOptions);
        }
    }

    [McpTool("create_todo")]
    [Description("Creates a new todo item")]
    public static async Task<string> CreateTodoAsync(
        [Description("The title of the new todo item")] [McpParameter("The title of the new todo item", Required = true)] string title,
        [Description("An optional description for the todo item")] [McpParameter("An optional description for the todo item", Required = false)] string? description = null)
    {
        try
        {
            var request = new CreateTodoRequest
            {
                Title = title,
                Description = description
            };
            var todo = await ApiClient.CreateTodoAsync(request);
            return JsonSerializer.Serialize(todo, JsonOptions);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message }, JsonOptions);
        }
    }

    [McpTool("update_todo")]
    [Description("Updates an existing todo item")]
    public static async Task<string> UpdateTodoAsync(
        [Description("The unique identifier of the todo item to update")] [McpParameter("The unique identifier of the todo item to update", Required = true)] int id,
        [Description("The new title for the todo item")] [McpParameter("The new title for the todo item", Required = false)] string? title = null,
        [Description("The new description for the todo item")] [McpParameter("The new description for the todo item", Required = false)] string? description = null,
        [Description("Whether the todo item is completed")] [McpParameter("Whether the todo item is completed", Required = false)] bool? isCompleted = null)
    {
        try
        {
            var request = new UpdateTodoRequest
            {
                Title = title,
                Description = description,
                IsCompleted = isCompleted
            };
            var todo = await ApiClient.UpdateTodoAsync(id, request);
            if (todo == null)
            {
                return JsonSerializer.Serialize(new { error = $"Todo with id {id} not found" }, JsonOptions);
            }
            return JsonSerializer.Serialize(todo, JsonOptions);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message }, JsonOptions);
        }
    }

    [McpTool("delete_todo")]
    [Description("Deletes a todo item by its ID")]
    public static async Task<string> DeleteTodoAsync(
        [Description("The unique identifier of the todo item to delete")] [McpParameter("The unique identifier of the todo item to delete", Required = true)] int id)
    {
        try
        {
            var success = await ApiClient.DeleteTodoAsync(id);
            if (success)
            {
                return JsonSerializer.Serialize(new { success = true, message = $"Todo with id {id} deleted successfully" }, JsonOptions);
            }
            return JsonSerializer.Serialize(new { success = false, error = $"Failed to delete todo with id {id}" }, JsonOptions);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message }, JsonOptions);
        }
    }

    [McpTool("get_todo_stats")]
    [Description("Retrieves statistics about todo items")]
    public static async Task<string> GetTodoStatsAsync()
    {
        try
        {
            var stats = await ApiClient.GetStatsAsync();
            return JsonSerializer.Serialize(stats, JsonOptions);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message }, JsonOptions);
        }
    }
}
