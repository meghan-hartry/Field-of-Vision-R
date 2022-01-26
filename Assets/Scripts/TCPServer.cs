using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace FieldofVision
{
    internal static class TCPServer
    {
        private static readonly IPAddress IP = IPAddress.Parse("127.0.0.1");
        private const int PORT = 50008;
        private const int BufferSize = 1024;

        private static object _lock = new object(); // sync lock 
        private static Task CurrentClientTask = null;
        private static TcpClient CurrentClient = null;
        private static CancellationTokenSource CancelListener = new CancellationTokenSource();
        private static bool CancelReader = false;
        internal static Task SocketServerTask;

        // The core server task
        internal static void StartListening()
        {
            SocketServerTask = Task.Run(async () =>
            {
                Debug.Log("Opening server on " + IP.ToString() + ", Port: " + PORT);
                var tcpListener = new TcpListener(IP, PORT);
                tcpListener.Start();
                TcpClient tcpClient = null;

                while (!MainExecution.Shutdown)
                {
                    tcpClient = await Task.Run(() => tcpListener.AcceptTcpClientAsync(), CancelListener.Token);
                    if (Connected(tcpClient))
                    {
                        Debug.Log("Client connected.");
                        var task = StartClientTaskAsync(tcpClient);
                    }
                }

                tcpListener.Stop();
                Debug.Log("We exited!!");
            });
        }

        // Register and handle the connection
        private static async Task StartClientTaskAsync(TcpClient tcpClient)
        {
            if (MainExecution.Shutdown) return;

            // Start the new reading task
            var clientTask = ReadingAsync(tcpClient);

            // Shutdown any previous connections
            Disconnect();
            CancelReader = false;

            // Save the new Client and its Task.
            lock (_lock) 
            {
                CurrentClient = tcpClient;
                CurrentClientTask = clientTask;
            }

            // Catch all errors of ReadingAsync
            try
            {
                await CurrentClientTask;
                // we may be on another thread after "await"
            }
            catch (Exception ex)
            {
                // log the error
                Debug.LogError(ex.ToString());
            }
        }

        // Handle new connection
        private static async Task ReadingAsync(TcpClient tcpClient)
        {
            if (MainExecution.Shutdown) return;
            await Task.Yield();
            // continue asynchronously on another threads

            Debug.Log("ReadingAsync started.");

            var buffer = new byte[BufferSize];
            var stringBuffer = new StringBuilder();
            //NetworkStream networkStream = null;

            while (Connected(tcpClient))
            {
                int byteCount = 0;
                try
                {
                    lock (_lock)
                    {
                        NetworkStream networkStream = tcpClient.GetStream();
                        if(networkStream.DataAvailable && !CancelReader)
                            byteCount = networkStream.Read(buffer, 0, buffer.Length);
                    }
                }
                catch (Exception)
                {
                    // Socket was closed while we were trying to read.
                    break;
                }

                if (byteCount > 0)
                {
                    var msg = Encoding.ASCII.GetString(buffer, 0, byteCount);
                    stringBuffer.Append(msg); // store

                    Debug.Log(string.Format("Read {0} bytes from socket. \n Data : {1}",
                            msg.Length, msg));

                    // Add message to processing queue.
                    Debug.Log("Enqueueing message.");
                    MainExecution.MainInstance.MessageProcessor.ProcessMessage(msg);
                }
            }

            /*if(networkStream != null)
                networkStream.Close();*/
            Debug.Log("ReadingAsync ended.");
        }

        internal static void Write(byte[] data)
        {
            try
            {
                lock (_lock)
                {
                    NetworkStream networkStream = CurrentClient.GetStream();
                    networkStream.Write(data, 0, data.Length);
                    Debug.Log("[Server] Response has been written");
                }
            }
            catch (System.IO.IOException)
            {
                // Socket was closed while we were trying to write.
                throw;
            }
        }

        // Check if client is still connected
        private static bool Connected(TcpClient tcpClient)
        {
            try
            {
                lock (_lock)
                {
                    if (tcpClient != null && tcpClient.Client != null && tcpClient.Client.Connected
                    && !MainExecution.Shutdown)
                    {
                        /* pear to the documentation on Poll:
                        * When passing SelectMode.SelectRead as a parameter to the Poll method it will return 
                        * -either- true if Socket.Listen(Int32) has been called and a connection is pending;
                        * -or- true if data is available for reading; 
                        * -or- true if the connection has been closed, reset, or terminated; 
                        * otherwise, returns false
                        */

                        byte[] buff = new byte[1];

                        // Detect if client connected, Poll returns false or data is available
                        if (!tcpClient.Client.Poll(0, SelectMode.SelectRead))
                        {
                            return true;
                        }

                        // Check if data is available, otherwise this is a bad connection
                        tcpClient.ReceiveTimeout = 1000; // set 1s Receive timeout.
                        if (tcpClient.Client.Receive(buff, SocketFlags.Peek) != 0)
                        {
                            return true;
                        }
                    }

                    Debug.Log("Bad connection.");
                    return false;
                }
            }
            catch(Exception e)
            {
                Debug.LogError(e.ToString());
                return false;
            }
        }

        internal static void Disconnect()
        {
            lock (_lock)
            {
                if (CurrentClient != null)
                {
                    CancelReader = true;
                    CurrentClient.Close();
                }

                if (CurrentClientTask != null)
                {
                    var wait = 0;
                    while (!CurrentClientTask.IsCompleted && wait < 1000)
                    {
                        wait += 200;
                        Thread.Sleep(200);
                    }
                    if (wait > 1000) 
                        throw new Exception("Could not complete task.");
                    CurrentClientTask = null;
                }

                if (MainExecution.Shutdown)
                    CancelListener.Cancel();
            }
        }
    }
}
