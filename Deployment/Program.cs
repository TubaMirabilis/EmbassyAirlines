using Amazon.CDK;
using Deployment;

var app = new App();
var env = new Amazon.CDK.Environment
{
    Account = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_ACCOUNT"),
    Region = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_REGION"),
};
var networkingStack = new NetworkingStack(app, "NetworkingStack", new StackProps
{
    Env = env
});
var databaseStack = new DatabaseStack(app, "DatabaseStack", new DatabaseStackProps
{
    Vpc = networkingStack.Vpc,
    Env = env
});
var sharedInfraStack = new SharedInfraStack(app, "SharedInfraStack", new StackProps
{
    Env = env
});
new AirportsServiceStack(app, "AirportsServiceStack", new AirportsServiceStackProps
{
    Api = sharedInfraStack.Api,
    Env = env,
    Vpc = networkingStack.Vpc
});
new AircraftServiceStack(app, "AircraftServiceStack", new AircraftServiceStackProps
{
    Api = sharedInfraStack.Api,
    DbInstance = databaseStack.DbInstance,
    DbProxy = databaseStack.DbProxy,
    DbProxySecurityGroup = databaseStack.DbProxySecurityGroup,
    Env = env,
    Vpc = networkingStack.Vpc
});
app.Synth();
