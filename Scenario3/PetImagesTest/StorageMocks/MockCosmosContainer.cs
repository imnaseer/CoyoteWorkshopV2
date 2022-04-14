// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PetImages;
using PetImages.Entities;
using PetImages.Storage;
using System.Threading.Tasks;

namespace PetImagesTest.StorageMocks
{
    public class MockCosmosContainer : ICosmosContainer, IAccountContainer, IImageContainer
    {
        private readonly string ContainerName;
        private readonly MockCosmosState State;

        public MockCosmosContainer(string containerName, MockCosmosState state)
        {
            this.ContainerName = containerName;
            this.State = state;
        }

        public Task<T> CreateItem<T>(T item)
            where T : DbItem
        {
            var itemCopy = TestHelper.Clone(item);

            return Task.Run(() =>
            {
                Logger.WriteLine($"Attempting to create an item with partition key: {item.PartitionKey}, id: {item.Id}");

                this.State.CreateItem(this.ContainerName, itemCopy);
                return itemCopy;
            });
        }

        public Task<T> GetItem<T>(string partitionKey, string id)
            where T : DbItem
        {
            return Task.Run(() =>
            {
                Logger.WriteLine($"Attempting to get an item with partition key: {partitionKey}, id: {id}");

                var item = this.State.GetItem(this.ContainerName, partitionKey, id);

                var itemCopy = TestHelper.Clone((T)item);

                return itemCopy;
            });
        }

        public Task<T> UpsertItem<T>(T item, string ifMatchEtag = null)
            where T : DbItem
        {
            return Task.Run(() =>
            {
                Logger.WriteLine($"Attempting to upsert an item with partition key: {item.PartitionKey}, id: {item.Id}");

                var itemCopy = TestHelper.Clone(item);
                this.State.UpsertItem(this.ContainerName, itemCopy, ifMatchEtag);
                return itemCopy;
            });
        }

        public Task<T> ReplaceItem<T>(T item, string ifMatchEtag = null)
            where T : DbItem
        {
            return Task.Run(() =>
            {
                Logger.WriteLine($"Attempting to replace an item with partition key: {item.PartitionKey}, id: {item.Id}");

                var itemCopy = TestHelper.Clone(item);
                this.State.ReplaceItem(this.ContainerName, itemCopy, ifMatchEtag);
                return itemCopy;
            });
        }

        public Task DeleteItem(string partitionKey, string id, string ifMatchEtag = null)
        {
            return Task.Run(() =>
            {
                Logger.WriteLine($"Attempting to delete an item with partition key: {partitionKey}, id: {id}");

                this.State.DeleteItem(this.ContainerName, partitionKey, id, ifMatchEtag);
            });
        }
    }
}
