using FluentAssertions;
using JonjubNet.Resilience.Core.Configuration;
using Xunit;

namespace JonjubNet.Resilience.Core.Tests.Configuration
{
    public class ResilienceConfigurationTests
    {
        [Fact]
        public void ResilienceConfiguration_ShouldHaveDefaultValues()
        {
            // Arrange & Act
            var config = new ResilienceConfiguration();

            // Assert
            config.Enabled.Should().BeTrue();
            config.ServiceName.Should().BeEmpty();
            config.Environment.Should().BeEmpty();
            config.CircuitBreaker.Should().NotBeNull();
            config.Retry.Should().NotBeNull();
            config.Timeout.Should().NotBeNull();
            config.Bulkhead.Should().NotBeNull();
            config.Fallback.Should().NotBeNull();
            config.Services.Should().NotBeNull();
        }

        [Fact]
        public void CircuitBreakerConfiguration_ShouldHaveDefaultValues()
        {
            // Arrange & Act
            var config = new CircuitBreakerConfiguration();

            // Assert
            config.Enabled.Should().BeTrue();
            config.FailureThreshold.Should().Be(5);
            config.SamplingDurationSeconds.Should().Be(30);
            config.MinimumThroughput.Should().Be(2);
            config.DurationOfBreakSeconds.Should().Be(60);
            config.EnableAdvancedCircuitBreaker.Should().BeFalse();
            config.FailureThresholdRatio.Should().Be(0.5);
            config.MinimumThroughputForAdvanced.Should().Be(10);
        }

        [Fact]
        public void RetryConfiguration_ShouldHaveDefaultValues()
        {
            // Arrange & Act
            var config = new RetryConfiguration();

            // Assert
            config.Enabled.Should().BeTrue();
            config.MaxRetryAttempts.Should().Be(3);
            config.BaseDelayMilliseconds.Should().Be(1000);
            config.MaxDelayMilliseconds.Should().Be(30000);
            config.BackoffStrategy.Should().Be("Exponential");
            config.JitterFactor.Should().Be(0.1);
            config.RetryableStatusCodes.Should().NotBeNull();
            config.RetryableStatusCodes.Should().Contain(new[] { 408, 429, 500, 502, 503, 504 });
            config.RetryableExceptionTypes.Should().NotBeNull();
            config.RetryableExceptionTypes.Should().Contain("HttpRequestException");
        }

        [Fact]
        public void TimeoutConfiguration_ShouldHaveDefaultValues()
        {
            // Arrange & Act
            var config = new TimeoutConfiguration();

            // Assert
            config.Enabled.Should().BeTrue();
            config.DefaultTimeoutSeconds.Should().Be(30);
            config.DatabaseTimeoutSeconds.Should().Be(15);
            config.ExternalApiTimeoutSeconds.Should().Be(10);
            config.CacheTimeoutSeconds.Should().Be(5);
            config.EnableTimeoutPerOperation.Should().BeTrue();
        }

        [Fact]
        public void BulkheadConfiguration_ShouldHaveDefaultValues()
        {
            // Arrange & Act
            var config = new BulkheadConfiguration();

            // Assert
            config.Enabled.Should().BeTrue();
            config.MaxConcurrency.Should().Be(10);
            config.MaxQueuedActions.Should().Be(20);
            config.Services.Should().NotBeNull();
        }

        [Fact]
        public void FallbackConfiguration_ShouldHaveDefaultValues()
        {
            // Arrange & Act
            var config = new FallbackConfiguration();

            // Assert
            config.Enabled.Should().BeTrue();
            config.EnableCacheFallback.Should().BeTrue();
            config.EnableDefaultResponseFallback.Should().BeTrue();
            config.CacheFallbackTtlSeconds.Should().Be(300);
            config.Services.Should().NotBeNull();
        }

        [Fact]
        public void ServiceResilienceConfiguration_ShouldHaveDefaultValues()
        {
            // Arrange & Act
            var config = new ServiceResilienceConfiguration();

            // Assert
            config.Enabled.Should().BeTrue();
            config.CircuitBreaker.Should().NotBeNull();
            config.Retry.Should().NotBeNull();
            config.Timeout.Should().NotBeNull();
            config.Bulkhead.Should().NotBeNull();
            config.Fallback.Should().NotBeNull();
        }
    }
}
