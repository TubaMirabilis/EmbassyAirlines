using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

namespace Deployment;

internal static class DynamoDbService
{
    public static async Task CreateTableIfNotExistsAsync(string tableName)
    {
        Console.WriteLine($"Creating DynamoDB table '{tableName}' if it does not exist...");
        using var ddbClient = new AmazonDynamoDBClient();
        var existingTables = await ddbClient.ListTablesAsync();
        if (existingTables.TableNames.Contains(tableName))
        {
            Console.WriteLine($"Table '{tableName}' already exists.");
            var describeResponse = await ddbClient.DescribeTableAsync(tableName);
            if (describeResponse.Table.TableStatus != TableStatus.ACTIVE)
            {
                await WaitForActiveAsync(ddbClient, tableName, 100);
            }
            return;
        }
        var request = new CreateTableRequest
        {
            TableName = tableName,
            KeySchema =
            [
                new KeySchemaElement("Id", KeyType.HASH)
            ],
            AttributeDefinitions =
            [
                new AttributeDefinition("Id", ScalarAttributeType.S)
            ],
            ProvisionedThroughput = new ProvisionedThroughput(1, 1)
        };
        await ddbClient.CreateTableAsync(request);
        await WaitForActiveAsync(ddbClient, tableName, 100);
    }
    private static async Task WaitForActiveAsync(AmazonDynamoDBClient ddbClient, string tableName, int attempts)
    {
        TableStatus status;
        var currentAttempt = 0;
        do
        {
            Console.WriteLine($"Waiting for table '{tableName}' to become ACTIVE... Attempt {currentAttempt + 1} of {attempts}");
            await Task.Delay(5000);
            var describeResponse = await ddbClient.DescribeTableAsync(tableName);
            status = describeResponse.Table.TableStatus;
            currentAttempt++;
        } while (status != TableStatus.ACTIVE && currentAttempt < attempts);
        if (status != TableStatus.ACTIVE)
        {
            throw new TimeoutException($"Table '{tableName}' did not become ACTIVE within the expected time.");
        }
        Console.WriteLine($"Table '{tableName}' is ACTIVE.");
    }
}
