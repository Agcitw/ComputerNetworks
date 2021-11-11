namespace Traceroute;

public static class Icmp
{
    public static byte[] CreateIcmpPackage()
    {
        var package = new byte[64];
        package[0] = 8;
        package[1] = 0;
        package[2] = 0xF7;
        package[3] = 0xFF;

        return package;
    }

    public static byte GetIcmpType(byte[] data)
    {
        return data[20];
    }
}