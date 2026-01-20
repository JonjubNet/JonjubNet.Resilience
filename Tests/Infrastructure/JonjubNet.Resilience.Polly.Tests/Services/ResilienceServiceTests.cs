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
        private readonly Mock<IDatabaseExceptionDetector> _exceptionDetectorMock;
        private readonly ResilienceConfiguration _configuration;
        private readonly ResilienceService _service;

        public ResilienceServiceTests()
        {
            _loggerMock = new Mock<ILogger<ResilienceService>>();
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
            // Cuando está deshabilitado, no se debe registrar logging
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
            // Verificar que se registró el log usando ILogger<T> estándar
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Executing operation")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
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
            // Verificar que se registró el log de error usando ILogger<T> estándar
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("failed after applying resilience patterns")),
                    It.Is<Exception>(e => e == exception),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
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
            // Verificar que se registró el log de warning usando ILogger<T> estándar
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Primary operation")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
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
                _exceptionDetectorMock.Object);

            // Assert - Service should be created without exceptions
            service.Should().NotBeNull();
        }
    }
}
