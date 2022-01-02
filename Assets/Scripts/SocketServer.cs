using System;
using System.Net;
using UnityEngine;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace FieldofVision
{
    public class SocketServer
    {
        // Thread signal.  
        private ManualResetEvent allDone = new ManualResetEvent(false);

        private Socket Listener;
        private MainExecution Main;

        // State object for reading client data asynchronously  
        private class StateObject
        {
            // Size of receive buffer.  
            public const int BufferSize = 1024;

            // Receive buffer.  
            public byte[] buffer = new byte[BufferSize];

            // Received data string.
            public StringBuilder sb = new StringBuilder();

            // Client socket.
            public Socket workSocket = null;
        }

        internal SocketServer(MainExecution main)
        {
            Main = main;
        }

        /// <summary> 	
        /// Setup socket server. 	
        /// </summary> 	
        internal void StartListening()
        {
            // Establish the local endpoint for the socket.  
            IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 50008);

            // Create a TCP/IP socket.  
            Listener = new Socket(ipAddress.AddressFamily,
                SocketType.Stream, ProtocolType.Tcp);

            Debug.Log("Opening server on " + localEndPoint.ToString());

            // Bind the socket to the local endpoint and listen for incoming connections.  
            Listener.Bind(localEndPoint);
            Listener.Listen(100);
            string data = string.Empty;

            while (!Main.Shutdown)
            {
                // Set the event to nonsignaled state.  
                allDone.Reset();

                // Start an asynchronous socket to listen for connections.  
                Listener.BeginAccept(new AsyncCallback(AcceptCallback), Listener);

                // Wait until a connection is made before continuing.  
                allDone.WaitOne();
            }
            if (Listener != null)
            {
                Listener.Close();
            }
            Debug.Log("Socket Server shut down.");
        }

        private void AcceptCallback(IAsyncResult ar)
        {
            // Signal the main thread to continue looking for new connections.
            if(!Main.Shutdown)
                Debug.Log("Socket connected.");
            allDone.Set();

            // Get the socket that handles the client request.  
            Socket listener = (Socket)ar.AsyncState;
            Socket handler;
            try
            {
                handler = listener.EndAccept(ar);
            }
            catch (ObjectDisposedException) 
            {
                // Shutting down, return from method.
                return;
            }

            // Create the state object.  
            StateObject state = new StateObject();
            state.workSocket = handler;
            handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                new AsyncCallback(ReadCallback), state);
        }

        private void ReadCallback(IAsyncResult ar)
        {
            string content = string.Empty;

            // Retrieve the state object and the handler socket  
            // from the asynchronous state object.  
            StateObject state = (StateObject)ar.AsyncState;
            Socket handler = state.workSocket;

            // Check if socket is still connected
            if (!SocketConnected(handler))
            {
                Debug.Log("Socket disconnected.");
                handler.Shutdown(SocketShutdown.Both);
                handler.Close();
                return;
            }

            // Read data from the client socket.
            int bytesRead = handler.EndReceive(ar);

            if (bytesRead > 0)
            {
                // There  might be more data, so store the data received so far.  
                var msg = Encoding.ASCII.GetString(state.buffer, 0, bytesRead);

                state.sb.Append(msg); // store

                Debug.Log(string.Format("Read {0} bytes from socket. \n Data : {1}",
                        msg.Length, msg));

                var previousCount = Main.PresentationControl.Responses.Count;

                // Add message to processing queue.
                Debug.Log("Enqueueing message.");
                Main.MessageProcessor.ProcessMessage(msg);

                // Wait for response
                /*while (Main.MessageProcessor.WaitForResponse)
                {
                    if (Main.PresentationControl.Responses.Count > previousCount) 
                    {
                        var lastResponse = Main.PresentationControl.Responses[previousCount];
                        var response = (lastResponse.Seen ? "1" : "0") + " " + lastResponse.Time.ToString();
                        Send(handler, response);
                        break;
                    }
                }*/

                // Keep watching for more messages.
                if (!Main.Shutdown)
                {
                    handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                        new AsyncCallback(ReadCallback), state);
                }
                else 
                {
                    handler.Shutdown(SocketShutdown.Both);
                    handler.Close();
                }
            }
        }

        private void Send(Socket handler, string data)
        {
            // Convert the string data to byte data using ASCII encoding.  
            byte[] byteData = Encoding.ASCII.GetBytes(data);

            // Begin sending the data to the remote device.  
            handler.BeginSend(byteData, 0, byteData.Length, 0,
                new AsyncCallback(SendCallback), handler);
        }

        private void SendCallback(IAsyncResult ar)
        {
            // Retrieve the socket from the state object.  
            Socket handler = (Socket)ar.AsyncState;

            // Complete sending the data to the remote device.  
            int bytesSent = handler.EndSend(ar);
            Debug.Log(string.Format("Sent {0} bytes to client.", bytesSent));

            handler.Shutdown(SocketShutdown.Both);
            handler.Close();
        }

        // Check if socket is still connected
        private bool SocketConnected(Socket s)
        {
            bool part1 = s.Poll(1000, SelectMode.SelectRead);
            bool part2 = (s.Available == 0);
            if ((part1 && part2) || !s.Connected)
                return false;
            else
                return true;
        }

        // Shutdown the socket server
        internal void ForceShutdown()
        {
            if (Listener != null) 
            {
                Debug.Log("Forcing Listener Close");
                Listener.Close();
            }
        }
    }
}
