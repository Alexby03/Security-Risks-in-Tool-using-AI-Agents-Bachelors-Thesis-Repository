using OpenAI;
using OpenAI.Chat;
using localdotnet.Services;
using System.ClientModel.Primitives;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<SqlService>();
builder.Services.AddTransient<Generator>();
builder.Services.AddTransient<Tester>();

builder.Services.AddSingleton<ChatClient>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var endpoint = config["AZURE_OPENAI_ENDPOINT"] ?? Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
    var deploymentName = config["DEPLOYMENT_NAME"] ?? Environment.GetEnvironmentVariable("DEPLOYMENT_NAME");
    var apiKey = config["AZURE_OPENAI_KEY"] ?? Environment.GetEnvironmentVariable("AZURE_OPENAI_KEY");

    if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(deploymentName))
        throw new InvalidOperationException("Set AZURE_OPENAI_ENDPOINT and DEPLOYMENT_NAME in config or environment variables.");

    if (string.IsNullOrWhiteSpace(apiKey))
        throw new InvalidOperationException("Endpoint requires an API Key variable AZURE_OPENAI_KEY.");

    var clientOptions = new OpenAIClientOptions 
    { 
        Endpoint = new Uri(endpoint) 
    };
    
    var openAIClient = new OpenAIClient(new System.ClientModel.ApiKeyCredential(apiKey), clientOptions);

    return openAIClient.GetChatClient(deploymentName);
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.MapPost("/tools/generate-scenarios", (
    IServiceProvider serviceProvider,
    IConfiguration config,
    ILogger<Program> logger) =>
{
    logger.LogInformation("Received POST /tools/generate-scenarios. Starting Task.Run...");

    _ = Task.Run(async () => 
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var generator = scope.ServiceProvider.GetRequiredService<Generator>();
            await generator.RunGenerationLoop();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "A critical error occurred within the background thread.");
        }
    });

    return Results.Accepted(value: new { 
        Message = "Generation of scenarios has started in the background, follow the process in the server console." 
    });
});

app.MapPost("/tools/test-scenarios", (
    IServiceScopeFactory scopeFactory,
    IConfiguration config,
    ILogger<Program> logger) =>
{
    logger.LogInformation("Received POST /tools/test-scenarios. Starting Task.Run with DeepSeek patch...");

    _ = Task.Run(async () =>
    {
        try
        {
            string target = Environment.GetEnvironmentVariable("TESTING_TARGET") 
                ?? "DeepSeek-V3.1";

            var endpoint = config["AZURE_OPENAI_ENDPOINT"] ?? Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
            var apiKey = config["AZURE_OPENAI_KEY"] ?? Environment.GetEnvironmentVariable("AZURE_OPENAI_KEY");

            var opts = new OpenAIClientOptions { Endpoint = new Uri(endpoint!) };
            
            opts.Transport = new HttpClientPipelineTransport(new HttpClient(new DeepSeekWorkaroundHandler 
            { 
                InnerHandler = new HttpClientHandler() 
            }));

            var baseClient = new OpenAIClient(new System.ClientModel.ApiKeyCredential(apiKey!), opts);

            using var scope = scopeFactory.CreateScope();
            var sql = scope.ServiceProvider.GetRequiredService<SqlService>();
            var testerLogger = scope.ServiceProvider.GetRequiredService<ILogger<Tester>>();

            var tester = new Tester(
                baseClient.GetChatClient(target), 
                sql, 
                testerLogger, 
                config
            );

            await tester.RunTestingLoop();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "A critical error occurred within the testing background thread.");
        }
    });

    return Results.Accepted(value: new
    {
        Message = "Testing of scenarios has started in the background with DeepSeek patch. Follow progress in console."
    });
});

app.MapPost("/tools/judge-failures", (
    IServiceScopeFactory scopeFactory,
    IConfiguration config,
    ILogger<Program> logger) =>
{
    logger.LogInformation("Received POST /tools/judge-failures. Starting Task.Run...");

    _ = Task.Run(async () =>
    {
        try
        {
            string target = Environment.GetEnvironmentVariable("JUDGING_TARGET")
                ?? throw new InvalidOperationException("JUDGING_TARGET env var is required.");

            var endpoint = config["AZURE_OPENAI_ENDPOINT"] ?? Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
            var apiKey = config["AZURE_OPENAI_KEY"] ?? Environment.GetEnvironmentVariable("AZURE_OPENAI_KEY");

            var opts = new OpenAIClientOptions { Endpoint = new Uri(endpoint!) };

            opts.Transport = new HttpClientPipelineTransport(new HttpClient(new DeepSeekWorkaroundHandler 
            { 
                InnerHandler = new HttpClientHandler() 
            }));

            var baseClient = new OpenAIClient(new System.ClientModel.ApiKeyCredential(apiKey!), opts);

            var allModels = new[] { "Mistral-Large-3", "DeepSeek-V3.1", "gpt-4o" };
            var judges = allModels.Where(m => m != target).ToArray();

            if (judges.Length < 2)
                throw new InvalidOperationException($"Could not find 2 judges for target '{target}'.");

            using var scope = scopeFactory.CreateScope();
            var sql = scope.ServiceProvider.GetRequiredService<SqlService>();
            var judgeLogger = scope.ServiceProvider.GetRequiredService<ILogger<Judger>>();

            var judger = new Judger(
                sql, judgeLogger,
                baseClient.GetChatClient(judges[0]), judges[0],
                baseClient.GetChatClient(judges[1]), judges[1],
                target
            );

            await judger.RunJudgingLoop();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "A critical error occurred within the judging background thread.");
        }
    });

    return Results.Accepted(value: new
    {
        Message = "Judging of failed scenarios has started in the background, follow the progress in the server console."
    });
});

app.Run();

// DeepSeek incompatibility with OpenAI SDK fix
public class DeepSeekWorkaroundHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var response = await base.SendAsync(request, ct);
        if (response.Content != null)
        {
            var content = await response.Content.ReadAsStringAsync();
            if (content.Contains("\"finish_reason\":\"tool_call\""))
            {
                content = content.Replace("\"finish_reason\":\"tool_call\"", "\"finish_reason\":\"tool_calls\"");
                response.Content = new StringContent(content, System.Text.Encoding.UTF8, "application/json");
            }
        }
        return response;
    }
}