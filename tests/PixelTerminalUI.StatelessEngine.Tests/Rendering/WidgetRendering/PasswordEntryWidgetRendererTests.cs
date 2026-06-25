using FluentAssertions;
using PixelTerminalUI.Contracts.Common;
using PixelTerminalUI.StatelessEngine.Rendering.WidgetRendering;
using PixelTerminalUI.StatelessEngine.Widgets;

namespace PixelTerminalUI.StatelessEngine.Tests.Rendering.WidgetRendering;

public sealed class PasswordEntryWidgetRendererTests : BaseWidgetRendererTests
{
    [Fact]
    public void SupportedWidgetType_WhenQueried_ShouldReturnPasswordEntryWidgetType()
    {
        // Arrange
        PasswordEntryWidgetRenderer renderer = new();

        // Act
        Type supportedType = renderer.SupportedWidgetType;

        // Assert
        supportedType
            .Should()
            .Be(typeof(PasswordEntryWidget), "because this renderer is strictly dedicated to handling secure polymorphic password input fields");
    }

    [Fact]
    public void Draw_WithValidValueAndNoFocus_ShouldMaskCharactersWithDefaultAsterisk()
    {
        // Arrange
        PasswordEntryWidgetRenderer renderer = new();
        int width = 8;
        int height = 3;
        int totalCellsCount = width * height;
        Pixel[] inputBuffer = new Pixel[totalCellsCount];

        PasswordEntryWidget pwdWidget = new()
        {
            Id = Guid.NewGuid(),
            Name = "PinInput",
            Left = 1,
            Top = 1,
            Width = 6,
            Height = 1,
            Value = "1234",
            Visible = true,
            EmptyEnterSymbol = '.',
            MaskChar = '*'
        };

        // Act
        renderer.Draw(inputBuffer, pwdWidget, Guid.NewGuid(), width, height);
        string actualVisualSnapshot = ConvertFlatBufferToVisualString(inputBuffer, width, height);

        // Expected output grid (8x3) where the value "1234" is converted into 4 asterisks ****
        string expectedVisualSnapshot =
            "        " + Environment.NewLine +
            " ****   " + Environment.NewLine +
            "        ";

        // Assert
        actualVisualSnapshot
            .Should()
            .Be(expectedVisualSnapshot, "because actual secret user input characters must always be masked with asterisks when the field loses focus");
    }

    [Fact]
    public void Draw_WithCustomMaskCharAndActiveFocus_ShouldMaskAndFillRemainingWithEmptyEnterSymbol()
    {
        // Arrange
        PasswordEntryWidgetRenderer renderer = new();
        int width = 8;
        int height = 3;
        int totalCellsCount = width * height;
        Pixel[] inputBuffer = new Pixel[totalCellsCount];
        Guid focusedWidgetId = Guid.NewGuid();

        PasswordEntryWidget pwdWidget = new()
        {
            Id = focusedWidgetId,
            Name = "TokenInput",
            Left = 0,
            Top = 0,
            Width = 7,
            Height = 1,
            Value = "AB",
            Visible = true,
            EmptyEnterSymbol = '#',
            MaskChar = 'X'
        };

        // Act
        renderer.Draw(inputBuffer, pwdWidget, focusedWidgetId, width, height);
        string actualVisualSnapshot = ConvertFlatBufferToVisualString(inputBuffer, width, height);

        // Expected output grid (8x3):
        // Left=0, value "AB" masked as "XX", remaining up to Width=7 filled with active focus symbol '#'
        string expectedVisualSnapshot =
            "XX##### " + Environment.NewLine +
            "        " + Environment.NewLine +
            "        ";

        // Assert
        actualVisualSnapshot
            .Should()
            .Be(expectedVisualSnapshot, "because custom masking characters override defaults and trailing active anchors fill empty spots layout configurations");
    }

    [Fact]
    public void Draw_WhenValueIsEmptyButHintExistsAndWidgetIsNotActive_ShouldNotRenderHint()
    {
        // Arrange
        PasswordEntryWidgetRenderer renderer = new();
        int width = 8;
        int height = 3;
        int totalCellsCount = width * height;
        Pixel[] inputBuffer = new Pixel[totalCellsCount];

        PasswordEntryWidget pwdWidget = new()
        {
            Id = Guid.NewGuid(),
            Name = "SecretInput",
            Left = 0,
            Top = 0,
            Width = 8,
            Height = 1,
            Value = string.Empty,
            Hint = "PASS",
            Visible = true,
            EmptyEnterSymbol = '.'
        };

        // Act
        renderer.Draw(inputBuffer, pwdWidget, Guid.NewGuid(), width, height);
        string actualVisualSnapshot = ConvertFlatBufferToVisualString(inputBuffer, width, height);

        string expectedVisualSnapshot =
            "        " + Environment.NewLine +
            "        " + Environment.NewLine +
            "        ";

        // Assert
        actualVisualSnapshot
            .Should()
            .Be(expectedVisualSnapshot, "because password fields are responsible only for masking numerical or text characters and should never dump metadata hints internally");
    }

    [Fact]
    public void Draw_WhenValueIsEmptyButHintExistsAndWidgetIsActive_ShouldNotRenderHint()
    {
        // Arrange
        PasswordEntryWidgetRenderer renderer = new();
        int width = 8;
        int height = 3;
        int totalCellsCount = width * height;
        Pixel[] inputBuffer = new Pixel[totalCellsCount];

        PasswordEntryWidget pwdWidget = new()
        {
            Id = Guid.NewGuid(),
            Name = "SecretInput",
            Left = 0,
            Top = 0,
            Width = 8,
            Height = 1,
            Value = string.Empty,
            Hint = "PASS",
            Visible = true,
            EmptyEnterSymbol = '.'
        };

        // Act
        renderer.Draw(inputBuffer, pwdWidget, pwdWidget.Id, width, height);
        string actualVisualSnapshot = ConvertFlatBufferToVisualString(inputBuffer, width, height);

        string expectedVisualSnapshot =
            "........" + Environment.NewLine +
            "        " + Environment.NewLine +
            "        ";

        // Assert
        actualVisualSnapshot
            .Should()
            .Be(expectedVisualSnapshot, "because an active empty password widget must project padding enter anchors without exposing structural string hints");
    }

    [Fact]
    public void Draw_WhenTextExceedsRightScreenBorder_ShouldClipSafelyAtScreenBoundaries()
    {
        // Arrange
        PasswordEntryWidgetRenderer renderer = new();
        int width = 8;
        int height = 3;
        int totalCellsCount = width * height;
        Pixel[] inputBuffer = new Pixel[totalCellsCount];

        PasswordEntryWidget pwdWidget = new()
        {
            Id = Guid.NewGuid(),
            Name = "LongPasswordInput",
            Left = 6,
            Top = 0,
            Width = 4,
            Height = 1,
            Value = "SECRET",
            Visible = true,
            MaskChar = '*'
        };

        // Act
        renderer.Draw(inputBuffer, pwdWidget, Guid.NewGuid(), width, height);
        string actualVisualSnapshot = ConvertFlatBufferToVisualString(inputBuffer, width, height);

        // Starts at Left=6, fits 2 masked chars into an 8-width screen buffer array
        string expectedVisualSnapshot =
            "      **" + Environment.NewLine +
            "        " + Environment.NewLine +
            "        ";

        // Assert
        actualVisualSnapshot
            .Should()
            .Be(expectedVisualSnapshot, "because the matrix rendering logic must clip trailing masked symbols to prevent array dimensions index out of bounds exceptions");
    }

    [Theory]
    [InlineData(-2, 0)]
    [InlineData(15, 0)]
    [InlineData(0, -2)]
    [InlineData(0, 10)]
    public void Draw_WhenCoordinatesAreOutOfBounds_ShouldHandleSafelyWithoutThrowingExceptions(int left, int top)
    {
        // Arrange
        PasswordEntryWidgetRenderer renderer = new();
        int width = 8;
        int height = 3;
        int totalCellsCount = width * height;
        Pixel[] inputBuffer = new Pixel[totalCellsCount];

        PasswordEntryWidget pwdWidget = new()
        {
            Id = Guid.NewGuid(),
            Name = "FaultyPasswordCoordinates",
            Left = left,
            Top = top,
            Width = 3,
            Height = 1,
            Value = "123",
            Visible = true
        };

        // Act
        Action act = () => renderer.Draw(inputBuffer, pwdWidget, Guid.NewGuid(), width, height);

        // Assert
        act
            .Should()
            .NotThrow("because enterprise rendering components must isolate coordinate calculation overflows from interrupting thread executions");
    }
}
