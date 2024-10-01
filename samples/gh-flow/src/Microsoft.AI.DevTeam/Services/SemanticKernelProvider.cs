// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.KernelMemory;
using Microsoft.SemanticKernel;

namespace Microsoft.AI.DevTeam;

/// <summary>
/// Extension methods for registering Semantic Kernel related services.
/// </summary>
public sealed class SemanticKernelProvider
{
    private readonly IKernelBuilder _builderChat;
    private static ILogger<SemanticKernelProvider> _logger;

    public SemanticKernelProvider(IServiceProvider serviceProvider, IConfiguration configuration, IHttpClientFactory httpClientFactory, ILogger<SemanticKernelProvider> logger)
    {
        this._builderChat = InitializeCompletionKernel(serviceProvider, configuration, httpClientFactory, logger);
        _logger = logger;
    }

    /// <summary>
    /// Produce semantic-kernel with only completion services for chat.
    /// </summary>
    public Kernel GetCompletionKernel() => this._builderChat.Build();

    private static IKernelBuilder InitializeCompletionKernel(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILogger<SemanticKernelProvider> logger)
    {
        var builder = Kernel.CreateBuilder();

        try
        {
            builder.Services.AddLogging(logging => logging.AddConsole().AddFilter("Microsoft.SemanticKernel", LogLevel.Trace));

            var memoryOptions = serviceProvider.GetRequiredService<IOptions<KernelMemoryConfig>>().Value;
            
            logger.LogInformation($"MEMORYOPTIONS.TEXTGENERATORTYPE {memoryOptions.TextGeneratorType}");

            if (string.IsNullOrEmpty(memoryOptions.TextGeneratorType))
            {
                memoryOptions.TextGeneratorType = "AzureOpenAI";
            }
            
            switch (memoryOptions.TextGeneratorType)
            {
                case string x when x.Equals("AzureOpenAI", StringComparison.OrdinalIgnoreCase):
                case string y when y.Equals("AzureOpenAIText", StringComparison.OrdinalIgnoreCase):
                    var azureAIOptions = memoryOptions.GetServiceConfig<AzureOpenAIConfig>(configuration, "AzureOpenAIText");
                    logger.LogInformation($"AZUREAIOPTIONS.DEPLOYMENT {azureAIOptions.Deployment}");
                    logger.LogInformation($"AZUREAIOPTIONS.ENDPOINT {azureAIOptions.Endpoint}");
                    logger.LogInformation($"AZUREAIOPTIONS.APIKEY {azureAIOptions.APIKey}");
    #pragma warning disable CA2000 // No need to dispose of HttpClient instances from IHttpClientFactory
                    builder.AddAzureOpenAIChatCompletion(
                        azureAIOptions.Deployment,
                        azureAIOptions.Endpoint,
                        azureAIOptions.APIKey,
                        httpClient: httpClientFactory.CreateClient());
                    break;

                case string x when x.Equals("OpenAI", StringComparison.OrdinalIgnoreCase):
                    var openAIOptions = memoryOptions.GetServiceConfig<OpenAIConfig>(configuration, "OpenAI");
                    builder.AddOpenAIChatCompletion(
                        openAIOptions.TextModel,
                        openAIOptions.APIKey,
                        httpClient: httpClientFactory.CreateClient());
    #pragma warning restore CA2000
                    break;

                default:
                    throw new ArgumentException($"Invalid {nameof(memoryOptions.TextGeneratorType)} value in 'KernelMemory' settings.");
            }
        }
        catch (Exception ex)
        {
            logger.LogInformation($"EXCEPTION IN INITIALIZECOMPLETIONKERNEL: {ex.Message}");
        }

        return builder;
    }
}
