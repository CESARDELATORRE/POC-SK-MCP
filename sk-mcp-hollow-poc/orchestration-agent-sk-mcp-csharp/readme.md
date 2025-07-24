## Build Semantic Kernel Agent MCP server 'orchestration-agent-sk-mcp-csharp'

- In VS Code terminal:
    cd orchestration-agent-sk-mcp-csharp/
    dotnet build

## Configure MCP start up settings

You can do this in two alternative ways.

### A. At workspace/project level.

======== TBD =======
======== TBD =======
======== TBD =======

### B. At global/machine context, available for any workspace in VS Code.

Add a mcp.json file to your global VS Code user config folder with comparable (different path to .exe) settings:

Folder path (Same path, different syntax):

    %APPDATA%\Code\User
    
    %USERPROFILE%\AppData\Roaming\Code\User
    
    C:\Users\<YOUR_USER>\AppData\Roaming\Code\User

Add the mcp.json file with comparable config:

    {
	"servers": {
        "orchestration-agent-sk-mcp-csharp": {
			"type": "stdio",
			"command": "C:\\Users\\<YOUR_USER>\\<YOUR_PATH>\\\\POC-SK-MCP\\sk-mcp-hollow-poc\\orchestration-agent-sk-mcp-csharp\\bin\\Debug\\net9.0\\orchestration-agent-sk-mcp-csharp.exe",
			"args": []
		}
	},
	"inputs": []
}


## Configure your users secrets for using a model in AZURE OPENAI SERVICES

1. You need to provision/deploy a genai model such as gpt-4o-mini in Azure OpenAI Services in your Azure subscription.

2. Once it's deployed, from the Azure portal, take the following data:

- AzureOpenAI:Endpoint
- AzureOpenAI:DeploymentName
- AzureOpenAI:ApiKey

3. With those values, create an empty *secrets.json* file in this path:

Alternative ways to go:

    %APPDATA%\Microsoft\UserSecrets\<user-secrets-id>\

    %USERPROFILE%\AppData\Roaming\Microsoft\UserSecrets\<user-secrets-id>\

The user-secrets-id should be the following because it's the id used in the *orchestration-agent-sk-mcp-csharp.csproj*:

    8ca9129d-caec-411b-aa66-b43ef94e65c1

So, basically, the final path for the file should be this one:

%APPDATA%\Microsoft\UserSecrets\8ca9129d-caec-411b-aa66-b43ef94e65c1\secrets.json

4. Copy/paste and add your values for your secrets.json file:

*secrets.json file:*
    {
    "AzureOpenAI:Endpoint": "https://your-endpoint-url/",
    "AzureOpenAI:DeploymentName": "your-model-deployment-name",
    "AzureOpenAI:ApiKey": "your-model-api-key"
    }