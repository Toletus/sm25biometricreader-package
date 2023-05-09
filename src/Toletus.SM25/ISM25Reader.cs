using Toletus.SM25.Command.Enums;

namespace Toletus.SM25;

interface ISM25Reader
{
    void Close();

    SM25Commands GetDeviceName();

    SM25Commands GetFWVersion();

    SM25Commands GetDeviceId();

    SM25Commands GetEmptyID();

    SM25Commands Enroll(ushort id);

    SM25Commands EnrollAndStoreinRAM();

    SM25Commands GetEnrollData();

    SM25Commands GetEnrollCount();

    SM25Commands ClearTemplate(ushort id);

    SM25Commands GetTemplateStatus(ushort id);

    SM25Commands ClearAllTemplate();

    SM25Commands SetDeviceId(ushort i);

    SM25Commands SetFingerTimeOut(ushort i);

    SM25Commands FPCancel();

    SM25Commands GetDuplicationCheck();

    SM25Commands SetDuplicationCheck(bool check);

    SM25Commands GetSecurityLevel();

    SM25Commands SetSecurityLevel(ushort level);

    SM25Commands GetFingerTimeOut();

    SM25Commands ReadTemplate(ushort id);

    SM25Commands WriteTemplate();

    SM25Commands WriteTemplateData(ushort id, byte[] template);
}