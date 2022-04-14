﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote;
using Microsoft.Coyote.SystematicTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PetImages;
using PetImages.Contracts;
using PetImages.Storage;
using PetImages.RetryFramework;
using PetImagesTest.Exceptions;
using PetImages.TestRetryFramework;
using PetImagesTest.StorageMocks;
using PetImagesTest.MessagingMocks;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Coyote.Specifications;
using PetImages.Messaging;
using Polly;

namespace PetImagesTest.Clients
{
    [TestClass]
    public class Tests
    {
        private static readonly bool useInMemoryClient = true;

        [TestMethod]
        public async Task TestFirstScenarioAsync()
        {
            var serviceClient = await InitializeSystemAsync();

            var accountName = "MyAccount";

            var account1 = new Account()
            {
                Name = accountName,
                ContactEmailAddress = "john@acme.com"
            };

            var account2 = new Account()
            {
                Name = accountName,
                ContactEmailAddress = "sally@contoso.com"
            };

            // Call CreateAccount twice without awaiting, which makes both methods run
            // asynchronously with each other.
            var task1 = serviceClient.CreateAccountAsync(account1);
            var task2 = serviceClient.CreateAccountAsync(account2);

            // Then wait both requests to complete.
            await Task.WhenAll(task1, task2);

            var statusCode1 = task1.Result.StatusCode;
            var statusCode2 = task2.Result.StatusCode;

            // Finally, assert that only one of the two requests succeeded and the other
            // failed. Note that we do not know which one of the two succeeded as the
            // requests ran concurrently (this is why we use an exclusive OR).
            Assert.IsTrue(
                (statusCode1 == HttpStatusCode.OK && statusCode2 == HttpStatusCode.Conflict) ||
                (statusCode1 == HttpStatusCode.Conflict && statusCode2 == HttpStatusCode.OK));
        }

        [TestMethod]
        public async Task TestSecondScenarioUpdateAsync()
        {
            var serviceClient = await InitializeSystemAsync();

            string accountName = "MyAccount";
            string imageName = "pet.jpg";
            string contentType = "image/jpeg";

            // Create an account request payload
            var account = new Account()
            {
                Name = accountName,
                ContactEmailAddress = "john@acme.com"
            };

            var accountResult = await serviceClient.CreateAccountAsync(account);
            Assert.IsTrue(accountResult.StatusCode == HttpStatusCode.OK);

            var createResult = await serviceClient.CreateOrUpdateImageAsync(
                accountName,
                new Image()
                {
                    Name = imageName,
                    ContentType = contentType,
                    Content = GetDogImageBytes()
                });
            Assert.IsTrue(createResult.StatusCode == HttpStatusCode.OK);


            var utcNow = DateTime.UtcNow;

            var updateResult1 = serviceClient.CreateOrUpdateImageAsync(
                accountName,
                new Image()
                {
                    Name = imageName,
                    ContentType = contentType,
                    Content = GetCatImageBytes(),
                    LastModifiedTimestamp = utcNow
                });
            var updateResult2 = serviceClient.CreateOrUpdateImageAsync(
                accountName,
                new Image()
                {
                    Name = imageName,
                    ContentType = contentType,
                    Content = GetParrotImageBytes(),
                    LastModifiedTimestamp = utcNow.AddDays(1)
                });

            await Task.WhenAll(updateResult1, updateResult2);

            var statusCode1 = updateResult1.Result.StatusCode;
            var statusCode2 = updateResult2.Result.StatusCode;

            Assert.IsTrue(
                (statusCode1 == HttpStatusCode.OK && statusCode2 == HttpStatusCode.Conflict) ||
                (statusCode1 == HttpStatusCode.Conflict && statusCode2 == HttpStatusCode.OK) ||
                (statusCode1 == HttpStatusCode.BadRequest && statusCode2 == HttpStatusCode.OK) ||
                (statusCode1 == HttpStatusCode.OK && statusCode2 == HttpStatusCode.OK));
        }

        [TestMethod]
        public async Task TestSecondScenarioCreateAsync()
        {
            var serviceClient = await InitializeSystemAsync();

            string accountName = "MyAccount";
            string imageName = "pet.jpg";
            string contentType = "image/jpeg";

            // Create an account request payload
            var account = new Account()
            {
                Name = accountName,
                ContactEmailAddress = "john@acme.com"
            };

            var accountResult = await serviceClient.CreateAccountAsync(account);
            Assert.IsTrue(accountResult.StatusCode == HttpStatusCode.OK);

            var utcNow = DateTime.UtcNow;

            var createResult1 = serviceClient.CreateOrUpdateImageAsync(
                accountName,
                new Image()
                {
                    Name = imageName,
                    ContentType = contentType,
                    Content = GetCatImageBytes(),
                    LastModifiedTimestamp = utcNow
                });
            var createResult2 = serviceClient.CreateOrUpdateImageAsync(
                accountName,
                new Image()
                {
                    Name = imageName,
                    ContentType = contentType,
                    Content = GetParrotImageBytes(),
                    LastModifiedTimestamp = utcNow.AddDays(1)
                });

            await Task.WhenAll(createResult1, createResult2);

            var statusCode1 = createResult1.Result.StatusCode;
            var statusCode2 = createResult2.Result.StatusCode;

            Assert.IsTrue(
                (statusCode1 == HttpStatusCode.OK && statusCode2 == HttpStatusCode.Conflict) ||
                (statusCode1 == HttpStatusCode.Conflict && statusCode2 == HttpStatusCode.OK) ||
                (statusCode1 == HttpStatusCode.BadRequest && statusCode2 == HttpStatusCode.OK) ||
                (statusCode1 == HttpStatusCode.OK && statusCode2 == HttpStatusCode.OK));
        }

        [TestMethod]
        public async Task TestThirdScenarioAsync()
        {
            var serviceClient = await InitializeSystemAsync();

            string accountName = "MyAccount";
            string imageName = "pet.jpg";
            string contentType = "image/jpeg";

            // Create an account request payload
            var account = new Account()
            {
                Name = accountName,
                ContactEmailAddress = "john@acme.com"
            };

            var accountResult = await serviceClient.CreateAccountAsync(account);
            Assert.IsTrue(accountResult.StatusCode == HttpStatusCode.OK);

            var task1 = serviceClient.CreateOrUpdateImageAsync(
                accountName,
                new Image() { Name = imageName, ContentType = contentType, Content = GetDogImageBytes() });
            var task2 = serviceClient.CreateOrUpdateImageAsync(
                accountName,
                new Image() { Name = imageName, ContentType = contentType, Content = GetCatImageBytes() });

            await Task.WhenAll(task1, task2);

            var statusCode1 = task1.Result.StatusCode;
            var statusCode2 = task2.Result.StatusCode;

            Assert.IsTrue(
                (statusCode1 == HttpStatusCode.OK && statusCode2 == HttpStatusCode.Conflict) ||
                (statusCode1 == HttpStatusCode.Conflict && statusCode2 == HttpStatusCode.OK) ||
                (statusCode1 == HttpStatusCode.OK && statusCode2 == HttpStatusCode.OK));

            var imageContentResult = await serviceClient.GetImageContentAsync(accountName, imageName);
            Assert.IsTrue(imageContentResult.StatusCode == HttpStatusCode.OK);

            if (statusCode1 == HttpStatusCode.OK && statusCode2 != HttpStatusCode.OK)
            {
                Assert.IsTrue(IsDogImage(imageContentResult.Resource));
            }
            else if (statusCode1 != HttpStatusCode.OK && statusCode2 == HttpStatusCode.OK)
            {
                Assert.IsTrue(IsCatImage(imageContentResult.Resource));
            }
            else
            {
                Assert.IsTrue(
                    IsDogImage(imageContentResult.Resource) ||
                    IsCatImage(imageContentResult.Resource));
            }
        }

        [TestMethod]
        public async Task TestFourthScenarioAsync()
        {
            var serviceClient = await InitializeSystemAsync();

            string accountName = "MyAccount";
            string imageName = "pet.jpg";
            string contentType = "image/jpeg";

            // Create an account request payload
            var account = new Account()
            {
                Name = accountName,
                ContactEmailAddress = "john@acme.com"
            };

            var accountResult = await serviceClient.CreateAccountAsync(account);
            Assert.IsTrue(accountResult.StatusCode == HttpStatusCode.OK);

            var task1 = serviceClient.CreateOrUpdateImageAsync(
                accountName,
                new Image() { Name = imageName, ContentType = contentType, Content = GetDogImageBytes() });
            var task2 = serviceClient.CreateOrUpdateImageAsync(
                accountName,
                new Image() { Name = imageName, ContentType = contentType, Content = GetCatImageBytes() });

            await Task.WhenAll(task1, task2);

            var statusCode1 = task1.Result.StatusCode;
            var statusCode2 = task2.Result.StatusCode;

            Assert.IsTrue(
                (statusCode1 == HttpStatusCode.OK && statusCode2 == HttpStatusCode.Conflict) ||
                (statusCode1 == HttpStatusCode.Conflict && statusCode2 == HttpStatusCode.OK) ||
                (statusCode1 == HttpStatusCode.OK && statusCode2 == HttpStatusCode.OK));

            Image image;
            while (true)
            {
                var imageResult = await serviceClient.GetImageAsync(accountName, imageName);
                Assert.IsTrue(imageResult.StatusCode == HttpStatusCode.OK);

                image = imageResult.Resource;
                if (image.State == ImageState.Created.ToString())
                {
                    break;
                }

                await Task.Delay(100);
            }

            var imageContentResult = await serviceClient.GetImageContentAsync(accountName, imageName);
            Assert.IsTrue(imageContentResult.StatusCode == HttpStatusCode.OK);

            var thumbnailContentResult = await serviceClient.GetImageThumbnailAsync(accountName, imageName);
            Assert.IsTrue(thumbnailContentResult.StatusCode == HttpStatusCode.OK);

            var imageContent = imageContentResult.Resource;
            var thumbnail = thumbnailContentResult.Resource;

            if (statusCode1 == HttpStatusCode.OK && statusCode2 != HttpStatusCode.OK)
            {
                Assert.IsTrue(IsDogImage(imageContent) && IsDogThumbnail(thumbnail));
            }
            else if (statusCode1 != HttpStatusCode.OK && statusCode2 == HttpStatusCode.OK)
            {
                Assert.IsTrue(IsCatImage(imageContent) && IsCatThumbnail(thumbnail));
            }
            else
            {
                Assert.IsTrue(
                    (IsDogImage(imageContent) && IsDogThumbnail(thumbnail)) ||
                    (IsCatImage(imageContent) && IsCatThumbnail(thumbnail)));
            }
        }

        [TestMethod]
        public async Task TestFifthScenarioAsync()
        {
            var randomizedFaultPolicy = TestRetryPolicyFactory.GetRandomPermanentFailureAsyncPolicy();
            var serviceClient = await InitializeSystemAsync(randomizedFaultPolicy);
            randomizedFaultPolicy.ShouldRandomlyFail = false;

            string accountName = "MyAccount";
            string imageName = "pet.jpg";
            string contentType = "image/jpeg";

            // Create an account request payload
            var account = new Account()
            {
                Name = accountName,
                ContactEmailAddress = "john@acme.com"
            };

            var accountResult = await serviceClient.CreateAccountAsync(account);
            Assert.IsTrue(accountResult.StatusCode == HttpStatusCode.OK);

            randomizedFaultPolicy.ShouldRandomlyFail = true;

            try
            {
                var _ = await serviceClient.CreateOrUpdateImageAsync(
                    accountName,
                    new Image() { Name = imageName, ContentType = contentType, Content = GetDogImageBytes() });
                Assert.IsTrue(_.StatusCode == HttpStatusCode.OK);
            }
            catch (SimulatedRandomFaultException)
            {
            }

            randomizedFaultPolicy.ShouldRandomlyFail = false;

            var imageResult = await serviceClient.GetImageAsync(accountName, imageName);
            Assert.IsTrue(
                    imageResult.StatusCode == HttpStatusCode.OK ||
                    imageResult.StatusCode == HttpStatusCode.NotFound);

            if (imageResult.StatusCode == HttpStatusCode.NotFound)
            {
                return;
            }

            while (true)
            {
                imageResult = await serviceClient.GetImageAsync(accountName, imageName);
                Assert.IsTrue(imageResult.StatusCode == HttpStatusCode.OK);

                var image = imageResult.Resource;
                if (image.State == ImageState.Created.ToString())
                {
                    break;
                }

                await Task.Delay(100);
            }
        }

        [TestMethod]
        public void SystematicTestFirstScenario()
        {
            RunSystematicTest(TestFirstScenarioAsync);
        }

        [TestMethod]
        public void SystematicTestSecondScenarioUpdate()
        {
            RunSystematicTest(TestSecondScenarioUpdateAsync);
        }

        [TestMethod]
        public void SystematicTestSecondScenarioCreate()
        {
            RunSystematicTest(TestSecondScenarioCreateAsync);
        }

        [TestMethod]
        public void SystematicTestThirdScenario()
        {
            RunSystematicTest(TestThirdScenarioAsync);
        }

        [TestMethod]
        public void SystematicTestFourthScenario()
        {
            RunSystematicTest(TestFourthScenarioAsync);
        }

        [TestMethod]
        public void SystematicTestFifthScenario()
        {
            RunSystematicTest(TestFifthScenarioAsync);
        }

        private static async Task<IServiceClient> InitializeSystemAsync(IAsyncPolicy asyncPolicy = null)
        {
            if (useInMemoryClient)
            {
                var cosmosState = new MockCosmosState();

                if (asyncPolicy == null)
                {
                    asyncPolicy = RetryPolicyFactory.GetAsyncRetryExponential();
                }

                var database = new WrappedCosmosDatabase(
                    new MockCosmosDatabase(cosmosState),
                    asyncPolicy);
                var accountContainer = (IAccountContainer)await database.CreateContainerAsync(Constants.AccountContainerName);
                var imageContainer = (IImageContainer)await database.CreateContainerAsync(Constants.ImageContainerName);

                var storageAccount = new WrappedStorageAccount(
                    new MockStorageAccount(),
                    asyncPolicy);

                var messagingClient = new WrappedMessagingClient(
                    new MockMessagingClient(accountContainer, imageContainer, storageAccount),
                    asyncPolicy);

                var serviceClient = new InMemoryTestServiceClient(
                    accountContainer,
                    imageContainer,
                    storageAccount,
                    messagingClient);

                return serviceClient;
            }
            else
            {
                var factory = new ServiceFactory();
                await factory.InitializeAccountContainerAsync();
                await factory.InitializeImageContainerAsync();
                await factory.InitializeMessagingClient();

                return new TestServiceClient(factory);
            }
        }

        /// <summary>
        /// Invoke the Coyote systematic testing engine to run the specified test multiple iterations,
        /// each iteration exploring potentially different interleavings using some underlying program
        /// exploration strategy (by default a uniform probabilistic strategy).
        /// </summary>
        /// <remarks>
        /// Learn more in our documentation: https://microsoft.github.io/coyote/how-to/unit-testing
        /// </remarks>
        private static void RunSystematicTest(Func<Task> test, string reproducibleScheduleFilePath = null)
        {
            // Configuration for how to run a concurrency unit test with Coyote.
            // This configuration will run the test 1000 times exploring different paths each time.
            var config = Configuration
                .Create()
                .WithMaxSchedulingSteps(5000)
                .WithTestingIterations(useInMemoryClient ? (uint)1000 : 100);

            if (reproducibleScheduleFilePath != null)
            {
                var trace = File.ReadAllText(reproducibleScheduleFilePath);
                config = config.WithReplayStrategy(trace);
            }

            async Task TestActionAsync()
            {
                Specification.RegisterMonitor<TestLivenessSpec>();
                await test();
                Specification.Monitor<TestLivenessSpec>(new TestTerminalEvent());
            };

            var testingEngine = TestingEngine.Create(config, TestActionAsync);

            try
            {
                testingEngine.Run();

                string assertionText = testingEngine.TestReport.GetText(config);
                assertionText +=
                    $"{Environment.NewLine} Random Generator Seed: " +
                    $"{testingEngine.TestReport.Configuration.RandomGeneratorSeed}{Environment.NewLine}";
                foreach (var bugReport in testingEngine.TestReport.BugReports)
                {
                    assertionText +=
                    $"{Environment.NewLine}" +
                    "Bug Report: " + bugReport.ToString(CultureInfo.InvariantCulture);
                }

                if (testingEngine.TestReport.NumOfFoundBugs > 0)
                {
                    var timeStamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH-mm-ssZ", CultureInfo.InvariantCulture);
                    var reproducibleTraceFileName = $"buggy-{timeStamp}.schedule";
                    assertionText += Environment.NewLine + "Reproducible trace which leads to the bug can be found at " +
                        $"{Path.Combine(Directory.GetCurrentDirectory(), reproducibleTraceFileName)}";

                    File.WriteAllText(reproducibleTraceFileName, testingEngine.ReproducibleTrace);
                }

                Assert.IsTrue(testingEngine.TestReport.NumOfFoundBugs == 0, assertionText);

                Console.WriteLine(testingEngine.TestReport.GetText(config));
            }
            finally
            {
                testingEngine.Stop();
            }
        }

        private static byte[] GetDogImageBytes() => new byte[] { 1, 2, 3 };
        private static byte[] GetCatImageBytes() => new byte[] { 4, 5, 6 };
        private static byte[] GetParrotImageBytes() => new byte[] { 7, 8, 9 };

        private static bool IsDogImage(byte[] bytes) => bytes.SequenceEqual(GetDogImageBytes());
        private static bool IsDogThumbnail(byte[] bytes) => bytes.SequenceEqual(GetDogImageBytes());
        private static bool IsCatImage(byte[] bytes) => bytes.SequenceEqual(GetCatImageBytes());
        private static bool IsCatThumbnail(byte[] bytes) => bytes.SequenceEqual(GetCatImageBytes());
        private static bool IsParrotImage(byte[] bytes) => bytes.SequenceEqual(GetParrotImageBytes());
        private static bool IsParrotThumbnail(byte[] bytes) => bytes.SequenceEqual(GetParrotImageBytes());
    }

    public class TestLivenessSpec : Monitor
    {
        [Hot]
        [Start]
        [OnEventGotoState(typeof(TestTerminalEvent), typeof(Terminal))]
        private class Init : State
        {
        }

        [OnEventGotoState(typeof(TestTerminalEvent), typeof(Terminal))]
        private class Terminal : State
        {
        }
    }

    public class TestTerminalEvent : Event
    {
    }
}
