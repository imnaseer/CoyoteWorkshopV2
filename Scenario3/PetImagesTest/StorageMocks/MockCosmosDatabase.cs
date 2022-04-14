// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PetImages.Storage;
using System.Threading.Tasks;

namespace PetImagesTest.StorageMocks
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
                this.State.CreateContainer(containerName);
                return new MockCosmosContainer(containerName, this.State);
            });
        }

        public Task<ICosmosContainer> GetContainerAsync(string containerName)
        {
            return Task.Run<ICosmosContainer>(() =>
            {
                this.State.EnsureContainerExistsInDatabase(containerName);
                return new MockCosmosContainer(containerName, this.State);
            });
        }
    }
}
