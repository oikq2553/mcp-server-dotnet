using McpDotNet;
using McpDotNet.Server;
using McpServer.Services;
using McpServer.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

var apiBaseUrl = builder.Configuration["API_BASE_URL"] ?? Environment.GetEnvironmentVariable("API_BASE_URL") ?? "http://localhost:5000";

builder.Services.AddHttpClient("ApiClient", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
});

builder.Services.AddSingleton<ApiClient>();

builder.Services.AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

var host = builder.Build();

TodoTools.ApiClient = host.Services.GetRequiredService<ApiClient>();

host.Run();
