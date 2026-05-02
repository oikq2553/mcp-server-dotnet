using Shared.Models;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

// In-memory storage
var todos = new List<TodoItem>();
var nextId = 1;

// GET /api/todos → List<TodoItem>
app.MapGet("/api/todos", () => todos)
   .WithName("GetAllTodos");

// GET /api/todos/{id} → TodoItem or 404
app.MapGet("/api/todos/{id:int}", (int id) =>
{
    var todo = todos.FirstOrDefault(t => t.Id == id);
    return todo is not null ? Results.Ok(todo) : Results.NotFound();
})
.WithName("GetTodoById");

// POST /api/todos → CreateTodoRequest body → TodoItem (201)
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
})
.WithName("CreateTodo");

// PUT /api/todos/{id} → UpdateTodoRequest body → TodoItem or 404
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
})
.WithName("UpdateTodo");

// DELETE /api/todos/{id} → 204 or 404
app.MapDelete("/api/todos/{id:int}", (int id) =>
{
    var todo = todos.FirstOrDefault(t => t.Id == id);
    if (todo is null) return Results.NotFound();
    todos.Remove(todo);
    return Results.NoContent();
})
.WithName("DeleteTodo");

// GET /api/todos/stats → TodoStats
app.MapGet("/api/todos/stats", () =>
{
    var completed = todos.Count(t => t.IsCompleted);
    return Results.Ok(new TodoStats
    {
        CompletedCount = completed,
        PendingCount = todos.Count - completed,
        TotalCount = todos.Count
    });
})
.WithName("GetTodoStats");

app.Run();
