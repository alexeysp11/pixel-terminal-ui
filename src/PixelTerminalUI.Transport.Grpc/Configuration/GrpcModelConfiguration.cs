using PixelTerminalUI.Contracts.Common;
using PixelTerminalUI.Contracts.Dto;
using ProtoBuf.Meta;

namespace PixelTerminalUI.Transport.Grpc.Configuration;

/// <summary>
/// Provides runtime semantic configuration layouts for protobuf-net binary serialization pipelines.
/// </summary>
public static class GrpcModelConfiguration
{
    /// <summary>
    /// Registers domain contracts into the global protobuf-net serialization model.
    /// </summary>
    /// <remarks>
    /// <b>Warning</b>: Setting <c>UseConstructor = false</c> bypasses parameterless and positional constructors entirely during 
    /// deserialization, meaning any validation logic placed inside record constructors will be ignored by the runtime.
    /// </remarks>
    public static void RegisterTerminalContracts()
    {
        // Explicitly register the system enum before configuring structures that depend on it
        RuntimeTypeModel.Default.Add(typeof(ConsoleColor), false);

        // Configure metadata execution layer schema for incoming terminal requests
        MetaType requestMeta = RuntimeTypeModel.Default.Add(typeof(TerminalRequest), false);
        requestMeta.UseConstructor = false;
        requestMeta
            .Add(1, nameof(TerminalRequest.SessionId))
            .Add(2, nameof(TerminalRequest.UserInput));

        // Configure deterministic flat compound response frame layout structures
        // Using explicit composition instead of polymorphism ensures cross-network payload stability
        MetaType responseMeta = RuntimeTypeModel.Default.Add(typeof(TerminalResponse), false);
        responseMeta.UseConstructor = false;
        responseMeta
            .Add(1, nameof(TerminalResponse.SessionId))
            .Add(2, nameof(TerminalResponse.Width))
            .Add(3, nameof(TerminalResponse.Height))
            .Add(4, nameof(TerminalResponse.FullFrame))
            .Add(5, nameof(TerminalResponse.Delta));

        // Configure nested data segments containing complete screen matrix representations
        MetaType fullFrameMeta = RuntimeTypeModel.Default.Add(typeof(FullFramePayload), false);
        fullFrameMeta.UseConstructor = false;
        fullFrameMeta.Add(1, nameof(FullFramePayload.ScreenBuffer));

        // Configure nested data segments containing targeted atomic frame mutations
        MetaType deltaMeta = RuntimeTypeModel.Default.Add(typeof(DeltaPayload), false);
        deltaMeta.UseConstructor = false;
        deltaMeta.Add(1, nameof(DeltaPayload.Mutations));

        // Configure structural attributes mapping target single pixel change matrix indexes
        MetaType mutationMeta = RuntimeTypeModel.Default.Add(typeof(PixelMutation), false);
        mutationMeta.Add(1, nameof(PixelMutation.Index))
            .Add(2, nameof(PixelMutation.PackedValue));

        // Configure base bitwise primitives storing specific structural cell matrix presentation tokens
        MetaType pixelMeta = RuntimeTypeModel.Default.Add(typeof(Pixel), false);
        pixelMeta.Add(1, nameof(Pixel.Symbol))
            .Add(2, nameof(Pixel.IsInverted))
            .Add(3, nameof(Pixel.Foreground))
            .Add(4, nameof(Pixel.Background));
    }
}
