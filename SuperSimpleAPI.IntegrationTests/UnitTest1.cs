using IdentityServer4.Contrib.AspNetCore.Testing.Builder;
using IdentityServer4.Contrib.AspNetCore.Testing.Configuration;
using IdentityServer4.Contrib.AspNetCore.Testing.Services;
using IdentityServer4.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
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
        public async Task Test_Not_Working()
        {
            var clientConfiguration = new ClientConfiguration("MyClient", "MySecret");

            var client = new Client
            {
                ClientId = clientConfiguration.Id,
                ClientSecrets = new List<Secret>
                {
                    new Secret(clientConfiguration.Secret.Sha256())
                },
                AllowedScopes = new[] { "api1" },
                AllowedGrantTypes = new[] { GrantType.ClientCredentials },
                AccessTokenType = AccessTokenType.Jwt,
                AllowOfflineAccess = true
            };

            var webHostBuilder = new IdentityServerWebHostBuilder()
                .AddClients(client)
                .AddApiResources(new ApiResource("api1", "api1name"))
                .CreateWebHostBuilder();

            var identityServerProxy = new IdentityServerProxy(webHostBuilder);
            var tokenResponse = await identityServerProxy.GetClientAccessTokenAsync(clientConfiguration, "api1");

            // *****
            // Note: creating an IdentityServerProxy above in order to get an access token
            // causes the next line to throw an exception stating: WebHostBuilder allows creation only of a single instance of WebHost
            // *****

            // Create an auth server from the IdentityServerWebHostBuilder 
            HttpMessageHandler handler;
            try
            {
                var fakeAuthServer = new TestServer(webHostBuilder);
                handler = fakeAuthServer.CreateHandler();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            // Set the BackChannelHandler of the 'production' IdentityServer to use the 
            // handler form the fakeAuthServer
            Startup.BackChannelHandler = handler;
            // Create the apiServer
            var apiServer = new TestServer(new WebHostBuilder().UseStartup<Startup>());
            var apiClient = apiServer.CreateClient();

            apiClient.SetBearerToken(tokenResponse.AccessToken);

            // Arrange
            var req = new HttpRequestMessage(new HttpMethod("GET"), "/api/values");

            // Act
            var response = await apiClient.SendAsync(req);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        }

    }
}
