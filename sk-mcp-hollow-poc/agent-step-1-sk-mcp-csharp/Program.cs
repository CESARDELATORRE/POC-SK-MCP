using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using System.ComponentModel;

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

await builder.Build().RunAsync();

[McpServerToolType]
public static class Step1AgentTool
{
    [McpServerTool, Description("Executes Step 1 and returns the result.")]
    public static string ExecuteStep1()
    {
        // Create a simple logger for console output
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole().SetMinimumLevel(LogLevel.Information);
        });
        var logger = loggerFactory.CreateLogger("Step1AgentTool");
        
        try
        {
            logger.LogInformation("Starting Step 1 execution");
            
            // Construct the path to the file
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..\\..\\..\\data", "JobDescription.txt");
            
            logger.LogDebug("Checking file existence at path: {FilePath}", filePath);
            
            if (!File.Exists(filePath))
            {
                const string errorMessage = "Job description file not found";
                logger.LogError("File not found at path: {FilePath}", filePath);
                throw new FileNotFoundException(errorMessage, filePath);
            }

            logger.LogDebug("Reading file content from: {FilePath}", filePath);
            string dataContent = File.ReadAllText(filePath);
            
            if (string.IsNullOrWhiteSpace(dataContent))
            {
                const string errorMessage = "Job description file is empty or contains only whitespace";
                logger.LogWarning("File content is empty or whitespace: {FilePath}", filePath);
                throw new InvalidOperationException(errorMessage);
            }

            logger.LogInformation("Successfully read job description data, content length: {ContentLength}", dataContent.Length);
            return dataContent;
        }
        catch (FileNotFoundException ex)
        {
            logger.LogError(ex, "Job description file not found during Step 1 execution");
            throw new InvalidOperationException("Unable to locate the job description file. Please ensure the file exists and try again.", ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogError(ex, "Access denied when reading job description file during Step 1 execution");
            throw new InvalidOperationException("Access denied when reading the job description file. Please check file permissions.", ex);
        }
        catch (IOException ex)
        {
            logger.LogError(ex, "I/O error occurred when reading job description file during Step 1 execution");
            throw new InvalidOperationException("An error occurred while reading the job description file. Please try again.", ex);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during Step 1 execution");
            throw new InvalidOperationException("An unexpected error occurred during Step 1 execution. Please try again.", ex);
        }
        finally
        {
            loggerFactory.Dispose();
        }
    }
}