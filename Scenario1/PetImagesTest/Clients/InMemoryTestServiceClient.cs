﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PetImages.Contracts;
using PetImages.Controllers;
using PetImages.Middleware;
using PetImages.Persistence;
using System;
using System.Net;
using System.Threading.Tasks;

namespace PetImagesTest.Clients
{
    public class InMemoryTestServiceClient : IServiceClient
    {
        private readonly ICosmosDatabase cosmosDatabase;

        public InMemoryTestServiceClient(ICosmosDatabase cosmosDatabase)
        {
            this.cosmosDatabase = cosmosDatabase;
        }

        public async Task<ServiceResponse<Account>> CreateAccountAsync(Account account)
        {
            var accountCopy = TestHelper.Clone(account);


            return await Task.Run(async () =>
            {
                var controller = new AccountController(this.cosmosDatabase);
                var actionResult = await InvokeControllerActionAsync(
                    HttpMethods.Post,
                    new Uri($"/accounts", UriKind.RelativeOrAbsolute),
                    async () => await controller.CreateAccountAsync(accountCopy));
                return ExtractServiceResponse<Account>(actionResult.Result);
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
            else if (response is FileContentResult fileContentResult)
            {
                return new ServiceResponse<T>()
                {
                    StatusCode = HttpStatusCode.OK,
                    Resource = CastToT<T>(fileContentResult.FileContents)
                };
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        private static T CastToT<T>(object o)
        {
            if (o is T)
            {
                return (T)o;
            }
            else
            {
                throw new InvalidCastException();
            }
        }
    }
}
