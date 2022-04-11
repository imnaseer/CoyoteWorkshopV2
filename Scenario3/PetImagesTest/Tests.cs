// Copyright (c) Microsoft Corporation.
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
        [TestMethod]
        public async Task TestFirstScenario()
        {
            var petImagesClient = await InitializeSystemAsync();

            // Create an account request payload
            var account = new Account()
            {
                Name = "MyAccount"
            };

            // Call CreateAccount twice without awaiting, which makes both methods run
            // asynchronously with each other.
            var task1 = petImagesClient.CreateAccountAsync(account);
            var task2 = petImagesClient.CreateAccountAsync(account);

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
        public async Task TestFirstScenarioAlternative()
        {
            // Initialize the in-memory service factory.
            using var factory = new ServiceFactory();
            await factory.InitializeAccountContainerAsync();
            await factory.InitializeImageContainerAsync();
            await factory.InitializeMessagingClient();

            using var client = new ServiceClient(factory);

            // Create an account request payload
            var account = new Account()
            {
                Name = "MyAccount"
            };
            // Call 'CreateAccount' twice without awaiting, which makes both methods run
            // asynchronously with each other.
            var task1 = client.CreateAccountAsync(account);
            var task2 = client.CreateAccountAsync(account);

            // Then wait both requests to complete.
            await Task.WhenAll(task1, task2);

            var statusCode1 = task1.Result;
            var statusCode2 = task2.Result;

            // Finally, assert that only one of the two requests succeeded and the other
            // failed. Note that we do not know which one of the two succeeded as the
            // requests ran concurrently (this is why we use an exclusive OR).
            Assert.IsTrue(
                (statusCode1 == HttpStatusCode.OK && statusCode2 == HttpStatusCode.Conflict) ||
                (statusCode1 == HttpStatusCode.Conflict && statusCode2 == HttpStatusCode.OK));
        }

        [TestMethod]
        public async Task TestSecondScenarioUpdate()
        {
            var petImagesClient = await InitializeSystemAsync();

            string accountName = "MyAccount";
            string imageName = "pet.jpg";
            string contentType = "image/jpeg";

            // Create an account request payload
            var account = new Account()
            {
                Name = accountName
            };

            var accountResult = await petImagesClient.CreateAccountAsync(account);
            Assert.IsTrue(accountResult.StatusCode == HttpStatusCode.OK);

            var createResult = await petImagesClient.CreateOrUpdateImageAsync(
                accountName,
                new ImageRecord()
                {
                    Name = imageName,
                    ContentType = contentType,
                    Content = GetDogImageBytes()
                });
            Assert.IsTrue(createResult.StatusCode == HttpStatusCode.OK);


            var utcNow = DateTime.UtcNow;

            var updateResult1 = petImagesClient.CreateOrUpdateImageAsync(
                accountName,
                new ImageRecord()
                {
                    Name = imageName,
                    ContentType = contentType,
                    Content = GetCatImageBytes(),
                    LastModifiedTimestamp = utcNow
                });
            var updateResult2 = petImagesClient.CreateOrUpdateImageAsync(
                accountName,
                new ImageRecord()
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
        public async Task TestSecondScenarioCreate()
        {
            var petImagesClient = await InitializeSystemAsync();

            string accountName = "MyAccount";
            string imageName = "pet.jpg";
            string contentType = "image/jpeg";

            // Create an account request payload
            var account = new Account()
            {
                Name = accountName
            };

            var accountResult = await petImagesClient.CreateAccountAsync(account);
            Assert.IsTrue(accountResult.StatusCode == HttpStatusCode.OK);

            var utcNow = DateTime.UtcNow;

            var createResult1 = petImagesClient.CreateOrUpdateImageAsync(
                accountName,
                new ImageRecord()
                {
                    Name = imageName,
                    ContentType = contentType,
                    Content = GetCatImageBytes(),
                    LastModifiedTimestamp = utcNow
                });
            var createResult2 = petImagesClient.CreateOrUpdateImageAsync(
                accountName,
                new ImageRecord()
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
        public async Task TestThirdScenario()
        {
            var petImagesClient = await InitializeSystemAsync();

            string accountName = "MyAccount";
            string imageName = "pet.jpg";
            string contentType = "image/jpeg";

            // Create an account request payload
            var account = new Account()
            {
                Name = accountName
            };

            var accountResult = await petImagesClient.CreateAccountAsync(account);
            Assert.IsTrue(accountResult.StatusCode == HttpStatusCode.OK);

            var task1 = petImagesClient.CreateOrUpdateImageAsync(
                accountName,
                new ImageRecord() { Name = imageName, ContentType = contentType, Content = GetDogImageBytes() });
            var task2 = petImagesClient.CreateOrUpdateImageAsync(
                accountName,
                new ImageRecord() { Name = imageName, ContentType = contentType, Content = GetCatImageBytes() });

            await Task.WhenAll(task1, task2);

            var statusCode1 = task1.Result.StatusCode;
            var statusCode2 = task2.Result.StatusCode;

            Assert.IsTrue(
                (statusCode1 == HttpStatusCode.OK && statusCode2 == HttpStatusCode.Conflict) ||
                (statusCode1 == HttpStatusCode.Conflict && statusCode2 == HttpStatusCode.OK) ||
                (statusCode1 == HttpStatusCode.OK && statusCode2 == HttpStatusCode.OK));

            var imageContentResult = await petImagesClient.GetImageAsync(accountName, imageName);
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
        public async Task TestFourthScenario()
        {
            var petImagesClient = await InitializeSystemAsync();

            string accountName = "MyAccount";
            string imageName = "pet.jpg";
            string contentType = "image/jpeg";

            // Create an account request payload
            var account = new Account()
            {
                Name = accountName
            };

            var accountResult = await petImagesClient.CreateAccountAsync(account);
            Assert.IsTrue(accountResult.StatusCode == HttpStatusCode.OK);

            var task1 = petImagesClient.CreateOrUpdateImageAsync(
                accountName,
                new ImageRecord() { Name = imageName, ContentType = contentType, Content = GetDogImageBytes() });
            var task2 = petImagesClient.CreateOrUpdateImageAsync(
                accountName,
                new ImageRecord() { Name = imageName, ContentType = contentType, Content = GetCatImageBytes() });

            await Task.WhenAll(task1, task2);

            var statusCode1 = task1.Result.StatusCode;
            var statusCode2 = task2.Result.StatusCode;

            Assert.IsTrue(
                (statusCode1 == HttpStatusCode.OK && statusCode2 == HttpStatusCode.Conflict) ||
                (statusCode1 == HttpStatusCode.Conflict && statusCode2 == HttpStatusCode.OK) ||
                (statusCode1 == HttpStatusCode.OK && statusCode2 == HttpStatusCode.OK));

            ImageRecord imageRecord;
            while (true)
            {
                var imageRecordResult = await petImagesClient.GetImageRecordAsync(accountName, imageName);
                Assert.IsTrue(imageRecordResult.StatusCode == HttpStatusCode.OK);

                imageRecord = imageRecordResult.Resource;
                if (imageRecord.State == ImageRecordState.Created.ToString())
                {
                    break;
                }

                await Task.Delay(100);
            }

            var imageContentResult = await petImagesClient.GetImageAsync(accountName, imageName);
            Assert.IsTrue(imageContentResult.StatusCode == HttpStatusCode.OK);

            var thumbnailContentResult = await petImagesClient.GetImageThumbnailAsync(accountName, imageName);
            Assert.IsTrue(thumbnailContentResult.StatusCode == HttpStatusCode.OK);

            var image = imageContentResult.Resource;
            var thumbnail = thumbnailContentResult.Resource;

            if (statusCode1 == HttpStatusCode.OK && statusCode2 != HttpStatusCode.OK)
            {
                Assert.IsTrue(IsDogImage(image) && IsDogThumbnail(thumbnail));
            }
            else if (statusCode1 != HttpStatusCode.OK && statusCode2 == HttpStatusCode.OK)
            {
                Assert.IsTrue(IsCatImage(image) && IsCatThumbnail(thumbnail));
            }
            else
            {
                Assert.IsTrue(
                    (IsDogImage(image) && IsDogThumbnail(thumbnail)) ||
                    (IsCatImage(image) && IsCatThumbnail(thumbnail)));
            }
        }

        [TestMethod]
        public async Task TestFifthScenario()
        {
            var randomizedFaultPolicy = TestRetryPolicyFactory.GetRandomPermanentFailureAsyncPolicy();
            var petImagesClient = await InitializeSystemAsync(randomizedFaultPolicy);
            randomizedFaultPolicy.ShouldRandomlyFail = false;

            string accountName = "MyAccount";
            string imageName = "pet.jpg";
            string contentType = "image/jpeg";

            // Create an account request payload
            var account = new Account()
            {
                Name = accountName
            };

            var accountResult = await petImagesClient.CreateAccountAsync(account);
            Assert.IsTrue(accountResult.StatusCode == HttpStatusCode.OK);

            randomizedFaultPolicy.ShouldRandomlyFail = true;

            try
            {
                var imageRecordResult = await petImagesClient.CreateOrUpdateImageAsync(
                    accountName,
                    new ImageRecord() { Name = imageName, ContentType = contentType, Content = GetDogImageBytes() });
                Assert.IsTrue(imageRecordResult.StatusCode == HttpStatusCode.OK);
            }
            catch (SimulatedRandomFaultException)
            {
            }

            randomizedFaultPolicy.ShouldRandomlyFail = false;

            ImageRecord imageRecord;
            while (true)
            {
                var imageRecordResult = await petImagesClient.GetImageRecordAsync(accountName, imageName);
                Assert.IsTrue(
                    imageRecordResult.StatusCode == HttpStatusCode.OK ||
                    imageRecordResult.StatusCode == HttpStatusCode.NotFound);

                if (imageRecordResult.StatusCode == HttpStatusCode.NotFound)
                {
                    break;
                }

                imageRecord = imageRecordResult.Resource;
                if (imageRecord.State == ImageRecordState.Created.ToString())
                {
                    break;
                }

                await Task.Delay(100);
            }
        }

        [TestMethod]
        public void SystematicTestFirstScenario()
        {
            RunSystematicTest(TestFirstScenario);
        }

        [TestMethod]
        public void SystematicTestFirstScenarioAlternative()
        {
            RunSystematicTest(TestFirstScenarioAlternative);
        }

        [TestMethod]
        public void SystematicTestSecondScenarioUpdate()
        {
            RunSystematicTest(TestSecondScenarioUpdate);
        }

        [TestMethod]
        public void SystematicTestSecondScenarioCreate()
        {
            RunSystematicTest(TestSecondScenarioCreate);
        }

        [TestMethod]
        public void SystematicTestThirdScenario()
        {
            RunSystematicTest(TestThirdScenario);
        }

        [TestMethod]
        public void SystematicTestFourthScenario()
        {
            RunSystematicTest(TestFourthScenario);
        }

        [TestMethod]
        public void SystematicTestFifthScenario()
        {
            RunSystematicTest(TestFifthScenario);
        }

        private static async Task<IPetImagesClient> InitializeSystemAsync(IAsyncPolicy asyncPolicy = null)
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

            var petImagesClient = new TestPetImagesClient(
                accountContainer,
                imageContainer,
                storageAccount,
                messagingClient);

            return petImagesClient;
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
                .WithTestingIterations(10);

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
