using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using PixelTerminalUI.Engine.Validators.ValidationProviders;
using PixelTerminalUI.Engine.Validators;
using PixelTerminalUI.Engine.Extensions.ServiceCollectionExtensions;

namespace PixelTerminalUI.Engine.Tests.Validators.ValidationProviders;

public sealed class ScreenValidationProviderTests
{
    [Fact]
    public void GetValidatorsForScreen_WhenConfiguredViaFluentApi_ShouldReturnRegisteredDelegates()
    {
        // Arrange
        IServiceCollection services = new ServiceCollection();
        string targetScreen = "TestScreen";

        ValidationDelegate lengthValidator = (screen, input) => input.Length > 10
            ? ValidationResult.Fail("Too long")
            : ValidationResult.Success();

        // Act
        services.AddScreenValidators(options =>
        {
            options.ForScreen(targetScreen)
                   .Add(lengthValidator);
        });

        ServiceProvider serviceProvider = services.BuildServiceProvider();
        IScreenValidationProvider sut = serviceProvider.GetRequiredService<IScreenValidationProvider>();
        IEnumerable<ValidationDelegate> result = sut.GetValidatorsForScreen(targetScreen);

        // Assert
        result
            .Should()
            .NotBeNull("because the provider must always return a valid collection instance")
            .And.ContainSingle("because exactly one validation delegate was attached to the target screen configuration structure");

        result.First()
            .Should()
            .BeSameAs(lengthValidator, "because the resolved validation delegate must match the exact reference configured inside the container builder setup");
    }

    [Fact]
    public void GetValidatorsForScreen_WhenMultipleValidatorsRegistered_ShouldPreserveSequenceOrder()
    {
        // Arrange
        IServiceCollection services = new ServiceCollection();
        string targetScreen = "OrderScreen";

        ValidationDelegate firstValidator = (screen, input) => ValidationResult.Success();
        ValidationDelegate secondValidator = (screen, input) => ValidationResult.Success();

        // Act
        services.AddScreenValidators(options =>
        {
            options.ForScreen(targetScreen)
                   .Add(firstValidator)
                   .Add(secondValidator);
        });

        ServiceProvider serviceProvider = services.BuildServiceProvider();
        IScreenValidationProvider sut = serviceProvider.GetRequiredService<IScreenValidationProvider>();
        List<ValidationDelegate> result = sut.GetValidatorsForScreen(targetScreen).ToList();

        // Assert
        result
            .Should()
            .HaveCount(2, "because multiple validation steps can be chained onto a single terminal interface layout");

        result[0]
            .Should()
            .BeSameAs(firstValidator, "because the validation execution engine relies on the strict registration sequence order array");

        result[1]
            .Should()
            .BeSameAs(secondValidator, "because downstream composite validation pipelines evaluate rules linearly");
    }

    [Fact]
    public void GetValidatorsForScreen_WhenScreenDoesNotExist_ShouldReturnEmptyCollection()
    {
        // Arrange
        IServiceCollection services = new ServiceCollection();

        services.AddScreenValidators(options =>
        {
            options.ForScreen("ExistingScreen").Add((screen, input) => ValidationResult.Success());
        });

        ServiceProvider serviceProvider = services.BuildServiceProvider();
        IScreenValidationProvider sut = serviceProvider.GetRequiredService<IScreenValidationProvider>();

        // Act
        IEnumerable<ValidationDelegate> result = sut.GetValidatorsForScreen("NonExistentScreen");

        // Assert
        result
            .Should()
            .NotBeNull("because short-circuit defensive programming rules out null collection references to dodge null reference crashes")
            .And.BeEmpty("because no functional verification rules were mapped to this requested view form component identity name");
    }

    [Fact]
    public void GetValidatorsForScreen_WhenScreenNameIsInvalid_ShouldReturnEmptyCollection()
    {
        // Arrange
        IServiceCollection services = new ServiceCollection();
        services.AddScreenValidators(options => { });

        ServiceProvider serviceProvider = services.BuildServiceProvider();
        IScreenValidationProvider sut = serviceProvider.GetRequiredService<IScreenValidationProvider>();

        // Act
        IEnumerable<ValidationDelegate> nullResult = sut.GetValidatorsForScreen(null!);
        IEnumerable<ValidationDelegate> emptyResult = sut.GetValidatorsForScreen(string.Empty);

        // Assert
        nullResult
            .Should()
            .NotBeNull("because invalid telemetry entries must still bypass standard internal lookup checks cleanly")
            .And.BeEmpty("because a null boundary parameter string maps onto zero configured layout records");

        emptyResult
            .Should()
            .NotBeNull("because white-space string requests require clean execution recovery boundaries inside data providers")
            .And.BeEmpty("because empty layout identifiers yield no matches inside the internal index map directory");
    }
}
