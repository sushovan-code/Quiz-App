using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace BidQuiz.Api;

public static class LanAddress
{
    /// <summary>Finds the IPv4 address phones can reach. Adapters with a gateway rule out most virtual NICs.</summary>
    public static IPAddress? Find() =>
        NetworkInterface.GetAllNetworkInterfaces()
            .Where(n => n.OperationalStatus == OperationalStatus.Up
                && n.NetworkInterfaceType is not (NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel))
            .Select(n => n.GetIPProperties())
            .Where(p => p.GatewayAddresses.Any(g => g.Address.AddressFamily == AddressFamily.InterNetwork
                && !g.Address.Equals(IPAddress.Any)))
            .SelectMany(p => p.UnicastAddresses)
            .Select(a => a.Address)
            .FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(a));
}
