﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PetImages.Entities;
using System.Threading.Tasks;
using Polly;

namespace PetImages.Storage.Resilient
{
    public class ResilientStorageAccount : IStorageAccount
    {
        private readonly IAsyncPolicy AsyncPolicy;

        private readonly IStorageAccount StorageAccount;

        public ResilientStorageAccount(IStorageAccount storageAccount, IAsyncPolicy asyncPolicy)
        {
            this.StorageAccount = storageAccount;
            this.AsyncPolicy = asyncPolicy;
        }

        public Task CreateContainerAsync(string containerName)
        {
            return this.AsyncPolicy.ExecuteAsync(() => this.StorageAccount.CreateContainerAsync(containerName));
        }

        public Task CreateOrUpdateBlockBlobAsync(string containerName, string blobName, string contentType, byte[] blobContents)
        {
            return this.AsyncPolicy.ExecuteAsync(() => this.StorageAccount.CreateOrUpdateBlockBlobAsync(containerName, blobName, contentType, blobContents));
        }

        public Task DeleteBlockBlobAsync(string containerName, string blobName)
        {
            return this.AsyncPolicy.ExecuteAsync(() => this.StorageAccount.DeleteBlockBlobAsync(containerName, blobName));
        }

        public Task DeleteContainerAsync(string containerName)
        {
            return this.AsyncPolicy.ExecuteAsync(() => this.StorageAccount.DeleteContainerAsync(containerName));
        }

        public Task<byte[]> GetBlockBlobAsync(string containerName, string blobName)
        {
            return this.AsyncPolicy.ExecuteAsync(() => this.StorageAccount.GetBlockBlobAsync(containerName, blobName));
        }
    }
}
