// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using PetImages.Entities;

namespace PetImages.Storage
{
    public class CosmosDatabase : ICosmosDatabase
    {
        private readonly CosmosClient cosmosClient;

        private Database cosmosDatabase;

        private readonly string databaseName;

        public static ICosmosDatabase Create(string databaseName)
        {
            var instance = new CosmosDatabase(databaseName);
            instance.Initialize();
            return instance;
        }

        public CosmosDatabase(string databaseName)
        {
            // Connect to the Azure Cosmos DB Emulator running locally
            // Ideally the endpoint and key should come from config
            // Ensure Emulator v2.14.6, otherwise initialization will fail
            this.cosmosClient = new CosmosClient(
               "https://localhost:8081",
                "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==");
            this.databaseName = databaseName;
        }

        public async Task<ICosmosContainer> CreateContainerAsync(string containerName)
        {
            await this.cosmosDatabase.CreateContainerIfNotExistsAsync(
                new ContainerProperties
                {
                    Id = containerName,
                    PartitionKeyPath = "/partitionKey",
                });

            var cosmosContainer = this.cosmosDatabase.GetContainer(containerName);
            return new CosmosContainer(cosmosContainer);
        }

        public Task<ICosmosContainer> GetContainerAsync(string containerName)
        {
            var cosmosContainer = this.cosmosDatabase.GetContainer(containerName);
            
            // Since getting container is not async
            return Task.FromResult((ICosmosContainer) new CosmosContainer(cosmosContainer));
        }

        private void Initialize()
        {
            var response = this.cosmosClient.CreateDatabaseIfNotExistsAsync(this.databaseName).Result;
            this.cosmosDatabase = this.cosmosClient.GetDatabase(this.databaseName);
        }
    }
}
