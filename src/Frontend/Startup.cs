using System;
using System.Net.Http;
using AuthHelp;
using Frontend.Auth;
using Grpc.Core;
using Ingredients.Protos;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orders.Protos;

namespace Frontend
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();

            services.AddHttpClient("ingredients")
                .ConfigurePrimaryHttpMessageHandler(DevelopmentModeCertificateHelper.CreateClientHandler);

            services.AddGrpcClient<IngredientsService.IngredientsServiceClient>(
                (provider, options) =>
                {
                    var config = provider.GetRequiredService<IConfiguration>();
                    var uri = config.GetServiceUri("Ingredients", "https");

                    options.Address = uri ?? new Uri("https://localhost:5003");
                })
                .ConfigureChannel((provider, channel) =>
                {
                    channel.HttpClient = provider.GetRequiredService<IHttpClientFactory>().CreateClient("ingredients");
                    channel.DisposeHttpClient = true;
                    channel.HttpHandler = null;
                });

            services.AddGrpcClient<OrderService.OrderServiceClient>(
                (provider, options) =>
                {
                    var config = provider.GetRequiredService<IConfiguration>();
                    var uri = config.GetServiceUri("Orders", "https");

                    options.Address = uri ?? new Uri("https://localhost:5005");
                })
                .ConfigureChannel((provider, channel) =>
                {
                    var authHelper = provider.GetRequiredService<AuthHelper>();
                    var credentials = CallCredentials.FromInterceptor(async (context, metadata) =>
                    {
                        var token = await authHelper.GetTokenAsync();
                        metadata.Add("Authorization", $"Bearer {token}");
                    });
                    channel.Credentials = ChannelCredentials.Create(new SslCredentials(), credentials);
                });

            services.AddHttpClient<AuthHelper>()
                .ConfigureHttpClient((provider, client) =>
                {
                    var config = provider.GetRequiredService<IConfiguration>();
                    var uri = config.GetServiceUri("Orders", "https");

                    client.BaseAddress = uri ?? new Uri("https://localhost:5005");
                    client.DefaultRequestVersion = new Version(2, 0);
                });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
