using System;
using EnumsNET;
using Toletus.Extensions;
using Toletus.SM25.Command;
using Toletus.SM25.Command.Enums;

namespace Toletus.SM25;

public partial class SM25Reader
{
    private ReaderResponseCommand? _responseCommand;

    private void ProcessResponse(byte[] response)
    {
        try
        {
            Log?.Invoke($" SM25 {Ip} < Raw Response {response.ToHexString(" ")} Length {response.Length}");

            while (response.Length > 0)
            {
                if (_responseCommand == null)
                    _responseCommand = new ReaderResponseCommand(ref response);
                else
                    _responseCommand.Add(ref response);

                lock (_responseCommand)
                {
                    if (!_responseCommand.IsResponseComplete)
                        continue;

                    Log?.Invoke($" SM25 {Ip} < { _responseCommand }");
                    ProcessResponseCommand(_responseCommand);
                    _responseCommand = null;
                }
            }
        }
        catch (Exception e)
        {
            Log?.Invoke($"{nameof(ProcessResponse)} { e.ToLogString(Environment.StackTrace) }");
            throw;
        }
    }

    private void ProcessResponseCommand(ReaderResponseCommand readerResponseCommand)
    {
        OnResponse?.Invoke(readerResponseCommand);

        if (LastReaderSendCommand != null)
            if (LastReaderSendCommand.Command == readerResponseCommand.Command || LastReaderSendCommand.Command == SM25Commands.FPCancel &&
                (readerResponseCommand.Command == SM25Commands.Enroll || readerResponseCommand.Command == SM25Commands.EnrollAndStoreinRAM || readerResponseCommand.Command == SM25Commands.Identify))
                LastReaderSendCommand.ReaderResponseCommand = readerResponseCommand;

        try
        {
            ValidateChecksum(readerResponseCommand);

            switch (readerResponseCommand.Command)
            {
                case SM25Commands.Enroll:
                case SM25Commands.EnrollAndStoreinRAM:
                    ProcessEnrollResponse(readerResponseCommand);
                    break;
                case SM25Commands.GetEmptyID:
                    ProcessEmptyIdResponse(readerResponseCommand);
                    break;
                case SM25Commands.ClearTemplate:
                    PreccessClearTemplateResponse(readerResponseCommand);
                    break;
                case SM25Commands.ClearAllTemplate:
                    ProcessClearAllTemplatesResponse(readerResponseCommand);
                    break;
                case SM25Commands.GetTemplateStatus:
                    ProcessTemplateStatusResponse(readerResponseCommand);
                    break;
                default:
                    var status = $"{readerResponseCommand.Command.AsString(EnumFormat.Description)} {readerResponseCommand.Data}";
                    SendStatus(status);
                    break;
            }
        }
        catch (ObjectDisposedException)
        {
            /* continue */
        }
    }

    private void ProcessTemplateStatusResponse(ReaderResponseCommand readerResponseCommand)
    {
        if (readerResponseCommand.Command == SM25Commands.GetTemplateStatus)
            readerResponseCommand.DataTemplateStatus = (TemplateStatus)readerResponseCommand.Data;

        SendStatus(readerResponseCommand.ReturnCode == ReturnCodes.ERR_SUCCESS
            ? $"{readerResponseCommand.DataTemplateStatus}"
            : $"{readerResponseCommand.DataReturnCode}");
    }

    private void ProcessClearAllTemplatesResponse(ReaderResponseCommand readerResponseCommand)
    {
        if (readerResponseCommand.ReturnCode == ReturnCodes.ERR_SUCCESS)
            SendStatus($"All tremplates removed. Qt. {readerResponseCommand.Data}");
        else
            SendStatus($"Can't remove all templates. {((ReturnCodes)readerResponseCommand.Data).AsString(EnumFormat.Description)}");
    }

    private void PreccessClearTemplateResponse(ReaderResponseCommand readerResponseCommand)
    {
        if (readerResponseCommand.ReturnCode == ReturnCodes.ERR_SUCCESS)
            SendStatus($"Template {readerResponseCommand.Data} removed");
        else
        {
            if (readerResponseCommand.Data == (ushort)ReturnCodes.ERR_TMPL_EMPTY)
                SendStatus($"Empty template");
            else
                SendStatus($"Can't remove template. {((ReturnCodes)readerResponseCommand.Data).AsString(EnumFormat.Description)}");
        }
    }

    private void ProcessEmptyIdResponse(ReaderResponseCommand readerResponseCommand)
    {
        SendStatus($"ID available {readerResponseCommand.Data}");
        OnIdAvailable?.Invoke(readerResponseCommand.Data);
    }

    private void ProcessEnrollResponse(ReaderResponseCommand readerResponseCommand)
    {
        var enrollStatus = new EnrollStatus { Ret = readerResponseCommand.ReturnCode, DataGD = readerResponseCommand.DataGD, DataReturnCode = readerResponseCommand.DataReturnCode };

        if (readerResponseCommand.ReturnCode == ReturnCodes.ERR_SUCCESS)
            ProcessEnrollResponseSuccess(readerResponseCommand, enrollStatus);
        else if (readerResponseCommand.ReturnCode == ReturnCodes.ERR_FAIL)
            ProcessEnrollResponseFail(readerResponseCommand, enrollStatus);

        OnEnrollStatus?.Invoke(enrollStatus);
    }

    private void ProcessEnrollResponseFail(ReaderResponseCommand readerResponseCommand, EnrollStatus enrollStatus)
    {
        switch (readerResponseCommand.DataReturnCode)
        {
            case ReturnCodes.ERR_TMPL_NOT_EMPTY:
                SendStatus("Template already enrolled");
                break;
            case ReturnCodes.ERR_BAD_QUALITY:
                SendStatus($"Bad quality, put your finger again");
                break;
            case ReturnCodes.ERR_GENERALIZE:
                Enrolling = false;
                SendStatus("Generalization error");
                OnGeneralizationFail?.Invoke();
                break;
            case ReturnCodes.ERR_TIME_OUT:
                Enrolling = false;
                SendStatus("Timeout");
                OnEnrollTimeout?.Invoke();
                break;
            case ReturnCodes.ERR_DUPLICATION_ID:
                SendStatus($"Id duplicated with {readerResponseCommand.Data >> 8}");
                enrollStatus.Data = readerResponseCommand.Data >> 8;
                break;
            case ReturnCodes.ERR_FP_CANCEL:
                Enrolling = false;
                OnEnroll?.Invoke(-1);
                SendStatus($"Canceled");
                break;
            default:
                break;
        }
    }

    private void ProcessEnrollResponseSuccess(ReaderResponseCommand readerResponseCommand, EnrollStatus enrollStatus)
    {
        switch (readerResponseCommand.DataGD)
        {
            case GDCodes.GD_NEED_FIRST_SWEEP:
                Enrolling = true;
                OnEnroll?.Invoke(1);
                SendStatus("Put your finger for the first time");
                break;
            case GDCodes.GD_NEED_SECOND_SWEEP:
                OnEnroll?.Invoke(2);
                SendStatus("Put your finger for the second time");
                break;
            case GDCodes.GD_NEED_THIRD_SWEEP:
                OnEnroll?.Invoke(3);
                SendStatus("Put your finger for the third time");
                break;
            case GDCodes.GD_NEED_RELEASE_FINGER:
                SendStatus("Take off your finger");
                break;
            default:
                Enrolling = false;
                OnEnroll?.Invoke(4);
                SendStatus($"Enroll {readerResponseCommand.Data}");
                enrollStatus.Data = readerResponseCommand.Data;
                break;
        }
    }

    private static void ValidateChecksum(ReaderResponseCommand readerResponseCommand)
    {
        if (readerResponseCommand.ChecksumIsValid) return;

        var msg =
            $"Response checksum is invalid. Response {readerResponseCommand.Payload.ToHexString(" ")} (Expected checksum {readerResponseCommand.ChecksumFromReturn} <> Checksum {readerResponseCommand.ChecksumCalculated})";

        Log?.Invoke(msg);
        throw new Exception(msg);
    }

    private void SendStatus(string status)
    {
        Log?.Invoke($" SM25 {Ip} Status {status}");
        OnStatus?.Invoke(status);
    }
}