using System.IO;

namespace agent_step_1_sk_mcp_csharp.Tests;

/// <summary>
/// A more testable version of the file reading logic for demonstration purposes
/// This shows how the original code could be refactored to be more testable
/// </summary>
public class JobDescriptionReader
{
    private readonly string _basePath;

    public JobDescriptionReader(string? basePath = null)
    {
        _basePath = basePath ?? AppDomain.CurrentDomain.BaseDirectory;
    }

    public string ReadJobDescription(string relativePath = "..\\..\\..\\data\\JobDescription.txt")
    {
        string filePath = Path.Combine(_basePath, relativePath);

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Job description file not found at {filePath}");
        }

        return File.ReadAllText(filePath);
    }

    public bool JobDescriptionExists(string relativePath = "..\\..\\..\\data\\JobDescription.txt")
    {
        string filePath = Path.Combine(_basePath, relativePath);
        return File.Exists(filePath);
    }
}
