// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PetImages;
using PetImages.Persistence;
using System.Threading.Tasks;

namespace PetImagesTest.PersistenceMocks
{
    public class MockCosmosDatabase : ICosmosDatabase
    {
        private readonly MockCosmosState State;

        public MockCosmosDatabase(MockCosmosState state)
        {
            this.State = state;
        }

        public Task<ICosmosContainer> CreateContainerAsync(string containerName)
        {
            return Task.Run<ICosmosContainer>(() =>
            {
                Logger.WriteLine($"Attempting to create CosmosDB container {containerName}");

                this.State.CreateContainer(containerName);
                return new MockCosmosContainer(containerName, this.State);
            });
        }

        public Task<ICosmosContainer> GetContainerAsync(string containerName)
        {
            return Task.Run<ICosmosContainer>(() =>
            {
                Logger.WriteLine($"Attempting to get Cosmos DB container {containerName}");

                this.State.EnsureContainerExistsInDatabase(containerName);
                return new MockCosmosContainer(containerName, this.State);
            });
        }
    }
}
