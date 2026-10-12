using System.Diagnostics.CodeAnalysis;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;

namespace AWS.Aspire.ServiceDefaults;

public static class SqsBatchProcessor
{
    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Required for SQS partial batch failure reporting.")]
    public static async Task<SQSBatchResponse> ProcessAsync(
        SQSEvent evnt,
        ILambdaContext context,
        Func<SQSEvent.SQSMessage, ILambdaContext, Task> handler)
    {
        var failures = new List<SQSBatchResponse.BatchItemFailure>();

        foreach (var message in evnt.Records)
        {
            try
            {
                await handler(message, context);
            }
            catch (Exception ex)
            {
                context.Logger.LogError(
                    $"Failed to process message {message.MessageId}: {ex}");

                failures.Add(new SQSBatchResponse.BatchItemFailure
                {
                    ItemIdentifier = message.MessageId
                });
            }
        }

        return new SQSBatchResponse(failures);
    }
}
