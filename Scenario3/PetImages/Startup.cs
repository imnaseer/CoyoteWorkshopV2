// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using PetImages.Messaging;
using PetImages.Storage;
using Swashbuckle.AspNetCore;

namespace PetImages
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
            services.AddControllers();

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo()
                {
                    Title = "PetImages API",
                    Version = "v1",
                    Description = "Description for the API goes here.",
                });
            });

            this.InitializeCosmosServices(services);
            this.InitializeStorageServices(services);
            this.InitializeQueueServices(services);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseSwagger();
            
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "PetImages API");
                c.RoutePrefix = string.Empty;
            });

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

        private void InitializeCosmosServices(IServiceCollection services)
        {
            var cosmosDatabase = CosmosDatabase.Create(Constants.DatabaseName);
            var accountCosmosContainer = cosmosDatabase.CreateContainerAsync(Constants.AccountContainerName).Result;
            services.AddSingleton((IAccountContainer)accountCosmosContainer);
            var imageCosmosContainer = cosmosDatabase.CreateContainerAsync(Constants.ImageContainerName).Result;
            services.AddSingleton((IImageContainer)imageCosmosContainer);
        }

        private void InitializeStorageServices(IServiceCollection services)
        {
            var storageAccount = new AzureStorageAccount();
            services.AddSingleton<IStorageAccount>(storageAccount);
        }

        private void InitializeQueueServices(IServiceCollection services)
        {
            var messagingClient = new StorageMessagingClient(Constants.ThumbnailQueueName);
            services.AddSingleton<IMessagingClient>(messagingClient);
            var messageReceiverClient = new StorageMessageReceiverClient(Constants.ThumbnailQueueName);
            services.AddSingleton<IMessageReceiver>(messageReceiverClient);
        }
    }
}
