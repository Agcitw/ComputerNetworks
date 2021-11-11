global using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Ping;

public static class Program
{
    private const int DefaultPayloadSize = 32;
    private const int DefaultRepetitionsCount = 4;
    private const int DefaultTimeoutInMilliseconds = 1000;

    public static void Main(string[] args)
    {
        var host = args[0];
            
        try
        {
            var ip = GetIpAddress(host);
            var endpoint = new IPEndPoint(ip, 0);
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.Icmp);
            socket.SendTimeout = socket.ReceiveTimeout = DefaultTimeoutInMilliseconds;

            try
            {
                socket.Connect(endpoint);
                    
                if (socket.Connected)
                {
                    Console.WriteLine($"Pinging {host}{(host == ip.ToString() ? string.Empty : $" [{ip}]")} with {DefaultPayloadSize} bytes of data:");
                    var sw = new Stopwatch();
                    var data = Enumerable.Repeat((byte)'0', DefaultPayloadSize).ToArray();
                        
                    for (ushort rep = 0; rep < DefaultRepetitionsCount; rep++)
                    {
                        var echoRequest = IcmpPacket.CreateEchoRequest(1, rep, data);
                        var reply = new byte[1024];
                        sw.Reset();
                        sw.Start();
                        socket.Send((byte[])echoRequest);

                        try
                        {
                            var bytesReceived = socket.Receive(reply);
                            sw.Stop();
                            var replyPayloadSize = bytesReceived - IcmpPacket.IpHeaderSize - IcmpPacket.IcmpHeaderSize;
                            var ttl = reply[8];

                            try
                            {
                                var echoReply = IcmpPacket.FromBytes(reply);
                                if (echoReply.Identifier == echoRequest.Identifier && echoReply.SequenceNumber == echoRequest.SequenceNumber)
                                    Console.WriteLine($"Reply from {ip}: bytes={replyPayloadSize} time={sw.ElapsedMilliseconds}ms TTL={ttl}");
                                else
                                    Console.WriteLine($"Reply from {ip}: wrong identifier or sequence number");
                            }
                            catch(IcmpChecksumException)
                            {
                                Console.WriteLine($"Reply from {ip}: INCORRECT CHECKSUM");
                            }
                        }
                        catch (SocketException)
                        {
                            Console.WriteLine($"Reply from {ip}: Destination host unreachable.");
                        }

                        if (rep < DefaultRepetitionsCount-1 && sw.Elapsed < TimeSpan.FromSeconds(1))
                            Thread.Sleep(TimeSpan.FromSeconds(1) - sw.Elapsed);
                    }
                }
                else 
                    Console.WriteLine($"Ping request could not find host {host}.");
            }
            catch (SocketException)
            {
                Console.WriteLine($"Ping could not connect to {host}.");
            }
        }
        catch (SocketException)
        {
            Console.WriteLine($"Ping request could not find host {host}. Please check the name and try again.");
        }
    }

    private static IPAddress GetIpAddress(string host)
    {
        if (IPAddress.TryParse(host, out var address)) 
            return address;
        var entry = Dns.GetHostEntry(host);
            
        return Array.Find(entry.AddressList, i => i.AddressFamily == AddressFamily.InterNetwork)!;
    }
}