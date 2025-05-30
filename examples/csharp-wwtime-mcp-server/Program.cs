using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.Server;
using System.ComponentModel;

var builder = Host.CreateEmptyApplicationBuilder(settings: null);

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

await builder.Build().RunAsync();

[McpServerToolType]
public static class EchoTool
{
    [McpServerTool, Description("Echoes the message back to the client.")]
    public static string Echo(string message) => $"Echo by Cesar's MCP-SERVER in C#: {message}";

    [McpServerTool, Description("Echoes in reverse the message sent.")]
    public static string ReverseEcho(string message) => new string(message.Reverse().ToArray());

    [McpServerTool, Description("Returns the current local time.")]
    public static string GetCurrentLocalTime() => $"The current local time is: {DateTime.Now:hh:mm tt}";
}