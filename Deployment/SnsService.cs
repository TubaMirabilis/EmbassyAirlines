using Amazon.SimpleNotificationService;

namespace Deployment;

internal static class SnsService
{
    public static async Task<string> EnsureTopicAsync(string topicName)
    {
        using var client = new AmazonSimpleNotificationServiceClient();
        var createResponse = await client.CreateTopicAsync(new Amazon.SimpleNotificationService.Model.CreateTopicRequest
        {
            Name = topicName
        });
        return createResponse.TopicArn;
    }
}
