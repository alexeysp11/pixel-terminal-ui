using PixelTerminalUI.Contracts.Common;
using PixelTerminalUI.Contracts.Dto;
using ProtoBuf.Meta;

namespace PixelTerminalUI.Transport.Grpc;

public static class GrpcModelConfiguration
{
    public static void RegisterTerminalContracts()
    {
        // Explicitly register the system enum before configuring structures that depend on it
        RuntimeTypeModel.Default.Add(typeof(ConsoleColor), false);

        // Configure request telemetry layout and explicitly bypass the parameterless constructor requirement
        MetaType requestMeta = RuntimeTypeModel.Default.Add(typeof(TerminalRequest), false);
        requestMeta.UseConstructor = false; // Tells protobuf-net to allocate memory without executing a default constructor
        requestMeta
            .Add(1, nameof(TerminalRequest.SessionId))
            .Add(2, nameof(TerminalRequest.UserInput));

        // Configure deterministic flat response layout with composite payloads
        MetaType responseMeta = RuntimeTypeModel.Default.Add(typeof(TerminalResponse), false);
        responseMeta.UseConstructor = false;
        responseMeta
            .Add(1, nameof(TerminalResponse.SessionId))
            .Add(2, nameof(TerminalResponse.Width))
            .Add(3, nameof(TerminalResponse.Height))
            .Add(4, nameof(TerminalResponse.FullFrame))
            .Add(5, nameof(TerminalResponse.Delta));

        // Configure composite structural sub-packages
        MetaType fullFrameMeta = RuntimeTypeModel.Default.Add(typeof(FullFramePayload), false);
        fullFrameMeta.UseConstructor = false;
        fullFrameMeta.Add(1, nameof(FullFramePayload.ScreenBuffer));

        MetaType deltaMeta = RuntimeTypeModel.Default.Add(typeof(DeltaPayload), false);
        deltaMeta.UseConstructor = false;
        deltaMeta.Add(1, nameof(DeltaPayload.Mutations));

        // Configure atomic bitwise structural components
        RuntimeTypeModel.Default.Add(typeof(PixelMutation), false)
            .Add(1, nameof(PixelMutation.Index))
            .Add(2, nameof(PixelMutation.PackedValue));

        RuntimeTypeModel.Default.Add(typeof(Pixel), false)
            .Add(1, nameof(Pixel.Symbol))
            .Add(2, nameof(Pixel.IsInverted))
            .Add(3, nameof(Pixel.Foreground))
            .Add(4, nameof(Pixel.Background));
    }
}
