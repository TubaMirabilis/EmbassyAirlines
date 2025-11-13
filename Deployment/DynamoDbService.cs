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
            Console.WriteLine($"Table '{tableName}' already exists. Skipping creation.");
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
    }
}
