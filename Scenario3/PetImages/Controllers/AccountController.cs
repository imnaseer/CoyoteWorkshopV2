// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PetImages.Contracts;
using PetImages.Entities;
using PetImages.Exceptions;
using PetImages.Messaging;
using PetImages.Storage;

namespace PetImages.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly ICosmosContainer CosmosContainer;

        public AccountController(IAccountContainer cosmosDb)
        {
            this.CosmosContainer = cosmosDb;
        }

        /// <summary>
        /// CreateAccountAsync fixed.
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<Account>> CreateAccountAsync(Account account)
        {
            var maybeError = ValidateAccount(account);
            if (maybeError != null)
            {
                return this.BadRequest(maybeError);
            }

            var accountItem = account.ToItem();

            try
            {
                accountItem = await this.CosmosContainer.CreateItem(accountItem);
            }
            catch (DatabaseItemAlreadyExistsException)
            {
                return this.Conflict();
            }

            return this.Ok(accountItem.ToAccount());
        }

        [HttpGet]
        public async Task<ActionResult<Account>> GetAccountAsync(string name)
        {
            try
            {
                var accountItem = await this.CosmosContainer.GetItem<AccountItem>(partitionKey: name, id: name);
                return this.Ok(accountItem.ToAccount());
            }
            catch (DatabaseItemDoesNotExistException)
            {
                return this.NotFound();
            }
        }

        [HttpDelete]
        public async Task<ActionResult<Account>> DeleteAccountAsync(string name)
        {
            try
            {
                await this.CosmosContainer.DeleteItem(partitionKey: name, id: name);
                return this.Ok();
            }
            catch (DatabaseItemDoesNotExistException)
            {
                return this.NoContent();
            }
        }

        /// <summary>
        /// CreateAccountAsync fixed.
        /// </summary>
        [HttpPost]
        [NonAction]
        public async Task<ActionResult<Account>> CreateAccountAsyncIncorrect(Account account)
        {
            var maybeError = ValidateAccount(account);
            if (maybeError != null)
            {
                return this.BadRequest(maybeError);
            }

            var accountItem = account.ToItem();

            if (await CosmosHelper.DoesItemExist<AccountItem>(
                this.CosmosContainer,
                accountItem.PartitionKey,
                accountItem.Id))
            {
                return this.Conflict();
            }

            var createdAccountItem = await this.CosmosContainer.CreateItem(accountItem);

            return this.Ok(createdAccountItem.ToAccount());
        }

        private static Error ValidateAccount(Account account)
        {
            if (account == null)
            {
                return ErrorFactory.ParsingError(nameof(Account));
            }

            if (string.IsNullOrWhiteSpace(account.Name))
            {
                return ErrorFactory.InvalidParameterValueError(nameof(Account.Name), account.Name);
            }

            return null;
        }
    }
}
