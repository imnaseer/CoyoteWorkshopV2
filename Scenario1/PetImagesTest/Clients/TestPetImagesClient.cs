// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Mvc;
using PetImages.Contracts;
using PetImages.Controllers;
using PetImages.Storage;
using System;
using System.Net;
using System.Threading.Tasks;

namespace PetImagesTest.Clients
{
    public class TestPetImagesClient : IPetImagesClient
    {
        private readonly ICosmosContainer AccountContainer;

        public TestPetImagesClient(ICosmosContainer accountContainer)
        {
            this.AccountContainer = accountContainer;
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

        /// <summary>
        /// Simulate middleware by wrapping invocation of controller in exception handling
        /// code which runs in middleware in production.
        /// </summary>
        private static async Task<ActionResult<T>> InvokeControllerAction<T>(Func<Task<ActionResult<T>>> lambda)
        {
            return await lambda();
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
