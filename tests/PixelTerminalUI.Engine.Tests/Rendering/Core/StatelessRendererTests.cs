using FluentAssertions;
using Moq;
using PixelTerminalUI.Contracts.Common;
using PixelTerminalUI.Engine.Rendering.Registries;
using PixelTerminalUI.Engine.Rendering.Core;
using PixelTerminalUI.Engine.Widgets;
using PixelTerminalUI.Engine.Rendering.WidgetRendering;
using PixelTerminalUI.Engine.Screens;

namespace PixelTerminalUI.Engine.Tests.Rendering.Core;

public sealed class StatelessRendererTests
{
    [Fact]
    public void Draw_ShouldQueryRegistryAndInvokeMatchingWidgetRenderer_WhenValidWidgetIsPresent()
    {
        // Arrange
        Guid widgetId = Guid.NewGuid();
        TextWidget sampleWidget = new()
        {
            Id = widgetId,
            Name = "SampleLabel",
            Left = 2,
            Top = 1,
            Width = 4,
            Height = 1,
            Value = "Test",
            Visible = true
        };

        SimpleMessageScreen screen = new()
        {
            Id = Guid.NewGuid(),
            SessionId = Guid.NewGuid(),
            Name = "TestScreen",
            Width = 10,
            Height = 3,
            Visible = true,
            Widgets = [sampleWidget],
            FocusedEntryWidgetId = null
        };

        int totalCellsCount = screen.Width * screen.Height;
        Pixel[] inputBuffer = new Pixel[totalCellsCount];

        // Mock the specific widget renderer behavior using flat array coordinate logic
        Mock<IWidgetRenderer> widgetRendererMock = new();
        widgetRendererMock
            .Setup(r => r.SupportedWidgetType)
            .Returns(typeof(TextWidget));

        widgetRendererMock
            .Setup(r => r.Draw(It.IsAny<Pixel[]>(), sampleWidget, screen.FocusedEntryWidgetId, screen.Width, screen.Height))
            .Callback<Pixel[], TextWidget, Guid?, int, int>((buffer, ctrl, focusedId, w, h) =>
            {
                // Simulate drawing "Test" at Left=2, Top=1 directly in the mock flat array callback
                int rowOffset = 1 * w;
                buffer[rowOffset + 2] = new Pixel('T', false);
                buffer[rowOffset + 3] = new Pixel('e', false);
                buffer[rowOffset + 4] = new Pixel('s', false);
                buffer[rowOffset + 5] = new Pixel('t', false);
            });

        // Mock the registry to return our mocked widget renderer
        Mock<IWidgetRendererRegistry> registryMock = new();
        registryMock
            .Setup(p => p.GetRenderer(typeof(TextWidget)))
            .Returns(widgetRendererMock.Object);

        // Inject the mocked registry into the System Under Test (SUT)
        StatelessRenderer renderer = new(registryMock.Object);

        // Act
        renderer.Draw(screen, inputBuffer);

        // Assert
        string actualVisualSnapshot = ConvertFlatBufferToVisualString(inputBuffer, screen.Width, screen.Height);
        string expectedVisualSnapshot =
            "          " + Environment.NewLine +
            "  Test    " + Environment.NewLine +
            "          ";

        actualVisualSnapshot
            .Should()
            .Be(expectedVisualSnapshot, "because the stateless engine must combine sub-renderer outputs into a coherent flat screen matrix representation");

        // Verify that the engine actually called the registry and the proper widget renderer with identical geometry contexts
        registryMock.Verify(r => r.GetRenderer(typeof(TextWidget)), Times.Once);
        widgetRendererMock.Verify(r => r.Draw(It.IsAny<Pixel[]>(), sampleWidget, screen.FocusedEntryWidgetId, screen.Width, screen.Height), Times.Once);
    }

    [Fact]
    public void Draw_WhenWidgetCoordinatesAreOutOfBounds_ShouldClipSafelyAndNotThrowIndexOutOfRangeException()
    {
        // Arrange
        Mock<IWidgetRendererRegistry> registryMock = new();
        Mock<IWidgetRenderer> widgetRendererMock = new();

        widgetRendererMock
            .Setup(r => r.SupportedWidgetType)
            .Returns(typeof(TextWidget));

        TextWidget outOfBoundsWidget = new()
        {
            Id = Guid.NewGuid(),
            Name = "BrokenCoordinatesLabel",
            Left = 50,
            Top = 20,
            Width = 10,
            Height = 1,
            Value = "Error Text",
            Visible = true
        };

        SimpleMessageScreen screen = new()
        {
            Id = Guid.NewGuid(),
            SessionId = Guid.NewGuid(),
            Name = "SmallScreen",
            Width = 5,
            Height = 5,
            Visible = true,
            Widgets = [outOfBoundsWidget]
        };

        int totalCellsCount = screen.Width * screen.Height;
        Pixel[] inputBuffer = new Pixel[totalCellsCount];

        widgetRendererMock
            .Setup(r => r.Draw(It.IsAny<Pixel[]>(), outOfBoundsWidget, It.IsAny<Guid?>(), screen.Width, screen.Height))
            .Callback<Pixel[], TextWidget, Guid?, int, int>((buffer, ctrl, focusedId, w, h) =>
            {
                int badX = ctrl.Left;
                int badY = ctrl.Top;

                // Protect the mock setup callback itself from causing runtime index crashes during evaluation
                if (badX >= 0 && badX < w && badY >= 0 && badY < h)
                {
                    buffer[badY * w + badX] = new Pixel('E', false);
                }
            });

        registryMock
            .Setup(p => p.GetRenderer(typeof(TextWidget)))
            .Returns(widgetRendererMock.Object);

        StatelessRenderer sut = new(registryMock.Object);

        // Act
        Action act = () => sut.Draw(screen, inputBuffer);

        // Assert
        act
            .Should()
            .NotThrow<IndexOutOfRangeException>("because the central StatelessRenderer engine or custom sub-renderers must enforce and wrap screen dimensions boundaries safely");
    }

    [Fact]
    public void Draw_WhenActiveWidgetIsEmpty_ShouldFillWidthWithEmptyEnterSymbol()
    {
        // Arrange
        Guid sessionId = Guid.NewGuid();
        Guid focusedWidgetId = Guid.NewGuid();

        // Create a focused text edit widget with an empty value and specific visual dots placeholder
        TextEntryWidget emptyActiveWidget = new()
        {
            Id = focusedWidgetId,
            Name = "ActiveBarcodeEditor",
            Left = 1,
            Top = 1,
            Width = 6,
            Height = 1,
            Value = string.Empty,
            Visible = true,
            EmptyEnterSymbol = '.'
        };

        SimpleMessageScreen screen = new()
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            Name = "ScanScreen",
            Width = 8,
            Height = 3,
            Visible = true,
            FocusedEntryWidgetId = focusedWidgetId,
            Widgets = [emptyActiveWidget]
        };

        int totalCellsCount = screen.Width * screen.Height;
        Pixel[] inputBuffer = new Pixel[totalCellsCount];

        // Wire up the actual concrete registry and rendering engine core
        List<IWidgetRenderer> boxRenderers = [new TextEntryWidgetRenderer()];
        WidgetRendererRegistry registry = new(boxRenderers);
        StatelessRenderer renderer = new(registry);

        // Act
        renderer.Draw(screen, inputBuffer);

        // Assert
        string actualVisualSnapshot = ConvertFlatBufferToVisualString(inputBuffer, screen.Width, screen.Height);

        // Expected grid representation (8x3 grid):
        // Line 0: "        " (8 spaces)
        // Line 1: " ...... " (1 space, 6 dots reflecting EmptyEnterSymbol, 1 space)
        // Line 2: "        " (8 spaces)
        string expectedVisualSnapshot =
            "        " + Environment.NewLine +
            " ...... " + Environment.NewLine +
            "        ";

        actualVisualSnapshot
            .Should()
            .Be(expectedVisualSnapshot, "because a focused editable widget with no text value must display its designated empty enter symbols across its entire layout width");
    }

    [Fact]
    public void Draw_WhenActiveWidgetHasHint_ShouldRenderHintCenteredAtTheBottomLine()
    {
        // Arrange
        Guid focusedWidgetId = Guid.NewGuid();

        TextEntryWidget activeWidget = new()
        {
            Id = focusedWidgetId,
            Name = "ActiveInput",
            Left = 0,
            Top = 0,
            Width = 4,
            Height = 1,
            Value = string.Empty,
            Hint = "Scan",
            Visible = true,
            EmptyEnterSymbol = '.'
        };

        SimpleMessageScreen screen = new()
        {
            Id = Guid.NewGuid(),
            SessionId = Guid.NewGuid(),
            Name = "TestScreen",
            Width = 10,
            Height = 3,
            Visible = true,
            FocusedEntryWidgetId = focusedWidgetId,
            Widgets = [activeWidget]
        };

        int totalCellsCount = screen.Width * screen.Height;
        Pixel[] inputBuffer = new Pixel[totalCellsCount];

        // Setting up real registry with the updated text edit renderer
        List<IWidgetRenderer> renderers = [new TextEntryWidgetRenderer()];
        WidgetRendererRegistry registry = new(renderers);
        StatelessRenderer sut = new(registry);

        // Act
        sut.Draw(screen, inputBuffer);
        string actualVisualSnapshot = ConvertFlatBufferToVisualString(inputBuffer, screen.Width, screen.Height);

        // Expected full 10x3 screen grid representation:
        // Line 0: "....      " -> Active widget drawn at Top=0 with dots, length 4, total width 10
        // Line 1: "          " -> Empty row padding
        // Line 2: "   Scan   " -> The Hint "Scan" centered precisely at the bottom line (Height - 1 = 2)
        string expectedVisualSnapshot =
            "....      " + Environment.NewLine +
            "          " + Environment.NewLine +
            "   Scan   ";

        // Assert
        actualVisualSnapshot
            .Should()
            .Be(expectedVisualSnapshot, "because the core rendering framework must project focused hints centered onto the terminal status line");
    }

    [Fact]
    public void Draw_ShouldInitializeEntireBufferWithSpacesBeforeRenderingWidgets()
    {
        // Arrange
        Mock<IWidgetRendererRegistry> registryMock = new();

        // Create an empty screen with no widgets to observe raw initialization state
        SimpleMessageScreen screen = new()
        {
            Id = Guid.NewGuid(),
            SessionId = Guid.NewGuid(),
            Name = "EmptyScreen",
            Width = 4,
            Height = 2,
            Visible = true,
            Widgets = []
        };

        int totalCellsCount = screen.Width * screen.Height;
        Pixel[] inputBuffer = new Pixel[totalCellsCount];

        StatelessRenderer sut = new(registryMock.Object);

        // Act
        sut.Draw(screen, inputBuffer);

        // Assert: Verify every single coordinate cell is filled with the default space padding character
        int screenWidth = screen.Width;
        int screenHeight = screen.Height;

        for (int y = 0; y < screenHeight; y++)
        {
            for (int x = 0; x < screenWidth; x++)
            {
                int flatIndex = y * screenWidth + x;

                inputBuffer[flatIndex].Symbol
                    .Should()
                    .Be(' ', "because the engine core must scrub the background layout matrix clean before rendering cycles start");
            }
        }
    }

    [Fact]
    public void Draw_WhenWidgetsOverlap_ShouldRenderInCollectionOrderAndLastWidgetMustOverwritePrevious()
    {
        // Arrange
        Mock<IWidgetRendererRegistry> registryMock = new();
        Mock<IWidgetRenderer> textRendererMock = new();

        textRendererMock
            .Setup(r => r.SupportedWidgetType)
            .Returns(typeof(TextWidget));

        // Two distinct label blocks targeting the exact same Left=0, Top=0 cell intersection point
        TextWidget underlyingLabel = new() { Id = Guid.NewGuid(), Name = "Underlying", Left = 0, Top = 0, Width = 1, Value = "A", Visible = true };
        TextWidget overwritingLabel = new() { Id = Guid.NewGuid(), Name = "Overwriting", Left = 0, Top = 0, Width = 1, Value = "B", Visible = true };

        SimpleMessageScreen screen = new()
        {
            Id = Guid.NewGuid(),
            SessionId = Guid.NewGuid(),
            Name = "OverlappingScreen",
            Width = 2,
            Height = 1,
            Visible = true,
            Widgets = [underlyingLabel, overwritingLabel]
        };

        int totalCellsCount = screen.Width * screen.Height;
        Pixel[] inputBuffer = new Pixel[totalCellsCount];

        // We simulate execution order directly within the mocked sequence loop callbacks using flat index mapping
        textRendererMock
            .Setup(r => r.Draw(It.IsAny<Pixel[]>(), underlyingLabel, It.IsAny<Guid?>(), screen.Width, screen.Height))
            .Callback<Pixel[], TextWidget, Guid?, int, int>((buf, ctrl, id, w, h) => buf[0] = new Pixel('A', false));

        textRendererMock
            .Setup(r => r.Draw(It.IsAny<Pixel[]>(), overwritingLabel, It.IsAny<Guid?>(), screen.Width, screen.Height))
            .Callback<Pixel[], TextWidget, Guid?, int, int>((buf, ctrl, id, w, h) => buf[0] = new Pixel('B', false));

        registryMock
            .Setup(r => r.GetRenderer(typeof(TextWidget)))
            .Returns(textRendererMock.Object);

        StatelessRenderer sut = new(registryMock.Object);

        // Act
        sut.Draw(screen, inputBuffer);

        // Assert
        inputBuffer[0].Symbol
            .Should()
            .Be('B', "because the framework structural pipeline must obey collection sequence ordering to handle visual overlapping stack rules");
    }

    private static string ConvertFlatBufferToVisualString(Pixel[] buffer, int width, int height)
    {
        List<string> lines = [];

        for (int y = 0; y < height; y++)
        {
            System.Text.StringBuilder lineBuilder = new();
            for (int x = 0; x < width; x++)
            {
                char symbol = buffer[y * width + x].Symbol;
                lineBuilder.Append(symbol == '\0' ? ' ' : symbol);
            }
            lines.Add(lineBuilder.ToString());
        }

        return string.Join(Environment.NewLine, lines);
    }

}
