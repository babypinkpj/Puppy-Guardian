using Unity.Services.Relay.Models;
using Unity.Networking.Transport.Relay;
using System.Linq;

public static class RelayUtils
{
    public static RelayServerData ToRelayServerData(this Allocation allocation, string connectionType)
    {
        var endpoint = allocation.ServerEndpoints.First(e => e.ConnectionType == connectionType);
        return new RelayServerData(
            endpoint.Host,
            (ushort)endpoint.Port,
            allocation.AllocationIdBytes,
            allocation.ConnectionData,
            allocation.ConnectionData,
            allocation.Key,
            connectionType == "dtls"
        );
    }

    public static RelayServerData ToRelayServerData(this JoinAllocation joinAllocation, string connectionType)
    {
        var endpoint = joinAllocation.ServerEndpoints.First(e => e.ConnectionType == connectionType);
        return new RelayServerData(
            endpoint.Host,
            (ushort)endpoint.Port,
            joinAllocation.AllocationIdBytes,
            joinAllocation.ConnectionData,
            joinAllocation.HostConnectionData,
            joinAllocation.Key,
            connectionType == "dtls"
        );
    }
}
