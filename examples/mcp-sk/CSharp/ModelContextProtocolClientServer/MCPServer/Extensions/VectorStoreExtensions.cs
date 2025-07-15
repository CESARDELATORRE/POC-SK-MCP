// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.VectorData;

namespace MCPServer;

/// <summary>
/// Extensions for vector stores.
/// </summary>
public static class VectorStoreExtensions
{
    /// <summary>
    /// Delegate to create a record from a string.
    /// </summary>
    /// <typeparam name="TKey">Type of the record key.</typeparam>
    /// <typeparam name="TRecord">Type of the record.</typeparam>
    public delegate TRecord CreateRecordFromString<TKey, TRecord>(string text, ReadOnlyMemory<float> vector) where TKey : notnull;

    /// <summary>
    /// Create a <see cref="VectorStoreCollection{TKey, TRecord}"/> from a list of strings by:
    /// </summary>
    /// <typeparam name="TKey">The data type of the record key.</typeparam>
    /// <typeparam name="TRecord">The data type of the record.</typeparam>
    /// <param name="vectorStore">The instance of <see cref="VectorStore"/> used to create the collection.</param>
    /// <param name="collectionName">The name of the collection.</param>
    /// <param name="entries">The list of strings to create records from.</param>
    /// <param name="embeddingGenerator">The text embedding generation service.</param>
    /// <param name="createRecord">The delegate which can create a record for each string and its embedding.</param>
    /// <param name="logger">Optional logger for error reporting.</param>
    /// <returns>The created collection.</returns>
    public static async Task<VectorStoreCollection<TKey, TRecord>> CreateCollectionFromListAsync<TKey, TRecord>(
        this VectorStore vectorStore,
        string collectionName,
        string[] entries,
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
        CreateRecordFromString<TKey, TRecord> createRecord,
        ILogger? logger = null)
        where TKey : notnull
        where TRecord : class
    {
        ArgumentNullException.ThrowIfNull(vectorStore);
        ArgumentNullException.ThrowIfNull(collectionName);
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(embeddingGenerator);
        ArgumentNullException.ThrowIfNull(createRecord);

        logger?.LogInformation("Creating vector store collection '{CollectionName}' from {EntryCount} entries", collectionName, entries.Length);

        try
        {
            // Get and create collection if it doesn't exist.
            var collection = vectorStore.GetCollection<TKey, TRecord>(collectionName);
            await collection.EnsureCollectionExistsAsync().ConfigureAwait(false);

            logger?.LogDebug("Vector store collection '{CollectionName}' ensured to exist", collectionName);

            // Create records and generate embeddings for them.
            var tasks = entries.Select((entry, index) => Task.Run(async () =>
            {
                try
                {
                    if (string.IsNullOrEmpty(entry))
                    {
                        logger?.LogWarning("Skipping empty entry at index {Index}", index);
                        return;
                    }

                    logger?.LogDebug("Processing entry {Index}: '{Entry}'", index, entry[..Math.Min(entry.Length, 50)]);
                    
                    var embedding = await embeddingGenerator.GenerateAsync(entry).ConfigureAwait(false);
                    var record = createRecord(entry, embedding.Vector);
                    await collection.UpsertAsync(record).ConfigureAwait(false);
                    
                    logger?.LogDebug("Successfully processed entry {Index}", index);
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "Failed to process entry {Index}: '{Entry}'", index, entry);
                    throw new InvalidOperationException($"Failed to process entry {index}: '{entry[..Math.Min(entry.Length, 50)]}'", ex);
                }
            }));

            await Task.WhenAll(tasks).ConfigureAwait(false);
            
            logger?.LogInformation("Successfully created vector store collection '{CollectionName}' with {EntryCount} entries", collectionName, entries.Length);
            return collection;
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to create vector store collection '{CollectionName}'", collectionName);
            throw new InvalidOperationException($"Failed to create vector store collection '{collectionName}'. Please check the connection and try again.", ex);
        }
    }
}
