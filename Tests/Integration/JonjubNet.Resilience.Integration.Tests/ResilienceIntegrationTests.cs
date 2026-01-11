using FluentAssertions;
using JonjubNet.Resilience;
using JonjubNet.Resilience.Core.Configuration;
using JonjubNet.Resilience.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace JonjubNet.Resilience.Integration.Tests
{
    public class ResilienceIntegrationTests
    {
        [Fact]
        public void AddResilienceInfrastructure_ShouldRegisterServices()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "Resilience:Enabled", "true" },
                    { "Resilience:Retry:Enabled", "true" },
                    { "Resilience:Retry:MaxRetryAttempts", "3" },
                    { "Resilience:CircuitBreaker:Enabled", "true" },
                    { "Resilience:Timeout:Enabled", "true" }
                })
                .Build();

            // Act
            services.AddResilienceInfrastructure(configuration);
            var serviceProvider = services.BuildServiceProvider();

            // Assert
            var resilienceService = serviceProvider.GetService<IResilienceService>();
            resilienceService.Should().NotBeNull();
        }

        [Fact]
        public async Task ResilienceService_WithConfiguration_ShouldWorkEndToEnd()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "Resilience:Enabled", "true" },
                    { "Resilience:Retry:Enabled", "true" },
                    { "Resilience:Retry:MaxRetryAttempts", "2" },
                    { "Resilience:CircuitBreaker:Enabled", "false" },
                    { "Resilience:Timeout:Enabled", "false" }
                })
                .Build();

            var loggingServiceMock = new Mock<IStructuredLoggingService>();
            services.AddSingleton(loggingServiceMock.Object);
            services.AddLogging();
            services.AddResilienceInfrastructure(configuration);

            var serviceProvider = services.BuildServiceProvider();
            var resilienceService = serviceProvider.GetRequiredService<IResilienceService>();

            // Act
            var result = await resilienceService.ExecuteWithResilienceAsync(
                async () => await Task.FromResult("success"),
                "IntegrationTest");

            // Assert
            result.Should().Be("success");
            loggingServiceMock.Verify(
                x => x.LogInformation(
                    It.IsAny<string>(),
                    "IntegrationTest",
                    "Resilience",
                    It.IsAny<Dictionary<string, object>>()),
                Times.Once);
        }

        [Fact]
        public void AddResilienceInfrastructure_WithCustomConfiguration_ShouldApplyConfiguration()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "Resilience:Enabled", "true" }
                })
                .Build();

            // Act
            services.AddResilienceInfrastructure(configuration, options =>
            {
                options.Retry.MaxRetryAttempts = 5;
                options.CircuitBreaker.FailureThreshold = 10;
            });

            var serviceProvider = services.BuildServiceProvider();
            var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<ResilienceConfiguration>>();

            // Assert
            options.Value.Retry.MaxRetryAttempts.Should().Be(5);
            options.Value.CircuitBreaker.FailureThreshold.Should().Be(10);
        }
    }
}
