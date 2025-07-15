// Copyright (c) Microsoft. All rights reserved.

using MCPServer.ProjectResources;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPServer.Tools;

/// <summary>
/// A collection of utility methods for working with mailbox.
/// </summary>
internal sealed class MailboxUtils
{
    /// <summary>
    /// Summarizes unread emails in the mailbox by using MCP sampling
    /// mechanism for summarization.
    /// </summary>
    [KernelFunction]
    public static async Task<string> SummarizeUnreadEmailsAsync(
        [FromKernelServices] IMcpServer server,
        [FromKernelServices] ILogger<MailboxUtils> logger)
    {
        logger.LogInformation("Starting email summarization process");

        try
        {
            if (server.ClientCapabilities?.Sampling is null)
            {
                const string errorMessage = "The client does not support sampling.";
                logger.LogError(errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            logger.LogDebug("Client sampling capabilities confirmed");

            // Create two sample emails with attachments
            Email email1, email2;
            
            try
            {
                logger.LogDebug("Creating sample emails with attachments");
                email1 = new Email
                {
                    Sender = "sales.report@example.com",
                    Subject = "Carretera Sales Report - Jan & Jun 2014",
                    Body = "Hi there, I hope this email finds you well! Please find attached the sales report for the first half of 2014. " +
                           "Please review the report and provide your feedback today, if possible." +
                           "By the way, we're having a BBQ this Saturday at my place, and you're welcome to join. Let me know if you can make it!",
                    Attachments = [EmbeddedResource.ReadAsBytes("SalesReport2014.png")]
                };

                email2 = new Email
                {
                    Sender = "hr.department@example.com",
                    Subject = "Employee Birthdays and Positions",
                    Body = "Attached is the list of employee birthdays and their positions. Please check it and let me know of any updates by tomorrow." +
                           "Also, we're planning a hike this Sunday morning. It would be great if you could join us. Let me know if you're interested!",
                    Attachments = [EmbeddedResource.ReadAsBytes("EmployeeBirthdaysAndPositions.png")]
                };
                
                logger.LogDebug("Successfully created {EmailCount} sample emails", 2);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to create sample emails or load embedded resources");
                throw new InvalidOperationException("Failed to create sample emails. Please check that the required resources are available.", ex);
            }

            CreateMessageRequestParams request = new()
            {
                SystemPrompt = "You are a helpful assistant. You will be provided with a list of emails. Please summarize them. Each email is followed by its attachments.",
                Messages = CreateMessagesFromEmails(email1, email2),
                Temperature = 0
            };

            logger.LogDebug("Sending sampling request to client with {MessageCount} messages", request.Messages.Count);

            // Send the sampling request to the client to summarize the emails
            CreateMessageResult result;
            try
            {
                result = await server.RequestSamplingAsync(request, cancellationToken: CancellationToken.None);
                logger.LogDebug("Successfully received sampling response from client");
            }
            catch (OperationCanceledException ex)
            {
                logger.LogWarning(ex, "Email summarization request was cancelled");
                throw new InvalidOperationException("The email summarization request was cancelled. Please try again.", ex);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to request sampling from MCP client");
                throw new InvalidOperationException("Failed to communicate with the MCP client. Please ensure the client is available and try again.", ex);
            }

            // Validate the response
            if (result?.Content?.Text is null)
            {
                logger.LogError("Received null or empty response from MCP client");
                throw new InvalidOperationException("The MCP client returned an empty response. Please try again.");
            }

            logger.LogInformation("Email summarization completed successfully");
            return result.Content.Text;
        }
        catch (Exception ex) when (!(ex is InvalidOperationException))
        {
            logger.LogError(ex, "Unexpected error occurred during email summarization");
            throw new InvalidOperationException("An unexpected error occurred while processing the email summarization. Please try again.", ex);
        }
    }

    /// <summary>
    /// Creates a list of SamplingMessage objects from a list of emails.
    /// </summary>
    /// <param name="emails">The list of emails.</param>
    /// <returns>A list of SamplingMessage objects.</returns>
    private static List<SamplingMessage> CreateMessagesFromEmails(params Email[] emails)
    {
        ArgumentNullException.ThrowIfNull(emails);
        
        var messages = new List<SamplingMessage>();

        foreach (var email in emails)
        {
            if (email == null)
            {
                continue; // Skip null emails
            }

            try
            {
                messages.Add(new SamplingMessage
                {
                    Role = Role.User,
                    Content = new Content
                    {
                        Text = $"Email from {email.Sender} with subject {email.Subject}. Body: {email.Body}",
                        Type = "text",
                        MimeType = "text/plain"
                    }
                });

                if (email.Attachments != null && email.Attachments.Count != 0)
                {
                    foreach (var attachment in email.Attachments)
                    {
                        if (attachment != null)
                        {
                            messages.Add(new SamplingMessage
                            {
                                Role = Role.User,
                                Content = new Content
                                {
                                    Type = "image",
                                    Data = Convert.ToBase64String(attachment),
                                    MimeType = "image/png",
                                }
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the error but continue processing other emails
                // In a real scenario, we might want to inject ILogger here too
                throw new InvalidOperationException($"Failed to process email from {email.Sender}: {email.Subject}", ex);
            }
        }

        return messages;
    }

    /// <summary>
    /// Represents an email.
    /// </summary>
    private sealed class Email
    {
        /// <summary>
        /// Gets or sets the email sender.
        /// </summary>
        public required string Sender { get; set; }

        /// <summary>
        /// Gets or sets the email subject.
        /// </summary>
        public required string Subject { get; set; }

        /// <summary>
        /// Gets or sets the email body.
        /// </summary>
        public required string Body { get; set; }

        /// <summary>
        /// Gets or sets the email attachments.
        /// </summary>
        public List<byte[]>? Attachments { get; set; }
    }
}
