using FluentAssertions;
using JonjubNet.Resilience.Core.Configuration;
using JonjubNet.Resilience.Core.Interfaces;
using JonjubNet.Resilience.Polly.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace JonjubNet.Resilience.Polly.Tests.Services
{
    public class ResilienceServiceTests
    {
        private readonly Mock<ILogger<ResilienceService>> _loggerMock;
        private readonly Mock<IStructuredLoggingService> _loggingServiceMock;
        private readonly Mock<IDatabaseExceptionDetector> _exceptionDetectorMock;
        private readonly ResilienceConfiguration _configuration;
        private readonly ResilienceService _service;

        public ResilienceServiceTests()
        {
            _loggerMock = new Mock<ILogger<ResilienceService>>();
            _loggingServiceMock = new Mock<IStructuredLoggingService>();
            _exceptionDetectorMock = new Mock<IDatabaseExceptionDetector>();

            _configuration = new ResilienceConfiguration
            {
                Enabled = true,
                Retry = new RetryConfiguration { Enabled = true, MaxRetryAttempts = 3 },
                CircuitBreaker = new CircuitBreakerConfiguration { Enabled = true },
                Timeout = new TimeoutConfiguration { Enabled = true }
            };

            var options = Options.Create(_configuration);
            _service = new ResilienceService(
                _loggerMock.Object,
                options,
                _loggingServiceMock.Object,
                _exceptionDetectorMock.Object);
        }

        [Fact]
        public async Task ExecuteWithResilienceAsync_WhenDisabled_ShouldExecuteDirectly()
        {
            // Arrange
            _configuration.Enabled = false;
            var options = Options.Create(_configuration);
            var service = new ResilienceService(
                _loggerMock.Object,
                options,
                _loggingServiceMock.Object,
                _exceptionDetectorMock.Object);

            var operationExecuted = false;

            // Act
            await service.ExecuteWithResilienceAsync(
                async () =>
                {
                    operationExecuted = true;
                    return await Task.FromResult("result");
                },
                "TestOperation");

            // Assert
            operationExecuted.Should().BeTrue();
            _loggingServiceMock.Verify(
                x => x.LogInformation(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, object>>()),
                Times.Never);
        }

        [Fact]
        public async Task ExecuteWithResilienceAsync_WhenEnabled_ShouldExecuteWithResilience()
        {
            // Arrange
            var operationExecuted = false;

            // Act
            var result = await _service.ExecuteWithResilienceAsync(
                async () =>
                {
                    operationExecuted = true;
                    return await Task.FromResult("result");
                },
                "TestOperation");

            // Assert
            operationExecuted.Should().BeTrue();
            result.Should().Be("result");
            _loggingServiceMock.Verify(
                x => x.LogInformation(
                    It.Is<string>(s => s.Contains("Executing operation")),
                    "TestOperation",
                    "Resilience",
                    It.IsAny<Dictionary<string, object>>()),
                Times.Once);
        }

        [Fact]
        public async Task ExecuteWithResilienceAsync_WhenOperationFails_ShouldLogError()
        {
            // Arrange
            var exception = new InvalidOperationException("Test exception");

            // Act
            Func<Task> act = async () => await _service.ExecuteWithResilienceAsync<string>(
                async () =>
                {
                    await Task.CompletedTask;
                    throw exception;
                },
                "TestOperation");

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>();
            _loggingServiceMock.Verify(
                x => x.LogError(
                    It.Is<string>(s => s.Contains("failed after applying resilience patterns")),
                    "TestOperation",
                    "Resilience",
                    null,
                    It.IsAny<Dictionary<string, object>>(),
                    exception),
                Times.Once);
        }

        [Fact]
        public async Task ExecuteHttpWithResilienceAsync_ShouldCallExecuteWithResilienceAsync()
        {
            // Arrange
            var httpResponse = new HttpResponseMessage(System.Net.HttpStatusCode.OK);

            // Act
            var result = await _service.ExecuteHttpWithResilienceAsync(
                async () => await Task.FromResult<HttpResponseMessage>(httpResponse),
                "HttpOperation");

            // Assert
            result.Should().Be(httpResponse);
        }

        [Fact]
        public async Task ExecuteDatabaseWithResilienceAsync_ShouldUseDatabasePipeline()
        {
            // Arrange
            var operationExecuted = false;

            // Act
            var result = await _service.ExecuteDatabaseWithResilienceAsync(
                async () =>
                {
                    operationExecuted = true;
                    return await Task.FromResult("db-result");
                },
                "DatabaseOperation");

            // Assert
            operationExecuted.Should().BeTrue();
            result.Should().Be("db-result");
        }

        [Fact]
        public async Task ExecuteWithFallbackAsync_WhenPrimarySucceeds_ShouldReturnPrimaryResult()
        {
            // Arrange
            _configuration.Fallback = new FallbackConfiguration { Enabled = true };
            var options = Options.Create(_configuration);
            var service = new ResilienceService(
                _loggerMock.Object,
                options,
                _loggingServiceMock.Object,
                _exceptionDetectorMock.Object);

            var fallbackExecuted = false;

            // Act
            var result = await service.ExecuteWithFallbackAsync(
                async () => await Task.FromResult("primary"),
                async () =>
                {
                    fallbackExecuted = true;
                    return await Task.FromResult("fallback");
                },
                "FallbackOperation");

            // Assert
            result.Should().Be("primary");
            fallbackExecuted.Should().BeFalse();
        }

        [Fact]
        public async Task ExecuteWithFallbackAsync_WhenPrimaryFails_ShouldReturnFallbackResult()
        {
            // Arrange
            _configuration.Fallback = new FallbackConfiguration { Enabled = true };
            var options = Options.Create(_configuration);
            var service = new ResilienceService(
                _loggerMock.Object,
                options,
                _loggingServiceMock.Object,
                _exceptionDetectorMock.Object);

            // Act
            var result = await service.ExecuteWithFallbackAsync(
                async () =>
                {
                    await Task.CompletedTask;
                    throw new InvalidOperationException("Primary failed");
                },
                async () => await Task.FromResult("fallback"),
                "FallbackOperation");

            // Assert
            result.Should().Be("fallback");
            _loggingServiceMock.Verify(
                x => x.LogWarning(
                    It.Is<string>(s => s.Contains("Primary operation")),
                    "FallbackOperation",
                    "Resilience",
                    null,
                    It.IsAny<Dictionary<string, object>>(),
                    It.IsAny<Exception>()),
                Times.Once);
        }

        [Fact]
        public async Task ExecuteWithFallbackAsync_WhenFallbackDisabled_ShouldStillUseFallback()
        {
            // Arrange
            _configuration.Fallback = new FallbackConfiguration { Enabled = false };
            var options = Options.Create(_configuration);
            var service = new ResilienceService(
                _loggerMock.Object,
                options,
                _loggingServiceMock.Object,
                _exceptionDetectorMock.Object);

            // Act
            var result = await service.ExecuteWithFallbackAsync(
                async () =>
                {
                    await Task.CompletedTask;
                    throw new InvalidOperationException("Primary failed");
                },
                async () => await Task.FromResult("fallback"),
                "FallbackOperation");

            // Assert
            result.Should().Be("fallback");
        }

        [Fact]
        public void ResilienceService_ShouldInitializePipelines()
        {
            // Arrange & Act
            var service = new ResilienceService(
                _loggerMock.Object,
                Options.Create(_configuration),
                _loggingServiceMock.Object,
                _exceptionDetectorMock.Object);

            // Assert - Service should be created without exceptions
            service.Should().NotBeNull();
        }
    }
}
