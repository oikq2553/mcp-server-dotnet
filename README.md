# .NET MCP Server 🎯

A Model Context Protocol (MCP) server built with .NET that exposes API functionality through standardized MCP tools.

## 🏗️ Architecture

### Local Mode (stdio)
```
┌─────────────────────┐         ┌─────────────────┐         ┌──────────────┐
│   Claude Desktop    │  stdin  │   MCP Server    │  HTTP   │   API Backend│
│   / Cursor / etc.   │ ──────▶ │   (.NET 10)     │ ──────▶ │  localhost:5000
└─────────────────────┘         └─────────────────┘         └──────────────┘
```

### Remote Mode (HTTP + Tunnel)
```
┌──────────────┐      HTTPS       ┌─────────────┐      HTTP      ┌──────────────┐
│  AI Client   │ ═══════════════▶ │  cloudflare │ ════════════▶ │  MCP Server  │
│  / curl      │   + X-API-Key    │  / ngrok    │                │  (localhost) │
└──────────────┘                  └─────────────┘                └──────────────┘
                                                                       │
                                                                       ▼
                                                                  ┌──────────────┐
                                                                  │  API Backend │
                                                                  │  localhost:5000
                                                                  └──────────────┘
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
├── McpServer/                      # MCP Server console app (stdio)
│   ├── McpServer.csproj
│   ├── Program.cs                  # Host builder & DI setup
│   ├── appsettings.json
│   ├── Services/
│   │   └── ApiClient.cs            # Typed HTTP client
│   └── Tools/
│       └── TodoTools.cs            # 6 MCP tools
└── McpServerHttp/                  # HTTP API wrapper (for ngrok/cloudflared)
    ├── McpServerHttp.csproj
    └── Program.cs                  # ASP.NET Core Minimal API + Security
```

## 🚀 Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- API backend running on `http://localhost:5000`

### Build (stdio MCP Server)

```bash
cd src/McpServer
dotnet build
```

### Run (stdio MCP Server)

```bash
# Default: connects to http://localhost:5000
dotnet run

# Custom API endpoint:
API_BASE_URL=http://localhost:8080 dotnet run
```

### Build & Run (HTTP API for remote deploy)

```bash
cd src/McpServerHttp
dotnet build
dotnet run

# With custom settings:
API_KEY=your-secret-key API_BASE_URL=http://localhost:8080 dotnet run
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

## 🔐 Security (HTTP API)

The HTTP API wrapper (`McpServerHttp`) includes:

| Feature | รายละเอียด |
|---------|-----------|
| **API Key Auth** | ต้องส่ง `X-API-Key` header |
| **Rate Limiting** | 60 requests/minute ต่อ IP |
| **CORS** | จำกัด origins |

**ตั้งค่า API Key ผ่าน:** `API_KEY` environment variable (ไม่มี default ใน production)

```bash
# ตัวอย่างการใช้งาน
curl -H "X-API-Key: $API_KEY" \
  https://your-url.trycloudflare.com/tools

# ดู health check (ไม่ต้อง API Key)
curl https://your-url.trycloudflare.com/health
```

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

- **.NET 10** — Runtime
- **mcpdotnet** (v1.2.0.1) — MCP framework (stdio transport)
- **ASP.NET Core Minimal API** — HTTP API wrapper
- **Microsoft.Extensions.Hosting** — Host builder
- **System.Text.Json** — JSON serialization
- **stdio transport** — MCP communication
- **cloudflared/ngrok** — Public tunnel

## 📄 License

MIT
