﻿using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;
using Microsoft.AspNetCore.TestHost;

namespace SuperSimpleAPI
{
    public class Startup
    {
        public IConfiguration Configuration { get; }
        private readonly HttpMessageHandler _identityServerMessageHandler;

        public Startup(IConfiguration configuration, HttpMessageHandler identityServerMessageHandler)
        {
            Configuration = configuration;
            _identityServerMessageHandler = identityServerMessageHandler;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddMvc()
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
            ConfigureAuth(services);
            services.Configure<ApiBehaviorOptions>(options => { options.SuppressModelStateInvalidFilter = true; });
        }

        public virtual void ConfigureAuth(IServiceCollection services)
        {
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddIdentityServerAuthentication(options =>
                {
                    options.Authority = Configuration.GetValue<string>("IdentityServerAuthority");
                    options.JwtBackChannelHandler = _identityServerMessageHandler;
                    options.RequireHttpsMetadata = false;
                });
        }


        public void Configure(IApplicationBuilder app)
        {
            app.UseAuthentication();
            app.UseMvc();
        }
    }
}