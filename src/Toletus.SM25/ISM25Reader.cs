using Toletus.SM25.Command.Enums;

namespace Toletus.SM25
{
    interface ISM25Reader
    {
        void Close();

        Commands GetDeviceName();

        Commands GetFWVersion();

        Commands GetDeviceId();

        Commands GetEmptyID();

        Commands Enroll(ushort id);

        Commands EnrollAndStoreinRAM();

        Commands GetEnrollData();

        Commands GetEnrollCount();

        Commands ClearTemplate(ushort id);

        Commands GetTemplateStatus(ushort id);

        Commands ClearAllTemplate();

        Commands SetDeviceId(ushort i);

        Commands SetFingerTimeOut(ushort i);

        Commands FPCancel();

        Commands GetDuplicationCheck();

        Commands SetDuplicationCheck(bool check);

        Commands GetSecurityLevel();

        Commands SetSecurityLevel(ushort level);

        Commands GetFingerTimeOut();

        Commands ReadTemplate(ushort id);

        Commands WriteTemplate();

        Commands WriteTemplateData(ushort id, byte[] template);
    }
}
