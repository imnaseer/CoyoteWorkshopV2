using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PetImages.Messaging;
using PetImages.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PetImages.Worker
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;

        private readonly IWorker GenerateThumbnailWorker;

        private readonly IMessageReceiver MessageReceiver;

        public Worker(ILogger<Worker> logger, IAccountContainer accountContainer, IImageContainer imageContainer, IStorageAccount storageAccount, IMessageReceiver messageReceiver)
        {
            _logger = logger;
            this.GenerateThumbnailWorker = new GenerateThumbnailWorker(accountContainer, imageContainer, storageAccount);
            this.MessageReceiver = messageReceiver;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var nextMessage = await this.MessageReceiver.ReadMessage();
                if (nextMessage != null)
                {
                    if(nextMessage.Type.Equals(Message.GenerateThumbnailMessageType, StringComparison.InvariantCultureIgnoreCase))
                    {
                        try
                        {
                            var thumbnailImageMessage = (GenerateThumbnailMessage)nextMessage;
                            _logger.LogInformation($"Processing Generate Thumbnail Message for {thumbnailImageMessage.AccountName} account's {thumbnailImageMessage.ImageName} image.");
                            await this.GenerateThumbnailWorker.ProcessMessage(thumbnailImageMessage);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"Error occured while processing thumbnail, reason: {ex}");
                        }
                    }
                    else
                    {
                        // throw for an invalid message type
                        _logger.LogError($"Invalid message type: {nextMessage.Type}");
                    }
                }
                else
                {
                    _logger.LogInformation("Worker running at: {time}. No messages processed.", DateTimeOffset.Now);
                    await Task.Delay(10000, stoppingToken); // configure delay?
                }
            }
        }
    }
}
