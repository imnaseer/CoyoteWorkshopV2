// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using PetImages;
using PetImages.Messaging;
using PetImages.RetryFramework;
using PetImages.Storage;
using PetImagesTest.MessagingMocks;
using PetImagesTest.StorageMocks;
using System.IO;
using System.Threading.Tasks;

namespace PetImagesTest
{
    internal class ServiceFactory : WebApplicationFactory<Startup>
    {
        private IAccountContainer AccountContainer;
        private IImageContainer ImageContainer;
        private IMessagingClient MessagingClient;

        private readonly IStorageAccount StorageAccount;
        private readonly ICosmosDatabase CosmosDatabase;

        public ServiceFactory()
        {
            this.StorageAccount = new WrappedStorageAccount(
                new MockStorageAccount(),
                RetryPolicyFactory.GetAsyncRetryExponential());

            this.CosmosDatabase = new WrappedCosmosDatabase(
                new MockCosmosDatabase(new MockCosmosState()),
                RetryPolicyFactory.GetAsyncRetryExponential());
        }

        internal async Task<IAccountContainer> InitializeAccountContainerAsync()
        {
            this.AccountContainer = (WrappedCosmosContainer)await this.CosmosDatabase.CreateContainerAsync(Constants.AccountContainerName);
            return this.AccountContainer;
        }

        internal async Task<IImageContainer> InitializeImageContainerAsync()
        {
            this.ImageContainer = (WrappedCosmosContainer)await this.CosmosDatabase.CreateContainerAsync(Constants.ImageContainerName);
            return this.ImageContainer;
        }

        internal Task<IMessagingClient> InitializeMessagingClient()
        {
            var messagingClient = new MockMessagingClient(this.AccountContainer, this.ImageContainer, this.StorageAccount);
            this.MessagingClient = new WrappedMessagingClient(
                messagingClient,
                RetryPolicyFactory.GetAsyncRetryExponential());
            return Task.FromResult(this.MessagingClient);
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseContentRoot(Directory.GetCurrentDirectory());
            builder.ConfigureServices(services =>
            {
                // Inject the mocks.
                services.AddSingleton(this.AccountContainer);
                services.AddSingleton(this.ImageContainer);
                services.AddSingleton(this.StorageAccount);
                services.AddSingleton(this.MessagingClient);
            });
        }
    }
}
