using Elastic.Ingest.Elasticsearch;
using Elastic.Ingest.Elasticsearch.DataStreams;
using Elastic.Serilog.Sinks;
using Elastic.Transport;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Context;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<ApiKeyAppNameOptions>(builder.Configuration.GetSection("ApiKeyAppNamePairs"));
var elasticsearchOptions = builder.Configuration.GetSection("ElasticSearch").Get<ElasticsearchOptions>();

if (elasticsearchOptions == null)
{
    throw new InvalidOperationException("Elasticsearch options not found.");
}

if (!Uri.TryCreate(elasticsearchOptions.Uri, UriKind.Absolute, out var elasticsearchUri))
{
    throw new InvalidOperationException("ElasticSearch:Uri is missing or invalid.");
}

builder.Host.UseSerilog((_, _, cfg) =>
{
    cfg.Enrich.FromLogContext()
        .WriteTo.Logger(consoleLogger =>
        {
            consoleLogger
                .Filter.ByExcluding(logEvent =>
                    logEvent.Properties.TryGetValue("SendToElastic", out var value)
                    && value is ScalarValue { Value: true }
                )
                .WriteTo.Console();
        })
        .WriteTo.Logger(elasticLogger =>
        {
            elasticLogger
                .Filter.ByIncludingOnly(logEvent =>
                    logEvent.Properties.TryGetValue("SendToElastic", out var value)
                    && value is ScalarValue { Value: true }
                )
                .WriteTo.Elasticsearch(
                    [elasticsearchUri],
                    options =>
                    {
                        options.DataStream = new DataStreamName("logs", "http-log", "default");
                        options.BootstrapMethod = BootstrapMethod.Failure;
                    },
                    transport =>
                    {
                        transport.Authentication(
                            new BasicAuthentication(elasticsearchOptions.Username, elasticsearchOptions.Password)
                        );
                    }
                );
        });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection();

app.MapPost("/log", (
        HttpRequest request,
        LogSubmissionSingle submission,
        ILogger<Program> logger,
        IOptions<ApiKeyAppNameOptions> apiKeyAppNameOptions
    ) =>
    {
        if (!request.Headers.TryGetValue("X-Api-Key", out var apiKey))
        {
            logger.LogWarning("Unauthorized single log submission attempt.");
            return Results.Unauthorized();
        }

        string apiKeyString = apiKey.ToString();
        KeyValuePair<string, string> apiKeyAppNamePair = apiKeyAppNameOptions.Value.FirstOrDefault(pair => pair.Value == apiKeyString);

        if (string.IsNullOrEmpty(apiKeyAppNamePair.Value))
        {
            logger.LogWarning("Unauthorized single log submission attempt.");
            return Results.Unauthorized();
        }

        using (LogContext.PushProperty("AppName", apiKeyAppNamePair.Key))
        using (LogContext.PushProperty("LogSource", submission.LogSource))
        using (LogContext.PushProperty("SendToElastic", true))
        {
#pragma warning disable CA2254
            logger.Log(submission.LogLevel, submission.LogMessage, submission.LogArgs);
#pragma warning restore CA2254
        }

        return Results.NoContent();
    })
    .WithName("Log")
    .AddOpenApiOperationTransformer((operation, _, _) =>
    {
        operation.Summary = "Logs a log submission.";
        return Task.CompletedTask;
    });

app.MapPost("/log/bulk", (
        HttpRequest request,
        LogSubmissionBulk bulkSubmission,
        ILogger<Program> logger,
        IOptions<ApiKeyAppNameOptions> apiKeyAppNameOptions
    ) =>
    {
        if (!request.Headers.TryGetValue("X-Api-Key", out var apiKey))
        {
            logger.LogWarning("Unauthorized bulk log submission attempt.");
            return Results.Unauthorized();
        }

        string apiKeyString = apiKey.ToString();
        KeyValuePair<string, string> apiKeyAppNamePair = apiKeyAppNameOptions.Value.FirstOrDefault(pair => pair.Value == apiKeyString);

        if (string.IsNullOrEmpty(apiKeyAppNamePair.Key))
        {
            logger.LogWarning("Unauthorized bulk log submission attempt.");
            return Results.Unauthorized();
        }

        using (LogContext.PushProperty("AppName", apiKeyAppNamePair.Key))
        using (LogContext.PushProperty("LogSource", bulkSubmission.LogSource))
        using (LogContext.PushProperty("SendToElastic", true))
        {
            foreach (var submission in bulkSubmission.Submissions)
            {
#pragma warning disable CA2254
                logger.Log(submission.LogLevel, submission.LogMessage, submission.LogArgs);
#pragma warning restore CA2254
            }
        }

        return Results.NoContent();
    }).WithName("Bulk log")
    .AddOpenApiOperationTransformer((operation, _, _) =>
    {
        operation.Summary = "For logging a lot of log submissions.";
        return Task.CompletedTask;
    });

app.Run();

internal record LogSubmissionBulk
{
    public required LogSubmission[] Submissions { get; init; }
    public required string LogSource { get; init; }
}

internal record LogSubmission
{
    public required LogLevel LogLevel { get; init; }
    public required string LogMessage { get; init; }
    public required object?[] LogArgs { get; init; }
}

internal record LogSubmissionSingle : LogSubmission
{
    public required string LogSource { get; init; }
}

internal sealed class ApiKeyAppNameOptions : Dictionary<string, string>;

internal record ElasticsearchOptions
{
    public required string Uri { get; init; }
    public required string Username { get; init; }
    public required string Password { get; init; }
}
