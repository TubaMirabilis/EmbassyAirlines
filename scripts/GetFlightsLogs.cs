#:project ../src/Shared.AWS.CloudWatchLogs
using Shared.AWS.CloudWatchLogs;

var logGroupName = "/aws/lambda/FlightsApiLambda";
var logs = CloudWatchLogsService.FetchRecentLogMessagesAsync(logGroupName);
Console.WriteLine(logs);
