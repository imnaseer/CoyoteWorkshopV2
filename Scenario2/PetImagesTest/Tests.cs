// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote;
using Microsoft.Coyote.SystematicTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PetImages;
using PetImages.Contracts;
using PetImagesTest.Clients;
using PetImagesTest.StorageMocks;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace PetImagesTest
{
    [TestClass]
    public class Tests
    {
        [TestMethod]
        public async Task TestFirstScenario()
        {
            // Initialize the mock in-memory DB and account manager.
            var cosmosState = new MockCosmosState();
            var database = new MockCosmosDatabase(cosmosState);
            var accountContainer = await database.CreateContainerAsync(Constants.AccountContainerName);
            var imageContainer = await database.CreateContainerAsync(Constants.ImageContainerName);
            var petImagesClient = new TestPetImagesClient(accountContainer, imageContainer);

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
        public void SystematicTestFirstScenario()
        {
            RunSystematicTest(TestFirstScenario);
        }

        [TestMethod]
        public async Task TestConcurrentImageCreates()
        {
            // Initialize the mock in-memory DB and account manager.
            var cosmosState = new MockCosmosState();
            var database = new MockCosmosDatabase(cosmosState);
            var accountContainer = await database.CreateContainerAsync(Constants.AccountContainerName);
            var imageContainer = await database.CreateContainerAsync(Constants.ImageContainerName);
            var petImagesClient = new TestPetImagesClient(accountContainer, imageContainer);

            string accountName = "MyAccount";
            string imageName = "pet.jpg";

            var createResult = await petImagesClient.CreateAccountAsync(new Account()
            {
                Name = accountName
            });
            Assert.IsTrue(createResult.StatusCode == HttpStatusCode.OK);

            var oldImage = new Image()
            {
                Name = imageName,
                Content = GetDogImageBytes(),
                LastModifiedTimestamp = DateTime.UtcNow
            };

            var newImage = new Image()
            {
                Name = imageName,
                Content = GetCatImageBytes(),
                LastModifiedTimestamp = DateTime.UtcNow.AddMinutes(10)
            };

            var createOldImageTask = petImagesClient.CreateOrUpdateImageAsync(accountName, oldImage);
            var createNewImageTask = petImagesClient.CreateOrUpdateImageAsync(accountName, newImage);

            await Task.WhenAll(createOldImageTask, createNewImageTask);

            var createOldImageResult = createOldImageTask.Result;
            var createNewImageResult = createNewImageTask.Result;

            Assert.IsTrue(
                (createOldImageResult.StatusCode == HttpStatusCode.Conflict && createNewImageResult.StatusCode == HttpStatusCode.OK) ||
                (createOldImageResult.StatusCode == HttpStatusCode.OK && createNewImageResult.StatusCode == HttpStatusCode.OK));

            var imageResult = await petImagesClient.GetImageAsync(accountName, imageName);
            Assert.IsTrue(imageResult.StatusCode == HttpStatusCode.OK);
            Assert.IsTrue(imageResult.Resource.LastModifiedTimestamp == newImage.LastModifiedTimestamp);
        }

        [TestMethod]
        public async Task TestConcurrentImageUpdates()
        {
            // Initialize the mock in-memory DB and account manager.
            var cosmosState = new MockCosmosState();
            var database = new MockCosmosDatabase(cosmosState);
            var accountContainer = await database.CreateContainerAsync(Constants.AccountContainerName);
            var imageContainer = await database.CreateContainerAsync(Constants.ImageContainerName);
            var petImagesClient = new TestPetImagesClient(accountContainer, imageContainer);

            string accountName = "MyAccount";
            string imageName = "pet.jpg";

            var createAccountResult = await petImagesClient.CreateAccountAsync(new Account()
            {
                Name = accountName
            });
            Assert.IsTrue(createAccountResult.StatusCode == HttpStatusCode.OK);

            var createImageResult = await petImagesClient.CreateOrUpdateImageAsync(accountName, new Image()
            {
                Name = imageName,
                Content = GetDogImageBytes(),
                LastModifiedTimestamp = DateTime.UtcNow
            });
            Assert.IsTrue(createImageResult.StatusCode == HttpStatusCode.OK);

            var oldImage = new Image()
            {
                Name = imageName,
                Content = GetDogImageBytes(),
                LastModifiedTimestamp = DateTime.UtcNow.AddMinutes(5)
            };

            var newImage = new Image()
            {
                Name = imageName,
                Content = GetCatImageBytes(),
                LastModifiedTimestamp = DateTime.UtcNow.AddMinutes(10)
            };

            var updateOldImageTask = petImagesClient.CreateOrUpdateImageAsync(accountName, oldImage);
            var updateNewImageTask = petImagesClient.CreateOrUpdateImageAsync(accountName, newImage);

            await Task.WhenAll(updateOldImageTask, updateNewImageTask);

            var updateOldImageResult = updateOldImageTask.Result;
            var updateNewImageResult = updateNewImageTask.Result;

            Assert.IsTrue(
                (updateOldImageResult.StatusCode == HttpStatusCode.Conflict && updateNewImageResult.StatusCode == HttpStatusCode.OK) ||
                (updateOldImageResult.StatusCode == HttpStatusCode.OK && updateNewImageResult.StatusCode == HttpStatusCode.OK));

            var imageResult = await petImagesClient.GetImageAsync(accountName, imageName);
            Assert.IsTrue(imageResult.StatusCode == HttpStatusCode.OK);
            Assert.IsTrue(imageResult.Resource.LastModifiedTimestamp == newImage.LastModifiedTimestamp);
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
            var config = Configuration.Create().WithTestingIterations(1000);

            if (reproducibleScheduleFilePath != null)
            {
                var trace = File.ReadAllText(reproducibleScheduleFilePath);
                config = config.WithReplayStrategy(trace);
            }

            var testingEngine = TestingEngine.Create(config, test);

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

        private static bool IsDogImage(byte[] imageBytes) => imageBytes.SequenceEqual(GetDogImageBytes());
        private static bool IsCatImage(byte[] imageBytes) => imageBytes.SequenceEqual(GetCatImageBytes());

        private static bool IsDogThumbnail(byte[] thumbnailBytes) => thumbnailBytes.SequenceEqual(GetDogImageBytes());
        private static bool IsCatThumbnail(byte[] thumbnailBytes) => thumbnailBytes.SequenceEqual(GetCatImageBytes());
    }
}
