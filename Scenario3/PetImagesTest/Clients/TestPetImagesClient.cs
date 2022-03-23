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
using PetImages.RetryFramework;
using Polly;
using PetImages.Messaging;

namespace PetImagesTest.Clients
{
    public class TestPetImagesClient : IPetImagesClient
    {
        private readonly IAccountContainer AccountContainer;
        private readonly IImageContainer ImageContainer;
        private readonly IStorageAccount BlobContainer;
        private readonly IMessagingClient MessagingClient;
        private readonly IAsyncPolicy AsyncPolicy;

        public TestPetImagesClient(IAccountContainer accountContainer)
        {
            this.AccountContainer = accountContainer;
            this.AsyncPolicy = RetryPolicyFactory.GetAsyncRetryExponential();
        }

        public TestPetImagesClient(IAccountContainer accountContainer, IImageContainer imageContainer, IStorageAccount blobContainer)
        {
            this.AccountContainer = accountContainer;
            this.ImageContainer = imageContainer;
            this.BlobContainer = blobContainer;
            this.AsyncPolicy = RetryPolicyFactory.GetAsyncRetryExponential();
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

            // TODO: Is this alright vs Task.Run()?
            return await this.AsyncPolicy.ExecuteAsync(async () =>
            {
                var controller = new AccountController(this.AccountContainer);
                var actionResult = await InvokeControllerAction(async () => await controller.CreateAccountAsync(accountCopy));
                return ExtractServiceResponse<Account>(actionResult.Result);
            });
        }

        public async Task<ServiceResponse<ImageRecord>> CreateImageAsync(string accountName, ImageRecord image)
        {
            var imageCopy = TestHelper.Clone(image);

            return await this.AsyncPolicy.ExecuteAsync(async () =>
            {
                var controller = new ImageController(this.AccountContainer, this.ImageContainer, this.BlobContainer, this.MessagingClient);
                var actionResult = await InvokeControllerAction(async () => await controller.CreateImageRecordSecondScenarioAsync(accountName, imageCopy));
                return ExtractServiceResponse<ImageRecord>(actionResult.Result);
            });
        }

        public async Task<ServiceResponse<byte[]>> GetImageAsync(string accountName, string imageName)
        {
            return await this.AsyncPolicy.ExecuteAsync(async () =>
            {
                var controller = new ImageController(this.AccountContainer, this.ImageContainer, this.BlobContainer, this.MessagingClient);
                var actionResult = await InvokeControllerAction(async () => await controller.GetImageContentsAsync(accountName, imageName));
                return ExtractServiceResponse<byte[]>(actionResult.Result);
            });
        }

        /// <summary>
        /// Simulate middleware by wrapping invocation of controller in exception handling
        /// code which runs in middleware in production.
        /// </summary>
        private static async Task<ActionResult<T>> InvokeControllerAction<T>(Func<Task<ActionResult<T>>> lambda)
        {
            try
            {
                return await lambda();
            }
            catch (SimulatedDatabaseFaultException)
            {
                return new ActionResult<T>(new StatusCodeResult((int)HttpStatusCode.ServiceUnavailable));
            }
        }

        private static ServiceResponse<T> ExtractServiceResponse<T>(ActionResult<T> actionResult)
        {
            var response = actionResult.Result;
            if (response is OkObjectResult okObjectResult)
            {
                return new ServiceResponse<T>()
                {
                    StatusCode = (HttpStatusCode)okObjectResult.StatusCode,
                    Resource = (T)okObjectResult.Value
                };
            }
            else if (response is StatusCodeResult statusCodeResult)
            {
                return new ServiceResponse<T>()
                {
                    StatusCode = (HttpStatusCode)statusCodeResult.StatusCode
                };
            }
            else
            {
                throw new InvalidOperationException();
            }
        }
    }
}
