using System.ServiceModel;
using PixelTerminalUI.Contracts.Dto;

namespace PixelTerminalUI.Transport.Grpc.Services;

[ServiceContract(Name = "PixelTerminalUI.TerminalService")]
public interface ITerminalService
{
    [OperationContract]
    ValueTask<TerminalResponse> ProcessTransactionAsync(TerminalRequest request);
}
