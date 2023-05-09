using System;
using System.Diagnostics;
using System.Threading;
using Toletus.SM25.Command;
using Toletus.SM25.Command.Enums;

namespace Toletus.SM25
{
    public class Sync : IDisposable
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly SM25Reader _scanner;
        private Commands _commandToWait;
        private ResponseCommand _responseCommand;

        public Sync(SM25Reader scanner)
        {
            _scanner = scanner;
            _scanner.OnResponse += ScannerOnResponse;
        }

        private void ScannerOnResponse(ResponseCommand responseCommand)
        {
            if (responseCommand.Command == _commandToWait)
                _responseCommand = responseCommand;
        }

        public ResponseCommand GetDeviceName()
        {
            BeforeSend(Commands.GetDeviceName);
            return GetReponse(_scanner.GetDeviceName());
        }

        private void BeforeSend(Commands command)
        {
            _responseCommand = null;

            if (!_scanner.Enrolling) return;

            Logger.Debug($" SM25 {_scanner.Ip} < Sending {command} while erolling. Was sent {nameof(_scanner.FPCancel)} before.");
            _scanner.FPCancel();
        }

        private ResponseCommand GetReponse(Commands command)
        {
            var sw = new Stopwatch();
            sw.Start();

            _commandToWait = command;

            while (_responseCommand == null && sw.Elapsed.TotalSeconds < 5)
            {
                Thread.Sleep(100);
            }

            sw.Stop();

            Logger.Debug($" SM25 {_scanner.Ip} < Proccess response total seconds {sw.Elapsed.TotalSeconds}");

            return _responseCommand;
        }

        public ResponseCommand GetFWVersion()
        {
            BeforeSend(Commands.GetFWVersion);
            return GetReponse(_scanner.GetFWVersion());
        }

        public ResponseCommand GetDeviceId()
        {
            BeforeSend(Commands.GetDeviceID);
            return GetReponse(_scanner.GetDeviceId());
        }

        public ResponseCommand GetEmptyID()
        {
            BeforeSend(Commands.GetEmptyID);
            return GetReponse(_scanner.GetEmptyID());
        }

        public ResponseCommand Enroll(ushort id)
        {
            BeforeSend(Commands.Enroll);
            return GetReponse(_scanner.Enroll(id));
        }

        public ResponseCommand EnrollAndStoreinRAM()
        {
            BeforeSend(Commands.EnrollAndStoreinRAM);
            return GetReponse(_scanner.EnrollAndStoreinRAM());
        }

        public ResponseCommand GetEnrollData()
        {
            throw new NotImplementedException();
        }

        public ResponseCommand GetEnrollCount()
        {
            BeforeSend(Commands.GetEnrollCount);
            return GetReponse(_scanner.GetEnrollCount());
        }

        public ResponseCommand ClearTemplate(ushort id)
        {
            BeforeSend(Commands.ClearTemplate);
            return GetReponse(_scanner.ClearTemplate(id));
        }

        public ResponseCommand GetTemplateStatus(ushort id)
        {
            throw new NotImplementedException();
        }

        public ResponseCommand ClearAllTemplate()
        {
            BeforeSend(Commands.ClearAllTemplate);
            return GetReponse(_scanner.ClearAllTemplate());
        }

        public ResponseCommand Desconectar()
        {
            throw new NotImplementedException();
        }

        public ResponseCommand SetDeviceId(ushort i)
        {
            throw new NotImplementedException();
        }

        public ResponseCommand SetFingerTimeOut(ushort i)
        {
            BeforeSend(Commands.SetFingerTimeOut);
            return GetReponse(_scanner.SetFingerTimeOut(i));
        }

        public ResponseCommand Configurar()
        {
            throw new NotImplementedException();
        }

        public ResponseCommand FPCancel()
        {
            throw new NotImplementedException();
        }

        public ResponseCommand GetDuplicationCheck()
        {
            BeforeSend(Commands.GetDuplicationCheck);
            return GetReponse(_scanner.GetDuplicationCheck());
        }

        public ResponseCommand SetDuplicationCheck(bool check)
        {
            throw new NotImplementedException();
        }

        public ResponseCommand GetSecurityLevel()
        {
            BeforeSend(Commands.GetSecurityLevel);
            return GetReponse(_scanner.GetSecurityLevel());
        }

        public ResponseCommand SetSecurityLevel(ushort level)
        {
            throw new NotImplementedException();
        }

        public ResponseCommand GetFingerTimeOut()
        {
            throw new NotImplementedException();
        }

        public ResponseCommand ReadTemplate(ushort id)
        {
            throw new NotImplementedException();
        }

        public ResponseCommand WriteTemplate()
        {
            BeforeSend(Commands.WriteTemplate);
            return GetReponse(_scanner.WriteTemplate());
        }

        public ResponseCommand WriteTemplateData(ushort id, byte[] template)
        {
            BeforeSend(Commands.WriteTemplate);
            return GetReponse(_scanner.WriteTemplateData(id, template));
        }

        public void Dispose()
        {
            _scanner.OnResponse -= ScannerOnResponse;
        }

        public ResponseCommand TestConnection()
        {
            BeforeSend(Commands.TestConnection);
            return GetReponse(_scanner.TestConnection());
        }
    }
}
