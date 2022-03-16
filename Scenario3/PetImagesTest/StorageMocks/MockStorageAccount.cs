// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading.Tasks;
using PetImages.Exceptions;
using PetImages.Storage;

namespace PetImagesTest.StorageMocks
{
    internal class MockStorageAccount : IStorageAccount
    {
        private readonly Dictionary<string, Dictionary<string, byte[]>> Containers;

        internal MockStorageAccount()
        {
            this.Containers = new Dictionary<string, Dictionary<string, byte[]>>();
        }

        public Task CreateContainerAsync(string containerName)
        {
            return Task.Run(() =>
            {
                EnsureContainerDoesNotExist(containerName);

                Containers[containerName] = new Dictionary<string, byte[]>();
            });
        }

        public Task DeleteContainerAsync(string containerName)
        {
            return Task.Run(() =>
            {
                EnsureContainerExists(containerName);
                Containers.Remove(containerName);
            });
        }

        public Task CreateOrUpdateBlockBlobAsync(string containerName, string blobName, string contentType, byte[] blobContents)
        {
            return Task.Run(() =>
            {
                EnsureContainerExists(containerName);

                var container = Containers[containerName];
                container[blobName] = blobContents;
            });
        }
        public Task<byte[]> GetBlockBlobAsync(string containerName, string blobName)
        {
            return Task.Run(() =>
            {
                EnsureBlobExists(containerName, blobName);

                var container = Containers[containerName];
                return container[blobName];
            });
        }

        public Task DeleteBlockBlobAsync(string containerName, string blobName)
        {
            return Task.Run(() =>
            {
                EnsureBlobExists(containerName, blobName);

                var container = Containers[containerName];
                container.Remove(blobName);
            });
        }

        private void EnsureContainerDoesNotExist(string containerName)
        {
            if (Containers.ContainsKey(containerName))
            {
                throw new StorageContainerAlreadyExistsException();
            }
        }

        private void EnsureContainerExists(string containerName)
        {
            if (!Containers.ContainsKey(containerName))
            {
                throw new StorageContainerDoesNotExistException();
            }
        }

        private void EnsureBlobExists(string containerName, string blobName)
        {
            EnsureContainerExists(containerName);

            var container = Containers[containerName];

            if (!container.ContainsKey(blobName))
            {
                throw new BlobDoesNotExistException();
            }
        }
    }
}
