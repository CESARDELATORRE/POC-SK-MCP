using System.IO;
using Xunit;

namespace agent_step_1_sk_mcp_csharp.Tests;

public class JobDescriptionReaderTests
{
    private readonly string _testDataPath;

    public JobDescriptionReaderTests()
    {
        _testDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData");
    }

    [Fact]
    public void ReadJobDescription_ShouldReturnContent_WhenFileExists()
    {
        // Arrange
        var reader = new JobDescriptionReader(_testDataPath);

        // Act
        var result = reader.ReadJobDescription("TestJobDescription.txt");

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Contains("Test Job Description", result);
        Assert.Contains("Test Technical Program Manager", result);
    }

    [Fact]
    public void ReadJobDescription_ShouldThrowFileNotFoundException_WhenFileDoesNotExist()
    {
        // Arrange
        var reader = new JobDescriptionReader(_testDataPath);

        // Act & Assert
        var exception = Assert.Throws<FileNotFoundException>(() => 
            reader.ReadJobDescription("NonExistentFile.txt"));
        
        Assert.Contains("Job description file not found", exception.Message);
    }

    [Fact]
    public void JobDescriptionExists_ShouldReturnTrue_WhenFileExists()
    {
        // Arrange
        var reader = new JobDescriptionReader(_testDataPath);

        // Act
        var result = reader.JobDescriptionExists("TestJobDescription.txt");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void JobDescriptionExists_ShouldReturnFalse_WhenFileDoesNotExist()
    {
        // Arrange
        var reader = new JobDescriptionReader(_testDataPath);

        // Act
        var result = reader.JobDescriptionExists("NonExistentFile.txt");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ReadJobDescription_ShouldHandleDifferentPaths()
    {
        // Arrange
        var currentDirectory = Directory.GetCurrentDirectory();
        var reader = new JobDescriptionReader(currentDirectory);

        // Act & Assert - This should not throw an exception even if file doesn't exist
        // because we're testing the path construction logic
        Assert.Throws<FileNotFoundException>(() => 
            reader.ReadJobDescription("some\\non\\existent\\path.txt"));
    }

    [Theory]
    [InlineData("TestJobDescription.txt")]
    [InlineData("./TestJobDescription.txt")]
    public void ReadJobDescription_ShouldHandleVariousPathFormats(string relativePath)
    {
        // Arrange
        var reader = new JobDescriptionReader(_testDataPath);

        // Act
        var result = reader.ReadJobDescription(relativePath);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Test Job Description", result);
    }
}
