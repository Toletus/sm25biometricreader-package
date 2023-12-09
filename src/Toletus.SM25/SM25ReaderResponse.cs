using System;
using EnumsNET;
using Toletus.Pack.Core;
using Toletus.Pack.Core.Extensions;
using Toletus.SM25.Command;
using Toletus.SM25.Command.Enums;

namespace Toletus.SM25;

public partial class SM25Reader
{
    private SM25Response? _responseCommand;

    private void ProcessResponse(byte[] response)
    {
        try
        {
            Log?.Invoke($" SM25 {Ip} < Raw Response {response.ToHexString(" ")} Length {response.Length}");

            while (response.Length > 0)
            {
                if (_responseCommand == null)
                    _responseCommand = new SM25Response(ref response);
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

    private void ProcessResponseCommand(SM25Response sm25Response)
    {
        OnResponse?.Invoke(sm25Response);

        if (LastSm25Send != null)
            if (LastSm25Send.Command == sm25Response.Command || LastSm25Send.Command == SM25Commands.FPCancel &&
                (sm25Response.Command == SM25Commands.Enroll || sm25Response.Command == SM25Commands.EnrollAndStoreinRAM || sm25Response.Command == SM25Commands.Identify))
                LastSm25Send.Sm25Response = sm25Response;

        try
        {
            ValidateChecksum(sm25Response);

            switch (sm25Response.Command)
            {
                case SM25Commands.Enroll:
                case SM25Commands.EnrollAndStoreinRAM:
                    ProcessEnrollResponse(sm25Response);
                    break;
                case SM25Commands.GetEmptyID:
                    ProcessEmptyIdResponse(sm25Response);
                    break;
                case SM25Commands.ClearTemplate:
                    PreccessClearTemplateResponse(sm25Response);
                    break;
                case SM25Commands.ClearAllTemplate:
                    ProcessClearAllTemplatesResponse(sm25Response);
                    break;
                case SM25Commands.GetTemplateStatus:
                    ProcessTemplateStatusResponse(sm25Response);
                    break;
                default:
                    var status = $"{sm25Response.Command.AsString(EnumFormat.Description)} {sm25Response.Data}";
                    SendStatus(status);
                    break;
            }
        }
        catch (ObjectDisposedException)
        {
            /* continue */
        }
    }

    private void ProcessTemplateStatusResponse(SM25Response sm25Response)
    {
        if (sm25Response.Command == SM25Commands.GetTemplateStatus)
            sm25Response.DataTemplateStatus = (TemplateStatus)sm25Response.Data;

        SendStatus(sm25Response.ReturnCode == ReturnCodes.ERR_SUCCESS
            ? $"{sm25Response.DataTemplateStatus}"
            : $"{sm25Response.DataReturnCode}");
    }

    private void ProcessClearAllTemplatesResponse(SM25Response sm25Response)
    {
        if (sm25Response.ReturnCode == ReturnCodes.ERR_SUCCESS)
            SendStatus($"All tremplates removed. Qt. {sm25Response.Data}");
        else
            SendStatus($"Can't remove all templates. {((ReturnCodes)sm25Response.Data).AsString(EnumFormat.Description)}");
    }

    private void PreccessClearTemplateResponse(SM25Response sm25Response)
    {
        if (sm25Response.ReturnCode == ReturnCodes.ERR_SUCCESS)
            SendStatus($"Template {sm25Response.Data} removed");
        else
        {
            if (sm25Response.Data == (ushort)ReturnCodes.ERR_TMPL_EMPTY)
                SendStatus($"Empty template");
            else
                SendStatus($"Can't remove template. {((ReturnCodes)sm25Response.Data).AsString(EnumFormat.Description)}");
        }
    }

    private void ProcessEmptyIdResponse(SM25Response sm25Response)
    {
        SendStatus($"ID available {sm25Response.Data}");
        OnIdAvailable?.Invoke(sm25Response.Data);
    }

    private void ProcessEnrollResponse(SM25Response sm25Response)
    {
        var enrollStatus = new EnrollStatus { Ret = sm25Response.ReturnCode, DataGD = sm25Response.DataGD, DataReturnCode = sm25Response.DataReturnCode };

        if (sm25Response.ReturnCode == ReturnCodes.ERR_SUCCESS)
            ProcessEnrollResponseSuccess(sm25Response, enrollStatus);
        else if (sm25Response.ReturnCode == ReturnCodes.ERR_FAIL)
            ProcessEnrollResponseFail(sm25Response, enrollStatus);

        OnEnrollStatus?.Invoke(enrollStatus);
    }

    private void ProcessEnrollResponseFail(SM25Response sm25Response, EnrollStatus enrollStatus)
    {
        switch (sm25Response.DataReturnCode)
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
                SendStatus($"Id duplicated with {sm25Response.Data >> 8}");
                enrollStatus.Data = sm25Response.Data >> 8;
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

    private void ProcessEnrollResponseSuccess(SM25Response sm25Response, EnrollStatus enrollStatus)
    {
        switch (sm25Response.DataGD)
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
                SendStatus($"Enroll {sm25Response.Data}");
                enrollStatus.Data = sm25Response.Data;
                break;
        }
    }

    private static void ValidateChecksum(SM25Response sm25Response)
    {
        if (sm25Response.ChecksumIsValid) return;

        var msg =
            $"Response checksum is invalid. Response {sm25Response.Payload.ToHexString(" ")} (Expected checksum {sm25Response.ChecksumFromReturn} <> Checksum {sm25Response.ChecksumCalculated})";

        Log?.Invoke(msg);
        throw new Exception(msg);
    }

    private void SendStatus(string status)
    {
        Log?.Invoke($" SM25 {Ip} Status {status}");
        OnStatus?.Invoke(status);
    }
}