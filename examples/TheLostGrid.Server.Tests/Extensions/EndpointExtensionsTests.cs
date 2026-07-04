using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TheLostGrid.Server.Endpoints;
using TheLostGrid.Server.Extensions;
using TheLostGrid.Server.Tests.Extensions.Fakes;

namespace TheLostGrid.Server.Tests.Extensions;

public sealed class EndpointExtensionsTests
{
    [Fact]
    public void AddModuleEndpoints_ShouldRegisterAvailableModules_WhenValidTypesExistInAssembly()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddModuleEndpoints();
        ServiceProvider provider = services.BuildServiceProvider();
        IEnumerable<IEndpointModule> registeredModules = provider.GetServices<IEndpointModule>();

        // Assert
        registeredModules
            .Should()
            .NotBeEmpty("because the assembly scanning framework must discover and harvest available concrete endpoint module implementation blocks")
            .And.Contain(m => m is FakeTestEndpointModule, "because any valid non-abstract module class present in the current application domain boundary must compile into the dependency injection container");
    }

    [Fact]
    public void MapModuleEndpoints_ShouldTriggerRoutingRegistration_ForEachDiscoveredModuleInstance()
    {
        // Arrange
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        Mock<IEndpointModule> firstModuleMock = new();
        Mock<IEndpointModule> secondModuleMock = new();

        builder.Services.AddSingleton(firstModuleMock.Object);
        builder.Services.AddSingleton(secondModuleMock.Object);

        WebApplication app = builder.Build();

        // Act
        app.MapModuleEndpoints();

        // Assert
        firstModuleMock.Verify(m => m.MapEndpoints(It.IsAny<IEndpointRouteBuilder>()), Times.Once);
        secondModuleMock.Verify(m => m.MapEndpoints(It.IsAny<IEndpointRouteBuilder>()), Times.Once);
    }
}

