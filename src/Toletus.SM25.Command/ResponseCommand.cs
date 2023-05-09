using System;
using System.Linq;
using Toletus.Extensions;
using Toletus.SM25.Command.Enums;

namespace Toletus.SM25.Command
{
    public class ResponseCommand
    {
        public bool IsResponseComplete => Payload.Length == 0 || Payload.Length == ResponseLenghtExpected;

        public ResponseCommand(ref byte[] response)
        {
            try
            {
                _payload = response;

                if (_payload.Length > ResponseLenghtExpected)
                {
                    _payload = _payload.Take(ResponseLenghtExpected).ToArray();
                    response = response.Skip(ResponseLenghtExpected).ToArray();
                }
                else
                    response = Array.Empty<byte>();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public void Add(ref byte[] response)
        {
            var missing = ResponseLenghtExpected - Payload.Length;
            var add = response.Length < missing ? response.Length : missing;

            var destinationIndex = Payload.Length;
            Array.Resize(ref _payload, _payload.Length + add);
            Array.Copy(response, 0, Payload, destinationIndex, add);
            response = response.Skip(add).ToArray();
        }

        private byte[] _payload;
        public byte[] Payload => _payload;
        public byte[] ReturnRaw => Payload.Skip(6).Take(2).ToArray();
        public byte[] RawData => Payload.Skip(8).Take(DataLen).ToArray();
        public byte[] Template => DataLen == 498 ? RawData.ToArray() : null;
        public int FullReturnLen => BitConverter.ToUInt16(Payload, 4);
        public int DataLen => FullReturnLen - 2;
        public ushort Data => RawData.Length == 0 ? (ushort)0 : BitConverter.ToUInt16(RawData, 0);
        public ushort ChecksumFromReturn => Payload.Length == 0 ? (ushort)0 : BitConverter.ToUInt16(Payload, Payload.Length - 2);
        public ushort ChecksumCalculated => Checksum.Calculate(Payload);
        public ResponsePrefixes Prefix
        {
            get
            {
                try
                {
                    return (ResponsePrefixes)BitConverter.ToUInt16(Payload, 0);
                }
                catch (Exception e)
                {
                    return ResponsePrefixes.Unknow;
                }
            }
        }

        public Commands Command
        {
            get
            {
                try
                {
                    return (Commands)BitConverter.ToUInt16(Payload, 2);
                }
                catch
                {
                    return Commands.Unknow;
                }
            }
        }

        public GDCodes DataGD => (GDCodes)Data;
        public TemplateStatus DataTemplateStatus { get; set; }
        public ReturnCodes DataReturnCode => (ReturnCodes)Data;
        public ReturnCodes ReturnCode => (ReturnCodes)BitConverter.ToUInt16(ReturnRaw, 0);
        public bool ChecksumIsValid => ChecksumFromReturn == ChecksumCalculated;

        public int ResponseLenghtExpected
        {
            get
            {
                switch (Prefix)
                {
                    case ResponsePrefixes.Unknow:
                        return 0;
                    case ResponsePrefixes.ResponseCommand:
                        return 24;
                    default:
                        switch (Command)
                        {
                            case Commands.ReadTemplate:
                                return 510;
                            case Commands.GetEnrollData:
                                return 508;
                            case Commands.WriteTemplate:
                                return 12;
                            default:
                                return 0;
                        }

                        break;
                }
            }
        }

        public override string ToString()
        {
            string ret = null;

            try
            {
                if (Payload == null) return string.Empty;

                ret = $"[{Payload.ToHexString(" ")}]";
                ret += $" {Command}";
                ret += $" {ReturnCode}";
                ret += $" {Data}";
                ret += $" {DataReturnCode}";
                ret += $" {DataGD}";
                ret += $" {Prefix}";
                ret += $" {nameof(ChecksumIsValid)} {ChecksumIsValid}";
            }
            catch (Exception ex)
            {

            }

            return ret; 
        }
    }
}