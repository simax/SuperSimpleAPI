using IdentityServer4.Contrib.AspNetCore.Testing.Builder;
using IdentityServer4.Contrib.AspNetCore.Testing.Configuration;
using IdentityServer4.Contrib.AspNetCore.Testing.Services;
using IdentityServer4.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Secret = IdentityServer4.Models.Secret;


namespace SuperSimpleAPI.IntegrationTests
{
    public class UnitTest1
    {
        [Fact]
        public async Task This_Test_Works_As_Long_as_No_Authorize_Attribute_On_Controller()
        {
            var server = new TestServer(new WebHostBuilder().UseStartup<Startup>());
            var apiClient = server.CreateClient();

            // Arrange
            var req = new HttpRequestMessage(new HttpMethod("GET"), "/api/values");

            // Act
            var response = await apiClient.SendAsync(req);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Test_Working_Correctly_With_Authorize_Attribute()
        {
            var clientConfiguration = new ClientConfiguration("MyClient", "MySecret");

            var client = new Client
            {
                ClientId = clientConfiguration.Id,
                ClientSecrets = new List<Secret>
                {
                    new Secret(clientConfiguration.Secret.Sha256())
                },
                AllowedScopes = new[] {"api1"},
                AllowedGrantTypes = new[] {GrantType.ClientCredentials},
                AccessTokenType = AccessTokenType.Jwt,
                AllowOfflineAccess = true
            };

            var webHostBuilder = new IdentityServerWebHostBuilder()
                .AddClients(client)
                .AddApiResources(new ApiResource("api1", "api1name"))
                .CreateWebHostBuilder();

            var identityServerProxy = new IdentityServerProxy(webHostBuilder);
            var tokenResponse = await identityServerProxy.GetClientAccessTokenAsync(clientConfiguration, "api1");

            var apiServer = new TestServer(new WebHostBuilder()
                .ConfigureAppConfiguration(builder =>
                {
                    var configuration = new ConfigurationBuilder()
                        .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.json"))
                        .Build();

                    builder.AddConfiguration(configuration);
                })
                .ConfigureServices(
                    services => services.AddSingleton(identityServerProxy.IdentityServer.CreateHandler()))
                .UseStartup<TestStartup>());
            var apiClient = apiServer.CreateClient();

            apiClient.SetBearerToken(tokenResponse.AccessToken);

            var response = await apiClient.GetAsync("/api/values");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}