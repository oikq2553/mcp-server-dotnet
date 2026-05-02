using System.Text.Json;
using System.Collections.Concurrent;
using McpServer.Services;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// ========== Security Configuration ==========
var apiKey = builder.Configuration["API_KEY"] ?? Environment.GetEnvironmentVariable("API_KEY");
var allowedOrigins = builder.Configuration.GetSection("ALLOWED_ORIGINS").Get<string[]>() 
    ?? new[] { "*" };

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("McpCors", policy =>
    {
        if (allowedOrigins.Contains("*"))
        {
            policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
        }
        else
        {
            policy.WithOrigins(allowedOrigins).AllowAnyMethod().AllowAnyHeader();
        }
    });
});

// API Client
var apiBaseUrl = builder.Configuration["API_BASE_URL"] ?? Environment.GetEnvironmentVariable("API_BASE_URL") ?? "http://localhost:5000";

builder.Services.AddHttpClient("ApiClient", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
});

builder.Services.AddSingleton<ApiClient>();

var app = builder.Build();

// ========== Security Middleware ==========

// 1. API Key Authentication
app.Use(async (context, next) =>
{
    // Skip API key check for health endpoint
    if (context.Request.Path == "/health")
    {
        await next();
        return;
    }

    var requestKey = context.Request.Headers["X-API-Key"].FirstOrDefault();
    
    if (string.IsNullOrEmpty(requestKey) || requestKey != apiKey)
    {
        context.Response.StatusCode = 401;
        context.Response.ContentType = "application/json";
        var error = JsonSerializer.Serialize(new { error = "Unauthorized: Invalid or missing X-API-Key header" });
        await context.Response.WriteAsync(error);
        return;
    }

    await next();
});

// 2. Rate Limiting (Simple in-memory)
var rateLimits = new ConcurrentDictionary<string, (int Count, DateTime ResetTime)>();
var maxRequestsPerMinute = 60;

app.Use(async (context, next) =>
{
    if (context.Request.Path == "/health")
    {
        await next();
        return;
    }

    var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    var now = DateTime.UtcNow;
    
    var (count, resetTime) = rateLimits.GetOrAdd(clientIp, _ => (0, now.AddMinutes(1)));
    
    if (now > resetTime)
    {
        count = 0;
        resetTime = now.AddMinutes(1);
    }
    
    count++;
    rateLimits[clientIp] = (count, resetTime);
    
    context.Response.Headers["X-RateLimit-Limit"] = maxRequestsPerMinute.ToString();
    context.Response.Headers["X-RateLimit-Remaining"] = Math.Max(0, maxRequestsPerMinute - count).ToString();
    context.Response.Headers["X-RateLimit-Reset"] = new DateTimeOffset(resetTime).ToUnixTimeSeconds().ToString();
    
    if (count > maxRequestsPerMinute)
    {
        context.Response.StatusCode = 429;
        context.Response.ContentType = "application/json";
        var error = JsonSerializer.Serialize(new { error = "Too many requests. Rate limit exceeded." });
        await context.Response.WriteAsync(error);
        return;
    }

    await next();
});

app.UseCors("McpCors");

// ========== API Endpoints ==========

app.MapGet("/health", () => new { status = "ok", timestamp = DateTime.UtcNow, version = "1.1.0-secure" });

app.MapGet("/tools", () =>
{
    var tools = new List<object>
    {
        new { name = "get_all_todos", description = "Retrieves all todo items from the API", parameters = new List<object>() },
        new { name = "get_todo_by_id", description = "Retrieves a single todo item by its ID", parameters = new List<object> { new { name = "id", type = "integer", required = true } } },
        new { name = "create_todo", description = "Creates a new todo item", parameters = new List<object> { new { name = "title", type = "string", required = true }, new { name = "description", type = "string", required = false } } },
        new { name = "update_todo", description = "Updates an existing todo item", parameters = new List<object> { new { name = "id", type = "integer", required = true }, new { name = "title", type = "string", required = false }, new { name = "description", type = "string", required = false }, new { name = "isCompleted", type = "boolean", required = false } } },
        new { name = "delete_todo", description = "Deletes a todo item by its ID", parameters = new List<object> { new { name = "id", type = "integer", required = true } } },
        new { name = "get_todo_stats", description = "Retrieves statistics about todo items", parameters = new List<object>() }
    };
    return tools;
});

app.MapGet("/tools/get_all_todos", async ([FromServices] ApiClient apiClient) =>
{
    try
    {
        var todos = await apiClient.GetAllTodosAsync();
        return Results.Ok(todos);
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});

app.MapGet("/tools/get_todo_by_id/{id:int}", async ([FromServices] ApiClient apiClient, int id) =>
{
    try
    {
        var todo = await apiClient.GetTodoByIdAsync(id);
        return todo == null ? Results.NotFound(new { error = $"Todo {id} not found" }) : Results.Ok(todo);
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});

app.MapPost("/tools/create_todo", async ([FromServices] ApiClient apiClient, [FromBody] Shared.Models.CreateTodoRequest request) =>
{
    try
    {
        var todo = await apiClient.CreateTodoAsync(request);
        return Results.Ok(todo);
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});

app.MapPut("/tools/update_todo/{id:int}", async ([FromServices] ApiClient apiClient, int id, [FromBody] Shared.Models.UpdateTodoRequest request) =>
{
    try
    {
        var todo = await apiClient.UpdateTodoAsync(id, request);
        return todo == null ? Results.NotFound(new { error = $"Todo {id} not found" }) : Results.Ok(todo);
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});

app.MapDelete("/tools/delete_todo/{id:int}", async ([FromServices] ApiClient apiClient, int id) =>
{
    try
    {
        var success = await apiClient.DeleteTodoAsync(id);
        return success ? Results.Ok(new { success = true, message = $"Todo {id} deleted" }) : Results.BadRequest(new { success = false });
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});

app.MapGet("/tools/get_todo_stats", async ([FromServices] ApiClient apiClient) =>
{
    try
    {
        var stats = await apiClient.GetStatsAsync();
        return Results.Ok(stats);
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});

var port = builder.Configuration["MCP_PORT"] ?? Environment.GetEnvironmentVariable("MCP_PORT") ?? "8788";
app.Run($"http://localhost:{port}");
