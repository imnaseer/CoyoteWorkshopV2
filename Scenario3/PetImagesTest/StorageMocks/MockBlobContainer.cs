// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Threading.Tasks;
using PetImages.Storage;

namespace PetImagesTest.StorageMocks
{
    // Buggy mock, need to fix these mocks as part of third scenario.
    internal class MockBlobContainerProvider : IBlobContainer
    {
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte[]>> Containers;

        internal MockBlobContainerProvider()
        {
            this.Containers = new ConcurrentDictionary<string, ConcurrentDictionary<string, byte[]>>();
        }

        public Task CreateContainerAsync(string containerName)
        {
            return Task.FromResult(this.Containers.TryAdd(containerName, new ConcurrentDictionary<string, byte[]>()));
        }

        public Task CreateContainerIfNotExistsAsync(string containerName)
        {
            return Task.FromResult(this.Containers.TryAdd(containerName, new ConcurrentDictionary<string, byte[]>()));
        }

        public Task DeleteContainerAsync(string containerName)
        {
            return Task.FromResult(this.Containers.TryRemove(containerName, out ConcurrentDictionary<string, byte[]> _));
        }

        public Task<bool> DeleteContainerIfExistsAsync(string containerName)
        {
            return Task.FromResult(this.Containers.TryRemove(containerName, out ConcurrentDictionary<string, byte[]> _));
        }

        public Task CreateOrUpdateBlobAsync(string containerName, string blobName, byte[] blobContents)
        {
            return Task.FromResult(this.Containers[containerName].AddOrUpdate(blobName, blobContents, (_, oldContents) => blobContents));
        }

        public Task<byte[]> GetBlobAsync(string containerName, string blobName)
        {
            var result = this.Containers[containerName][blobName];
            return Task.FromResult(result);
        }

        public Task<bool> ExistsBlobAsync(string containerName, string blobName)
        {
            var finalResult = this.Containers.TryGetValue(containerName, out ConcurrentDictionary<string, byte[]> container) &&
                    container.ContainsKey(blobName);
            return Task.FromResult(finalResult);
        }

        public Task DeleteBlobAsync(string containerName, string blobName)
        {
            return Task.FromResult(this.Containers[containerName].TryRemove(blobName, out byte[] _));
        }

        public Task<bool> DeleteBlobIfExistsAsync(string containerName, string blobName)
        {
            bool finalResult;
            if (!this.Containers.TryGetValue(containerName, out ConcurrentDictionary<string, byte[]> container))
            {
                finalResult = false;
            }

            finalResult = container.TryRemove(blobName, out byte[] _);
            return Task.FromResult(finalResult);
        }

        public Task DeleteAllBlobsAsync(string containerName)
        {
            if (this.Containers.TryGetValue(containerName, out ConcurrentDictionary<string, byte[]> container))
            {
                container.Clear();
            }
            return Task.CompletedTask;
        }
    }
}
