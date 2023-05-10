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
    private ReaderResponseCommand? _responseCommand;

    public Sync(SM25Reader scanner)
    {
        _scanner = scanner;
        _scanner.OnResponse += ScannerOnResponse;
    }

    private void ScannerOnResponse(ReaderResponseCommand? responseCommand)
    {
        if (responseCommand?.Command == _sm25CommandToWait)
            _responseCommand = responseCommand;
    }

    public ReaderResponseCommand? GetDeviceName()
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

    private ReaderResponseCommand? GetReponse(SM25Commands sm25Command)
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

    public ReaderResponseCommand? GetFWVersion()
    {
        BeforeSend(SM25Commands.GetFWVersion);
        return GetReponse(_scanner.GetFWVersion());
    }

    public ReaderResponseCommand? GetDeviceId()
    {
        BeforeSend(SM25Commands.GetDeviceID);
        return GetReponse(_scanner.GetDeviceId());
    }

    public ReaderResponseCommand? GetEmptyID()
    {
        BeforeSend(SM25Commands.GetEmptyID);
        return GetReponse(_scanner.GetEmptyID());
    }

    public ReaderResponseCommand? Enroll(ushort id)
    {
        BeforeSend(SM25Commands.Enroll);
        return GetReponse(_scanner.Enroll(id));
    }

    public ReaderResponseCommand? EnrollAndStoreinRAM()
    {
        BeforeSend(SM25Commands.EnrollAndStoreinRAM);
        return GetReponse(_scanner.EnrollAndStoreinRAM());
    }

    public ReaderResponseCommand GetEnrollData()
    {
        throw new NotImplementedException();
    }

    public ReaderResponseCommand? GetEnrollCount()
    {
        BeforeSend(SM25Commands.GetEnrollCount);
        return GetReponse(_scanner.GetEnrollCount());
    }

    public ReaderResponseCommand? ClearTemplate(ushort id)
    {
        BeforeSend(SM25Commands.ClearTemplate);
        return GetReponse(_scanner.ClearTemplate(id));
    }

    public ReaderResponseCommand GetTemplateStatus(ushort id)
    {
        throw new NotImplementedException();
    }

    public ReaderResponseCommand? ClearAllTemplate()
    {
        BeforeSend(SM25Commands.ClearAllTemplate);
        return GetReponse(_scanner.ClearAllTemplate());
    }

    public ReaderResponseCommand SetDeviceId(ushort i)
    {
        throw new NotImplementedException();
    }

    public ReaderResponseCommand? SetFingerTimeOut(ushort i)
    {
        BeforeSend(SM25Commands.SetFingerTimeOut);
        return GetReponse(_scanner.SetFingerTimeOut(i));
    }

    public ReaderResponseCommand FPCancel()
    {
        throw new NotImplementedException();
    }

    public ReaderResponseCommand? GetDuplicationCheck()
    {
        BeforeSend(SM25Commands.GetDuplicationCheck);
        return GetReponse(_scanner.GetDuplicationCheck());
    }

    public ReaderResponseCommand SetDuplicationCheck(bool check)
    {
        throw new NotImplementedException();
    }

    public ReaderResponseCommand? GetSecurityLevel()
    {
        BeforeSend(SM25Commands.GetSecurityLevel);
        return GetReponse(_scanner.GetSecurityLevel());
    }

    public ReaderResponseCommand SetSecurityLevel(ushort level)
    {
        throw new NotImplementedException();
    }

    public ReaderResponseCommand GetFingerTimeOut()
    {
        throw new NotImplementedException();
    }

    public ReaderResponseCommand ReadTemplate(ushort id)
    {
        throw new NotImplementedException();
    }

    public ReaderResponseCommand? WriteTemplate()
    {
        BeforeSend(SM25Commands.WriteTemplate);
        return GetReponse(_scanner.WriteTemplate());
    }

    public ReaderResponseCommand? WriteTemplateData(ushort id, byte[] template)
    {
        BeforeSend(SM25Commands.WriteTemplate);
        return GetReponse(_scanner.WriteTemplateData(id, template));
    }

    public void Dispose()
    {
        _scanner.OnResponse -= ScannerOnResponse;
    }

    public ReaderResponseCommand? TestConnection()
    {
        BeforeSend(SM25Commands.TestConnection);
        return GetReponse(_scanner.TestConnection());
    }
}