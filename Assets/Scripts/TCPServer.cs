using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

namespace FieldofVision
{
    internal class TCPServer
    {
        private static byte[] Buffer = new byte[BufferSize];
        private static readonly StringBuilder StringBuffer = new StringBuilder();
        private static readonly IPAddress IP = IPAddress.Parse("127.0.0.1");
        private const int PORT = 50008;
        private const int BufferSize = 1024;
        private static MainExecution Main => MainExecution.MainInstance;

        private static TcpListener Listener = null;
        private static TcpClient Client = null;

        internal static void StartListening()
        {
            try
            {
                Listener = new TcpListener(IP, PORT);

                // Start listening for client requests.
                Listener.Start();

                Debug.Log("Opening server on " + IP.ToString() + ", Port: " + PORT);

                // Start an asynchronous socket to listen for connections.  
                Listener.BeginAcceptTcpClient(new AsyncCallback(AcceptClient), null);
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
                Main.ErrorOccurred.Invoke(e.Message);
            }
        }

        internal static void Write(byte[] data)
        {
            try
            {
                if (MainExecution.Shutdown) return;

                if (Connected())
                {
                    lock (Client)
                    {
                        var stream = Client.GetStream();
                        stream.BeginWrite(data, 0, data.Length, WriteCallback, null);
                    }
                }
                else
                {
                    Main.ErrorOccurred.Invoke("Error: No client connected.");
                    return;
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
                Main.ErrorOccurred.Invoke(e.Message);
            }
            finally
            {
                Debug.Log("Write ended.");
            }
        }

        internal static void Read()
        {
            try
            {
                if (MainExecution.Shutdown) return;

                if (Connected())
                {
                    // Clear buffers
                    Buffer = new byte[BufferSize];
                    StringBuffer.Clear();

                    lock(Client)
                    {
                        // Start reading
                        var stream = Client.GetStream();
                        stream.BeginRead(Buffer, 0, Buffer.Length, ReadCallback, null);
                    }
                }
                else
                {
                    Debug.Log("No client connected.");

                    // Start listening for client connections.
                    //Listener.BeginAcceptTcpClient(new AsyncCallback(AcceptClient), null);
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
                Main.ErrorOccurred.Invoke(e.Message);
            }
            finally
            {
                Debug.Log("Read ended.");
            }
        }

        internal static void Disconnect() 
        {
            try
            {
                // Close existing client.
                if (Client != null)
                {
                    Client.Close();
                }

                // Stop listening for new clients.
                if (Listener != null)
                {
                    Listener.Stop();
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
                Main.ErrorOccurred.Invoke(e.Message);
            }
        }

        private static void AcceptClient(IAsyncResult ar)
        {
            try 
            {
                if (MainExecution.Shutdown || Listener == null) return;

                // End the operation and display the received data on
                // the console.
                Client = Listener.EndAcceptTcpClient(ar);

                if (Connected())
                {
                    Debug.Log("Client connected.");

                    // Start receiving
                    Read();
                }
                else
                {
                    // Continue listening for client connections.
                    Listener.BeginAcceptTcpClient(new AsyncCallback(AcceptClient), null);
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
                Main.ErrorOccurred.Invoke(e.Message);
            }
            finally
            {
                Debug.Log("AcceptClient ended.");
            }
        }

        private static void ReadCallback(IAsyncResult ar)
        {
            try
            {
                if (!MainExecution.Shutdown && Connected())
                {
                    int bytesRead = 0;

                    lock (Client)
                    {
                        var stream = Client.GetStream();

                        // Read data from the client socket.
                        bytesRead = stream.EndRead(ar);
                    }

                    if (bytesRead > 0)
                    {
                        // There might be more data, so store the data received so far.  
                        var msg = Encoding.ASCII.GetString(Buffer, 0, bytesRead);

                        StringBuffer.Append(msg); // store

                        Debug.Log(string.Format("Read {0} bytes from socket. \n Data : {1}",
                                msg.Length, msg));

                        // Add message to processing queue.
                        Debug.Log("Enqueueing message.");
                        Main.MessageProcessor.ProcessMessage(msg);
                    } 
                }

                // Continue receiving
                Read();
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
                Main.ErrorOccurred.Invoke(e.Message);
            }
            finally
            {
                Debug.Log("ReadCallback ended.");
            }
        }

        private static void WriteCallback(IAsyncResult ar) 
        {
            try 
            {
                if (!MainExecution.Shutdown && Connected())
                {
                    lock (Client)
                    {
                        var stream = Client.GetStream();

                        stream.EndWrite(ar);
                    }
                    Debug.Log(string.Format("Sent message to client."));
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
                Main.ErrorOccurred.Invoke(e.Message);
            }
            finally
            {
                Debug.Log("WriteCallback ended.");
            }
        }

        // Check if client is still connected
        internal static bool Connected()
        {
            try
            {
                lock (Client) 
                {
                    if (MainExecution.Shutdown || Client == null || Client.Client == null || !Client.Client.Connected)
                    {
                        return false;
                    }

                    /* pear to the documentation on Poll:
                    * When passing SelectMode.SelectRead as a parameter to the Poll method it will return 
                    * -either- true if Socket.Listen(Int32) has been called and a connection is pending;
                    * -or- true if data is available for reading; 
                    * -or- true if the connection has been closed, reset, or terminated; 
                    * otherwise, returns false
                    */

                    // Detect if client disconnected
                    if (Client.Client.Poll(0, SelectMode.SelectRead))
                    {
                        byte[] buff = new byte[1];
                        if (Client.Client.Receive(buff, SocketFlags.Peek) == 0)
                        {
                            // Client disconnected
                            Client.Close();
                            return false;
                        }
                        else
                        {
                            return true;
                        }
                    }

                    return true;
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
                Main.ErrorOccurred.Invoke(e.Message);
                return false;
            }
        }
    }
}
