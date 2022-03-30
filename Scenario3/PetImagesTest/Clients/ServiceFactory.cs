// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using PetImages.Messaging;
using PetImages.Storage;
using PetImagesTest.MessagingMocks;
using PetImagesTest.StorageMocks;

namespace PetImages.Tests
{
    internal class ServiceFactory : WebApplicationFactory<Startup>
    {
        private MockCosmosContainer AccountContainer;
        private MockCosmosContainer ImageContainer;
        private MockMessagingClient MessagingClient;

        private readonly MockStorageAccount StorageAccount;
        private readonly MockCosmosDatabase CosmosDatabase;

        // TODO: Mechanism to introduce faults
        public ServiceFactory()
        {
            this.StorageAccount = new MockStorageAccount();
            this.CosmosDatabase = new MockCosmosDatabase(new MockCosmosState());
        }

        internal async Task<MockCosmosContainer> InitializeAccountContainerAsync()
        {
            this.AccountContainer = (MockCosmosContainer)await this.CosmosDatabase.CreateContainerAsync(Constants.AccountContainerName);
            return this.AccountContainer;
        }

        internal async Task<MockCosmosContainer> InitializeImageContainerAsync()
        {
            this.ImageContainer = (MockCosmosContainer)await this.CosmosDatabase.CreateContainerAsync(Constants.ImageContainerName);
            return this.ImageContainer;
        }

        internal MockMessagingClient InitializeMessagingClient()
        {
            this.MessagingClient = new MockMessagingClient(this.AccountContainer, this.ImageContainer, this.StorageAccount);
            return this.MessagingClient;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseContentRoot(Directory.GetCurrentDirectory());
            builder.ConfigureTestServices(services =>
            {
                // Inject the mocks.
                services.AddSingleton<IAccountContainer, MockCosmosContainer>(container => this.AccountContainer);
                services.AddSingleton<IImageContainer, MockCosmosContainer>(container => this.ImageContainer);
                services.AddSingleton<IStorageAccount, MockStorageAccount>(provider => this.StorageAccount);
                services.AddSingleton<IMessagingClient, MockMessagingClient>(provider => this.MessagingClient);
            });
        }
    }
}
