using System;
using EnumsNET;
using Toletus.Extensions;
using Toletus.SM25.Command;
using Toletus.SM25.Command.Enums;

namespace Toletus.SM25;

public partial class SM25Reader
{
    private ReaderResponse? _responseCommand;

    private void ProcessResponse(byte[] response)
    {
        try
        {
            Log?.Invoke($" SM25 {Ip} < Raw Response {response.ToHexString(" ")} Length {response.Length}");

            while (response.Length > 0)
            {
                if (_responseCommand == null)
                    _responseCommand = new ReaderResponse(ref response);
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

    private void ProcessResponseCommand(ReaderResponse readerResponse)
    {
        OnResponse?.Invoke(readerResponse);

        if (LastReaderSend != null)
            if (LastReaderSend.Command == readerResponse.Command || LastReaderSend.Command == SM25Commands.FPCancel &&
                (readerResponse.Command == SM25Commands.Enroll || readerResponse.Command == SM25Commands.EnrollAndStoreinRAM || readerResponse.Command == SM25Commands.Identify))
                LastReaderSend.ReaderResponse = readerResponse;

        try
        {
            ValidateChecksum(readerResponse);

            switch (readerResponse.Command)
            {
                case SM25Commands.Enroll:
                case SM25Commands.EnrollAndStoreinRAM:
                    ProcessEnrollResponse(readerResponse);
                    break;
                case SM25Commands.GetEmptyID:
                    ProcessEmptyIdResponse(readerResponse);
                    break;
                case SM25Commands.ClearTemplate:
                    PreccessClearTemplateResponse(readerResponse);
                    break;
                case SM25Commands.ClearAllTemplate:
                    ProcessClearAllTemplatesResponse(readerResponse);
                    break;
                case SM25Commands.GetTemplateStatus:
                    ProcessTemplateStatusResponse(readerResponse);
                    break;
                default:
                    var status = $"{readerResponse.Command.AsString(EnumFormat.Description)} {readerResponse.Data}";
                    SendStatus(status);
                    break;
            }
        }
        catch (ObjectDisposedException)
        {
            /* continue */
        }
    }

    private void ProcessTemplateStatusResponse(ReaderResponse readerResponse)
    {
        if (readerResponse.Command == SM25Commands.GetTemplateStatus)
            readerResponse.DataTemplateStatus = (TemplateStatus)readerResponse.Data;

        SendStatus(readerResponse.ReturnCode == ReturnCodes.ERR_SUCCESS
            ? $"{readerResponse.DataTemplateStatus}"
            : $"{readerResponse.DataReturnCode}");
    }

    private void ProcessClearAllTemplatesResponse(ReaderResponse readerResponse)
    {
        if (readerResponse.ReturnCode == ReturnCodes.ERR_SUCCESS)
            SendStatus($"All tremplates removed. Qt. {readerResponse.Data}");
        else
            SendStatus($"Can't remove all templates. {((ReturnCodes)readerResponse.Data).AsString(EnumFormat.Description)}");
    }

    private void PreccessClearTemplateResponse(ReaderResponse readerResponse)
    {
        if (readerResponse.ReturnCode == ReturnCodes.ERR_SUCCESS)
            SendStatus($"Template {readerResponse.Data} removed");
        else
        {
            if (readerResponse.Data == (ushort)ReturnCodes.ERR_TMPL_EMPTY)
                SendStatus($"Empty template");
            else
                SendStatus($"Can't remove template. {((ReturnCodes)readerResponse.Data).AsString(EnumFormat.Description)}");
        }
    }

    private void ProcessEmptyIdResponse(ReaderResponse readerResponse)
    {
        SendStatus($"ID available {readerResponse.Data}");
        OnIdAvailable?.Invoke(readerResponse.Data);
    }

    private void ProcessEnrollResponse(ReaderResponse readerResponse)
    {
        var enrollStatus = new EnrollStatus { Ret = readerResponse.ReturnCode, DataGD = readerResponse.DataGD, DataReturnCode = readerResponse.DataReturnCode };

        if (readerResponse.ReturnCode == ReturnCodes.ERR_SUCCESS)
            ProcessEnrollResponseSuccess(readerResponse, enrollStatus);
        else if (readerResponse.ReturnCode == ReturnCodes.ERR_FAIL)
            ProcessEnrollResponseFail(readerResponse, enrollStatus);

        OnEnrollStatus?.Invoke(enrollStatus);
    }

    private void ProcessEnrollResponseFail(ReaderResponse readerResponse, EnrollStatus enrollStatus)
    {
        switch (readerResponse.DataReturnCode)
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
                SendStatus($"Id duplicated with {readerResponse.Data >> 8}");
                enrollStatus.Data = readerResponse.Data >> 8;
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

    private void ProcessEnrollResponseSuccess(ReaderResponse readerResponse, EnrollStatus enrollStatus)
    {
        switch (readerResponse.DataGD)
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
                SendStatus($"Enroll {readerResponse.Data}");
                enrollStatus.Data = readerResponse.Data;
                break;
        }
    }

    private static void ValidateChecksum(ReaderResponse readerResponse)
    {
        if (readerResponse.ChecksumIsValid) return;

        var msg =
            $"Response checksum is invalid. Response {readerResponse.Payload.ToHexString(" ")} (Expected checksum {readerResponse.ChecksumFromReturn} <> Checksum {readerResponse.ChecksumCalculated})";

        Log?.Invoke(msg);
        throw new Exception(msg);
    }

    private void SendStatus(string status)
    {
        Log?.Invoke($" SM25 {Ip} Status {status}");
        OnStatus?.Invoke(status);
    }
}