using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using FluentValidation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ResolveOps.Application;
using ResolveOps.Application.Documents;
using ResolveOps.Messaging;
using ResolveOps.Modules.Documents.Infrastructure;
using ResolveOps.Persistence;

namespace ResolveOps.Modules.Documents;

public static class DocumentsModule
{
    public static IServiceCollection AddDocumentsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var assembly = typeof(DocumentsModule).Assembly;

        // Register this assembly's EF configurations with AppDbContext.
        AppDbContext.AddConfigurationAssembly(assembly);

        // Register validators.
        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        // Auto-register handlers.
        services.AddHandlersFromAssembly(assembly);

        // Auto-register endpoints.
        services.AddEndpointsFromAssembly(assembly);

        // ── Object storage configuration ─────────────────────────────────────
        services.Configure<ObjectStorageOptions>(
            configuration.GetSection(ObjectStorageOptions.SectionName));

        var storageSection = configuration.GetSection(ObjectStorageOptions.SectionName);
        var serviceUrl = storageSection["ServiceUrl"] ?? "http://localhost:9000";
        var accessKey = storageSection["AccessKey"] ?? "minioadmin";
        var secretKey = storageSection["SecretKey"] ?? "minioadmin";
        var region = storageSection["Region"] ?? "us-east-1";

        services.AddSingleton<IAmazonS3>(_ =>
        {
            var config = new AmazonS3Config
            {
                ServiceURL = serviceUrl,
                ForcePathStyle = true,  // Required for MinIO
                RegionEndpoint = RegionEndpoint.GetBySystemName(region),
            };

            return new AmazonS3Client(
                new BasicAWSCredentials(accessKey, secretKey),
                config);
        });

        // ── Storage service ──────────────────────────────────────────────────
        services.AddScoped<IObjectStorageService, MinIoObjectStorageService>();

        // ── Malware scanner (development stub; swap for production ClamAV adapter in Phase 16) ──
        services.AddScoped<IMalwareScanner, DevelopmentMalwareScanner>();

        // ── Outbox writer (needed by document processing worker) ─────────────
        services.AddScoped<IOutboxWriter, OutboxWriter>();

        return services;
    }

    public static IEndpointRouteBuilder MapDocumentsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var assembly = typeof(DocumentsModule).Assembly;
        endpoints.MapEndpointsFromAssembly(assembly);
        return endpoints;
    }
}
