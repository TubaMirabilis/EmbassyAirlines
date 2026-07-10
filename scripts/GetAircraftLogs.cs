#:project ../src/Shared.AWS.CloudWatchLogs
using Shared.AWS.CloudWatchLogs;

var logGroupName = "/aws/lambda/AircraftApiLambda";
var logs = CloudWatchLogsService.FetchRecentLogMessagesAsync(logGroupName);
Console.WriteLine(logs);
