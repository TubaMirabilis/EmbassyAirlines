#:project ../src/Shared.AWS.CloudWatchLogs
using Shared.AWS.CloudWatchLogs;

var logGroupName = "/aws/lambda/AirportsApiLambda";
var logs = CloudWatchLogsService.FetchRecentLogMessagesAsync(logGroupName);
Console.WriteLine(logs);
