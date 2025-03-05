var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAWSLambdaHosting(LambdaEventSource.HttpApi);
var app = builder.Build();
app.MapGet("hello", () => "Hello World!");
app.Run();
