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
    private ReaderResponse? _responseCommand;

    public Sync(SM25Reader scanner)
    {
        _scanner = scanner;
        _scanner.OnResponse += ScannerOnResponse;
    }

    private void ScannerOnResponse(ReaderResponse? responseCommand)
    {
        if (responseCommand?.Command == _sm25CommandToWait)
            _responseCommand = responseCommand;
    }

    public ReaderResponse? GetDeviceName()
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

    private ReaderResponse? GetReponse(SM25Commands sm25Command)
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

    public ReaderResponse? GetFWVersion()
    {
        BeforeSend(SM25Commands.GetFWVersion);
        return GetReponse(_scanner.GetFWVersion());
    }

    public ReaderResponse? GetDeviceId()
    {
        BeforeSend(SM25Commands.GetDeviceID);
        return GetReponse(_scanner.GetDeviceId());
    }

    public ReaderResponse? GetEmptyID()
    {
        BeforeSend(SM25Commands.GetEmptyID);
        return GetReponse(_scanner.GetEmptyID());
    }

    public ReaderResponse? Enroll(ushort id)
    {
        BeforeSend(SM25Commands.Enroll);
        return GetReponse(_scanner.Enroll(id));
    }

    public ReaderResponse? EnrollAndStoreinRAM()
    {
        BeforeSend(SM25Commands.EnrollAndStoreinRAM);
        return GetReponse(_scanner.EnrollAndStoreinRAM());
    }

    public ReaderResponse GetEnrollData()
    {
        throw new NotImplementedException();
    }

    public ReaderResponse? GetEnrollCount()
    {
        BeforeSend(SM25Commands.GetEnrollCount);
        return GetReponse(_scanner.GetEnrollCount());
    }

    public ReaderResponse? ClearTemplate(ushort id)
    {
        BeforeSend(SM25Commands.ClearTemplate);
        return GetReponse(_scanner.ClearTemplate(id));
    }

    public ReaderResponse GetTemplateStatus(ushort id)
    {
        throw new NotImplementedException();
    }

    public ReaderResponse? ClearAllTemplate()
    {
        BeforeSend(SM25Commands.ClearAllTemplate);
        return GetReponse(_scanner.ClearAllTemplate());
    }

    public ReaderResponse SetDeviceId(ushort i)
    {
        throw new NotImplementedException();
    }

    public ReaderResponse? SetFingerTimeOut(ushort i)
    {
        BeforeSend(SM25Commands.SetFingerTimeOut);
        return GetReponse(_scanner.SetFingerTimeOut(i));
    }

    public ReaderResponse FPCancel()
    {
        throw new NotImplementedException();
    }

    public ReaderResponse? GetDuplicationCheck()
    {
        BeforeSend(SM25Commands.GetDuplicationCheck);
        return GetReponse(_scanner.GetDuplicationCheck());
    }

    public ReaderResponse SetDuplicationCheck(bool check)
    {
        throw new NotImplementedException();
    }

    public ReaderResponse? GetSecurityLevel()
    {
        BeforeSend(SM25Commands.GetSecurityLevel);
        return GetReponse(_scanner.GetSecurityLevel());
    }

    public ReaderResponse SetSecurityLevel(ushort level)
    {
        throw new NotImplementedException();
    }

    public ReaderResponse GetFingerTimeOut()
    {
        throw new NotImplementedException();
    }

    public ReaderResponse ReadTemplate(ushort id)
    {
        throw new NotImplementedException();
    }

    public ReaderResponse? WriteTemplate()
    {
        BeforeSend(SM25Commands.WriteTemplate);
        return GetReponse(_scanner.WriteTemplate());
    }

    public ReaderResponse? WriteTemplateData(ushort id, byte[] template)
    {
        BeforeSend(SM25Commands.WriteTemplate);
        return GetReponse(_scanner.WriteTemplateData(id, template));
    }

    public void Dispose()
    {
        _scanner.OnResponse -= ScannerOnResponse;
    }

    public ReaderResponse? TestConnection()
    {
        BeforeSend(SM25Commands.TestConnection);
        return GetReponse(_scanner.TestConnection());
    }
}