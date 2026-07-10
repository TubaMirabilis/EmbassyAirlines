using Amazon.CloudWatchLogs;
using Amazon.CloudWatchLogs.Model;

namespace Shared.AWS.CloudWatchLogs;

public static class CloudWatchLogsService
{
    public static async Task<string> FetchRecentLogMessagesAsync(string logGroupName)
    {
        using var client = new AmazonCloudWatchLogsClient();
        var logStreamsResponse = await client.DescribeLogStreamsAsync(new DescribeLogStreamsRequest
        {
            LogGroupName = logGroupName,
            OrderBy = "LastEventTime",
            Descending = true,
            Limit = 1
        });
        var logStreamName = logStreamsResponse.LogStreams[0].LogStreamName;
        var logEventsResponse = await client.GetLogEventsAsync(new GetLogEventsRequest
        {
            LogGroupName = logGroupName,
            LogStreamName = logStreamName,
            StartFromHead = false
        });
        return string.Join(Environment.NewLine, logEventsResponse.Events.Select(e => e.Message));
    }
}
