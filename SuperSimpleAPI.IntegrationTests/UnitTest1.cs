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
using System.Net.Mime;
using System.Reflection;
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

        private HttpClient _apiClient;
        private IdentityServerProxy _identityServerProxy;
        private ClientConfiguration _clientConfiguration = new ClientConfiguration("MyClient", "MySecret");

        public UnitTest1()
        {
            var client = new Client
            {
                ClientId = _clientConfiguration.Id,
                ClientSecrets = new List<Secret> { new Secret(_clientConfiguration.Secret.Sha256()) },
                AllowedScopes = new[] { "api1" },
                AllowedGrantTypes = new[] { GrantType.ClientCredentials },
                AccessTokenType = AccessTokenType.Jwt,
                AllowOfflineAccess = true
            };

            var webHostBuilder = new IdentityServerWebHostBuilder()
                .AddClients(client)
                .AddApiResources(new ApiResource("api1", "api1name"))
                .CreateWebHostBuilder();

            _identityServerProxy = new IdentityServerProxy(webHostBuilder);

            var apiServer = new TestServer(new WebHostBuilder()
                .ConfigureAppConfiguration(builder =>
                {
                    var configuration = new ConfigurationBuilder()
                        .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appSettings.json"))
                        .Build();

                    builder.AddConfiguration(configuration);
                })
                .ConfigureServices(services =>
                    services.AddSingleton(_identityServerProxy.IdentityServer.CreateHandler()))
                .UseStartup<TestStartup>()
                // So we can use the TestStartup class from the IntegrationTests assembly
                .UseSetting(WebHostDefaults.ApplicationKey, typeof(Program).GetTypeInfo().Assembly.FullName));


            _apiClient = apiServer.CreateClient();

        }


        [Fact]
        public async Task Test_Working_Correctly_With_Authorize_Attribute()
        {
            var tokenResponse = await _identityServerProxy.GetClientAccessTokenAsync(_clientConfiguration, "api1");
            _apiClient.SetBearerToken(tokenResponse.AccessToken);

            var response = await _apiClient.GetAsync("/api/values");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}