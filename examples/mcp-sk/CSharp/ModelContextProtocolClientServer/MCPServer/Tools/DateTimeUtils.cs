// Copyright (c) Microsoft. All rights reserved.

using System.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace MCPServer.Tools;

/// <summary>
/// A collection of utility methods for working with date time.
/// </summary>
internal sealed class DateTimeUtils
{
    /// <summary>
    /// Retrieves the current date time in UTC.
    /// </summary>
    /// <param name="logger">Logger for error and audit logging.</param>
    /// <returns>The current date time in UTC.</returns>
    [KernelFunction, Description("Retrieves the current date time in UTC.")]
    public static string GetCurrentDateTimeInUtc([FromKernelServices] ILogger<DateTimeUtils> logger)
    {
        try
        {
            var utcNow = DateTime.UtcNow;
            var formattedDateTime = utcNow.ToString("yyyy-MM-dd HH:mm:ss");
            
            logger.LogDebug("Current UTC date time retrieved: {DateTime}", formattedDateTime);
            return formattedDateTime;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to retrieve current UTC date time");
            return "Unable to retrieve current date time";
        }
    }
}
