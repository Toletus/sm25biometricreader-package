using System;
using System.Net;
using Toletus.SM25.Base;
using Toletus.SM25.Command;

namespace Toletus.SM25;

public partial class SM25Reader : SM25ReaderBase, ISM25Reader
{
    public delegate void RetornoHandler(ReaderResponse readerResponse);

    public event Action<string>? OnStatus;
    public event RetornoHandler? OnResponse;
    public event Action<int?>? OnIdAvailable;
    public event Action<int?>? OnEnroll;
    public event Action<EnrollStatus>? OnEnrollStatus;
    public event Action? OnEnrollTimeout;
    public event Action? OnGeneralizationFail;

    public Sync Sync { get; }
    public bool Present { get; internal set; }

    public SM25Reader(IPAddress ip) : base(ip)
    {
        Sync = new Sync(this);
        OnRawResponse += SM25BioOnRawResponse;
        OnSend += SM25Bio_OnSend;
    }

    private void SM25Bio_OnSend(ReaderSend readerSend)
    {
        Log?.Invoke($" SM25 {Ip} > {readerSend}");
    }

    private void SM25BioOnRawResponse(byte[] response)
    {
        Present = true;
        ProcessResponse(response);
    }
}