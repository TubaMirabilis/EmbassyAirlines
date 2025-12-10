#:package AWSSDK.CloudWatchLogs
using Amazon.CloudWatchLogs;

using var client = new AmazonCloudWatchLogsClient();
const int limit = 50;
var logGroupName = "/aws/lambda/AircraftApiLambda";

var logStreamsResponse = await client.DescribeLogStreamsAsync(new Amazon.CloudWatchLogs.Model.DescribeLogStreamsRequest
{
    LogGroupName = logGroupName,
    OrderBy = "LastEventTime",
    Descending = true,
    Limit = 1
});

var logStreamName = logStreamsResponse.LogStreams[0].LogStreamName;

var logEventsResponse = await client.GetLogEventsAsync(new Amazon.CloudWatchLogs.Model.GetLogEventsRequest
{
    LogGroupName = logGroupName,
    LogStreamName = logStreamName,
    Limit = limit,
    StartFromHead = false
});

var logs = string.Join(Environment.NewLine, logEventsResponse.Events.Select(e => e.Message));
Console.WriteLine(logs);