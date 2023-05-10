using System;
using Toletus.SM25.Command;
using Toletus.SM25.Command.Enums;

namespace Toletus.SM25;

public partial class SM25Reader 
{
    public SM25Commands GetDeviceName()
    {
        return Send(new ReaderSendCommand(SM25Commands.GetDeviceName));
    }

    public SM25Commands GetFWVersion()
    {
        return Send(new ReaderSendCommand(SM25Commands.GetFWVersion));
    }

    public SM25Commands GetDeviceId()
    {
        return Send(new ReaderSendCommand(SM25Commands.GetDeviceID));
    }

    public SM25Commands GetEmptyID()
    {
        return Send(new ReaderSendCommand(SM25Commands.GetEmptyID));
    }

    public SM25Commands Enroll(ushort id)
    {
        return Send(new ReaderSendCommand(SM25Commands.Enroll, id));
    }

    public SM25Commands EnrollAndStoreinRAM()
    {
        return Send(new ReaderSendCommand(SM25Commands.EnrollAndStoreinRAM));
    }

    public SM25Commands GetEnrollData()
    {
        return Send(new ReaderSendCommand(SM25Commands.GetEnrollData));
    }

    public SM25Commands GetEnrollCount()
    {
        return Send(new ReaderSendCommand(SM25Commands.GetEnrollCount));
    }

    public SM25Commands ClearTemplate(ushort id)
    {
        return Send(new ReaderSendCommand(SM25Commands.ClearTemplate, id));
    }

    public SM25Commands GetTemplateStatus(ushort id)
    {
        return Send(new ReaderSendCommand(SM25Commands.GetTemplateStatus, id));
    }

    public SM25Commands ClearAllTemplate()
    {
        return Send(new ReaderSendCommand(SM25Commands.ClearAllTemplate));
    }

    public new void Close()
    {
        base.Close();
    }

    public SM25Commands SetDeviceId(ushort i)
    {
        return Send(new ReaderSendCommand(SM25Commands.SetDeviceID, i));
    }

    public SM25Commands SetFingerTimeOut(ushort i)
    {
        return Send(new ReaderSendCommand(SM25Commands.SetFingerTimeOut, i));
    }

    public SM25Commands FPCancel()
    {
        return Send(new ReaderSendCommand(SM25Commands.FPCancel));
    }

    public SM25Commands GetDuplicationCheck()
    {
        return Send(new ReaderSendCommand(SM25Commands.GetDuplicationCheck));
    }

    public SM25Commands SetDuplicationCheck(bool check)
    {
        return Send(new ReaderSendCommand(SM25Commands.SetDuplicationCheck, check ? 1 : 0));
    }

    public SM25Commands GetSecurityLevel()
    {
        return Send(new ReaderSendCommand(SM25Commands.GetSecurityLevel));
    }

    public SM25Commands SetSecurityLevel(ushort level)
    {
        return Send(new ReaderSendCommand(SM25Commands.SetSecurityLevel, level));
    }

    public SM25Commands GetFingerTimeOut()
    {
        return Send(new ReaderSendCommand(SM25Commands.GetFingerTimeOut));
    }

    public SM25Commands ReadTemplate(ushort id)
    {
        return Send(new ReaderSendCommand(SM25Commands.ReadTemplate, id));
    }

    public SM25Commands WriteTemplate()
    {
        return Send(new ReaderSendCommand(SM25Commands.WriteTemplate, 498));
    }

    public SM25Commands WriteTemplateData(ushort id, byte[] template)
    {
        if (template.Length > 498) throw new Exception($"Template {id} biometrico excede tamanho esperado.");

        var data = new byte[2 + template.Length];
        data[0] = (byte)id;
        data[1] = (byte)(id >> 8);
        Array.Copy(template, 0, data, 2, template.Length);

        return Send(new ReaderSendCommand(SM25Commands.WriteTemplate, data));
    }

    public SM25Commands TestConnection()
    {
        return Send(new ReaderSendCommand(SM25Commands.TestConnection));
    }

    private new SM25Commands Send(ReaderSendCommand readerSendCommand)
    {
        if (Enrolling && readerSendCommand.Command != SM25Commands.FPCancel)
        {
            Log?.Invoke(
                $" Bio enviando comando {readerSendCommand.Command} enquanto cadastrando, enviado FPCancel antes.");
                
            base.Send(new ReaderSendCommand(SM25Commands.FPCancel));
        }

        return base.Send(readerSendCommand);
    }
}