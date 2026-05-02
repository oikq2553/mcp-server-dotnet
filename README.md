# .NET MCP Server 🎯

A Model Context Protocol (MCP) server built with .NET that exposes API functionality through standardized MCP tools.

## 🏗️ Architecture

```
┌─────────────────────┐         ┌─────────────────┐         ┌──────────────┐
│   Claude Desktop    │  stdin  │   MCP Server    │  HTTP   │   API Backend│
│   / Cursor / etc.   │ ──────▶ │   (.NET 8)      │ ──────▶ │  localhost:5000
└─────────────────────┘         └─────────────────┘         └──────────────┘
```

## 📦 Project Structure

```
src/
├── Shared/                         # Shared models library
│   ├── Shared.csproj
│   └── Models/
│       ├── TodoItem.cs
│       ├── CreateTodoRequest.cs
│       ├── UpdateTodoRequest.cs
│       └── TodoStats.cs
└── McpServer/                      # MCP Server console app
    ├── McpServer.csproj
    ├── Program.cs                  # Host builder & DI setup
    ├── appsettings.json
    ├── Services/
    │   └── ApiClient.cs            # Typed HTTP client
    └── Tools/
        └── TodoTools.cs            # 6 MCP tools
```

## 🚀 Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- API backend running on `http://localhost:5000`

### Build

```bash
cd src/McpServer
dotnet build
```

### Run

```bash
# Default: connects to http://localhost:5000
dotnet run

# Custom API endpoint:
API_BASE_URL=http://localhost:8080 dotnet run
```

## 🔧 MCP Tools

| Tool | Description | Parameters |
|------|-------------|------------|
| `get_all_todos` | Get all todo items | None |
| `get_todo_by_id` | Get a todo by ID | `id` (int) |
| `create_todo` | Create a new todo | `title` (string), `description` (string?, optional) |
| `update_todo` | Update a todo | `id` (int), `title` (string?, optional), `description` (string?, optional), `isCompleted` (bool?, optional) |
| `delete_todo` | Delete a todo | `id` (int) |
| `get_todo_stats` | Get todo statistics | None |

## ⚙️ Configuration

Set the API base URL via:
1. Environment variable: `API_BASE_URL`
2. `appsettings.json`
3. Defaults to: `http://localhost:5000`

## 🔌 Connect to AI Clients

### Claude Desktop

Add to `claude_desktop_config.json`:

```json
{
  "mcpServers": {
    "todos": {
      "command": "dotnet",
      "args": ["run", "--project", "/path/to/McpServer.csproj"]
    }
  }
}
```

### Cursor

Add to `.cursor/mcp.json`:

```json
{
  "servers": [
    {
      "name": "todos",
      "command": "dotnet",
      "args": ["run", "--project", "/path/to/McpServer.csproj"]
    }
  ]
}
```

## 🛠️ Tech Stack

- **.NET 8** — Runtime
- **mcpdotnet** (v1.2.0.1) — MCP framework
- **Microsoft.Extensions.Hosting** — Host builder
- **System.Text.Json** — JSON serialization
- **stdio transport** — MCP communication

## 📄 License

MIT
