using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Reflection;
//using CopilotChat.Shared;
//using CopilotChat.WebApi.Auth;
//using CopilotChat.WebApi.Models.Storage;
//using CopilotChat.WebApi.Options;
using Microsoft.AI.DevTeam.Options;
//using CopilotChat.WebApi.Services;
//using CopilotChat.WebApi.Storage;
//using CopilotChat.WebApi.Utilities;
using Microsoft.AI.DevTeam.Utilities;
using Microsoft.AspNetCore.Authentication;
//using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
//using Microsoft.Identity.Web;
//using Microsoft.KernelMemory;
//using Microsoft.KernelMemory.Diagnostics;

using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using Azure.Core;
using Microsoft.SemanticKernel.Http;
using Microsoft.SemanticKernel.Memory;

namespace Microsoft.AI.DevTeam.Extensions
{
    public static class ServiceExtensions
    {
        internal static IServiceCollection AddPlugins(this IServiceCollection services, IConfiguration configuration)
        {
            var plugins = configuration.GetSection("Plugins").Get<List<Plugin>>() ?? new List<Plugin>();
            var logger = services.BuildServiceProvider().GetRequiredService<ILogger<Program>>();
            logger.LogDebug("Found {0} plugins.", plugins.Count);

            // Validate the plugins
            Dictionary<string, Plugin> validatedPlugins = new();
            foreach (Plugin plugin in plugins)
            {
                if (validatedPlugins.ContainsKey(plugin.Name))
                {
                    logger.LogWarning("Plugin '{0}' is defined more than once. Skipping...", plugin.Name);
                    continue;
                }

                var pluginManifestUrl = PluginUtils.GetPluginManifestUri(plugin.ManifestDomain);
                using var request = new HttpRequestMessage(HttpMethod.Get, pluginManifestUrl);
                // Need to set the user agent to avoid 403s from some sites.
                //request.Headers.Add("User-Agent", Telemetry.HttpUserAgent);
                try
                {
                    logger.LogInformation("Adding plugin: {0}.", plugin.Name);
                    // using var httpClient = new HttpClient();
                    // var response = httpClient.SendAsync(request).Result;
                    // if (!response.IsSuccessStatusCode)
                    // {
                    //     throw new InvalidOperationException($"Plugin '{plugin.Name}' at '{pluginManifestUrl}' returned status code '{response.StatusCode}'.");
                    // }
                    // validatedPlugins.Add(plugin.Name, plugin);
                    logger.LogInformation("Added plugin: {0}.", plugin.Name);
                }
                catch (Exception ex) when (ex is InvalidOperationException || ex is AggregateException)
                {
                    logger.LogWarning(ex, "Plugin '{0}' at {1} responded with error. Skipping...", plugin.Name, pluginManifestUrl);
                }
                catch (Exception ex) when (ex is UriFormatException)
                {
                    logger.LogWarning("Plugin '{0}' at {1} is not a valid URL. Skipping...", plugin.Name, pluginManifestUrl);
                }
            }

            // Add the plugins
            services.AddSingleton<IDictionary<string, Plugin>>(validatedPlugins);

            return services;
        }

    }
}