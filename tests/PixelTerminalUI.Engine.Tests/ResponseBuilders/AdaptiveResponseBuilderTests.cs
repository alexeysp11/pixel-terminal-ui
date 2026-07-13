using FluentAssertions;
using PixelTerminalUI.Contracts.Common;
using PixelTerminalUI.Contracts.Dto;
using PixelTerminalUI.Engine.Extensions.ServiceCollectionExtensions;
using PixelTerminalUI.Engine.ResponseBuilders;

namespace PixelTerminalUI.Engine.Tests.ResponseBuilders;

public sealed class AdaptiveResponseBuilderTests
{
    private readonly AdaptiveResponseBuilder _sut = new(new PixelTerminalUIOptions
    {
        EnableDoubleBuffering = true,
        DoubleBufferingThreshold = 0.25
    });

    [Fact]
    public void Build_WhenHistoricalBufferIsNull_ShouldReturnFullFramePayload()
    {
        // Arrange
        Guid sessionId = Guid.NewGuid();
        int width = 4;
        int height = 2;
        uint[] currentBuffer = [1, 2, 3, 4, 5, 6, 7, 8];
        uint[]? historicalBuffer = null;

        // Act
        TerminalResponse response = _sut.Build(sessionId, currentBuffer, historicalBuffer, width, height);

        // Assert
        response
            .Should()
            .NotBeNull("because a missing historical buffer indicates a cold start session render state");

        response.FullFrame
            .Should()
            .NotBeNull("because the framework must broadcast the entire screen topology when no historical baseline exists");

        response.Delta
            .Should()
            .BeNull("because a full viewport initialization packet must exclude differential delta mutation blocks");

        response.FullFrame!.ScreenBuffer
            .Should()
            .BeSameAs(currentBuffer, "because the full frame response container must directly enclose the latest reference frame buffer array");
    }

    [Fact]
    public void Build_WhenHistoricalBufferLengthIsInvalid_ShouldReturnFullFramePayload()
    {
        // Arrange
        Guid sessionId = Guid.NewGuid();
        int width = 3;
        int height = 2;
        uint[] currentBuffer = [1, 2, 3, 4, 5, 6];
        uint[] historicalBuffer = [1, 2, 3, 4, 5]; // Length is 5, expected width * height = 6

        // Act
        TerminalResponse response = _sut.Build(sessionId, currentBuffer, historicalBuffer, width, height);

        // Assert
        response
            .Should()
            .NotBeNull("because mismatching screen data boundaries force an immediate engine recovery action");

        response.FullFrame
            .Should()
            .NotBeNull("because terminal geometry changes require a full viewport redraw sequence on the client");

        response.Delta
            .Should()
            .BeNull("because geometry recovery routines override incremental delta tracking optimizations");
    }

    [Fact]
    public void Build_WhenNoChangesDetected_ShouldReturnDeltaPayloadWithEmptyMutations()
    {
        // Arrange
        Guid sessionId = Guid.NewGuid();
        int width = 2;
        int height = 2;
        uint[] currentBuffer = [5, 6, 7, 8];
        uint[] historicalBuffer = [5, 6, 7, 8];

        // Act
        TerminalResponse response = _sut.Build(sessionId, currentBuffer, historicalBuffer, width, height);

        // Assert
        response
            .Should()
            .NotBeNull("because stable screen frames must yield a valid data frame package");

        response.Delta
            .Should()
            .NotBeNull("because identical pixel states represent a zero percentage change matrix layout");

        response.FullFrame
            .Should()
            .BeNull("because delta updates must omit the redundant heavy full screen layout data segments");

        response.Delta!.Mutations
            .Should()
            .BeEmpty("because no pixel indexes diverged from the historical background cache data snapshot");
    }
    [Fact]
    public void Build_WhenChangesAreBelowThreshold_ShouldReturnDeltaPayloadWithPreciseMutations()
    {
        // Arrange
        Guid sessionId = Guid.NewGuid();
        int width = 10;
        int height = 1; // 10 total cells, 1 change = 10% ratio (which is below the 25% threshold)
        uint[] currentBuffer = [9, 2, 3, 4, 5, 6, 7, 8, 9, 10];
        uint[] historicalBuffer = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10]; // Element at index 0 is modified

        // Act
        TerminalResponse response = _sut.Build(sessionId, currentBuffer, historicalBuffer, width, height);

        // Assert
        response
            .Should()
            .NotBeNull("because localized sub-threshold screen shifts must generate data updates");

        response.Delta
            .Should()
            .NotBeNull("because a low density modification ratio optimizes perfectly into a coordinate delta structure");

        response.FullFrame
            .Should()
            .BeNull("because low density changes must omit the redundant heavy full screen layout data segments");

        response.Delta!.Mutations
            .Should()
            .ContainSingle("because only one isolated cell position was mutated relative to the historical tracking layer")
            .Which.Should()
            .Match<PixelMutation>(m => m.Index == 0 && m.PackedValue == 9, "the individual delta point metadata must exactly record the altered stream coordinate and payload");
    }

    [Fact]
    public void Build_WhenChangesAreExactlyAtThreshold_ShouldStillReturnDeltaPayload()
    {
        // Arrange
        Guid sessionId = Guid.NewGuid();
        int width = 4;
        int height = 1; // 4 total cells, 1 change = 25% ratio (exactly equal to the threshold)
        uint[] currentBuffer = [99, 2, 3, 4];
        uint[] historicalBuffer = [1, 2, 3, 4]; // Element at index 0 is modified

        // Act
        TerminalResponse response = _sut.Build(sessionId, currentBuffer, historicalBuffer, width, height);

        // Assert
        response
            .Should()
            .NotBeNull("the matrix evaluation system must deliver structural network packages at boundary conditions");

        response.Delta
            .Should()
            .NotBeNull("because the algorithm boundary relies on strict inequality comparison rules");

        response.FullFrame
            .Should()
            .BeNull("because exact boundary conditions matching delta constraints must suppress full frame fallback routines");

        response.Delta!.Mutations
            .Should()
            .ContainSingle("because the strict mathematical ratio boundary preserves structural compaction routines")
            .Which.Should()
            .Match<PixelMutation>(m => m.Index == 0 && m.PackedValue == 99, "the resulting single delta component must hold the exact change offset coordinates");
    }

    [Fact]
    public void Build_WhenChangesAreAboveThreshold_ShouldFallbackToFullFramePayload()
    {
        // Arrange
        Guid sessionId = Guid.NewGuid();
        int width = 4;
        int height = 1; // 4 total cells, 2 changes = 50% ratio (which exceeds the 25% threshold)
        uint[] currentBuffer = [99, 88, 3, 4];
        uint[] historicalBuffer = [1, 2, 3, 4]; // Elements at indexes 0 and 1 are modified

        // Act
        TerminalResponse response = _sut.Build(sessionId, currentBuffer, historicalBuffer, width, height);

        // Assert
        response
            .Should()
            .NotBeNull("because heavy graphic viewport layout transitions must complete execution correctly");

        response.FullFrame
            .Should()
            .NotBeNull("because sending dense collections of individual object coordinates creates more serialization overhead than flat primitive arrays");

        response.Delta
            .Should()
            .BeNull("because dense high saturation modifications must completely deactivate incremental delta payloads");

        response.FullFrame!.ScreenBuffer
            .Should()
            .BeSameAs(currentBuffer, "because high density mutations command an immediate swap back to total screen state replication vectors");
    }

    [Fact]
    public void Build_WhenMatrixDimensionsAreZero_ShouldReturnDeltaPayloadWithEmptyMutations()
    {
        // Arrange
        Guid sessionId = Guid.NewGuid();
        int width = 0;
        int height = 0;
        uint[] currentBuffer = [];
        uint[] historicalBuffer = [];

        // Act
        TerminalResponse response = _sut.Build(sessionId, currentBuffer, historicalBuffer, width, height);

        // Assert
        response
            .Should()
            .NotBeNull("because empty geometry scenarios must safely yield a valid output container");

        response.Delta
            .Should()
            .NotBeNull("because zero changes with zero cells evaluate mathematically into a zero mutation delta framework");

        // Fix typo in original assert explanation ("DeltaResponse" -> "Delta payload properties")
        response.FullFrame
            .Should()
            .BeNull("because a zero dimension viewport cannot allocate or initialize a valid full frame screen buffer layout");

        response.Delta!.Mutations
            .Should()
            .BeEmpty("because no cell iterations could possibly execute or diverge within a zero dimension space");
    }

    [Fact]
    public void Build_WhenCurrentBufferIsLargerThanStatedGeometry_ShouldEvaluateOnlyWithinStatedBoundsAndReturnDelta()
    {
        // Arrange
        Guid sessionId = Guid.NewGuid();
        int width = 2;
        int height = 1; // Expected cells count is 2
        uint[] currentBuffer = [1, 2, 99, 99];
        uint[] historicalBuffer = [1, 2]; // Historical buffer strictly matches the logical geometry shape

        // Act
        TerminalResponse response = _sut.Build(sessionId, currentBuffer, historicalBuffer, width, height);

        // Assert
        response
            .Should()
            .NotBeNull("because the system must tolerate trailing raw buffer allocation anomalies outside the target area");

        response.Delta
            .Should()
            .NotBeNull("because the internal loop boundary stops strictly at the stated total cells count threshold");

        response.FullFrame
            .Should()
            .BeNull("because buffer anomalies outside logical geometry limits must not trigger an explicit full frame recovery sequence");

        response.Delta!.Mutations
            .Should()
            .BeEmpty("because the cells within the evaluated coordinates 0 and 1 match the historical baseline perfectly");
    }

    [Fact]
    public void Build_WhenSingleCellMatrixIsMutated_ShouldFallbackToFullFramePayload()
    {
        // Arrange
        Guid sessionId = Guid.NewGuid();
        int width = 1;
        int height = 1; // 1 cell total, 1 change = 100% ratio (which is above the 25% threshold)
        uint[] currentBuffer = [55];
        uint[] historicalBuffer = [11];

        // Act
        TerminalResponse response = _sut.Build(sessionId, currentBuffer, historicalBuffer, width, height);

        // Assert
        response
            .Should()
            .NotBeNull("because single element state mutations must resolve into a concrete transfer model package");

        response.FullFrame
            .Should()
            .NotBeNull("because changing the only existing cell yields a maximum saturation change factor which triggers full array distribution");

        response.Delta
            .Should()
            .BeNull("because absolute 100% viewport mutations completely eliminate the serialization efficiency of individual delta points");
    }
}
