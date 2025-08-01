## Build MCP server 'agent-step-1-sk-mcp-csharp'

- In VS Code terminal:

```bash
cd agent-step-1-sk-mcp-csharp/
dotnet build
```

## Configure MCP start up settings

You can do this in two alternative ways.

### A. At workspace/project level.

Add the mcp.json file within the folder .vscode of your workspace. In this case:

```bash
YOUR-REPO-PATH/sk-mcp-hollow-poc/.vscode/mcp.json
```

This is the recommended approach for a development environment, so it uses a relative path to the C# project:

```json
{
"servers": {
    "agent-step-1": {
      "command": "dotnet",
      "args": ["run", "--project", "./agent-step-1-sk-mcp-csharp/agent-step-1-sk-mcp-csharp.csproj"],
      "env": {
        "DOTNET_ENVIRONMENT": "Development"
      }
    }
},
"inputs": []
}
```


### B. At global/machine context, available for any workspace in VS Code.

Add a mcp.json file to your global VS Code user config folder with comparable (different path to .exe) settings:

Folder path (Same path, different syntax):

```bash
    %APPDATA%\Code\User
    %USERPROFILE%\AppData\Roaming\Code\User
    C:\Users\<YOUR_USER>\AppData\Roaming\Code\User
```

Add the mcp.json file with comparable config:

This is the recommended approach for a development environment, so it uses a relative path to the C# project:

```json
{
"servers": {
    "agent-step-1": {
      "command": "dotnet",
      "args": ["run", "--project", "./agent-step-1-sk-mcp-csharp/agent-step-1-sk-mcp-csharp.csproj"],
      "env": {
        "DOTNET_ENVIRONMENT": "Development"
      }
    }
},
"inputs": []
}
```

This other approach is not recommended as it uses a specific full path to the executable:

```json
{
"servers": {
    "agent-step-1-sk-mcp-csharp": {
        "type": "stdio",
        "command": "C:\\Users\\<YOUR_USER>\\<YOUR_PATH>\\POC-SK-MCP\\sk-mcp-hollow-poc\\agent-step-1-sk-mcp-csharp\\bin\\Debug\\net9.0\\agent-step-1-sk-mcp-csharp.exe",
        "args": []
    }
},
"inputs": []
}
```



