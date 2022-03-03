﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using PetImages.Contracts;

namespace PetImagesTest.Clients
{
    public interface IPetImagesClient
    {
        public Task<ServiceResponse<Account>> CreateAccountAsync(Account account);

        public Task<ServiceResponse<Image>> CreateOrUpdateImageAsync(string accountName, Image image);

        public Task<ServiceResponse<Image>> GetImageAsync(string accountName, string imageName);
    }
}
