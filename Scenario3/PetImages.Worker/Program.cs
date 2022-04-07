using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PetImages.Messaging;
using PetImages.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PetImages.Worker
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    var cosmosDatabase = CosmosDatabase.Create(Constants.DatabaseName);
                    var accountContainer = cosmosDatabase.GetContainer(Constants.AccountContainerName).Result;
                    var imageContainer = cosmosDatabase.GetContainer(Constants.ImageContainerName).Result;
                    var storageAccount = new AzureStorageAccount();

                    services.AddSingleton((IAccountContainer) accountContainer);
                    services.AddSingleton((IImageContainer) imageContainer);
                    services.AddSingleton<IStorageAccount>(storageAccount);
                    services.AddSingleton<IMessageReceiver>(_ => new StorageMessageReceiverClient(Constants.ThumbnailQueueName));
                    services.AddSingleton<IMessagingClient>(_ => new StorageMessagingClient(Constants.ThumbnailQueueName));
                    services.AddHostedService<BackgroundJobService>();
                });
    }
}
