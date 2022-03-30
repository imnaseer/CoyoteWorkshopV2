﻿using PetImages.Contracts;
using System;
using System.Net;
using System.Threading.Tasks;

namespace PetImages
{
    public interface IClient : IDisposable
    {
        Task<HttpStatusCode> CreateAccountAsync(Account account);

        Task<HttpStatusCode> CreateImageAsync(string accountName, ImageRecord image);

        Task<(HttpStatusCode, Account)> GetAccountAsync(string accountName);

        Task<HttpStatusCode> DeleteAccountAsync(string accountName);

        Task<(HttpStatusCode, ImageRecord)> CreateOrUpdateImageAsync(string accountName, ImageRecord image);

        Task<(HttpStatusCode, byte[])> GetImageAsync(string accountName, string imageName);

        Task<(HttpStatusCode, byte[])> GetImageThumbnailAsync(string accountName, string imageName);

        Task<HttpStatusCode> DeleteAsync(string accountName, string imageName);
    }
}