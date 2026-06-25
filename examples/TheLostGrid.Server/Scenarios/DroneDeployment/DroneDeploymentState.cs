namespace TheLostGrid.Server.Scenarios.DroneDeployment;

/// <summary>
/// Specifies the execution checkpoint steps inside the drone deployment state machine.
/// </summary>
public enum DroneDeploymentState
{
    /// <summary>
    /// The drone system linkage is active and waiting for a tactical vector directive.
    /// </summary>
    AwaitingCommand = 0,

    /// <summary>
    /// The link is being disconnected to safely route back to the central hub environment.
    /// </summary>
    TerminatingLink = 1
}
