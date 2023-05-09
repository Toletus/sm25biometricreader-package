using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Toletus.Extensions;
using Toletus.SM25.Command;
using Toletus.SM25.Command.Enums;

namespace Toletus.SM25.Base
{
    public class SM25ReaderBase
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private TcpClient _client;
        //private Thread _reponseThread;
        protected SendCommand LastSendCommand;

        public IPAddress Ip;
        public int Port = 7879;

        public event Action<ConnectionStatus> OnConnectionStateChanged;
        public event Action<SendCommand> OnSend;
        public event Action<byte[]> OnRawResponse;

        public bool Busy { get; set; }
        public bool Enrolling { get; set; }

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

        public SM25ReaderBase(IPAddress ip)
        {
            Ip = ip;
        }

        public void TestFingerprintReaderConnection()
        {
            try
            {
                var client = new TcpClient();
                var connectDone = new ManualResetEvent(false);

                var endConnect = new AsyncCallback(o =>
                {
                    var state = (TcpClient)o.AsyncState;
                    state.EndConnect(o);
                    connectDone.Set();
                    Logger.Debug($"SM25 {Ip} Connection Test {client.Connected}");
                    OnConnectionStateChanged?.Invoke(client.Connected
                        ? ConnectionStatus.Connected
                        : ConnectionStatus.Closed);

                    Thread.Sleep(1000);

                    client.GetStream().Close();
                    client.Close();
                    client.Dispose();

                    Logger.Debug($"SM25 {Ip} Connection Test Closed");
                });

                var result = client.BeginConnect(Ip, Port, endConnect, client);
                connectDone.WaitOne(TimeSpan.FromSeconds(5));
            }
            catch (Exception e)
            {
                Logger.Debug($"SM25 {nameof(TestFingerprintReaderConnection)} Error {e.MessagesToString()}");
            }
        }

        public void Connect()
        {
            try
            {
                Logger.Debug($"Connecting to SM25 {Ip} Reader");
                _client = new TcpClient();
                _client.Connect(Ip, Port);
                Thread.Sleep(500);

                StartResponseThread();
            }
            catch (Exception e)
            {
                Logger.Debug($"Error connecting to SM25 {Ip} Reader {e.ToLogString(Environment.StackTrace)}");
                CloseClient();
                OnConnectionStateChanged?.Invoke(ConnectionStatus.Closed);
                return;
            }

            Logger.Debug($"SM25 {Ip} Reader Connected {Connected}");

            OnConnectionStateChanged?.Invoke(Connected ? ConnectionStatus.Connected : ConnectionStatus.Closed);
        }

        private CancellationTokenSource _cts;
        private void StartResponseThread()
        {
            //if (_reponseThread != null && _reponseThread.IsAlive) return;

            //_reponseThread = new Thread(ReceiveResponse) { Name = "SM25Response", IsBackground = true };
            //_reponseThread.Start();

            // Create the token source.
            _cts = new CancellationTokenSource();

            // Pass the token to the cancelable operation.
            ThreadPool.QueueUserWorkItem(new WaitCallback(ReceiveResponse), _cts.Token);
        }

        public void Close()
        {
            if (Enrolling) Send(new SendCommand(Commands.FPCancel));

            try
            {
                //_reponseThread?.Abort();
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

            Logger.Debug($"Closing SM25 {Ip} Reader");
            //Logger.Debug(Environment.StackTrace);

            try
            {
                _client?.Close();
            }
            catch { }

            _client?.Dispose();
            _client = null;

            Logger.Debug($"Closed SM25 {Ip}");
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
                        Logger.Debug($"ReceiveResponse CancellationRequested");
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
                Logger.Debug($"ThreadAbortException {e.ToLogString(Environment.StackTrace)}");
            }
            catch (ObjectDisposedException e)
            {
                Logger.Debug($"ObjectDisposedException {e.ToLogString(Environment.StackTrace)}");
            }
            catch (IOException e)
            {
                Logger.Debug($"Connection closed. Receive response finised. (IOException)");
                //Logger.Debug($"Connection closed. Receive response finised. IOException {e.ToLogString(Environment.StackTrace)}");
                if (_client != null && _client.Connected)
                    Close();
            }
            catch (InvalidOperationException e)
            {
                Logger.Debug($"InvalidOperationException {e.ToLogString(Environment.StackTrace)}");
                if (_client != null && _client.Connected)
                    Close();
            }
            catch (SocketException e)
            {
                Logger.Debug($"SocketException {e.ToLogString(Environment.StackTrace)}");
            }
            catch (Exception e)
            {
                Logger.Debug($"Exception {e.ToLogString(Environment.StackTrace)}");
            }
        }

        protected Commands Send(SendCommand sendCommand)
        {
            if (Enrolling && sendCommand.Command != Commands.FPCancel)
            {
                Logger.Debug($"Command {sendCommand.Command} ignored. Expected to finish enroll or FPCancel command.");
                return sendCommand.Command;
            }

            if (_client == null || !_client.Connected)
                throw new Exception($"Fingerprint {Ip} reader is not connected. Command sent {sendCommand}");

            _client?.GetStream().Write(sendCommand.Payload, 0, sendCommand.Payload.Length);

            OnSend?.Invoke(sendCommand);

            LastSendCommand = sendCommand;

            return sendCommand.Command;
        }
    }
}