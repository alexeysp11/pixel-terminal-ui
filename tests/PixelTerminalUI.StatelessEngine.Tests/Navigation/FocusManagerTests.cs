using FluentAssertions;
using PixelTerminalUI.StatelessEngine.Navigation;
using PixelTerminalUI.StatelessEngine.Screens;
using PixelTerminalUI.StatelessEngine.Widgets;

namespace PixelTerminalUI.StatelessEngine.Tests.Navigation;

public sealed class FocusManagerTests
{
    [Fact]
    public void GetNextFocus_ShouldSortGeometrically_WhenTabIndexIsMissing()
    {
        // Arrange
        FocusManager sut = new();
        Guid upperFieldId = Guid.NewGuid();
        Guid lowerFieldId = Guid.NewGuid();

        TextEntryWidget lowerWidget = new() { Id = lowerFieldId, Name = "Lower", Value = string.Empty, Top = 5, Left = 0, Visible = true };
        TextEntryWidget upperWidget = new() { Id = upperFieldId, Name = "Upper", Value = string.Empty, Top = 1, Left = 0, Visible = true };

        SimpleMessageScreen screen = new()
        {
            Id = Guid.NewGuid(),
            SessionId = Guid.NewGuid(),
            Name = "SimpleMessage",
            FocusedEntryWidgetId = upperFieldId, // Focus is currently at the top field
            Widgets = [lowerWidget, upperWidget] // Intentionally unstructured insertion order
        };

        // Act
        Guid? nextFocusId = sut.GetNextFocus(screen);

        // Assert
        nextFocusId
            .Should()
            .Be(lowerFieldId, "because geometric top-to-bottom sorting dictates the focus layout flow when tab indexes are null");
    }

    [Fact]
    public void GetNextFocus_ShouldObeyExplicitTabIndex_OverridingGeometricCoordinates()
    {
        // Arrange
        FocusManager sut = new();
        Guid geometricTopId = Guid.NewGuid();
        Guid geometricBottomId = Guid.NewGuid();

        // Bottom field has TabIndex 1, Top field has TabIndex 2 (Non-linear forced flow)
        TextEntryWidget bottomWidget = new() { Id = geometricBottomId, Name = "Bottom", Value = string.Empty, Top = 10, Left = 0, TabIndex = 1, Visible = true };
        TextEntryWidget topWidget = new() { Id = geometricTopId, Name = "Top", Value = string.Empty, Top = 1, Left = 0, TabIndex = 2, Visible = true };

        SimpleMessageScreen screen = new()
        {
            Id = Guid.NewGuid(),
            SessionId = Guid.NewGuid(),
            Name = "SimpleMessage",
            FocusedEntryWidgetId = geometricBottomId, // Currently focused on bottom item (TabIndex 1)
            Widgets = [bottomWidget, topWidget]
        };

        // Act
        Guid? nextFocusId = sut.GetNextFocus(screen);

        // Assert
        nextFocusId
            .Should()
            .Be(geometricTopId, "because forced explicit TabIndex weights must supersede baseline physical row coordinate checks");
    }

    [Fact]
    public void GetNextFocus_WhenAtLastElement_ShouldLoopBackToFirstElement()
    {
        // Arrange
        FocusManager sut = new();
        Guid firstFieldId = Guid.NewGuid();
        Guid lastFieldId = Guid.NewGuid();

        TextEntryWidget firstWidget = new() { Id = firstFieldId, Name = "First", Value = string.Empty, Top = 1, Visible = true };
        TextEntryWidget lastWidget = new() { Id = lastFieldId, Name = "Last", Value = string.Empty, Top = 2, Visible = true };

        SimpleMessageScreen screen = new()
        {
            Id = Guid.NewGuid(),
            SessionId = Guid.NewGuid(),
            Name = "SimpleMessage",
            FocusedEntryWidgetId = lastFieldId, // Focus is already at the very bottom field
            Widgets = [firstWidget, lastWidget]
        };

        // Act
        Guid? nextFocusId = sut.GetNextFocus(screen);

        // Assert
        nextFocusId
            .Should()
            .Be(firstFieldId, "because reaching the end of input loops should reset active tracking back to the starting layout element index");
    }

    [Fact]
    public void GetPreviousFocus_WhenAtFirstElement_ShouldLoopBackwardToLastElement()
    {
        // Arrange
        FocusManager sut = new();
        Guid firstFieldId = Guid.NewGuid();
        Guid lastFieldId = Guid.NewGuid();

        TextEntryWidget firstWidget = new() { Id = firstFieldId, Name = "First", Value = string.Empty, Top = 1, Visible = true };
        TextEntryWidget lastWidget = new() { Id = lastFieldId, Name = "Last", Value = string.Empty, Top = 2, Visible = true };

        SimpleMessageScreen screen = new()
        {
            Id = Guid.NewGuid(),
            SessionId = Guid.NewGuid(),
            Name = "SimpleMessage",
            FocusedEntryWidgetId = firstFieldId, // Focus is at the very top item
            Widgets = [firstWidget, lastWidget]
        };

        // Act
        Guid? previousFocusId = sut.GetPreviousFocus(screen);

        // Assert
        previousFocusId
            .Should()
            .Be(lastFieldId, "because hitting navigation reverse keys at index zero must instantly loop focus back onto the trailing block entity");
    }
}
