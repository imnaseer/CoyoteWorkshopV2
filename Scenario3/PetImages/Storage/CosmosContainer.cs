// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PetImages.Entities;
using System.Threading.Tasks;
using System.Net;
using Microsoft.Azure.Cosmos;
using PetImages.Exceptions;

namespace PetImages.Storage
{
    public class CosmosContainer : ICosmosContainer
    {
        private readonly Container cosmosContainer;

        public CosmosContainer(Container cosmosContainer)
        {
            this.cosmosContainer = cosmosContainer;
        }

        public async Task<T> CreateItem<T>(T row) where T : DbItem
        {
            var response = await this.cosmosContainer.CreateItemAsync(row);
            return response.Resource;
        }

        public async Task DeleteItem(string partitionKey, string id, string ifMatchEtag = null)
        {
            var response = await this.cosmosContainer.DeleteItemAsync<DbItem>(
                id,
                new PartitionKey(partitionKey),
                new ItemRequestOptions() { IfMatchEtag = ifMatchEtag });

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                throw new DatabaseItemDoesNotExistException();
            }
        }

        public async Task<T> GetItem<T>(string partitionKey, string id) where T : DbItem
        {
            // TODO: This throws an exception instead of providing a response with 404.
            var response = await this.cosmosContainer.ReadItemAsync<T>(id, new PartitionKey(partitionKey));
            
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                throw new DatabaseItemDoesNotExistException();
            }

            return response.Resource;
        }

        public async Task<T> ReplaceItem<T>(T row, string ifMatchEtag = null) where T : DbItem
        {
            var response = await this.cosmosContainer.ReplaceItemAsync(
                row,
                row.Id,
                new PartitionKey(row.PartitionKey),
                new ItemRequestOptions() { IfMatchEtag = ifMatchEtag });
            
            // TODO: Throw appropriate exceptions
            return response.Resource;
        }

        public async Task<T> UpsertItem<T>(T row, string ifMatchEtag = null) where T : DbItem
        {
            var response = await this.cosmosContainer.UpsertItemAsync(
                row,
                new PartitionKey(row.PartitionKey),
                new ItemRequestOptions() { IfMatchEtag = ifMatchEtag });
            
            // TODO: Throw appropriate exceptions
            return response.Resource;
        }
    }
}
