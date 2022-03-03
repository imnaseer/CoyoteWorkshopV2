﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Random;
using PetImages.Entities;
using PetImages.Storage;
using System.Threading.Tasks;

namespace PetImagesTest.StorageMocks
{
    public class MockCosmosContainer : ICosmosContainer
    {
        private readonly string ContainerName;
        private readonly MockCosmosState State;

        public MockCosmosContainer(string containerName, MockCosmosState state)
        {
            this.ContainerName = containerName;
            this.State = state;
        }

        public Task<T> CreateItem<T>(T item)
            where T : DbItem
        {
            var itemCopy = TestHelper.Clone(item);

            return Task.Run(() =>
            {
                this.State.CreateItem(this.ContainerName, itemCopy);
                return itemCopy;
            });
        }

        public Task<T> GetItem<T>(string partitionKey, string id)
            where T : DbItem
        {
            return Task.Run(() =>
            {
                var item = this.State.GetItem(this.ContainerName, partitionKey, id);

                var itemCopy = TestHelper.Clone((T)item);

                return itemCopy;
            });
        }

        public Task<T> UpsertItem<T>(T item)
            where T : DbItem
        {
            return Task.Run(() =>
            {
                var itemCopy = TestHelper.Clone(item);
                this.State.UpsertItem(this.ContainerName, itemCopy);
                return itemCopy;
            });
        }

        public Task DeleteItem(string partitionKey, string id)
        {
            return Task.Run(() =>
            {
                this.State.DeleteItem(this.ContainerName, partitionKey, id);
            });
        }
    }
}