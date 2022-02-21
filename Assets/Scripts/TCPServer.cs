using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace FieldofVision
{
    public class TCPServer
    {
        private readonly IPAddress IP = IPAddress.Parse("127.0.0.1");
        private const int PORT = 50008;
        private const int BufferSize = 1024;
        private bool Reading = false;
        private object _lock = new object(); // sync lock 
        private CancellationTokenSource CancelListener = null;
        private CancellationTokenSource CancelReading = null;
        private TcpClient CurrentClient = null;

        // todo: should only make status public
        public Task ListeningTask; 
        public Task ReadingTask;

        // only getter public
        public StringBuilder Buffer = new StringBuilder();

        // The core server task
        public void StartListening()
        {
            CancelListener = new CancellationTokenSource();
            Debug.Log("Opening server on " + IP.ToString() + ", Port: " + PORT);
            var tcpListener = new TcpListener(IP, PORT);
            tcpListener.Start();
            TcpClient tcpClient = null;

            ListeningTask = Task.Run(async () =>
            {
                while (!CancelListener.IsCancellationRequested)
                {
                    // This uses the asynchronous method AcceptTcpClientAsync, but then blocks using await. This is not a mistake, the synchronous AcceptTcpClient method is buggy
                    // and proper cancellation is impossible. Using the Async task allows the CancellationToken to cleanly end listening.
                    // Don't change this. I've spend dozens of hours on this single issue.
                    tcpClient = await Task.Run(() => tcpListener.AcceptTcpClientAsync(), CancelListener.Token);

                    // Close existing connections
                    Disconnect();

                    lock (_lock)
                    {
                        CurrentClient = tcpClient;

                        // This discards any pending data and Winsock resets the connection.
                        CurrentClient.LingerState = new LingerOption(enable: true, seconds: 0);

                        Debug.Log("Client connected.");
                    }
                }

                tcpListener.Stop();
                Debug.Log("Server stopped listening.");
            });
        }

        public Task StopListening() 
        {
            Debug.Log("Cancelling listener.");
            Disconnect();
            CancelListener.Cancel();

            // Check if the ListeningTask is stuck waiting for a new connection
            if (ListeningTask.Status == TaskStatus.WaitingForActivation)
            {
                try
                {
                    Debug.Log("Connecting socket to force close listener.");
                    // connect a socket so it can exit
                    IPEndPoint ipe = new IPEndPoint(IP, PORT);
                    using var socket = new Socket(ipe.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    socket.Connect(ipe);
                    socket.LingerState = new LingerOption(enable: true, seconds: 0);
                    socket.Close();
                    socket.Dispose();
                }
                catch (Exception e)
                {
                    // Socket may throw if cancellation succeeds.
                    Debug.Log("Error: " + e.Message);
                }
            }

            return Task.Run(() => 
            {
                // Wait for Task to finish, with 5 second timeout.
                ListeningTask.Wait(5);
                if (!ListeningTask.IsCompleted) 
                {
                    throw new Exception("Could not cancel listener.");
                }
            });
        }

        public void WaitForClientConnection() 
        {
            // Wait for Task to finish, with 5 second timeout.
            bool success = SpinWait.SpinUntil(() => CurrentClient != null, TimeSpan.FromSeconds(5));
            if (!success)
            {
                throw new Exception("No client found.");
            }
        }

        // Register and handle the connection
        public void StartReading()
        {
            Debug.Log("Started reading.");
            CancelReading = new CancellationTokenSource();
            var byteBuffer = new byte[BufferSize];

            ReadingTask = Task.Run(() =>
            {
                Reading = true;
                while (!CancelReading.IsCancellationRequested)
                {
                    int byteCount = 0;
                    try
                    {
                        lock (_lock)
                        {
                            NetworkStream networkStream = CurrentClient.GetStream();
                            if (networkStream.DataAvailable)
                            {
                                byteCount = networkStream.Read(byteBuffer, 0, byteBuffer.Length);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        // Socket was closed while we were trying to read.
                        Debug.Log("Error: " + e.Message);
                    }

                    if (byteCount > 0)
                    {
                        var msg = Encoding.ASCII.GetString(byteBuffer, 0, byteCount);
                        Buffer.Append(msg); // store

                        Debug.Log(string.Format("Read {0} bytes from socket. \n Data : {1}", msg.Length, msg));

                        // Add message to processing queue.
                        Debug.Log("Enqueueing message.");
                        //MainExecution.MainInstance.MessageProcessor.ProcessMessage(msg);
                    }
                    Thread.Sleep(100);
                }

                //tcpListener.Stop();
                Debug.Log("Server stopped reading.");
                Reading = false;
            }, CancelReading.Token);

        }

        public void StopReading()
        {
            Debug.Log("Cancelling reading.");
            CancelReading.Cancel();

            Task.Run(() =>
            {
                if (ReadingTask != null) 
                {
                    // Wait for Task to finish, with 5 second timeout.
                    ReadingTask.Wait(5);
                    Thread.Sleep(200); // give it time to cleanup
                    if (!ReadingTask.IsCompleted && Reading == true)
                    {
                        throw new Exception("Could not cancel reading.");
                    }
                }
            }).Wait();
        }

        public Task Write(byte[] data)
        {
            return Task.Run(() =>
            {
                WaitForClientConnection();
                lock (_lock)
                {
                    using NetworkStream networkStream = CurrentClient.GetStream();
                    networkStream.Write(data, 0, data.Length);
                    Debug.Log("[Server] Response has been written");
                }

                return Task.CompletedTask;
            });
        }

        public void Disconnect()
        {
            lock (_lock) 
            {
                if (Reading == true)
                {
                    StopReading();
                }
                if (CurrentClient != null)
                {
                    Debug.Log("Closing Client.");
                    CurrentClient.Close();
                    CurrentClient.Dispose();
                }
            }
        }

        public static bool IsConnected(Socket socket) 
        {
            // The socket is not null and thinks it's connected
            if (socket != null && socket.Connected)
            {
                byte[] buff = new byte[1];

                // The socket is CONNECTED when Poll is FALSE.
                if (!socket.Poll(0, SelectMode.SelectRead))
                {
                    return true;
                }

                // The socket is CONNECTED if data is available when Poll is TRUE.
                var timeoutSetting = socket.ReceiveTimeout; // save timeout setting
                socket.ReceiveTimeout = 200; // temporarily set 200ms receive timeout.
                if (socket.Receive(buff, SocketFlags.Peek) != 0)
                {
                    return true;
                }
                socket.ReceiveTimeout = timeoutSetting; // reset timeout setting

            }
            return false;
        }
    }
}
