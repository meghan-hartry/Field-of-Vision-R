using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace FieldofVision
{
    public class TCPServer
    {
        #region Events

        /// <summary>
        /// ClientConnected event, raised when a client connects to the server. Used for flow control and unit testing.
        /// </summary>
        public UnityEvent ClientConnected { get; private set; } = new UnityEvent();

        /// <summary>
        /// ListeningStopped event, raised when the server task that listens for clients ends. Used for unit testing only.
        /// </summary>
        public UnityEvent ListeningStopped { get; private set; } = new UnityEvent();

        /// <summary>
        /// ReadingStopped event, raised when the server task that reads data from clients ends. Used for unit testing only.
        /// </summary>
        public UnityEvent ReadingStopped { get; private set; } = new UnityEvent();

        /// <summary>
        /// MessageWritten event, raised when the server task that writes data to the connected client ends. Used for unit testing only.
        /// </summary>
        public UnityEvent MessageWritten { get; private set; } = new UnityEvent();

        /// <summary>
        /// MessageReceived event, raised when the server recevies message data from the client. Used for unit testing only.
        /// </summary>
        public UnityEvent MessageReceived { get; private set; } = new UnityEvent();

        #endregion

        #region Properties and Fields

        /// <summary>
        /// Buffer for recevied messages.
        /// </summary>
        public ConcurrentQueue<string> Buffer = new ConcurrentQueue<string>();

        /// <summary>
        /// Returns the status of the listening background task.
        /// </summary>
        public TaskStatus? ListeningTaskStatus => ListeningTask.Status;

        /// <summary>
        /// Returns the status of the reading background task.
        /// </summary>
        public TaskStatus? ReadingTaskStatus => ReadingTask.Status;

        private readonly IPAddress IP = IPAddress.Parse("127.0.0.1");
        private const int PORT = 50008;
        private const int BufferSize = 1024;
        private object _lock = new object(); // sync lock 
        private CancellationTokenSource CancelListener = null;
        private CancellationTokenSource CancelReading = null;
        private TcpClient CurrentClient = null;
        private NetworkStream Stream = null;
        private Task ListeningTask;
        private Task ReadingTask;

        #endregion

        #region Public Methods

        /// <summary>
        /// Begin listening for client connections.
        /// </summary>
        public void StartListening()
        {
            CancelListener = new CancellationTokenSource();
            Debug.Log("Opening server on " + IP.ToString() + ", Port: " + PORT);
            var tcpListener = new TcpListener(IP, PORT);
            tcpListener.Start();
            TcpClient tcpClient = null;
            ClientConnected.AddListener(StartReading); // Start reading on client connection.

            ListeningTask = Task.Run(async () =>
            {
                while (!CancelListener.IsCancellationRequested)
                {
                    // This uses the asynchronous method AcceptTcpClientAsync, but then blocks using await. This is not a mistake, the synchronous AcceptTcpClient
                    // method is buggy and proper cancellation is impossible. Using the Async task allows the CancellationToken to cleanly end listening.
                    // Trust me - don't change this!!
                    tcpClient = await Task.Run(() => tcpListener.AcceptTcpClientAsync(), CancelListener.Token);

                    // Close existing connections
                    Disconnect();

                    lock (_lock)
                    {
                        CurrentClient = tcpClient;

                        // This discards any pending data and Winsock resets the connection.
                        CurrentClient.LingerState = new LingerOption(enable: true, seconds: 0);

                        Debug.Log("Client connected.");
                        ClientConnected.Invoke();
                    }
                }

                tcpListener.Stop();
                Debug.Log("Server stopped listening.");
                ListeningStopped.Invoke();
            });
        }

        /// <summary>
        /// Stop listening for client connections.
        /// </summary>
        public void StopListening()
        {
            if (ListeningTask == null) return;
            Debug.Log("Cancelling listener.");
            ClientConnected.RemoveListener(StartReading);
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
        }

        /// <summary>
        /// Start reading data from the connected client.
        /// </summary>
        public void StartReading()
        {
            Debug.Log("Started reading.");
            CancelReading = new CancellationTokenSource();
            var byteBuffer = new byte[BufferSize];

            ReadingTask = Task.Run(() =>
            {
                if (ClientConnected == null) throw new Exception("Read Error: Client must be connected write is called.");
                while (!CancelReading.IsCancellationRequested)
                {
                    int byteCount = 0;
                    try
                    {
                        lock (_lock)
                        {
                            if (CurrentClient == null) break;
                            if(Stream == null) Stream = CurrentClient.GetStream();

                            if (Stream.DataAvailable)
                            {
                                byteCount = Stream.Read(byteBuffer, 0, byteBuffer.Length);
                            }
                        }
                    }
                    catch(Exception e)
                    {
                        // Socket was closed while we were trying to read.
                        Debug.Log("Stopped reading: " + e.Message);
                        break;
                    }

                    if (byteCount > 0)
                    {
                        var msg = Encoding.ASCII.GetString(byteBuffer, 0, byteCount);
                        Buffer.Enqueue(msg); // store

                        Debug.Log(string.Format("Read {0} bytes from socket. \n Data : {1}", msg.Length, msg));
                        MessageReceived.Invoke();
                    }
                }

                Debug.Log("Server stopped reading.");
                ReadingStopped.Invoke();
            }, CancelReading.Token);
        }

        /// <summary>
        /// Stop reading data from the connected client.
        /// </summary>
        public void StopReading()
        {
            if (CancelReading.IsCancellationRequested) return;
            Debug.Log("Cancelling reading.");
            CancelReading.Cancel();
        }

        /// <summary>
        /// Write data to the connected client.
        /// </summary>
        public void Write(byte[] data)
        {
            // need to wait for reading to stop and for client to connect.
            if (ReadingTask != null && !ReadingTask.IsCompleted)
            {
                StopReading();
                ReadingTask.ContinueWith(antecedent => { WriteHandler(data, true); });    // Write when read stops
            }
            else 
            {
                WriteHandler(data, false);
            }
        }

        /// <summary>
        /// Disconnect the connected client.
        /// </summary>
        public void Disconnect()
        {
            lock (_lock) 
            {
                if (Stream != null)
                {
                    Stream.Dispose();
                    Stream = null;
                }
                if (ReadingTask != null && !ReadingTask.IsCompleted)
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

        /// <summary>
        /// Shutdown the TCP Server.
        /// </summary>
        public void Shutdown() 
        {
            Disconnect();
            StopListening();

            ClientConnected.RemoveAllListeners();
            ListeningStopped.RemoveAllListeners();
            ReadingStopped.RemoveAllListeners();
            MessageWritten.RemoveAllListeners();
            MessageReceived.RemoveAllListeners();
        }

        public bool IsConnected()
        {
            // The socket believes it's connected
            if (ClientConnected != null && CurrentClient.Connected)
            {
                byte[] buff = new byte[1];

                // The socket is CONNECTED when Poll is FALSE.
                if (!CurrentClient.Client.Poll(0, SelectMode.SelectRead))
                {
                    return true;
                }

                // The socket is CONNECTED if data is available when Poll is TRUE.
                var timeoutSetting = CurrentClient.Client.ReceiveTimeout; // save timeout setting
                CurrentClient.Client.ReceiveTimeout = 200; // temporarily set 200ms receive timeout.
                if (CurrentClient.Client.Receive(buff, SocketFlags.Peek) != 0)
                {
                    return true;
                }
                CurrentClient.Client.ReceiveTimeout = timeoutSetting; // reset timeout setting

            }
            return false;
        }

        #endregion

        #region Private Methods

        private void WriteHandler(byte[] data, bool wasReading)
        {
            Task.Run(() =>
            {
                if (ClientConnected == null) throw new Exception("Write Error: Client must be connected write is called.");

                lock (_lock)
                {
                    if (Stream == null) Stream = CurrentClient.GetStream();
                    Stream.Write(data, 0, data.Length);
                }
                Debug.Log("[Server] Response has been written");
                MessageWritten.Invoke(); // Signal that write is complete.
                if (wasReading) StartReading(); // Start reading again
            });
        }

        #endregion
    }
}
