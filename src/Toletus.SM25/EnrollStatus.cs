using Toletus.SM25.Command.Enums;

namespace Toletus.SM25
{
    public struct EnrollStatus
    {
        public ReturnCodes Ret { get; set; }
        public GDCodes DataGD { get; set; }
        public ReturnCodes DataReturnCode { get; set; }
        public int Data { get; set; }
    }
}
