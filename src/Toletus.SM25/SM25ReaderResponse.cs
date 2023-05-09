using System;
using EnumsNET;
using Toletus.Extensions;
using Toletus.SM25.Command;
using Toletus.SM25.Command.Enums;

namespace Toletus.SM25;

public partial class SM25Reader
{
    private ResponseCommand _responseCommand;

    private void ProcessResponse(byte[] response)
    {
        try
        {
            Log?.Invoke($" SM25 {Ip} < Raw Response {response.ToHexString(" ")} Length {response.Length}");

            while (response.Length > 0)
            {
                if (_responseCommand == null)
                    _responseCommand = new ResponseCommand(ref response);
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

    private void ProcessResponseCommand(ResponseCommand responseCommand)
    {
        OnResponse?.Invoke(responseCommand);

        if (LastSendCommand != null)
            if (LastSendCommand.Command == responseCommand.Command || LastSendCommand.Command == Commands.FPCancel &&
                (responseCommand.Command == Commands.Enroll || responseCommand.Command == Commands.EnrollAndStoreinRAM || responseCommand.Command == Commands.Identify))
                LastSendCommand.ResponseCommand = responseCommand;

        try
        {
            ValidateChecksum(responseCommand);

            switch (responseCommand.Command)
            {
                case Commands.Enroll:
                case Commands.EnrollAndStoreinRAM:
                    ProcessEnrollResponse(responseCommand);
                    break;
                case Commands.GetEmptyID:
                    ProcessEmptyIdResponse(responseCommand);
                    break;
                case Commands.ClearTemplate:
                    PreccessClearTemplateResponse(responseCommand);
                    break;
                case Commands.ClearAllTemplate:
                    ProcessClearAllTemplatesResponse(responseCommand);
                    break;
                case Commands.GetTemplateStatus:
                    ProcessTemplateStatusResponse(responseCommand);
                    break;
                default:
                    var status = $"{responseCommand.Command.AsString(EnumFormat.Description)} {responseCommand.Data}";
                    SendStatus(status);
                    break;
            }
        }
        catch (ObjectDisposedException e)
        {
            /* continue */
        }
    }


    private void ProcessTemplateStatusResponse(ResponseCommand responseCommand)
    {
        if (responseCommand.Command == Commands.GetTemplateStatus)
            responseCommand.DataTemplateStatus = (TemplateStatus)responseCommand.Data;

        SendStatus(responseCommand.ReturnCode == ReturnCodes.ERR_SUCCESS
            ? $"{responseCommand.DataTemplateStatus}"
            : $"{responseCommand.DataReturnCode}");
    }

    private void ProcessClearAllTemplatesResponse(ResponseCommand responseCommand)
    {
        if (responseCommand.ReturnCode == ReturnCodes.ERR_SUCCESS)
            SendStatus($"All tremplates removed. Qt. {responseCommand.Data}");
        else
            SendStatus($"Can't remove all templates. {((ReturnCodes)responseCommand.Data).AsString(EnumFormat.Description)}");
    }

    private void PreccessClearTemplateResponse(ResponseCommand responseCommand)
    {
        if (responseCommand.ReturnCode == ReturnCodes.ERR_SUCCESS)
            SendStatus($"Template {responseCommand.Data} removed");
        else
        {
            if (responseCommand.Data == (ushort)ReturnCodes.ERR_TMPL_EMPTY)
                SendStatus($"Empty template");
            else
                SendStatus($"Can't remove template. {((ReturnCodes)responseCommand.Data).AsString(EnumFormat.Description)}");
        }
    }

    private void ProcessEmptyIdResponse(ResponseCommand responseCommand)
    {
        SendStatus($"ID available {responseCommand.Data}");
        OnIdAvailable?.Invoke(responseCommand.Data);
    }

    private void ProcessEnrollResponse(ResponseCommand responseCommand)
    {
        var enrollStatus = new EnrollStatus { Ret = responseCommand.ReturnCode, DataGD = responseCommand.DataGD, DataReturnCode = responseCommand.DataReturnCode };

        if (responseCommand.ReturnCode == ReturnCodes.ERR_SUCCESS)
            ProcessEnrollResponseSuccess(responseCommand, enrollStatus);
        else if (responseCommand.ReturnCode == ReturnCodes.ERR_FAIL)
            ProcessEnrollResponseFail(responseCommand, enrollStatus);

        OnEnrollStatus?.Invoke(enrollStatus);
    }

    private void ProcessEnrollResponseFail(ResponseCommand responseCommand, EnrollStatus enrollStatus)
    {
        switch (responseCommand.DataReturnCode)
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
                SendStatus($"Id duplicated with {responseCommand.Data >> 8}");
                enrollStatus.Data = responseCommand.Data >> 8;
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

    private void ProcessEnrollResponseSuccess(ResponseCommand responseCommand, EnrollStatus enrollStatus)
    {
        switch (responseCommand.DataGD)
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
                SendStatus($"Enroll {responseCommand.Data}");
                enrollStatus.Data = responseCommand.Data;
                break;
        }
    }

    private static void ValidateChecksum(ResponseCommand responseCommand)
    {
        if (responseCommand.ChecksumIsValid) return;

        var msg =
            $"Response checksum is invalid. Response {responseCommand.Payload.ToHexString(" ")} (Expected checksum {responseCommand.ChecksumFromReturn} <> Checksum {responseCommand.ChecksumCalculated})";

        Log?.Invoke(msg);
        throw new Exception(msg);
    }

    private void SendStatus(string status)
    {
        Log?.Invoke($" SM25 {Ip} Status {status}");
        OnStatus?.Invoke(status);
    }
}