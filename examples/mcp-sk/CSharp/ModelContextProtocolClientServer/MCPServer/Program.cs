// Copyright (c) Microsoft. All rights reserved.

using MCPServer;
using MCPServer.ProjectResources;
using MCPServer.Prompts;
using MCPServer.Resources;
using MCPServer.Tools;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Connectors.InMemory;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

var builder = Host.CreateEmptyApplicationBuilder(settings: null);

// Load and validate configuration
(string embeddingModelId, string chatModelId, string apiKey) = GetConfiguration();

// Register the kernel
IKernelBuilder kernelBuilder = builder.Services.AddKernel();

// Register SK plugins
kernelBuilder.Plugins.AddFromType<DateTimeUtils>();
kernelBuilder.Plugins.AddFromType<WeatherUtils>();
kernelBuilder.Plugins.AddFromType<MailboxUtils>();

// Register SK agent as plugin
kernelBuilder.Plugins.AddFromFunctions("Agents", [AgentKernelFunctionFactory.CreateFromAgent(CreateSalesAssistantAgent(chatModelId, apiKey))]);

// Register embedding generation service and in-memory vector store
kernelBuilder.Services.AddSingleton<VectorStore, InMemoryVectorStore>();
kernelBuilder.Services.AddOpenAIEmbeddingGenerator(embeddingModelId, apiKey);

// Register MCP server
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()

    // Add all functions from the kernel plugins to the MCP server as tools
    .WithTools()

    // Register the `getCurrentWeatherForCity` prompt
    .WithPrompt(PromptDefinition.Create(EmbeddedResource.ReadAsString("getCurrentWeatherForCity.json")))

    // Register vector search as MCP resource template
    .WithResourceTemplate(CreateVectorStoreSearchResourceTemplate())

    // Register the cat image as a MCP resource
    .WithResource(ResourceDefinition.CreateBlobResource(
        uri: "image://cat.jpg",
        name: "cat-image",
        content: EmbeddedResource.ReadAsBytes("cat.jpg"),
        mimeType: "image/jpeg"));

await builder.Build().RunAsync();

/// <summary>
/// Gets configuration.
/// </summary>
static (string EmbeddingModelId, string ChatModelId, string ApiKey) GetConfiguration()
{
    try
    {
        // Load and validate configuration
        IConfigurationRoot config = new ConfigurationBuilder()
            .AddUserSecrets<Program>()
            .AddEnvironmentVariables()
            .Build();

        if (config["OpenAI:ApiKey"] is not { } apiKey)
        {
            const string Message = "Please provide a valid OpenAI:ApiKey to run this sample. See the associated README.md for more details.";
            Console.Error.WriteLine(Message);
            throw new InvalidOperationException(Message);
        }

        string embeddingModelId = config["OpenAI:EmbeddingModelId"] ?? "text-embedding-3-small";
        string chatModelId = config["OpenAI:ChatModelId"] ?? "gpt-4o-mini";

        Console.WriteLine($"Configuration loaded successfully - EmbeddingModel: {embeddingModelId}, ChatModel: {chatModelId}");
        return (embeddingModelId, chatModelId, apiKey);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Failed to load configuration: {ex.Message}");
        throw new InvalidOperationException("Failed to load application configuration. Please check your configuration settings.", ex);
    }
}
static ResourceTemplateDefinition CreateVectorStoreSearchResourceTemplate(Kernel? kernel = null)
{
    return new ResourceTemplateDefinition
    {
        Kernel = kernel,
        ResourceTemplate = new()
        {
            UriTemplate = "vectorStore://{collection}/{prompt}",
            Name = "Vector Store Record Retrieval",
            Description = "Retrieves relevant records from the vector store based on the provided prompt."
        },
        Handler = async (
            RequestContext<ReadResourceRequestParams> context,
            string collection,
            string prompt,
            [FromKernelServices] IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
            [FromKernelServices] VectorStore vectorStore,
            [FromKernelServices] ILogger<Program> logger,
            CancellationToken cancellationToken) =>
        {
            logger.LogInformation("Processing vector store resource request for collection '{Collection}' with prompt '{Prompt}'", collection, prompt);

            try
            {
                // Get the vector store collection
                VectorStoreCollection<Guid, TextDataModel> vsCollection = vectorStore.GetCollection<Guid, TextDataModel>(collection);

                // Check if the collection exists, if not create and populate it
                if (!await vsCollection.CollectionExistsAsync(cancellationToken))
                {
                    logger.LogDebug("Collection '{Collection}' does not exist, creating it", collection);

                    static TextDataModel CreateRecord(string text, ReadOnlyMemory<float> embedding)
                    {
                        return new()
                        {
                            Key = Guid.NewGuid(),
                            Text = text,
                            Embedding = embedding
                        };
                    }

                    string content;
                    try
                    {
                        content = EmbeddedResource.ReadAsString("semantic-kernel-info.txt");
                        logger.LogDebug("Successfully loaded semantic kernel info content");
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to load embedded resource 'semantic-kernel-info.txt'");
                        throw new InvalidOperationException("Failed to load required embedded resource. Please check the application configuration.", ex);
                    }

                    // Create a collection from the lines in the file
                    await vectorStore.CreateCollectionFromListAsync<Guid, TextDataModel>(collection, content.Split('\n'), embeddingGenerator, CreateRecord, logger);
                }

                // Generate embedding for the prompt
                ReadOnlyMemory<float> promptEmbedding;
                try
                {
                    promptEmbedding = (await embeddingGenerator.GenerateAsync(prompt, cancellationToken: cancellationToken)).Vector;
                    logger.LogDebug("Successfully generated embedding for prompt");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to generate embedding for prompt: '{Prompt}'", prompt);
                    throw new InvalidOperationException("Failed to generate embedding for the search prompt. Please try again.", ex);
                }

                // Retrieve top three matching records from the vector store
                var result = vsCollection.SearchAsync(promptEmbedding, top: 3, cancellationToken: cancellationToken);

                // Return the records as resource contents
                List<ResourceContents> contents = [];

                try
                {
                    await foreach (var record in result)
                    {
                        contents.Add(new TextResourceContents()
                        {
                            Text = record.Record.Text,
                            Uri = context.Params!.Uri!,
                            MimeType = "text/plain",
                        });
                    }
                    
                    logger.LogInformation("Successfully retrieved {ResultCount} matching records for prompt", contents.Count);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to retrieve matching records from vector store");
                    throw new InvalidOperationException("Failed to retrieve matching records. Please try again.", ex);
                }

                return new ReadResourceResult { Contents = contents };
            }
            catch (Exception ex) when (!(ex is InvalidOperationException))
            {
                logger.LogError(ex, "Unexpected error processing vector store resource request");
                throw new InvalidOperationException("An unexpected error occurred while processing the vector store request. Please try again.", ex);
            }
        }
    };
}

static Agent CreateSalesAssistantAgent(string chatModelId, string apiKey)
{
    try
    {
        IKernelBuilder kernelBuilder = Kernel.CreateBuilder();

        // Register the SK plugin for the agent to use
        kernelBuilder.Plugins.AddFromType<OrderProcessingUtils>();

        // Register chat completion service
        kernelBuilder.Services.AddOpenAIChatCompletion(chatModelId, apiKey);

        // Using a dedicated kernel with the `OrderProcessingUtils` plugin instead of the global kernel has a few advantages:
        // - The agent has access to only relevant plugins, leading to better decision-making regarding which plugin to use.
        //   Fewer plugins mean less ambiguity in selecting the most appropriate one for a given task.
        // - The plugin is isolated from other plugins exposed by the MCP server. As a result the client's Agent/AI model does
        //   not have access to irrelevant plugins.
        Kernel kernel = kernelBuilder.Build();

        // Define the agent
        return new ChatCompletionAgent()
        {
            Name = "SalesAssistant",
            Instructions = "You are a sales assistant. Place orders for items the user requests and handle refunds.",
            Description = "Agent to invoke to place orders for items the user requests and handle refunds.",
            Kernel = kernel,
            Arguments = new KernelArguments(new PromptExecutionSettings() { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() }),
        };
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Failed to create sales assistant agent: {ex.Message}");
        throw new InvalidOperationException("Failed to create sales assistant agent. Please check the configuration and try again.", ex);
    }
}
