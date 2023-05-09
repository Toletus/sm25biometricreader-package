using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Toletus.Extensions;
using Toletus.SM25.Command;
using Toletus.SM25.Command.Enums;

namespace Toletus.SM25.Base;

public class SM25ReaderBase
{
    public static Action<string>? Log;

    private TcpClient _client;
    //private Thread _reponseThread;
    protected ReaderSendCommand LastReaderSendCommand;

    public IPAddress Ip;
    public int Port = 7879;

    public event Action<ConnectionStatus>? OnConnectionStateChanged;
    public event Action<ReaderSendCommand>? OnSend;
    public event Action<byte[]>? OnRawResponse;

    public bool Busy { get; set; } = false;
    public bool Enrolling { get; set; }

    public SM25ReaderBase(IPAddress ip)
    {
        Ip = ip;
    }

    public bool Connected
    {
        get
        {
            try
            {
                return _client != null && _client.Connected;
            }
            catch
            {
                return false;
            }
        }
    }

    public void TestFingerprintReaderConnection()
    {
        try
        {
            var client = new TcpClient();
            var connectDone = new ManualResetEvent(false);

            var endConnect = new AsyncCallback(o =>
            {
                var state = (TcpClient)o.AsyncState!;
                state.EndConnect(o);
                connectDone.Set();
                    
                Log?.Invoke($"SM25 {Ip} Connection Test {client.Connected}");
                    
                OnConnectionStateChanged?.Invoke(client.Connected
                    ? ConnectionStatus.Connected
                    : ConnectionStatus.Closed);

                Thread.Sleep(1000);

                client.GetStream().Close();
                client.Close();
                client.Dispose();

                Log?.Invoke($"SM25 {Ip} Connection Test Closed");
            });

            var result = client.BeginConnect(Ip, Port, endConnect, client);
            connectDone.WaitOne(TimeSpan.FromSeconds(5));
        }
        catch (Exception e)
        {
            Log?.Invoke($"SM25 {nameof(TestFingerprintReaderConnection)} Error {e.MessagesToString()}");
        }
    }

    public void Connect()
    {
        try
        {
            Log?.Invoke($"Connecting to SM25 {Ip} Reader");
                
            _client = new TcpClient();
            _client.Connect(Ip, Port);
            Thread.Sleep(500);

            StartResponseThread();
        }
        catch (Exception e)
        {
            Log?.Invoke($"Error connecting to SM25 {Ip} Reader {e.ToLogString(Environment.StackTrace)}");
                
            CloseClient();
            OnConnectionStateChanged?.Invoke(ConnectionStatus.Closed);
            return;
        }

        Log?.Invoke($"SM25 {Ip} Reader Connected {Connected}");

        OnConnectionStateChanged?.Invoke(Connected ? ConnectionStatus.Connected : ConnectionStatus.Closed);
    }

    private CancellationTokenSource _cts;
    private void StartResponseThread()
    {
        _cts = new CancellationTokenSource();

        ThreadPool.QueueUserWorkItem(ReceiveResponse, _cts.Token);
    }

    public void Close()
    {
        if (Enrolling) Send(new ReaderSendCommand(Commands.FPCancel));

        try
        {
            _cts?.Cancel();
        }
        catch (Exception ex)
        {
        }
        finally
        {
            CloseClient();
            Enrolling = false;
            OnConnectionStateChanged?.Invoke(ConnectionStatus.Closed);
        }
    }

    private void CloseClient()
    {
        if (_client == null) return;

        Log?.Invoke($"Closing SM25 {Ip} Reader");

        try
        {
            _client?.Close();
        }
        catch { }

        _client?.Dispose();
        _client = null;

        Log?.Invoke($"Closed SM25 {Ip}");
    }

    private void ReceiveResponse(object obj)
    {
        CancellationToken token = (CancellationToken)obj;

        var buffer = new byte[1024];

        try
        {
            var readBytes = 1;

            while (readBytes != 0)
            {
                if (token.IsCancellationRequested)
                {
                    Log?.Invoke($"ReceiveResponse CancellationRequested");
                    return;
                }

                var stream = _client?.GetStream();

                if (stream == null)
                    return;

                readBytes = stream.Read(buffer, 0, buffer.Length);

                var ret = buffer.Take(readBytes).ToArray();

                if (ret.Length == 1 && ret[0] == 0) continue;

                OnRawResponse?.Invoke(ret);
            }
        }
        catch (ThreadAbortException e)
        {
            Log?.Invoke($"ThreadAbortException {e.ToLogString(Environment.StackTrace)}");
        }
        catch (ObjectDisposedException e)
        {
            Log?.Invoke($"ObjectDisposedException {e.ToLogString(Environment.StackTrace)}");
        }
        catch (IOException e)
        {
            Log?.Invoke($"Connection closed. Receive response finised. (IOException)");
            //Log?.Invoke($"Connection closed. Receive response finised. IOException {e.ToLogString(Environment.StackTrace)}");
            if (_client != null && _client.Connected)
                Close();
        }
        catch (InvalidOperationException e)
        {
            Log?.Invoke($"InvalidOperationException {e.ToLogString(Environment.StackTrace)}");
            if (_client != null && _client.Connected)
                Close();
        }
        catch (SocketException e)
        {
            Log?.Invoke($"SocketException {e.ToLogString(Environment.StackTrace)}");
        }
        catch (Exception e)
        {
            Log?.Invoke($"Exception {e.ToLogString(Environment.StackTrace)}");
        }
    }

    protected Commands Send(ReaderSendCommand readerSendCommand)
    {
        if (Enrolling && readerSendCommand.Command != Commands.FPCancel)
        {
            Log?.Invoke($"Command {readerSendCommand.Command} ignored. Expected to finish enroll or FPCancel command.");
            return readerSendCommand.Command;
        }

        if (_client == null || !_client.Connected)
            throw new Exception($"Fingerprint {Ip} reader is not connected. Command sent {readerSendCommand}");

        _client?.GetStream().Write(readerSendCommand.Payload, 0, readerSendCommand.Payload.Length);

        OnSend?.Invoke(readerSendCommand);

        LastReaderSendCommand = readerSendCommand;

        return readerSendCommand.Command;
    }
}