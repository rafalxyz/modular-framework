﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Modular.Abstractions.Dispatchers;
using Modular.Abstractions.Modules;
using Modular.Abstractions.Storage;
using Modular.Abstractions.Time;
using Modular.Infrastructure.Api;
using Modular.Infrastructure.Auth;
using Modular.Infrastructure.Commands;
using Modular.Infrastructure.Contexts;
using Modular.Infrastructure.Dispatchers;
using Modular.Infrastructure.Events;
using Modular.Infrastructure.Exceptions;
using Modular.Infrastructure.Kernel;
using Modular.Infrastructure.Logging;
using Modular.Infrastructure.Messaging;
using Modular.Infrastructure.Messaging.Outbox;
using Modular.Infrastructure.Modules;
using Modular.Infrastructure.Queries;
using Modular.Infrastructure.Security;
using Modular.Infrastructure.Serialization;
using Modular.Infrastructure.Storage;
using Modular.Infrastructure.Time;

namespace Modular.Infrastructure
{
    public static class Extensions
    {
        private const string CorrelationIdKey = "correlation-id";
        
        public static IServiceCollection AddInitializer<T>(this IServiceCollection services) where T : class, IInitializer
            => services.AddTransient<IInitializer, T>();
        
        public static IServiceCollection AddModularInfrastructure(this IServiceCollection services,
            IList<Assembly> assemblies, IList<IModule> modules) 
        {
            var disabledModules = new List<string>();
            using (var serviceProvider = services.BuildServiceProvider())
            {
                var configuration = serviceProvider.GetRequiredService<IConfiguration>();
                foreach (var (key, value) in configuration.AsEnumerable())
                {
                    if (!key.Contains(":module:enabled"))
                    {
                        continue;
                    }

                    if (!bool.Parse(value))
                    {
                        disabledModules.Add(key.Split(":")[0]);
                    }
                }
            }

            services.AddCorsPolicy();
            services.AddSwaggerGen(swagger =>
            {
                swagger.EnableAnnotations();
                swagger.CustomSchemaIds(x => x.FullName);
                swagger.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Modular API",
                    Version = "v1"
                });
            });

            var appOptions = services.GetOptions<AppOptions>("app");
            services.AddSingleton(appOptions);

            services.AddMemoryCache();
            services.AddHttpClient();
            services.AddSingleton<IRequestStorage, RequestStorage>();
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddSingleton<IJsonSerializer, SystemTextJsonSerializer>();
            services.AddModuleInfo(modules);
            services.AddModuleRequests(assemblies);
            services.AddAuth(modules);
            services.AddErrorHandling();
            services.AddContext();
            services.AddCommands(assemblies);
            services.AddQueries(assemblies);
            services.AddEvents(assemblies);
            services.AddDomainEvents(assemblies);
            services.AddMessaging();
            services.AddSecurity();
            services.AddOutbox();
            services.AddSingleton<IClock, UtcClock>();
            services.AddSingleton<IDispatcher, InMemoryDispatcher>();
            services.AddControllers()
                .ConfigureApplicationPartManager(manager =>
                {
                    var removedParts = new List<ApplicationPart>();
                    foreach (var disabledModule in disabledModules)
                    {
                        var parts = manager.ApplicationParts.Where(x => x.Name.Contains(disabledModule,
                            StringComparison.InvariantCultureIgnoreCase));
                        removedParts.AddRange(parts);
                    }

                    foreach (var part in removedParts)
                    {
                        manager.ApplicationParts.Remove(part);
                    }
                    
                    manager.FeatureProviders.Add(new publicControllerFeatureProvider());
                });
            
            return services;
        }

        public static IApplicationBuilder UseModularInfrastructure(this IApplicationBuilder app)
        {
            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.All
            });
            app.UseCors("cors");
            app.UseCorrelationId();
            app.UseErrorHandling();
            app.UseSwagger();
            app.UseReDoc(reDoc =>
            {
                reDoc.RoutePrefix = "docs";
                reDoc.SpecUrl("/swagger/v1/swagger.json");
                reDoc.DocumentTitle = "Modular API";
            });
            app.UseAuth();
            app.UseContext();
            app.UseLogging();
            app.UseRouting();
            app.UseAuthorization();

            return app;
        }

        public static T GetOptions<T>(this IServiceCollection services, string sectionName) where T : new()
        {
            using var serviceProvider = services.BuildServiceProvider();
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            return configuration.GetOptions<T>(sectionName);
        }

        public static T GetOptions<T>(this IConfiguration configuration, string sectionName) where T : new()
        {
            var options = new T();
            configuration.GetSection(sectionName).Bind(options);
            return options;
        }

        public static string GetModuleName(this object value)
            => value?.GetType().GetModuleName() ?? string.Empty;

        public static string GetModuleName(this Type type, string namespacePart = "Modules", int splitIndex = 2)
        {
            if (type?.Namespace is null)
            {
                return string.Empty;
            }

            return type.Namespace.Contains(namespacePart)
                ? type.Namespace.Split(".")[splitIndex].ToLowerInvariant()
                : string.Empty;
        }
        
        public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
            => app.Use((ctx, next) =>
            {
                ctx.Items.Add(CorrelationIdKey, Guid.NewGuid());
                return next();
            });
        
        public static Guid? TryGetCorrelationId(this HttpContext context)
            => context.Items.TryGetValue(CorrelationIdKey, out var id) ? (Guid) id : null;
    }
}