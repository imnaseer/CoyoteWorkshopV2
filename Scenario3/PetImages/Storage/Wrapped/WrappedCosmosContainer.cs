﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PetImages.Entities;
using Polly;
using System.Threading.Tasks;

namespace PetImages.Storage
{
    public class WrappedCosmosContainer : ICosmosContainer, IAccountContainer, IImageContainer
    {
        private readonly IAsyncPolicy AsyncPolicy;

        private readonly ICosmosContainer CosmosContainer;

        public WrappedCosmosContainer(ICosmosContainer cosmosContainer, IAsyncPolicy asyncPolicy)
        {
            this.CosmosContainer = cosmosContainer;
            this.AsyncPolicy = asyncPolicy;
        }

        public Task<T> CreateItem<T>(T row) where T : DbItem
        {
            return this.AsyncPolicy.ExecuteAsync(() => this.CosmosContainer.CreateItem(row));
        }

        public Task DeleteItem(string partitionKey, string id, string ifMatchEtag = null)
        {
            return this.AsyncPolicy.ExecuteAsync(() => this.CosmosContainer.DeleteItem(partitionKey, id, ifMatchEtag));
        }

        public Task<T> GetItem<T>(string partitionKey, string id) where T : DbItem
        {
            return this.AsyncPolicy.ExecuteAsync(() => this.CosmosContainer.GetItem<T>(partitionKey, id));
        }

        public Task<T> ReplaceItem<T>(T row, string ifMatchEtag = null) where T : DbItem
        {
            return this.AsyncPolicy.ExecuteAsync(() => this.CosmosContainer.ReplaceItem(row, ifMatchEtag));
        }

        public Task<T> UpsertItem<T>(T row, string ifMatchEtag = null) where T : DbItem
        {
            return this.AsyncPolicy.ExecuteAsync(() => this.CosmosContainer.UpsertItem(row, ifMatchEtag));
        }
    }
}
