using Amazon.CDK;
using Deployment;

var app = new App();
new EmbassyAirlinesStack(app, "EmbassyAirlinesStack", new StackProps
{
    Env = new Amazon.CDK.Environment
    {
        Account = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_ACCOUNT"),
        Region = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_REGION"),
    }
});
app.Synth();
