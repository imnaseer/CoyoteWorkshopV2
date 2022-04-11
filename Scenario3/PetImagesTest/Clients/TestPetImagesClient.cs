// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PetImages.Contracts;
using PetImages.Controllers;
using PetImages.Storage;
using PetImagesTest.Exceptions;
using PetImages.Messaging;

namespace PetImagesTest.Clients
{
    public class TestPetImagesClient : IPetImagesClient
    {
        private readonly IAccountContainer AccountContainer;
        private readonly IImageContainer ImageContainer;
        private readonly IStorageAccount BlobContainer;
        private readonly IMessagingClient MessagingClient;

        public TestPetImagesClient(IAccountContainer accountContainer)
            : this(accountContainer, null, null, null)
        {
        }

        public TestPetImagesClient(IAccountContainer accountContainer, IImageContainer imageContainer, IStorageAccount blobContainer)
            : this(accountContainer, imageContainer, blobContainer, null)
        {
        }

        public TestPetImagesClient(IAccountContainer accountContainer, IImageContainer imageContainer, IStorageAccount blobContainer, IMessagingClient messagingClient)
        {
            this.AccountContainer = accountContainer;
            this.ImageContainer = imageContainer;
            this.BlobContainer = blobContainer;
            this.MessagingClient = messagingClient;
        }

        public async Task<ServiceResponse<Account>> CreateAccountAsync(Account account)
        {
            var accountCopy = TestHelper.Clone(account);


            return await Task.Run(async () =>
            {
                var controller = new AccountController(this.AccountContainer);
                var actionResult = await InvokeControllerAction(async () => await controller.CreateAccountAsync(accountCopy));
                return ExtractServiceResponse<Account>(actionResult.Result);
            });
        }

        public async Task<ServiceResponse<ImageRecord>> CreateOrUpdateImageAsync(string accountName, ImageRecord image)
        {
            var imageCopy = TestHelper.Clone(image);

            return await Task.Run(async () =>
            {
                var controller = new ImageController(this.AccountContainer, this.ImageContainer, this.BlobContainer, this.MessagingClient);
                var actionResult = await InvokeControllerAction(async () => await controller.CreateImageRecordFourthScenarioAsync(accountName, imageCopy));
                return ExtractServiceResponse<ImageRecord>(actionResult.Result);
            });
        }


        public async Task<ServiceResponse<ImageRecord>> GetImageRecordAsync(string accountName, string imageName)
        {
            return await Task.Run(async () =>
            {
                var controller = new ImageController(this.AccountContainer, this.ImageContainer, this.BlobContainer, this.MessagingClient);
                var actionResult = await InvokeControllerAction(async () => await controller.GetImageRecord(accountName, imageName));
                return ExtractServiceResponse<ImageRecord>(actionResult.Result);
            });
        }

        public async Task<ServiceResponse<byte[]>> GetImageAsync(string accountName, string imageName)
        {
            return await Task.Run(async () =>
            {
                var controller = new ImageController(this.AccountContainer, this.ImageContainer, this.BlobContainer, this.MessagingClient);
                var actionResult = await InvokeControllerAction(async () => await controller.GetImageContentsAsync(accountName, imageName));
                return ExtractServiceResponse<byte[]>(actionResult.Result);
            });
        }

        public async Task<ServiceResponse<byte[]>> GetImageThumbnailAsync(string accountName, string imageName)
        {
            return await Task.Run(async () =>
            {
                var controller = new ImageController(this.AccountContainer, this.ImageContainer, this.BlobContainer, this.MessagingClient);
                var actionResult = await InvokeControllerAction(async () => await controller.GetImageThumbailAsync(accountName, imageName));
                return ExtractServiceResponse<byte[]>(actionResult.Result);
            });
        }

        /// <summary>
        /// Simulate middleware by wrapping invocation of controller in exception handling
        /// code which runs in middleware in production.
        /// </summary>
        private static async Task<ActionResult<T>> InvokeControllerAction<T>(Func<Task<ActionResult<T>>> lambda)
        {
            return await lambda();
        }

        private static ServiceResponse<T> ExtractServiceResponse<T>(ActionResult<T> actionResult)
            where T : class
        {
            var response = actionResult.Result;
            if (response is ObjectResult objectResult)
            {
                var success = objectResult.StatusCode >= 200 && objectResult.StatusCode <= 299;

                return new ServiceResponse<T>()
                {
                    StatusCode = (HttpStatusCode)objectResult.StatusCode,
                    Resource = success ? (T)objectResult.Value : null,
                    Error = !success ? (Error)objectResult.Value : null
                };
            }
            else if (response is StatusCodeResult statusCodeResult)
            {
                return new ServiceResponse<T>()
                {
                    StatusCode = (HttpStatusCode)statusCodeResult.StatusCode,
                };
            }
            else
            {
                throw new InvalidOperationException();
            }
        }
    }
}
