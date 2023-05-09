using System;
using System.Linq;
using Toletus.Extensions;
using Toletus.SM25.Command.Enums;

namespace Toletus.SM25.Command;

public class ReaderSendCommand
{
    public ReaderSendCommand(SM25Commands sm25Command) : this(sm25Command, null)
    { }

    public ReaderSendCommand(SM25Commands sm25Command, int parameter) : this(sm25Command, BitConverter.GetBytes(parameter).Take(2).ToArray())
    { }

    public ReaderSendCommand(SM25Commands sm25Command, byte[] parameter = null)
    {
        Len = GetLen(parameter?.Length ?? 0);
        Sm25Command = sm25Command;
        Parameter = GetParameter(parameter);
        Payload = GetPayload();
    }

    public byte[] Payload { get; }
    public ResponsePrefixes Prefix => Parameter.Length == 16 ? ResponsePrefixes.ResponseCommand : ResponsePrefixes.ResponseDataPacket;
    public int Len { get; set; }
    public SM25Commands Sm25Command { get; set; }
    public byte[] Parameter { get; }
    public ushort ChecksumCalculated { get; private set; }

    public ReaderResponseCommand ReaderResponseCommand { get; set; }

    private static byte[] GetParameter(byte[] parameter)
    {
        if (parameter == null || parameter.Length < 16) Array.Resize(ref parameter, 16);
        return parameter;
    }

    private static int GetLen(int len)
    {
        var b = BitConverter.GetBytes(len);
        var hexArray = b.ToHexStringArray().Take(2);
        var h = string.Join(string.Empty, hexArray);
        return int.Parse(h, System.Globalization.NumberStyles.HexNumber);
    }

    public byte[] GetPayload()
    {
        /* Payload
         *
         * /- Prefix (2 bytes) (0x55aa or 0x5aa5)
         * |
         * | /- Sm25Command (2 bytes)
         * | |
         * | | /- Len (2 bytes)
         * | | |
         * | | | /- Parameters ([..])
         * | | | |                       
         * | | | |   /- Checksum (2 bytes) 
         * | | | |   |
         * 012345[..]01
         * 
        */

        var pre = (ushort)Prefix;
        var cmd = (ushort)Sm25Command;

        var rawSend = new byte[8 + Parameter.Length];
        rawSend[0] = (byte)(pre >> 8);
        rawSend[1] = (byte)pre;
        rawSend[2] = (byte)cmd;
        rawSend[3] = (byte)(cmd >> 8);
        rawSend[4] = (byte)(Len >> 8);
        rawSend[5] = (byte)Len;

        Array.Copy(Parameter, 0, rawSend, 6, Parameter.Length);

        ChecksumCalculated = Checksum.Calculate(rawSend);
        rawSend[rawSend.Length - 2] = (byte)ChecksumCalculated;
        rawSend[rawSend.Length - 1] = (byte)(ChecksumCalculated >> 8);

        return rawSend;
    }

    public override string ToString()
    {
        return $"{nameof(Sm25Command)} {Sm25Command} {Payload.ToHexString(" ")}";
    }
}