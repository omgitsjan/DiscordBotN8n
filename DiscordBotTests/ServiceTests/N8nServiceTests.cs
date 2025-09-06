using DiscordBot.Interfaces;
using DiscordBot.Services;
using Microsoft.Extensions.Configuration;
using Moq;
using RestSharp;

namespace DiscordBotTests.ServiceTests
{
    [TestFixture]
    public class N8nServiceTests
    {
        private Mock<IHttpService> _mockHttpService = null!;
        private N8nService _n8nService = null!;
        private const string ValidPrompt = "How are you?";
        private const string ValidUserId = "42";

        [SetUp]
        public void Setup()
        {
            _mockHttpService = new Mock<IHttpService>();
            var configBuilder = new ConfigurationBuilder();
            configBuilder.AddInMemoryCollection(
            [
                new KeyValuePair<string, string?>("n8n:ApiKey", "testKey"),
                new KeyValuePair<string, string?>("n8n:AiAgentWorkflowUrl", "https://n8n.example.com/webhook-test/discordbot"),
            ]);
            var configuration = configBuilder.Build();
            _n8nService = new N8nService(_mockHttpService.Object, configuration);
        }

        [Test]
        public async Task AiAgentAsyncWithValidResponseReturnsSuccessAndContent()
        {
            // Arrange
            const string responseMessage = "This is the answer.";
            const string? jsonResponse = "{\"result\": \"" + responseMessage + "\"}";
            _mockHttpService.Setup(x => x.GetResponseFromUrl(It.IsAny<string>(), It.IsAny<Method>(),
                It.IsAny<string>(), It.IsAny<List<KeyValuePair<string, string>>>(), It.IsAny<object>()))
                .ReturnsAsync(new HttpResponse(true, jsonResponse));

            // Act
            var (success, content) = await _n8nService.AiAgentAsync(ValidPrompt, ValidUserId);

            // Assert
            Assert.That(success, Is.True);
            Assert.That(content, Is.EqualTo(responseMessage));
        }

        [Test]
        public async Task AiAgentAsyncWithNoApiKeyReturnsError()
        {
            // Arrange
            var configBuilder = new ConfigurationBuilder();
            configBuilder.AddInMemoryCollection(
            [
                new KeyValuePair<string, string?>("n8n:ApiKey", ""),
                new KeyValuePair<string, string?>("n8n:AiAgentWorkflowUrl", "https://n8n.example.com/webhook-test/discordbot"),
            ]);
            var configuration = configBuilder.Build();
            var service = new N8nService(_mockHttpService.Object, configuration);

            // Act
            var (success, content) = await service.AiAgentAsync(ValidPrompt, ValidUserId);

            // Assert
            Assert.That(success, Is.False);
            Assert.That(content, Is.EqualTo("No n8n URL or API key provided. Please contact the developer to add valid configuration!"));
        }

        [Test]
        public async Task AiAgentAsyncWithDeserializationErrorReturnsError()
        {
            // Arrange
            _mockHttpService.Setup(x => x.GetResponseFromUrl(It.IsAny<string>(), It.IsAny<Method>(),
                It.IsAny<string>(), It.IsAny<List<KeyValuePair<string, string>>>(), It.IsAny<object>()))
                .ReturnsAsync(new HttpResponse(true, "{\"unexpected\": \"value\"}"));

            // Act
            var (success, content) = await _n8nService.AiAgentAsync(ValidPrompt, ValidUserId);

            // Assert
            Assert.That(success, Is.True); // Because fallback to raw content!
            Assert.That(content, Is.EqualTo("{\"unexpected\": \"value\"}"));
        }

        [Test]
        public async Task AiAgentAsyncWithBackendErrorReturnsError()
        {
            // Arrange
            const string errorContent = "Internal N8n Server Error";
            _mockHttpService.Setup(x => x.GetResponseFromUrl(It.IsAny<string>(), It.IsAny<Method>(),
                It.IsAny<string>(), It.IsAny<List<KeyValuePair<string, string>>>(), It.IsAny<object>()))
                .ReturnsAsync(new HttpResponse(false, errorContent));

            // Act
            var (success, content) = await _n8nService.AiAgentAsync(ValidPrompt, ValidUserId);

            // Assert
            Assert.That(success, Is.False);
            Assert.That(content, Is.EqualTo(errorContent));
        }
    }
}
