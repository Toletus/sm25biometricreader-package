namespace Toletus.SM25.Command;

public static class Checksum
{
    public static ushort Calculate(byte[] packet)
    {
        ushort checksum = 0;
        for (var i = 0; i < packet.Length - 2; i++) checksum += packet[i];
        return checksum;
    }
}