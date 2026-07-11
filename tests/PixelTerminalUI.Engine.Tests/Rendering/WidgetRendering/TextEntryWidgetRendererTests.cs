using FluentAssertions;
using PixelTerminalUI.Contracts.Common;
using PixelTerminalUI.Engine.Rendering.WidgetRendering;
using PixelTerminalUI.Engine.Widgets;

namespace PixelTerminalUI.Engine.Tests.Rendering.WidgetRendering;

public sealed class TextEntryWidgetRendererTests : BaseWidgetRendererTests
{
    [Fact]
    public void SupportedWidgetType_WhenQueried_ShouldReturnTextEntryWidgetType()
    {
        // Arrange
        TextEntryWidgetRenderer renderer = new();

        // Act
        Type supportedType = renderer.SupportedWidgetType;

        // Assert
        supportedType
            .Should()
            .Be(typeof(TextEntryWidget), "because this renderer is specifically dedicated to handling interactive terminal input text fields");
    }

    [Fact]
    public void Draw_WithValidValueAndNoFocus_ShouldRenderTextAndFillRemainingWithSpaces()
    {
        // Arrange
        TextEntryWidgetRenderer renderer = new();
        int width = 8;
        int height = 3;
        int totalCellsCount = width * height;
        Pixel[] inputBuffer = new Pixel[totalCellsCount];

        TextEntryWidget entryWidget = new()
        {
            Id = Guid.NewGuid(),
            Name = "QtyInput",
            Left = 1,
            Top = 1,
            Width = 5,
            Height = 1,
            Value = "12",
            Visible = true,
            EmptyEnterSymbol = '.'
        };

        // Act
        renderer.Draw(inputBuffer, entryWidget, Guid.NewGuid(), width, height);
        string actualVisualSnapshot = ConvertFlatBufferToVisualString(inputBuffer, width, height);

        // Expected output grid (8x3):
        // Line 0: "        "
        // Line 1: " 12     " (Starts at Left=1, value is "12", remaining width of 5 is filled with spaces ' ')
        // Line 2: "        "
        string expectedVisualSnapshot =
            "        " + Environment.NewLine +
            " 12     " + Environment.NewLine +
            "        ";

        // Assert
        actualVisualSnapshot
            .Should()
            .Be(expectedVisualSnapshot, "because when an input field is not focused, its trailing padding inside the Width boundary must be standard empty spaces");
    }

    [Fact]
    public void Draw_WithValidValueAndActiveFocus_ShouldRenderTextAndFillRemainingWithEmptyEnterSymbol()
    {
        // Arrange
        TextEntryWidgetRenderer renderer = new();
        int width = 8;
        int height = 3;
        int totalCellsCount = width * height;
        Pixel[] inputBuffer = new Pixel[totalCellsCount];
        Guid focusedWidgetId = Guid.NewGuid();

        TextEntryWidget entryWidget = new()
        {
            Id = focusedWidgetId,
            Name = "SkuInput",
            Left = 1,
            Top = 1,
            Width = 6,
            Height = 1,
            Value = "ABC",
            Visible = true,
            EmptyEnterSymbol = '.'
        };

        // Act
        renderer.Draw(inputBuffer, entryWidget, focusedWidgetId, width, height);
        string actualVisualSnapshot = ConvertFlatBufferToVisualString(inputBuffer, width, height);

        // Expected output grid (8x3):
        // Line 0: "        "
        // Line 1: " ABC... " (Left=1, value is "ABC", remaining 3 characters of the total Width=6 are filled with dots '.')
        // Line 2: "        "
        string expectedVisualSnapshot =
            "        " + Environment.NewLine +
            " ABC... " + Environment.NewLine +
            "        ";

        // Assert
        actualVisualSnapshot
            .Should()
            .Be(expectedVisualSnapshot, "because focused fields must display their designated EmptyEnterSymbol to give the user a clear visual text insertion guide layout");
    }

    [Fact]
    public void Draw_WhenValueIsEmptyButHintExistsAndWidgetIsNotFocused_ShouldNotRenderAnything()
    {
        // Arrange
        TextEntryWidgetRenderer renderer = new();
        int width = 8;
        int height = 3;
        int totalCellsCount = width * height;
        Pixel[] inputBuffer = new Pixel[totalCellsCount];

        TextEntryWidget entryWidget = new()
        {
            Id = Guid.NewGuid(),
            Name = "PalletInput",
            Left = 0,
            Top = 0,
            Width = 7,
            Height = 1,
            Value = string.Empty,
            Hint = "SCAN",
            Visible = true,
            EmptyEnterSymbol = '-'
        };

        // Act
        renderer.Draw(inputBuffer, entryWidget, Guid.NewGuid(), width, height);
        string actualVisualSnapshot = ConvertFlatBufferToVisualString(inputBuffer, width, height);

        // Expecting the full 8x3 grid output representation
        string expectedVisualSnapshot =
            "        " + Environment.NewLine +
            "        " + Environment.NewLine +
            "        ";

        // Assert
        actualVisualSnapshot
            .Should()
            .Be(expectedVisualSnapshot, "because when the widget does not have focus and its value buffer is empty, it must not output its hint placeholder internally onto its immediate layout row");
    }

    [Fact]
    public void Draw_WhenValueIsEmptyWithHintAndHasActiveFocus_ShouldNotRenderHint()
    {
        // Arrange
        TextEntryWidgetRenderer renderer = new();
        int width = 8;
        int height = 3;
        int totalCellsCount = width * height;
        Pixel[] inputBuffer = new Pixel[totalCellsCount];
        Guid focusedWidgetId = Guid.NewGuid();

        TextEntryWidget entryWidget = new()
        {
            Id = focusedWidgetId,
            Name = "CellInput",
            Left = 0,
            Top = 0,
            Width = 7,
            Height = 1,
            Value = string.Empty,
            Hint = "LOC",
            Visible = true,
            EmptyEnterSymbol = '*'
        };

        // Act
        renderer.Draw(inputBuffer, entryWidget, focusedWidgetId, width, height);
        string actualVisualSnapshot = ConvertFlatBufferToVisualString(inputBuffer, width, height);

        // Expecting the full 8x3 grid output representation
        string expectedVisualSnapshot =
            "******* " + Environment.NewLine +
            "        " + Environment.NewLine +
            "        ";

        // Assert
        actualVisualSnapshot
            .Should()
            .Be(expectedVisualSnapshot, "because even when drawing placeholder hint text, active focus rules must still apply trailing empty enter symbols across the whole layout width");
    }

    [Fact]
    public void Draw_WhenTextExceedsRightScreenBorder_ShouldClipSafelyAtScreenBoundaries()
    {
        // Arrange
        TextEntryWidgetRenderer renderer = new();
        int width = 8;
        int height = 3;
        int totalCellsCount = width * height;
        Pixel[] inputBuffer = new Pixel[totalCellsCount];

        TextEntryWidget entryWidget = new()
        {
            Id = Guid.NewGuid(),
            Name = "LongInput",
            Left = 5,
            Top = 0,
            Width = 5,
            Height = 1,
            Value = "12345",
            Visible = true
        };

        // Act
        renderer.Draw(inputBuffer, entryWidget, Guid.NewGuid(), width, height);
        string actualVisualSnapshot = ConvertFlatBufferToVisualString(inputBuffer, width, height);

        // Expecting the full 8x3 grid output representation. 
        // Left=5, buffer width is 8. Characters at indexes 5, 6, 7 will be '1', '2', '3'.
        string expectedVisualSnapshot =
            "     123" + Environment.NewLine +
            "        " + Environment.NewLine +
            "        ";

        // Assert
        actualVisualSnapshot
            .Should()
            .Be(expectedVisualSnapshot, "the pipeline must truncate characters that spill outside the physical width dimensions of the underlying viewport matrix array");
    }

    [Theory]
    [InlineData(-1, 0)]  // Negative Left
    [InlineData(12, 0)]  // Out of bounds Right
    [InlineData(0, -1)]  // Negative Top
    [InlineData(0, 5)]   // Out of bounds Bottom
    public void Draw_WhenCoordinatesAreOutOfBounds_ShouldHandleSafelyWithoutThrowingExceptions(int left, int top)
    {
        // Arrange
        TextEntryWidgetRenderer renderer = new();
        int width = 8;
        int height = 3;
        int totalCellsCount = width * height;
        Pixel[] inputBuffer = new Pixel[totalCellsCount];

        TextEntryWidget entryWidget = new()
        {
            Id = Guid.NewGuid(),
            Name = "FaultyCoordinatesInput",
            Left = left,
            Top = top,
            Width = 3,
            Height = 1,
            Value = "ABC",
            Visible = true
        };

        // Act
        Action act = () => renderer.Draw(inputBuffer, entryWidget, Guid.NewGuid(), width, height);

        // Assert
        act
            .Should()
            .NotThrow("because industrial TUI rendering components must aggressively wrap index operations to insulate application threads from runtime boundary indexing crashes");
    }
}
