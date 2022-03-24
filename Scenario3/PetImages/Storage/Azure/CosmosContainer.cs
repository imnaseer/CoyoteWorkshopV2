// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PetImages.Entities;
using System.Threading.Tasks;
using System.Net;
using Microsoft.Azure.Cosmos;
using PetImages.Exceptions;
using System;

namespace PetImages.Storage
{
    public class CosmosContainer : ICosmosContainer, IImageContainer, IAccountContainer
    {
        private readonly Container cosmosContainer;

        public CosmosContainer(Container cosmosContainer)
        {
            this.cosmosContainer = cosmosContainer;
        }

        public async Task<T> CreateItem<T>(T row) where T : DbItem
        {
            return await this.PerformCosmosOperationOrThrow<T>(
                () => this.cosmosContainer.CreateItemAsync(row));
        }

        public async Task DeleteItem(string partitionKey, string id, string ifMatchEtag = null)
        {
            try
            {
                var response = await this.cosmosContainer.DeleteItemAsync<DbItem>(
                id,
                new PartitionKey(partitionKey),
                new ItemRequestOptions() { IfMatchEtag = ifMatchEtag });
            }
            catch(CosmosException ex)
            {
                throw CosmosToDatabaseExceptionProvider(ex)();
            }
        }

        public async Task<T> GetItem<T>(string partitionKey, string id) where T : DbItem
        {
            return await this.PerformCosmosOperationOrThrow(() => 
                this.cosmosContainer.ReadItemAsync<T>(
                    id, new PartitionKey(partitionKey)));
        }

        public async Task<T> ReplaceItem<T>(T row, string ifMatchEtag = null) where T : DbItem
        {
            return await this.PerformCosmosOperationOrThrow(() =>
                this.cosmosContainer.ReplaceItemAsync(
                    row,
                    row.Id,
                    new PartitionKey(row.PartitionKey),
                    new ItemRequestOptions() { IfMatchEtag = ifMatchEtag }));
        }

        public async Task<T> UpsertItem<T>(T row, string ifMatchEtag = null) where T : DbItem
        {
            return await this.PerformCosmosOperationOrThrow(() =>
                this.cosmosContainer.UpsertItemAsync(
                    row,
                    new PartitionKey(row.PartitionKey),
                    new ItemRequestOptions() { IfMatchEtag = ifMatchEtag }));
        }

        private async Task<T> PerformCosmosOperationOrThrow<T>(Func<Task<ItemResponse<T>>> cosmosFunc)
            where T: DbItem
        {
            try
            {
                var response = await cosmosFunc();
                return response.Resource;
            }
            catch (CosmosException cosmosException)
            {
                throw CosmosToDatabaseExceptionProvider(cosmosException)();
            }
        }

        private Func<DatabaseException> CosmosToDatabaseExceptionProvider(CosmosException cosmosException)
        {
            if (cosmosException.StatusCode == HttpStatusCode.NotFound)
            {
                return () => new DatabaseItemDoesNotExistException();
            }
            else if (cosmosException.StatusCode == HttpStatusCode.Conflict)
            {
                return () => new DatabaseItemAlreadyExistsException();
            }
            else if (cosmosException.StatusCode == HttpStatusCode.PreconditionFailed)
            {
                return () => new DatabasePreconditionFailedException();
            }
            else
            {
                return () => new DatabaseException();
            }
        }
    }
}
