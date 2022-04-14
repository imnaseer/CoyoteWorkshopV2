// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PetImages.Entities;
using System.Threading.Tasks;

namespace PetImages.Storage
{
    /// <summary>
    /// Interface of a Cosmos DB container. This can be implemented
    /// for production or with a mock for (systematic) testing.
    /// </summary>
    public interface ICosmosContainer
    {
        public Task<T> CreateItem<T>(T row)
            where T : DbItem;

        public Task<T> GetItem<T>(string partitionKey, string id)
           where T : DbItem;

        public Task<T> UpsertItem<T>(T row, string ifMatchEtag = null)
            where T : DbItem;

        public Task<T> ReplaceItem<T>(T row, string ifMatchEtag = null)
            where T : DbItem;

        public Task DeleteItem(string partitionKey, string id, string ifMatchEtag = null);
    }
}
