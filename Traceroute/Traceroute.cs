using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace Traceroute;

public static class Traceroute
{
    private const int MaxHop = 30;
    private const int MaxPackage = 3;
    private const int MaxTimeOut = 3000;

    public static void Start(string host)
    {
        IPHostEntry ipHost;
            
        try
        {
            ipHost = Dns.GetHostEntry(host);
        }
        catch (SocketException)
        {
            Console.WriteLine("Cannot resolve system hostname" + host + ".");
            return;
        }

        var ipPoint = new IPEndPoint(ipHost.AddressList[0], 0);
        EndPoint endPoint = ipPoint;
        var socket = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.Icmp);
        socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, MaxTimeOut);
        Console.WriteLine("Trace route to" + host + $" [{ipPoint.Address}]");
        Console.WriteLine("with the maximum number of jumps " + MaxHop + ":\n");
        var data = Icmp.CreateIcmpPackage();
        var ttl = 1;
        var isEndPointReached = false;

        for (var i = 0; i < MaxHop; i++)
        {
            var errorAnswer = 0; 
            Console.Write("{0,2}", i + 1);
            socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.IpTimeToLive, ttl++);
            var receivedData = new byte[512];
                
            for (var j = 0; j < MaxPackage; j++)
            {
                try
                {
                    var sw = new Stopwatch();
                    sw.Start();
                    socket.SendTo(data, data.Length, SocketFlags.None, ipPoint);
                    socket.ReceiveFrom(receivedData, ref endPoint);
                    sw.Stop();
                    var deltaTime = sw.ElapsedMilliseconds;
                    isEndPointReached = IsDestinationReached(receivedData, (int)deltaTime);
                }
                catch (SocketException)
                {
                    errorAnswer++;
                    Console.Write("{0,10}", "*");                        
                }
            }

            Console.Write(errorAnswer == MaxPackage ? "  Timed out request.\n" : $"  {GetIpAddress(endPoint)} \n");

            if (!isEndPointReached) 
                continue;

            Console.WriteLine("\nTrace completed.");
            break;
        }
    }

    private static bool IsDestinationReached(byte[] receivedMessage, int responseTime)
    {
        int receivedType = Icmp.GetIcmpType(receivedMessage);
            
        switch (receivedType)
        {
            case 0:
                Console.Write("{0, 10}", responseTime + " ms");
                return true;
            case 11:
                Console.Write("{0, 10}", responseTime + " ms");
                break;
        }
            
        return false;
    }

    private static string GetIpAddress(EndPoint ipAndPort)
    {
        return ipAndPort.ToString()?[..(ipAndPort.ToString()!.Length - 2)];
    }
}