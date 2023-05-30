using System;
using System.Diagnostics;
using System.Threading;
using Toletus.SM25.Base;
using Toletus.SM25.Command;
using Toletus.SM25.Command.Enums;

namespace Toletus.SM25;

public class Sync : IDisposable
{
    private readonly SM25Reader _scanner;
    private SM25Commands _sm25CommandToWait;
    private SM25Response? _responseCommand;

    public Sync(SM25Reader scanner)
    {
        _scanner = scanner;
        _scanner.OnResponse += ScannerOnResponse;
    }

    private void ScannerOnResponse(SM25Response? responseCommand)
    {
        if (responseCommand?.Command == _sm25CommandToWait)
            _responseCommand = responseCommand;
    }

    public SM25Response? GetDeviceName()
    {
        BeforeSend(SM25Commands.GetDeviceName);
        return GetReponse(_scanner.GetDeviceName());
    }

    private void BeforeSend(SM25Commands sm25Command)
    {
        _responseCommand = null;

        if (!_scanner.Enrolling) return;

        SM25ReaderBase.Log?.Invoke($" SM25 {_scanner.Ip} < Sending {sm25Command} while erolling. Was sent {nameof(_scanner.FPCancel)} before.");
        _scanner.FPCancel();
    }

    private SM25Response? GetReponse(SM25Commands sm25Command)
    {
        var sw = new Stopwatch();
        sw.Start();

        _sm25CommandToWait = sm25Command;

        while (_responseCommand == null && sw.Elapsed.TotalSeconds < 5)
        {
            Thread.Sleep(100);
        }

        sw.Stop();

        SM25ReaderBase.Log?.Invoke($" SM25 {_scanner.Ip} < Proccess response total seconds {sw.Elapsed.TotalSeconds}");

        return _responseCommand;
    }

    public SM25Response? GetFWVersion()
    {
        BeforeSend(SM25Commands.GetFWVersion);
        return GetReponse(_scanner.GetFWVersion());
    }

    public SM25Response? GetDeviceId()
    {
        BeforeSend(SM25Commands.GetDeviceID);
        return GetReponse(_scanner.GetDeviceId());
    }

    public SM25Response? GetEmptyID()
    {
        BeforeSend(SM25Commands.GetEmptyID);
        return GetReponse(_scanner.GetEmptyID());
    }

    public SM25Response? Enroll(ushort id)
    {
        BeforeSend(SM25Commands.Enroll);
        return GetReponse(_scanner.Enroll(id));
    }

    public SM25Response? EnrollAndStoreinRAM()
    {
        BeforeSend(SM25Commands.EnrollAndStoreinRAM);
        return GetReponse(_scanner.EnrollAndStoreinRAM());
    }

    public SM25Response GetEnrollData()
    {
        throw new NotImplementedException();
    }

    public SM25Response? GetEnrollCount()
    {
        BeforeSend(SM25Commands.GetEnrollCount);
        return GetReponse(_scanner.GetEnrollCount());
    }

    public SM25Response? ClearTemplate(ushort id)
    {
        BeforeSend(SM25Commands.ClearTemplate);
        return GetReponse(_scanner.ClearTemplate(id));
    }

    public SM25Response GetTemplateStatus(ushort id)
    {
        throw new NotImplementedException();
    }

    public SM25Response? ClearAllTemplate()
    {
        BeforeSend(SM25Commands.ClearAllTemplate);
        return GetReponse(_scanner.ClearAllTemplate());
    }

    public SM25Response SetDeviceId(ushort i)
    {
        throw new NotImplementedException();
    }

    public SM25Response? SetFingerTimeOut(ushort i)
    {
        BeforeSend(SM25Commands.SetFingerTimeOut);
        return GetReponse(_scanner.SetFingerTimeOut(i));
    }

    public SM25Response FPCancel()
    {
        throw new NotImplementedException();
    }

    public SM25Response? GetDuplicationCheck()
    {
        BeforeSend(SM25Commands.GetDuplicationCheck);
        return GetReponse(_scanner.GetDuplicationCheck());
    }

    public SM25Response SetDuplicationCheck(bool check)
    {
        throw new NotImplementedException();
    }

    public SM25Response? GetSecurityLevel()
    {
        BeforeSend(SM25Commands.GetSecurityLevel);
        return GetReponse(_scanner.GetSecurityLevel());
    }

    public SM25Response SetSecurityLevel(ushort level)
    {
        throw new NotImplementedException();
    }

    public SM25Response GetFingerTimeOut()
    {
        throw new NotImplementedException();
    }

    public SM25Response ReadTemplate(ushort id)
    {
        throw new NotImplementedException();
    }

    public SM25Response? WriteTemplate()
    {
        BeforeSend(SM25Commands.WriteTemplate);
        return GetReponse(_scanner.WriteTemplate());
    }

    public SM25Response? WriteTemplateData(ushort id, byte[] template)
    {
        BeforeSend(SM25Commands.WriteTemplate);
        return GetReponse(_scanner.WriteTemplateData(id, template));
    }

    public void Dispose()
    {
        _scanner.OnResponse -= ScannerOnResponse;
    }

    public SM25Response? TestConnection()
    {
        BeforeSend(SM25Commands.TestConnection);
        return GetReponse(_scanner.TestConnection());
    }
}