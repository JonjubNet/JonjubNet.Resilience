using FluentAssertions;
using JonjubNet.Resilience.Core.Interfaces;
using System;
using Xunit;

namespace JonjubNet.Resilience.Core.Tests.Interfaces
{
    public class IDatabaseExceptionDetectorTests
    {
        [Fact]
        public void IDatabaseExceptionDetector_ShouldBeAnInterface()
        {
            // Arrange & Act
            var type = typeof(IDatabaseExceptionDetector);

            // Assert
            type.IsInterface.Should().BeTrue();
        }

        [Fact]
        public void IDatabaseExceptionDetector_ShouldHaveIsTransientMethod()
        {
            // Arrange
            var type = typeof(IDatabaseExceptionDetector);

            // Act
            var method = type.GetMethod("IsTransient", new[] { typeof(Exception) });

            // Assert
            method.Should().NotBeNull();
            method!.ReturnType.Should().Be(typeof(bool));
        }

        [Fact]
        public void IDatabaseExceptionDetector_ShouldHaveIsConnectionExceptionMethod()
        {
            // Arrange
            var type = typeof(IDatabaseExceptionDetector);

            // Act
            var method = type.GetMethod("IsConnectionException", new[] { typeof(Exception) });

            // Assert
            method.Should().NotBeNull();
            method!.ReturnType.Should().Be(typeof(bool));
        }
    }
}
