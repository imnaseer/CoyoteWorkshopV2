// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.Cosmos;
using PetImages.Entities;
using PetImages.Exceptions;
using System;
using System.Net;
using System.Threading.Tasks;

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
            return await this.PerformCosmosOperationOrThrowAsync<T>(
                () => this.cosmosContainer.CreateItemAsync(row));
        }

        public async Task DeleteItem(string partitionKey, string id, string ifMatchEtag = null)
        {
            _ = await this.PerformCosmosOperationOrThrowAsync<DbItem>(
                () => this.cosmosContainer.DeleteItemAsync<DbItem>(
                    id,
                    new PartitionKey(partitionKey),
                    new ItemRequestOptions() { IfMatchEtag = ifMatchEtag }));
        }

        public async Task<T> GetItem<T>(string partitionKey, string id) where T : DbItem
        {
            return await this.PerformCosmosOperationOrThrowAsync(() =>
                this.cosmosContainer.ReadItemAsync<T>(
                    id, new PartitionKey(partitionKey)));
        }

        public async Task<T> ReplaceItem<T>(T row, string ifMatchEtag = null) where T : DbItem
        {
            return await this.PerformCosmosOperationOrThrowAsync(() =>
                this.cosmosContainer.ReplaceItemAsync(
                    row,
                    row.Id,
                    new PartitionKey(row.PartitionKey),
                    new ItemRequestOptions() { IfMatchEtag = ifMatchEtag }));
        }

        public async Task<T> UpsertItem<T>(T row, string ifMatchEtag = null) where T : DbItem
        {
            return await this.PerformCosmosOperationOrThrowAsync(() =>
                this.cosmosContainer.UpsertItemAsync(
                    row,
                    new PartitionKey(row.PartitionKey),
                    new ItemRequestOptions() { IfMatchEtag = ifMatchEtag }));
        }

        private async Task<T> PerformCosmosOperationOrThrowAsync<T>(Func<Task<ItemResponse<T>>> cosmosFunc)
            where T : DbItem
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
                return () => new DatabaseItemDoesNotExistException(cosmosException);
            }
            else if (cosmosException.StatusCode == HttpStatusCode.Conflict)
            {
                return () => new DatabaseItemAlreadyExistsException(cosmosException);
            }
            else if (cosmosException.StatusCode == HttpStatusCode.PreconditionFailed)
            {
                return () => new DatabasePreconditionFailedException(cosmosException);
            }
            else
            {
                return () => new DatabaseException(cosmosException);
            }
        }
    }
}
