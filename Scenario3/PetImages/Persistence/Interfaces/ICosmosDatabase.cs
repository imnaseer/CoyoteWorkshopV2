// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;

namespace PetImages.Persistence
{
    /// <summary>
    /// Interface of a Cosmos DB database. This can be implemented
    /// for production or with a mock for (systematic) testing.
    /// </summary>
    public interface ICosmosDatabase
    {
        Task<ICosmosContainer> CreateContainerAsync(string containerName);

        Task<ICosmosContainer> GetContainerAsync(string containerName);
    }
}
