// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Mvc;
using PetImages.Contracts;
using PetImages.CosmosContracts;
using PetImages.Exceptions;
using PetImages.Persistence;
using System.Threading.Tasks;

namespace PetImages.Controllers
{
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly IAccountContainer AccountContainer;

        public AccountController(IAccountContainer accountContainer)
        {
            this.AccountContainer = accountContainer;
        }

        /// <summary>
        /// CreateAccountAsync fixed.
        /// </summary>
        [HttpPost]
        [Route(Routes.Accounts)]
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
                accountItem = await this.AccountContainer.CreateItem(accountItem);
            }
            catch (DatabaseItemAlreadyExistsException)
            {
                return this.Conflict();
            }

            return this.Ok(accountItem.ToAccount());
        }

        [HttpGet]
        [Route(Routes.AccountInstance)]
        public async Task<ActionResult<Account>> GetAccountAsync([FromRoute] string accountName)
        {
            try
            {
                var accountItem = await this.AccountContainer.GetItem<AccountItem>(partitionKey: accountName, id: accountName);
                return this.Ok(accountItem.ToAccount());
            }
            catch (DatabaseItemDoesNotExistException)
            {
                return this.NotFound();
            }
        }

        [HttpDelete]
        [Route(Routes.AccountInstance)]
        public async Task<ActionResult<Account>> DeleteAccountAsync([FromRoute] string accountName)
        {
            try
            {
                await this.AccountContainer.DeleteItem(partitionKey: accountName, id: accountName);
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
        [Route(Routes.Accounts)]
        [NonAction]
        public async Task<ActionResult<Account>> CreateAccountAsyncIncorrectAsync(Account account)
        {
            var maybeError = ValidateAccount(account);
            if (maybeError != null)
            {
                return this.BadRequest(maybeError);
            }

            var accountItem = account.ToItem();

            if (await CosmosHelper.DoesItemExistAsync<AccountItem>(
                this.AccountContainer,
                accountItem.PartitionKey,
                accountItem.Id))
            {
                return this.Conflict();
            }

            var createdAccountItem = await this.AccountContainer.CreateItem(accountItem);

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

            if (string.IsNullOrWhiteSpace(account.ContactEmailAddress))
            {
                return ErrorFactory.InvalidParameterValueError(nameof(Account.Name), account.Name);
            }

            return null;
        }
    }
}
