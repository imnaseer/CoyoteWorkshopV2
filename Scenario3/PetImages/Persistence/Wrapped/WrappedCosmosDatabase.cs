// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Polly;
using System.Threading.Tasks;

namespace PetImages.Persistence
{
    public class WrappedCosmosDatabase : ICosmosDatabase
    {
        private readonly IAsyncPolicy AsyncPolicy;

        private readonly ICosmosDatabase CosmosDatabase;

        public WrappedCosmosDatabase(ICosmosDatabase cosmosDatabase, IAsyncPolicy asyncPolicy)
        {
            this.CosmosDatabase = cosmosDatabase;
            this.AsyncPolicy = asyncPolicy;
        }

        public async Task<ICosmosContainer> CreateContainerAsync(string containerName)
        {
            var cosmosContainer = await CosmosDatabase.CreateContainerAsync(containerName);

            return new WrappedCosmosContainer(
                cosmosContainer,
                AsyncPolicy);
        }

        public async Task<ICosmosContainer> GetContainerAsync(string containerName)
        {
            var cosmosContainer = await CosmosDatabase.GetContainerAsync(containerName);

            return new WrappedCosmosContainer(
                cosmosContainer,
                AsyncPolicy);
        }
    }
}
