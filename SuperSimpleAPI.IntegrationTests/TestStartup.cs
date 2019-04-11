using System.Net.Http;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SuperSimpleAPI.IntegrationTests
{
    public class TestStartup : Startup
    {
        public HttpMessageHandler _identityServerMessageHandler;

        public TestStartup(IConfiguration configuration, HttpMessageHandler identityServerMessageHandler) : base(configuration)
        {
            _identityServerMessageHandler = identityServerMessageHandler;
        }

        public override void ConfigureAuth(IServiceCollection services)
        {
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddIdentityServerAuthentication(options =>
                {
                    options.Authority = Configuration.GetValue<string>("IdentityServerAuthority");
                    options.RequireHttpsMetadata = false;
                    options.JwtBackChannelHandler = _identityServerMessageHandler;
                });
        }

    }
}