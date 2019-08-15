using FunctionMonkey.Abstractions;
using FunctionMonkey.Abstractions.Builders;
using RedClientDeploy.Commands;
using RedClientDeploy.Handlers;
using RedClientDeploy.Services;
using RedClientDeploy.Services2;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace RedClientDeploy
{
    public class FunctionAppConfiguration : IFunctionAppConfiguration
    {
        public void Build(IFunctionHostBuilder builder)
        {
            JsonConvert.DefaultSettings = () =>
            {
                return new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
            };

            builder
                .Setup((serviceCollection, commandRegistry) =>
                {
                    commandRegistry.Register<TenantQueryCommandHandler>();
                    commandRegistry.Register<CreateTenantCommandHandler>();
                    serviceCollection.AddHttpClient<Services2.TableauService>();
                    serviceCollection.AddSingleton(c => new SnowflakeService());
                    serviceCollection.AddSingleton(c => new ArtifactService());
                })
                .Functions(functions => functions
                    .HttpRoute("v1/red2/tenant", route => route
                        .HttpFunction<TenantQueryCommand>("/{name}", HttpMethod.Get)
                        .HttpFunction<CreateTenantCommand>(HttpMethod.Post)
                    )
                );
        }
    }
}
