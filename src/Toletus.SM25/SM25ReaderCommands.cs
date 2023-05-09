using System;
using Toletus.SM25.Command;
using Toletus.SM25.Command.Enums;

namespace Toletus.SM25;

public partial class SM25Reader 
{
    public Commands GetDeviceName()
    {
        return Send(new ReaderSendCommand(Commands.GetDeviceName));
    }

    public Commands GetFWVersion()
    {
        return Send(new ReaderSendCommand(Commands.GetFWVersion));
    }

    public Commands GetDeviceId()
    {
        return Send(new ReaderSendCommand(Commands.GetDeviceID));
    }

    public Commands GetEmptyID()
    {
        return Send(new ReaderSendCommand(Commands.GetEmptyID));
    }

    public Commands Enroll(ushort id)
    {
        return Send(new ReaderSendCommand(Commands.Enroll, id));
    }

    public Commands EnrollAndStoreinRAM()
    {
        return Send(new ReaderSendCommand(Commands.EnrollAndStoreinRAM));
    }

    public Commands GetEnrollData()
    {
        return Send(new ReaderSendCommand(Commands.GetEnrollData));
    }

    public Commands GetEnrollCount()
    {
        return Send(new ReaderSendCommand(Commands.GetEnrollCount));
    }

    public Commands ClearTemplate(ushort id)
    {
        return Send(new ReaderSendCommand(Commands.ClearTemplate, id));
    }

    public Commands GetTemplateStatus(ushort id)
    {
        return Send(new ReaderSendCommand(Commands.GetTemplateStatus, id));
    }

    public Commands ClearAllTemplate()
    {
        return Send(new ReaderSendCommand(Commands.ClearAllTemplate));
    }

    public new void Close()
    {
        base.Close();
    }

    public Commands SetDeviceId(ushort i)
    {
        return Send(new ReaderSendCommand(Commands.SetDeviceID, i));
    }

    public Commands SetFingerTimeOut(ushort i)
    {
        return Send(new ReaderSendCommand(Commands.SetFingerTimeOut, i));
    }

    public Commands FPCancel()
    {
        return Send(new ReaderSendCommand(Commands.FPCancel));
    }

    public Commands GetDuplicationCheck()
    {
        return Send(new ReaderSendCommand(Commands.GetDuplicationCheck));
    }

    public Commands SetDuplicationCheck(bool check)
    {
        return Send(new ReaderSendCommand(Commands.SetDuplicationCheck, check ? 1 : 0));
    }

    public Commands GetSecurityLevel()
    {
        return Send(new ReaderSendCommand(Commands.GetSecurityLevel));
    }

    public Commands SetSecurityLevel(ushort level)
    {
        return Send(new ReaderSendCommand(Commands.SetSecurityLevel, level));
    }

    public Commands GetFingerTimeOut()
    {
        return Send(new ReaderSendCommand(Commands.GetFingerTimeOut));
    }

    public Commands ReadTemplate(ushort id)
    {
        return Send(new ReaderSendCommand(Commands.ReadTemplate, id));
    }

    public Commands WriteTemplate()
    {
        return Send(new ReaderSendCommand(Commands.WriteTemplate, 498));
    }

    public Commands WriteTemplateData(ushort id, byte[] template)
    {
        if (template.Length > 498) throw new Exception($"Template {id} biometrico excede tamanho esperado.");

        var data = new byte[2 + template.Length];
        data[0] = (byte)id;
        data[1] = (byte)(id >> 8);
        Array.Copy(template, 0, data, 2, template.Length);

        return Send(new ReaderSendCommand(Commands.WriteTemplate, data));
    }

    public Commands TestConnection()
    {
        return Send(new ReaderSendCommand(Commands.TestConnection));
    }

    private new Commands Send(ReaderSendCommand readerSendCommand)
    {
        if (Enrolling && readerSendCommand.Command != Commands.FPCancel)
        {
            Log?.Invoke(
                $" Bio enviando comando {readerSendCommand.Command} enquanto cadastrando, enviado FPCancel antes.");
                
            base.Send(new ReaderSendCommand(Commands.FPCancel));
        }

        return base.Send(readerSendCommand);
    }
}