// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace MCPServer.Tools;

/// <summary>
/// A collection of utility methods for working with orders.
/// </summary>
internal sealed class OrderProcessingUtils
{
    /// <summary>
    /// Places an order for the specified item.
    /// </summary>
    /// <param name="itemName">The name of the item to be ordered.</param>
    /// <param name="logger">Logger for error and audit logging.</param>
    /// <returns>A string indicating the result of the order placement.</returns>
    [KernelFunction]
    public string PlaceOrder(string itemName, [FromKernelServices] ILogger<OrderProcessingUtils> logger)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(itemName))
            {
                logger.LogWarning("Order placement attempted with empty or null item name");
                return "failed: Item name is required";
            }

            logger.LogInformation("Processing order for item: {ItemName}", itemName);

            // In a real implementation, this would interact with external order processing systems
            // For this POC, we simulate success
            
            logger.LogInformation("Order successfully placed for item: {ItemName}", itemName);
            return "success";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to place order for item: {ItemName}", itemName);
            return "failed: An error occurred while processing the order";
        }
    }

    /// <summary>
    /// Executes a refund for the specified item.
    /// </summary>
    /// <param name="itemName">The name of the item to be refunded.</param>
    /// <param name="logger">Logger for error and audit logging.</param>
    /// <returns>A string indicating the result of the refund execution.</returns>
    [KernelFunction]
    public string ExecuteRefund(string itemName, [FromKernelServices] ILogger<OrderProcessingUtils> logger)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(itemName))
            {
                logger.LogWarning("Refund execution attempted with empty or null item name");
                return "failed: Item name is required";
            }

            logger.LogInformation("Processing refund for item: {ItemName}", itemName);

            // In a real implementation, this would interact with external refund processing systems
            // For this POC, we simulate success
            
            logger.LogInformation("Refund successfully executed for item: {ItemName}", itemName);
            return "success";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to execute refund for item: {ItemName}", itemName);
            return "failed: An error occurred while processing the refund";
        }
    }
}
