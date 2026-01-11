using FluentAssertions;
using JonjubNet.Resilience.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace JonjubNet.Resilience.Core.Tests.Interfaces
{
    public class IResilienceServiceTests
    {
        [Fact]
        public void IResilienceService_ShouldBeAnInterface()
        {
            // Arrange & Act
            var type = typeof(IResilienceService);

            // Assert
            type.IsInterface.Should().BeTrue();
        }

        [Fact]
        public void IResilienceService_ShouldHaveExecuteWithResilienceAsync()
        {
            // Arrange
            var type = typeof(IResilienceService);

            // Act
            var methods = type.GetMethods();
            var method = Array.Find(methods, m => m.Name == "ExecuteWithResilienceAsync");

            // Assert
            method.Should().NotBeNull();
            method!.IsGenericMethod.Should().BeTrue();
        }

        [Fact]
        public void IResilienceService_ShouldHaveExecuteHttpWithResilienceAsync()
        {
            // Arrange
            var type = typeof(IResilienceService);

            // Act
            var method = type.GetMethod("ExecuteHttpWithResilienceAsync");

            // Assert
            method.Should().NotBeNull();
            method!.ReturnType.Should().Be(typeof(Task<HttpResponseMessage>));
        }

        [Fact]
        public void IResilienceService_ShouldHaveExecuteDatabaseWithResilienceAsync()
        {
            // Arrange
            var type = typeof(IResilienceService);

            // Act
            var methods = type.GetMethods();
            var method = Array.Find(methods, m => m.Name == "ExecuteDatabaseWithResilienceAsync");

            // Assert
            method.Should().NotBeNull();
            method!.IsGenericMethod.Should().BeTrue();
        }

        [Fact]
        public void IResilienceService_ShouldHaveExecuteWithFallbackAsync()
        {
            // Arrange
            var type = typeof(IResilienceService);

            // Act
            var methods = type.GetMethods();
            var method = Array.Find(methods, m => m.Name == "ExecuteWithFallbackAsync");

            // Assert
            method.Should().NotBeNull();
            method!.IsGenericMethod.Should().BeTrue();
        }
    }
}
