using Amazon.CDK;
using Deployment;

var app = new App();
var env = new Amazon.CDK.Environment
{
    Account = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_ACCOUNT"),
    Region = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_REGION"),
};
var sharedInfraStack = new SharedInfraStack(app, "SharedInfraStack", new StackProps
{
    Env = env
});
new AirportsServiceStack(app, "AirportsServiceStack", new AirportsServiceStackProps
{
    Api = sharedInfraStack.Api,
    Env = env
});
app.Synth();
