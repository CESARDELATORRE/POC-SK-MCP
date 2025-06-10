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
public static class Step1AgentTool
{
    [McpServerTool, Description("Executes Step 1 and returns the result.")]
    public static string ExecuteStep1()
    {
        //return "Step 1 execution - From SK-MCP Agent";
        //return "A beautiful young princess named Snow White, envied by her wicked stepmother the Queen for being “the fairest of them all,” flees into the forest when the Queen seeks to kill her out of jealousy; taken in by seven kindly dwarfs who work in the mines, Snow White finds refuge and happiness, but the Queen discovers she’s still alive and uses dark magic to disguise herself and trick the girl into eating a poisoned apple that puts her into a deathlike sleep; the dwarfs, heartbroken, lay her in a glass coffin until a passing prince, moved by her beauty and innocence, kisses her—breaking the curse and awakening her; ultimately, good triumphs over evil, as Snow White and the prince unite in love and the wicked Queen meets a deserved end, bringing peace and justice to the kingdom.";
        // Construct the path to the file
        string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..\\..\\..\\data", "JobDescription.txt");

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Job description file not found at {filePath}");
        }

        string dataContent = File.ReadAllText(filePath);

        return dataContent;
    }
}