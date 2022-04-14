﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PetImages.Contracts;
using PetImages.Controllers;
using PetImages.Messaging;
using PetImages.Middleware;
using PetImages.Storage;
using System;
using System.Net;
using System.Threading.Tasks;

namespace PetImagesTest.Clients
{
    public class InMemoryTestServiceClient : IServiceClient
    {
        private readonly IAccountContainer accountContainer;
        private readonly IImageContainer imageContainer;
        private readonly IStorageAccount blobContainer;
        private readonly IMessagingClient messagingClient;

        public InMemoryTestServiceClient(IAccountContainer accountContainer)
            : this(accountContainer, null, null, null)
        {
        }

        public InMemoryTestServiceClient(IAccountContainer accountContainer, IImageContainer imageContainer, IStorageAccount blobContainer)
            : this(accountContainer, imageContainer, blobContainer, null)
        {
        }

        public InMemoryTestServiceClient(IAccountContainer accountContainer, IImageContainer imageContainer, IStorageAccount blobContainer, IMessagingClient messagingClient)
        {
            this.accountContainer = accountContainer;
            this.imageContainer = imageContainer;
            this.blobContainer = blobContainer;
            this.messagingClient = messagingClient;
        }

        public async Task<ServiceResponse<Account>> CreateAccountAsync(Account account)
        {
            var accountCopy = TestHelper.Clone(account);


            return await Task.Run(async () =>
            {
                var controller = new AccountController(this.accountContainer);
                var actionResult = await InvokeControllerActionAsync(
                    HttpMethods.Post,
                    new Uri($"/accounts", UriKind.RelativeOrAbsolute),
                    async () => await controller.CreateAccountAsync(accountCopy));
                return ExtractServiceResponse<Account>(actionResult.Result);
            });
        }

        public async Task<ServiceResponse<Image>> CreateOrUpdateImageAsync(string accountName, Image image)
        {
            var imageCopy = TestHelper.Clone(image);

            return await Task.Run(async () =>
            {
                var controller = new ImageController(this.accountContainer, this.imageContainer, this.blobContainer, this.messagingClient);
                var actionResult = await InvokeControllerActionAsync(
                    HttpMethods.Put,
                    new Uri($"/accounts/{accountName}/images", UriKind.RelativeOrAbsolute),
                    async () => await controller.CreateImageFourthScenarioAsync(accountName, imageCopy));
                return ExtractServiceResponse<Image>(actionResult.Result);
            });
        }

        public async Task<ServiceResponse<Image>> GetImageAsync(string accountName, string imageName)
        {
            return await Task.Run(async () =>
            {
                var controller = new ImageController(this.accountContainer, this.imageContainer, this.blobContainer, this.messagingClient);
                var actionResult = await InvokeControllerActionAsync(
                    HttpMethods.Get,
                    new Uri($"/accounts/{accountName}/images/{imageName}", UriKind.RelativeOrAbsolute),
                    async () => await controller.GetImageAsync(accountName, imageName));
                return ExtractServiceResponse<Image>(actionResult.Result);
            });
        }

        public async Task<ServiceResponse<byte[]>> GetImageContentAsync(string accountName, string imageName)
        {
            return await Task.Run(async () =>
            {
                var controller = new ImageController(this.accountContainer, this.imageContainer, this.blobContainer, this.messagingClient);
                var actionResult = await InvokeControllerActionAsync(
                    HttpMethods.Get,
                    new Uri($"/accounts/{accountName}/images/{imageName}/content", UriKind.RelativeOrAbsolute),
                    async () => await controller.GetImageContentsAsync(accountName, imageName));
                return ExtractServiceResponse<byte[]>(actionResult.Result);
            });
        }

        public async Task<ServiceResponse<byte[]>> GetImageThumbnailAsync(string accountName, string imageName)
        {
            return await Task.Run(async () =>
            {
                var controller = new ImageController(this.accountContainer, this.imageContainer, this.blobContainer, this.messagingClient);
                var actionResult = await InvokeControllerActionAsync(
                    HttpMethods.Get,
                    new Uri($"/accounts/{accountName}/images/{imageName}/thumbnail", UriKind.RelativeOrAbsolute),
                    async () => await controller.GetImageThumbailAsync(accountName, imageName));
                return ExtractServiceResponse<byte[]>(actionResult.Result);
            });
        }

        /// <summary>
        /// Simulate middleware by wrapping invocation of controller in exception handling
        /// code which runs in middleware in production.
        /// </summary>
        private static async Task<ActionResult<T>> InvokeControllerActionAsync<T>(
            string httpMethod,
            Uri path,
            Func<Task<ActionResult<T>>> lambda)
        {
            ActionResult<T> result = null;
            var middlewareChain = new RequestIdMiddleware(async (httpContext) => result = await lambda());

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Method = httpMethod.ToString();
            httpContext.Request.Path = path.ToString();

            await middlewareChain.InvokeAsync(httpContext);

            return result;
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
