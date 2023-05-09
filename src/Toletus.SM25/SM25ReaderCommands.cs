using System;
using Toletus.SM25.Command;
using Toletus.SM25.Command.Enums;

namespace Toletus.SM25
{
    public partial class SM25Reader 
    {
        public Commands GetDeviceName()
        {
            return Send(new SendCommand(Commands.GetDeviceName));
        }

        public Commands GetFWVersion()
        {
            return Send(new SendCommand(Commands.GetFWVersion));
        }

        public Commands GetDeviceId()
        {
            return Send(new SendCommand(Commands.GetDeviceID));
        }

        public Commands GetEmptyID()
        {
            return Send(new SendCommand(Commands.GetEmptyID));
        }

        public Commands Enroll(ushort id)
        {
            return Send(new SendCommand(Commands.Enroll, id));
        }

        public Commands EnrollAndStoreinRAM()
        {
            return Send(new SendCommand(Commands.EnrollAndStoreinRAM));
        }

        public Commands GetEnrollData()
        {
            return Send(new SendCommand(Commands.GetEnrollData));
        }

        public Commands GetEnrollCount()
        {
            return Send(new SendCommand(Commands.GetEnrollCount));
        }

        public Commands ClearTemplate(ushort id)
        {
            return Send(new SendCommand(Commands.ClearTemplate, id));
        }

        public Commands GetTemplateStatus(ushort id)
        {
            return Send(new SendCommand(Commands.GetTemplateStatus, id));
        }

        public Commands ClearAllTemplate()
        {
            return Send(new SendCommand(Commands.ClearAllTemplate));
        }

        public new void Close()
        {
            base.Close();
        }

        public Commands SetDeviceId(ushort i)
        {
            return Send(new SendCommand(Commands.SetDeviceID, i));
        }

        public Commands SetFingerTimeOut(ushort i)
        {
            return Send(new SendCommand(Commands.SetFingerTimeOut, i));
        }

        public Commands FPCancel()
        {
            return Send(new SendCommand(Commands.FPCancel));
        }

        public Commands GetDuplicationCheck()
        {
            return Send(new SendCommand(Commands.GetDuplicationCheck));
        }

        public Commands SetDuplicationCheck(bool check)
        {
            return Send(new SendCommand(Commands.SetDuplicationCheck, check ? 1 : 0));
        }

        public Commands GetSecurityLevel()
        {
            return Send(new SendCommand(Commands.GetSecurityLevel));
        }

        public Commands SetSecurityLevel(ushort level)
        {
            return Send(new SendCommand(Commands.SetSecurityLevel, level));
        }

        public Commands GetFingerTimeOut()
        {
            return Send(new SendCommand(Commands.GetFingerTimeOut));
        }

        public Commands ReadTemplate(ushort id)
        {
            return Send(new SendCommand(Commands.ReadTemplate, id));
        }

        public Commands WriteTemplate()
        {
            return Send(new SendCommand(Commands.WriteTemplate, 498));
        }

        public Commands WriteTemplateData(ushort id, byte[] template)
        {
            if (template.Length > 498) throw new Exception($"Template {id} biometrico excede tamanho esperado.");

            var data = new byte[2 + template.Length];
            data[0] = (byte)id;
            data[1] = (byte)(id >> 8);
            Array.Copy(template, 0, data, 2, template.Length);

            return Send(new SendCommand(Commands.WriteTemplate, data));
        }

        public Commands TestConnection()
        {
            return Send(new SendCommand(Commands.TestConnection));
        }

        private new Commands Send(SendCommand sendCommand)
        {
            if (Enrolling && sendCommand.Command != Commands.FPCancel)
            {
                Logger.Debug(
                    $" Bio enviando comando {sendCommand.Command} enquanto cadastrando, enviado FPCancel antes.");
                
                base.Send(new SendCommand(Commands.FPCancel));
            }

            return base.Send(sendCommand);
        }
    }
}
