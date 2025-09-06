using DiscordBot.Services;
using Moq;
using Newtonsoft.Json;
using RestSharp;
using System.Net;
using ParameterType = RestSharp.ParameterType;

namespace DiscordBotTests.ServiceTests
{
    [TestFixture]
    public class HttpServiceTests
    {
        private Mock<IRestClient> _mockRestClient = null!;
        private HttpService _httpService = null!;

        [SetUp]
        public void SetUp()
        {
            _mockRestClient = new Mock<IRestClient>();
            _httpService = new HttpService(_mockRestClient.Object);
        }

        [Test]
        public async Task GetResponseFromUrlWithValidResponseReturnsSuccess()
        {
            // Arrange
            const string resource = "https://api.example.com/test";
            const string content = "response content";
            var response = new RestResponse
            {
                StatusCode = HttpStatusCode.OK,
                Content = content,
                IsSuccessStatusCode = true
            };
            _mockRestClient
                .Setup(client => client.ExecuteAsync(It.IsAny<RestRequest>(), default))
                .ReturnsAsync(response);

            // Act
            var result = await _httpService.GetResponseFromUrl(resource);

            // Assert
            Assert.That(result.IsSuccessStatusCode, Is.True);
            Assert.That(result.Content, Is.EqualTo(content));
        }

        [Test]
        public async Task GetResponseFromUrlWithErrorResponseReturnsError()
        {
            // Arrange
            const string resource = "https://api.example.com/test";
            const string errorMessage = "Error message";
            var response = new RestResponse
            {
                StatusCode = HttpStatusCode.BadRequest,
                ErrorMessage = errorMessage
            };
            _mockRestClient
                .Setup(client => client.ExecuteAsync(It.IsAny<RestRequest>(), default))
                .ReturnsAsync(response);

            // Act
            var result = await _httpService.GetResponseFromUrl(resource, errorMessage: errorMessage);

            // Assert
            Assert.That(result.IsSuccessStatusCode, Is.False);
            var expectedError = $"StatusCode: {(int)response.StatusCode} ({response.StatusCode}) | {errorMessage}";
            Assert.That(result.Content, Is.EqualTo(expectedError));
        }

        [Test]
        public async Task GetResponseFromUrlWithHeadersSendsHeaders()
        {
            // Arrange
            const string resource = "https://api.example.com/test";
            var headers = new List<KeyValuePair<string, string>>
            {
                new("Header1", "Value1"),
                new("Header2", "Value2")
            };
            var response = new RestResponse
            {
                StatusCode = HttpStatusCode.OK,
                Content = "OK",
                IsSuccessStatusCode = true
            };
            _mockRestClient
                .Setup(client => client.ExecuteAsync(It.IsAny<RestRequest>(), default))
                .Callback<RestRequest, CancellationToken>((req, _) =>
                {
                    foreach (var header in headers)
                    {
                        var actualValue = req.Parameters.FirstOrDefault(p => p.Name == header.Key)?.Value?.ToString();
                        Assert.That(actualValue, Is.EqualTo(header.Value));
                    }
                })
                .ReturnsAsync(response);

            // Act
            var result = await _httpService.GetResponseFromUrl(resource, headers: headers);

            // Assert
            Assert.That(result.IsSuccessStatusCode, Is.True);
            Assert.That(result.Content, Is.EqualTo(response.Content));
        }

        [Test]
        public async Task GetResponseFromUrlWithJsonBodySendsJsonBody()
        {
            // Arrange
            const string resource = "https://api.example.com/test";
            var jsonObj = new { key = "value" };
            var response = new RestResponse
            {
                StatusCode = HttpStatusCode.OK,
                IsSuccessStatusCode = true
            };
            RestRequest? capturedRequest = null;
            _mockRestClient
                .Setup(client => client.ExecuteAsync(It.IsAny<RestRequest>(), It.IsAny<CancellationToken>()))
                .Callback<RestRequest, CancellationToken>((req, _) => capturedRequest = req)
                .ReturnsAsync(response);

            // Act
            await _httpService.GetResponseFromUrl(resource, jsonBody: jsonObj);

            // Assert
            Assert.That(capturedRequest, Is.Not.Null);

            var bodyParameter = capturedRequest?.Parameters.FirstOrDefault(
                p => p.Type == ParameterType.RequestBody
            );

            Assert.That(bodyParameter, Is.Not.Null);
            Assert.That(bodyParameter?.Type, Is.EqualTo(ParameterType.RequestBody));
            Assert.That(bodyParameter?.Value, Is.Not.Null);
        }

    }
}
