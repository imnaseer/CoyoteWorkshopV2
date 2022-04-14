// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PetImages.Entities;
using PetImages.Exceptions;
using System.Threading.Tasks;

namespace PetImages.Storage
{
    public static class CosmosHelper
    {
        public static async Task<bool> DoesItemExistAsync<T>(ICosmosContainer container, string partitionKey, string id)
            where T : DbItem
        {
            try
            {
                await container.GetItem<T>(partitionKey, id);
                return true;
            }
            catch (DatabaseItemDoesNotExistException)
            {
                return false;
            }
        }

        public static async Task<T> GetItemIfExistsAsync<T>(ICosmosContainer container, string partitionKey, string id)
            where T : DbItem
        {
            try
            {
                return await container.GetItem<T>(partitionKey, id);
            }
            catch (DatabaseItemDoesNotExistException)
            {
                return null;
            }
        }
    }
}
