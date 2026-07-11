using FluentAssertions;
using PixelTerminalUI.Contracts.Common;
using PixelTerminalUI.Engine.Rendering.Core;
using PixelTerminalUI.Engine.Rendering.WidgetRendering;
using PixelTerminalUI.Engine.Widgets;

namespace PixelTerminalUI.Engine.Tests.Rendering.WidgetRendering;

public sealed class TextWidgetRendererTests : BaseWidgetRendererTests
{
    [Fact]
    public void SupportedWidgetType_WhenQueried_ShouldReturnCorrectTextWidgetType()
    {
        // Arrange
        TextWidgetRenderer renderer = new();

        // Act
        Type supportedType = renderer.SupportedWidgetType;

        // Assert
        supportedType
            .Should()
            .Be(typeof(TextWidget), "because this specific renderer is designed to handle fundamental base label and text blocks");
    }

    [Fact]
    public void Draw_WithValidCoordinates_ShouldRenderTextPerfect()
    {
        // Arrange
        int width = 10;
        int height = 3;
        int totalCellsCount = width * height;
        Pixel[] inputBuffer = new Pixel[totalCellsCount];

        TextWidget widget = new()
        {
            Id = Guid.NewGuid(),
            Name = "StandardLabel",
            Left = 2,
            Top = 1,
            Width = 4,
            Height = 1,
            Value = "TEST",
            Visible = true
        };

        // Act
        StatelessRenderer.DrawDefaultText(inputBuffer, widget, width, height);
        string actualVisualSnapshot = ConvertFlatBufferToVisualString(inputBuffer, width, height);

        // Expected output grid:
        // Line 0: "          "
        // Line 1: "  TEST    "
        // Line 2: "          "
        string expectedVisualSnapshot =
            "          " + Environment.NewLine +
            "  TEST    " + Environment.NewLine +
            "          ";

        // Assert
        actualVisualSnapshot
            .Should()
            .Be(expectedVisualSnapshot, "because valid bounds and short text must align seamlessly with coordinates");
    }

    [Fact]
    public void Draw_WhenInvertedIsTrue_ShouldApplyInvertedFlagToPixels()
    {
        // Arrange
        int width = 5;
        int height = 1;
        int totalCellsCount = width * height;
        Pixel[] inputBuffer = new Pixel[totalCellsCount];

        TextWidget widget = new()
        {
            Id = Guid.NewGuid(),
            Name = "InvertedLabel",
            Left = 1,
            Top = 0,
            Width = 3,
            Height = 1,
            Value = "OK",
            Visible = true,
            Inverted = true
        };

        // Act
        StatelessRenderer.DrawDefaultText(inputBuffer, widget, width, height);

        // Assert
        inputBuffer[0 * width + 0].IsInverted
            .Should()
            .BeFalse("because index 0 is empty padding space");

        inputBuffer[0 * width + 1].Symbol
            .Should()
            .Be('O');

        inputBuffer[0 * width + 1].IsInverted
            .Should()
            .BeTrue("because the widget layout enforces inverted pixel rendering for contrast selection");

        inputBuffer[0 * width + 2].Symbol
            .Should()
            .Be('K');

        inputBuffer[0 * width + 2].IsInverted
            .Should()
            .BeTrue("because every single character block within inverted widget must inherit inversion state");
    }

    [Fact]
    public void Draw_WhenValueIsNullOrEmpty_ShouldReturnImmediatelyWithoutModifyingBuffer()
    {
        // Arrange
        int width = 5;
        int height = 1;
        int totalCellsCount = width * height;
        Pixel[] inputBuffer = new Pixel[totalCellsCount];
        inputBuffer[0 * width + 2] = new Pixel('X', false);

        TextWidget widget = new()
        {
            Id = Guid.NewGuid(),
            Name = "EmptyLabel",
            Left = 2,
            Top = 0,
            Width = 1,
            Height = 1,
            Value = string.Empty,
            Visible = true
        };

        // Act
        StatelessRenderer.DrawDefaultText(inputBuffer, widget, width, height);

        // Assert
        inputBuffer[0 * width + 2].Symbol
            .Should()
            .Be('X', "because empty or null widget values must short-circuit execution without wiping pre-existing cell payloads");
    }

    [Fact]
    public void Draw_WhenTextExceedsWidgetWidth_ShouldClipTextToFitWidgetWidth()
    {
        // Arrange
        int width = 10;
        int height = 1;
        int totalCellsCount = width * height;
        Pixel[] inputBuffer = new Pixel[totalCellsCount];

        TextWidget widget = new()
        {
            Id = Guid.NewGuid(),
            Name = "OverflowLabel",
            Left = 1,
            Top = 0,
            Width = 3,
            Height = 1,
            Value = "LONGTEXTVALUE",
            Visible = true
        };

        // Act
        StatelessRenderer.DrawDefaultText(inputBuffer, widget, width, height);
        string actualVisualSnapshot = ConvertFlatBufferToVisualString(inputBuffer, width, height);

        // Assert
        actualVisualSnapshot
            .Should()
            .Be(" LON      ", "because text layout must be rigidly clipped to fit inside specified widget structural Width limits");
    }

    [Fact]
    public void Draw_WhenTextExceedsRightScreenBorder_ShouldClipSafelyAtScreenBoundaries()
    {
        // Arrange
        int width = 6;
        int height = 1;
        int totalCellsCount = width * height;
        Pixel[] inputBuffer = new Pixel[totalCellsCount];

        TextWidget widget = new()
        {
            Id = Guid.NewGuid(),
            Name = "ScreenEdgeLabel",
            Left = 4,
            Top = 0,
            Width = 5,
            Height = 1,
            Value = "ABCDE",
            Visible = true
        };

        // Act
        StatelessRenderer.DrawDefaultText(inputBuffer, widget, width, height);
        string actualVisualSnapshot = ConvertFlatBufferToVisualString(inputBuffer, width, height);

        // Assert
        actualVisualSnapshot
            .Should()
            .Be("    AB", "because rendering pipelines must truncate pixels that project past the physical display dimensions limit");
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(10, 0)]
    [InlineData(0, -1)]
    [InlineData(0, 3)]
    public void Draw_WhenWidgetIsPositionedCompletelyOutOfBounds_ShouldHandleGracefullyAndNotThrow(int left, int top)
    {
        // Arrange
        int width = 10;
        int height = 3;
        int totalCellsCount = width * height;
        Pixel[] inputBuffer = new Pixel[totalCellsCount];

        TextWidget widget = new()
        {
            Id = Guid.NewGuid(),
            Name = "OutOfBoundsLabel",
            Left = left,
            Top = top,
            Width = 2,
            Height = 1,
            Value = "HI",
            Visible = true
        };

        // Act
        Action act = () => StatelessRenderer.DrawDefaultText(inputBuffer, widget, width, height);

        // Assert
        act
            .Should()
            .NotThrow("because terminal engines must insulate against invalid external boundary coordinate integer values without crashing services");
    }
}
