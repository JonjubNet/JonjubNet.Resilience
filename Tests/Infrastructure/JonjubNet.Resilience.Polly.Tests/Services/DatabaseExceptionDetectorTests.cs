using FluentAssertions;
using JonjubNet.Resilience.Core.Interfaces;
using JonjubNet.Resilience.Polly.Services;
using System;
using System.Threading.Tasks;
using Xunit;

namespace JonjubNet.Resilience.Polly.Tests.Services
{
    public class DatabaseExceptionDetectorTests
    {
        private readonly IDatabaseExceptionDetector _detector;

        public DatabaseExceptionDetectorTests()
        {
            _detector = new DatabaseExceptionDetector();
        }

        [Fact]
        public void IsTransient_WithNullException_ShouldReturnFalse()
        {
            // Arrange & Act
            var result = _detector.IsTransient(null!);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsTransient_WithTimeoutException_ShouldReturnTrue()
        {
            // Arrange
            var exception = new TimeoutException("Operation timed out");

            // Act
            var result = _detector.IsTransient(exception);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsTransient_WithTaskCanceledException_ShouldReturnTrue()
        {
            // Arrange
            var exception = new TaskCanceledException("Task was canceled");

            // Act
            var result = _detector.IsTransient(exception);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsTransient_WithDbUpdateConcurrencyException_ShouldReturnFalse()
        {
            // Arrange - Create a mock exception with the same name
            var exception = CreateMockDbUpdateConcurrencyException();

            // Act
            var result = _detector.IsTransient(exception);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsConnectionException_WithNullException_ShouldReturnFalse()
        {
            // Arrange & Act
            var result = _detector.IsConnectionException(null!);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsConnectionException_WithConnectionMessage_ShouldReturnTrue()
        {
            // Arrange
            var exception = new Exception("Unable to connect to database server");

            // Act
            var result = _detector.IsConnectionException(exception);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsConnectionException_WithNetworkMessage_ShouldReturnTrue()
        {
            // Arrange
            var exception = new Exception("Network error occurred");

            // Act
            var result = _detector.IsConnectionException(exception);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsConnectionException_WithTimeoutMessage_ShouldReturnTrue()
        {
            // Arrange
            var exception = new Exception("Connection timeout");

            // Act
            var result = _detector.IsConnectionException(exception);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsConnectionException_WithUnrelatedException_ShouldReturnFalse()
        {
            // Arrange
            var exception = new InvalidOperationException("Invalid operation");

            // Act
            var result = _detector.IsConnectionException(exception);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsConnectionException_WithEmptyMessage_ShouldReturnFalse()
        {
            // Arrange
            var exception = new Exception(string.Empty);

            // Act
            var result = _detector.IsConnectionException(exception);

            // Assert
            result.Should().BeFalse();
        }

        private static Exception CreateMockDbUpdateConcurrencyException()
        {
            // Create a mock exception with the same name for testing
            return new Exception("DbUpdateConcurrencyException");
        }
    }
}
