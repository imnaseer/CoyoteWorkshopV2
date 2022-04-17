﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PetImages.Contracts;
using PetImages.Entities;
using PetImages.Exceptions;
using PetImages.Storage;

namespace PetImages.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly ICosmosContainer CosmosContainer;

        public AccountController(ICosmosContainer cosmosDb)
        {
            this.CosmosContainer = cosmosDb;
        }

        /// <summary>
        /// Create an account
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<Account>> CreateAccountAsync(Account account)
        {
            var accountItem = account.ToItem();

            try
            {
                var createdAccountItem = await this.CosmosContainer.CreateItem(accountItem);
                return this.Ok(createdAccountItem.ToAccount());
            }
            catch (DatabaseItemAlreadyExistsException)
            {
                return this.Conflict();
            }
        }
    }
}
