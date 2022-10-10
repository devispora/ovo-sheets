using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Add AWS Lambda support. When application is run in Lambda Kestrel is swapped out as the web server with Amazon.Lambda.AspNetCoreServer. This
// package will act as the webserver translating request and responses between the Lambda event source and ASP.NET Core.
builder.Services.AddAWSLambdaHosting(LambdaEventSource.RestApi);

string[] scopes =
{
    DriveService.Scope.Drive,
    DriveService.Scope.DriveFile,
    DriveService.Scope.DriveMetadata,
    SheetsService.Scope.Drive,
    SheetsService.Scope.Spreadsheets,
    SheetsService.Scope.DriveFile
};

ServiceAccountCredential? credential = null;

var credentialFile = builder.Configuration["Google:CredentialFile"];

using (var stream = new FileStream(credentialFile, FileMode.Open, FileAccess.Read))
{
    credential = ServiceAccountCredential.FromServiceAccountData(stream);

    credential = new ServiceAccountCredential(new ServiceAccountCredential.Initializer(credential.Id)
    {
        User = credential.User,
        Key = credential.Key,
        KeyId = credential.KeyId,
        Scopes = scopes
    });
}

if (credential != null)
{
    builder.Services.AddSingleton(new DriveService(new BaseClientService.Initializer
    {
        ApplicationName = "OvO Sheets",
        HttpClientInitializer = credential
    }));

    builder.Services.AddSingleton(new SheetsService(new BaseClientService.Initializer
    {
        ApplicationName = "OvO Sheets",
        HttpClientInitializer = credential
    }));
}

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/", () => "Welcome to running ASP.NET Core Minimal API on AWS Lambda");

app.Run();