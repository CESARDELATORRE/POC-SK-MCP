using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Net.Http;
using System.Text;
using ModelContextProtocol.Server;
using ModelContextProtocol;
using ModelContextProtocol.Client; // Added namespace for MCP-related functionality
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Connectors.OpenAI;


// Works with MCP
var builder = Host.CreateEmptyApplicationBuilder(settings: null);
builder.Services
            .AddMcpServer()
            .WithStdioServerTransport()
            .WithToolsFromAssembly();

builder.Services.AddLogging(c =>
{
    c.AddConsole().SetMinimumLevel(LogLevel.Information);
    c.AddDebug().SetMinimumLevel(LogLevel.Debug);
});

/*
// Dows not work with MCP
var builder = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((context, config) =>
    {
        config.AddUserSecrets<Program>();
    })
    .ConfigureServices((context, services) =>
    {
        services.AddMcpServer()
                .WithStdioServerTransport()
                .WithToolsFromAssembly();
    });
*/

await builder.Build().RunAsync();

[McpServerToolType]
public static class WorkflowOrchestratorTool
{
    [McpServerTool, Description("Runs a simple workflow orchestration and returns the concatenated output.")]
    public static async Task<string> RunWorkflowOrchestrationAsync()
    {
        // Create a simple logger for console output
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole().SetMinimumLevel(LogLevel.Information);
        });
        var logger = loggerFactory.CreateLogger("WorkflowOrchestratorTool");
        
        try
        {
            logger.LogInformation("Starting workflow orchestration");
            
            var config = new ConfigurationBuilder()
                            .AddUserSecrets<Program>()
                            .AddEnvironmentVariables()
                            .Build();

            var result = new StringBuilder();

            // Configuration validation with enhanced error handling
            if (config["AzureOpenAI:ApiKey"] is not { } apiKey)
            {
                const string error = "Please provide a valid AzureOpenAI:ApiKey to run this sample.";
                logger.LogError("Missing required configuration: AzureOpenAI:ApiKey");
                throw new InvalidOperationException(error);
            }

            if (config["AzureOpenAI:DeploymentName"] is not { } deploymentName)
            {
                const string error = "Please provide a valid AzureOpenAI:DeploymentName to run this sample.";
                logger.LogError("Missing required configuration: AzureOpenAI:DeploymentName");
                throw new InvalidOperationException(error);
            }

            if (config["AzureOpenAI:Endpoint"] is not { } endpoint)
            {
                const string error = "Please provide a valid AzureOpenAI:Endpoint to run this sample.";
                logger.LogError("Missing required configuration: AzureOpenAI:Endpoint");
                throw new InvalidOperationException(error);
            }

            logger.LogInformation("Configuration validated successfully");
            logger.LogDebug("AzureOpenAI:Endpoint: {Endpoint}", endpoint);
            logger.LogDebug("AzureOpenAI:DeploymentName: {DeploymentName}", deploymentName);
            logger.LogDebug("AzureOpenAI:ApiKey: [REDACTED]");

            // Log configuration info
            result.AppendLine($"AzureOpenAI:Endpoint: {endpoint}" );
            result.AppendLine($"AzureOpenAI:DeploymentName: {deploymentName}");
            result.AppendLine($"AzureOpenAI:ApiKey: {apiKey}");

            result.AppendLine($"Base Directory of Orchestrator: {AppContext.BaseDirectory}");

            string mcpClientFilePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..\\..\\..\\..", "agent-step-1-sk-mcp-csharp/bin/Debug/net8.0/agent-step-1-sk-mcp-csharp.exe"));
            result.AppendLine($"MCP Client FilePath: {mcpClientFilePath}");

            logger.LogDebug("MCP Client FilePath: {FilePath}", mcpClientFilePath);

            // Validate MCP client file exists
            if (!File.Exists(mcpClientFilePath))
            {
                const string errorMessage = "MCP client executable not found. Please ensure the agent-step-1-sk-mcp-csharp project is built.";
                logger.LogError("MCP client executable not found at path: {FilePath}", mcpClientFilePath);
                throw new FileNotFoundException(errorMessage, mcpClientFilePath);
            }

            // Create MCP client with error handling
            logger.LogInformation("Creating MCP client for Step 1 agent");
            await using IMcpClient mcpClientStep1 = await McpClientFactory.CreateAsync(
                                                            new StdioClientTransport(
                new()
                {
                    Name = "agent-step-1-sk-mcp-csharp",
                    Command = mcpClientFilePath,  
                    Arguments = Array.Empty<string>()
                }));

            // Retrieve tools with error handling
            logger.LogInformation("Retrieving available tools from MCP client");
            var step1Tools = await mcpClientStep1.ListToolsAsync().ConfigureAwait(false);

            if (step1Tools == null || !step1Tools.Any())
            {
                const string errorMessage = "No tools available from MCP client";
                logger.LogWarning("No tools retrieved from MCP client");
                throw new InvalidOperationException(errorMessage);
            }

            result.AppendLine("Available tools:");
            logger.LogInformation("Retrieved {ToolCount} tools from MCP client", step1Tools.Count());
            
            // Log the available tools
            foreach (var tool in step1Tools)
            {
                result.AppendLine($"Tool Name: {tool.Name}, Description: {tool.Description}");
                logger.LogDebug("Available tool: {ToolName} - {ToolDescription}", tool.Name, tool.Description);
            }

            // Create a Semantic Kernel instance with error handling
            logger.LogInformation("Creating Semantic Kernel instance");
            var kernelBuilder = Kernel.CreateBuilder();
            kernelBuilder.Services
                .AddLogging(c =>
                {
                    c.AddDebug().SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Information);
                    c.AddConsole(); // Add console logging
                });

            try
            {
                kernelBuilder.Services.AddAzureOpenAIChatCompletion(
                    endpoint: endpoint,
                    deploymentName: deploymentName,
                    apiKey: apiKey);

                Kernel kernel = kernelBuilder.Build();
                logger.LogInformation("Semantic Kernel instance created successfully");

                // Add Step1Tools to Semantic Kernel
                logger.LogInformation("Adding MCP tools to Semantic Kernel");
        #pragma warning disable SKEXP0001
                kernel.Plugins.AddFromFunctions("Step1Tools", step1Tools.Select(tool => tool.AsKernelFunction()));
        #pragma warning restore SKEXP0001

                // Enable automatic function calling
        #pragma warning disable SKEXP0001
                OpenAIPromptExecutionSettings executionSettings = new()
                {
                    Temperature = 0,
                    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(options: new() { RetainArgumentTypes = true })
                };
        #pragma warning restore SKEXP0001

                // 1. Run InvokePrompt to summarize what's returned by Step1Tools.ExecuteStep1 tool
                logger.LogInformation("Executing first prompt: summarization");
                var prompt = "Summarize in ten words the content returned by Step1Tools.ExecuteStep1 tool: ";
                var promptResult = await kernel.InvokePromptAsync(prompt, new(executionSettings)).ConfigureAwait(false);
                result.AppendLine($"Prompt: {prompt}");
                result.AppendLine($"Prompt's result against Step1Tool: {promptResult}");
                logger.LogInformation("First prompt executed successfully");

                // 2. Using a prompt to make a question about the content returned by Step1Tools.ExecuteStep1
                logger.LogInformation("Executing second prompt: Q&A");
                var promptQA = "Answer questions about content returned by the tool Step1Tools.ExecuteStep1. What's the name of the Job Position?";
                var promptQAResult = await kernel.InvokePromptAsync(promptQA, new(executionSettings)).ConfigureAwait(false);
                result.AppendLine($"Prompt QA: {promptQA}");
                result.AppendLine($"Prompt QA result against Step1Tool: {promptQAResult}");
                logger.LogInformation("Second prompt executed successfully");

                // 3. Use ChatCompletionAgent to answer questions about the content returned by Step1Tools.ExecuteStep1
                logger.LogInformation("Creating and executing ChatCompletionAgent");
                ChatCompletionAgent agent = new()
                {
                    Instructions = "Answer questions about content returned by the tool Step1Tools.ExecuteStep1.",
                    Name = "QA_Agent_for_Step1Tools.ExecuteStep1", //Name must not have spaces or will violate the expected pattern.
                    Kernel = kernel,
                    Arguments = new KernelArguments(executionSettings),
                };

                // Declare agentResponseItem with the correct type
                AgentResponseItem<Microsoft.SemanticKernel.ChatMessageContent>? agentResponseItem = null;
                var qaAgentResponse = string.Empty;
                try
                {
                            // Respond to user input, invoking functions where appropriate.
                    agentResponseItem = await agent.InvokeAsync("What's the name of the Job Position?").FirstAsync();
                    qaAgentResponse = agentResponseItem.Message.ToString(); // Use the Message property to access the content
                    logger.LogInformation("ChatCompletionAgent executed successfully");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error during ChatCompletionAgent execution");
                    throw new InvalidOperationException("Failed to execute ChatCompletionAgent. Please check the configuration and try again.", ex);
                }
                
                // Define the other steps of the workflow
                var step1 = "Step 1 execution: " + promptResult.ToString();
                var step2 = "Step 2 execution: " + promptQAResult.ToString();
                var step3 = "Step 3 execution: " + qaAgentResponse;

                // Concatenate the outputs in an understandable way
                result.AppendLine("Workflow Orchestration Output:");
                result.AppendLine(step1); 
                result.AppendLine(step2);
                result.AppendLine(step3);

                // Ensure results are returned for VS Code Chat Agent Mode
                var finalResult = result.ToString();
                logger.LogInformation("Workflow orchestration completed successfully");
                return finalResult;

            }
            catch (UnauthorizedAccessException ex)
            {
                logger.LogError(ex, "Authentication failed during Azure OpenAI communication");
                throw new InvalidOperationException("Authentication failed when communicating with Azure OpenAI. Please check your API key and try again.", ex);
            }
            catch (HttpRequestException ex)
            {
                logger.LogError(ex, "Network error during Azure OpenAI communication");
                throw new InvalidOperationException("Network error when communicating with Azure OpenAI. Please check your connection and try again.", ex);
            }
        }
        catch (FileNotFoundException ex)
        {
            logger.LogError(ex, "Required file not found during workflow orchestration");
            throw new InvalidOperationException("Required file not found. Please ensure all necessary files are present and try again.", ex);
        }
        catch (InvalidOperationException)
        {
            // Re-throw InvalidOperationException as they already have user-friendly messages
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during workflow orchestration");
            throw new InvalidOperationException("An unexpected error occurred during workflow orchestration. Please try again.", ex);
        }
        finally
        {
            loggerFactory.Dispose();
        }
    }
}

