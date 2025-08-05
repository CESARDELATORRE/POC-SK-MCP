using System.IO;
using Xunit;

namespace agent_step_1_sk_mcp_csharp.Tests;

public class Step1AgentToolTests
{
    [Fact]
    public void ExecuteStep1_ShouldReturnJobDescriptionContent_WhenFileExists()
    {
        // Arrange & Act
        // This test works because we copy the data file to the test output directory
        var result = Step1AgentTool.ExecuteStep1();

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Contains("Azure AI Delivery Technical Program Manager", result);
    }

    [Fact]
    public void ExecuteStep1_ShouldReturnStringContent()
    {
        // Act
        var result = Step1AgentTool.ExecuteStep1();

        // Assert
        Assert.IsType<string>(result);
        Assert.True(result.Length > 0);
    }

    [Fact]
    public void ExecuteStep1_ShouldContainExpectedJobInformation()
    {
        // Act
        var result = Step1AgentTool.ExecuteStep1();

        // Assert - Check for key information that should be in the job description
        Assert.Contains("Technical Program Manager", result);
        Assert.Contains("Microsoft", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Azure", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ExecuteStep1_ShouldReturnConsistentContent()
    {
        // Act - Call multiple times to ensure consistency
        var result1 = Step1AgentTool.ExecuteStep1();
        var result2 = Step1AgentTool.ExecuteStep1();

        // Assert
        Assert.Equal(result1, result2);
        Assert.True(result1.Length > 1000); // Job description should be substantial
    }
}
