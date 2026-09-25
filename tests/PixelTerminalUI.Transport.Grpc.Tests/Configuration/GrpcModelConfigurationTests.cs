using FluentAssertions;
using PixelTerminalUI.Contracts.Common;
using PixelTerminalUI.Contracts.Dto;
using PixelTerminalUI.Transport.Grpc.Configuration;
using ProtoBuf.Meta;

namespace PixelTerminalUI.Transport.Grpc.Tests.Configuration;

/// <summary>
/// Contains compilation constraints and meta-model verification specifications for <see cref="GrpcModelConfiguration"/>.
/// </summary>
public sealed class GrpcModelConfigurationTests
{
    static GrpcModelConfigurationTests()
    {
        // Explicitly initialize the runtime configuration layout once before running the test suite
        GrpcModelConfiguration.RegisterTerminalContracts();
    }

    [Theory]
    [InlineData(typeof(TerminalRequest))]
    [InlineData(typeof(TerminalResponse))]
    [InlineData(typeof(FullFramePayload))]
    [InlineData(typeof(DeltaPayload))]
    [InlineData(typeof(FocusedInputPayload))]
    public void RegisterTerminalContracts_ShouldDisableConstructorActivation_ForImmutableRecordPayloads(Type targetType)
    {
        // Act
        MetaType metaType = RuntimeTypeModel.Default[targetType];

        // Assert
        metaType.Should().NotBeNull($"because {targetType.Name} must be registered in the global configuration layer");

        metaType.UseConstructor
            .Should()
            .BeFalse($"because skipping the default constructor prevents instantiation failures on C# positional records");
    }

    [Fact]
    public void RegisterTerminalContracts_ShouldMapTerminalRequestFields_WithDeterministicFieldIndexes()
    {
        // Act
        MetaType metaType = RuntimeTypeModel.Default[typeof(TerminalRequest)];

        // Assert
        metaType.GetFields()
            .Should()
            .HaveCount(2, "because the incoming request frame contains exactly two operational parameters");

        metaType[1].Name.Should().Be(nameof(TerminalRequest.SessionId), "field index 1 must map exactly to the session identification token");
        metaType[2].Name.Should().Be(nameof(TerminalRequest.UserInput), "field index 2 must map exactly to the raw interactive console input stream");
    }

    [Fact]
    public void RegisterTerminalContracts_ShouldMapTerminalResponseFields_WithFlatCompositionStructure()
    {
        // Act
        MetaType metaType = RuntimeTypeModel.Default[typeof(TerminalResponse)];

        // Assert
        metaType.GetFields()
            .Should()
            .HaveCount(6, "because the core response package enforces a strict 6-tier flattening layout layout instead of polymorphism");

        metaType[1].Name.Should().Be(nameof(TerminalResponse.SessionId));
        metaType[2].Name.Should().Be(nameof(TerminalResponse.Width));
        metaType[3].Name.Should().Be(nameof(TerminalResponse.Height));
        metaType[4].Name.Should().Be(nameof(TerminalResponse.FullFrame), "field index 4 isolates full viewport screen matrices updates");
        metaType[5].Name.Should().Be(nameof(TerminalResponse.Delta), "field index 5 isolates fine-grained individual mutations packets");
        metaType[6].Name.Should().Be(nameof(TerminalResponse.FocusedInput), "field index 6 isolates the focused input widget geometry used for inline cursor rendering");
    }

    [Fact]
    public void RegisterTerminalContracts_ShouldMapNestedPayloadPrimitives_WithCorrectBorders()
    {
        // Act
        MetaType fullFrameMeta = RuntimeTypeModel.Default[typeof(FullFramePayload)];
        MetaType deltaMeta = RuntimeTypeModel.Default[typeof(DeltaPayload)];
        MetaType mutationMeta = RuntimeTypeModel.Default[typeof(PixelMutation)];
        MetaType focusedInputMeta = RuntimeTypeModel.Default[typeof(FocusedInputPayload)];

        // Assert
        fullFrameMeta[1].Name.Should().Be(nameof(FullFramePayload.ScreenBuffer));
        deltaMeta[1].Name.Should().Be(nameof(DeltaPayload.Mutations));

        mutationMeta[1].Name.Should().Be(nameof(PixelMutation.Index), "index 1 is reserved for 1D buffer cell layout coordinate maps");
        mutationMeta[2].Name.Should().Be(nameof(PixelMutation.PackedValue), "index 2 stores bit-packed foreground, background, and character state structures");

        focusedInputMeta.GetFields()
            .Should()
            .HaveCount(9, "because the focused input geometry packet exposes exactly nine client-facing rendering properties");

        focusedInputMeta[1].Name.Should().Be(nameof(FocusedInputPayload.X), "index 1 carries the absolute column of the focused widget's left edge");
        focusedInputMeta[2].Name.Should().Be(nameof(FocusedInputPayload.Y), "index 2 carries the absolute row of the focused widget");
        focusedInputMeta[3].Name.Should().Be(nameof(FocusedInputPayload.MaxLength), "index 3 lets the client clamp local echo to the widget's rendered width");
        focusedInputMeta[4].Name.Should().Be(nameof(FocusedInputPayload.IsMasked), "index 4 tells the client whether to echo a mask character instead of raw keystrokes");
        focusedInputMeta[5].Name.Should().Be(nameof(FocusedInputPayload.EmptyFillChar), "index 5 lets the client restore the correct placeholder glyph after a backspace");
        focusedInputMeta[6].Name.Should().Be(nameof(FocusedInputPayload.InitialValue), "index 6 lets the client seed its local edit buffer so backspace can remove previously committed text");
        focusedInputMeta[7].Name.Should().Be(nameof(FocusedInputPayload.Foreground), "index 7 lets the client match the widget's foreground color instead of leaking stale console colors");
        focusedInputMeta[8].Name.Should().Be(nameof(FocusedInputPayload.Background), "index 8 lets the client match the widget's background color instead of leaking stale console colors");
        focusedInputMeta[9].Name.Should().Be(nameof(FocusedInputPayload.Inverted), "index 9 tells the client whether to swap foreground and background, matching the server-side pixel renderer");
    }

    [Fact]
    public void RegisterTerminalContracts_ShouldMapBitwisePixelStructure_WithoutBreakingDataLayout()
    {
        // Act
        MetaType pixelMeta = RuntimeTypeModel.Default[typeof(Pixel)];

        // Assert
        pixelMeta.GetFields()
            .Should()
            .HaveCount(4, "because the foundational presentation structural block contains 4 isolated color and typography metadata features");

        pixelMeta[1].Name.Should().Be(nameof(Pixel.Symbol));
        pixelMeta[2].Name.Should().Be(nameof(Pixel.IsInverted));
        pixelMeta[3].Name.Should().Be(nameof(Pixel.Foreground));
        pixelMeta[4].Name.Should().Be(nameof(Pixel.Background));
    }
}
