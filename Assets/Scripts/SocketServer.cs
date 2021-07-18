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
        public static ManualResetEvent allDone = new ManualResetEvent(false);

        // State object for reading client data asynchronously  
        public class StateObject
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

        /// <summary> 	
        /// Setup socket server. 	
        /// </summary> 	
        internal static void StartListening()
        {
            // Establish the local endpoint for the socket.  
            IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 50008);

            // Create a TCP/IP socket.  
            Socket listener = new Socket(ipAddress.AddressFamily,
                SocketType.Stream, ProtocolType.Tcp);

            Debug.Log("Opening server on " + localEndPoint.ToString());

            // Bind the socket to the local endpoint and listen for incoming connections.  
            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(100);
                string data = string.Empty;

                while (true)
                {
                    // Set the event to nonsignaled state.  
                    allDone.Reset();

                    // Start an asynchronous socket to listen for connections.  
                    listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);

                    // Wait until a connection is made before continuing.  
                    allDone.WaitOne();
                }
            }
            catch (ThreadAbortException)
            {
                Debug.Log("Thread aborted.");
            }
            catch (Exception e)
            {
                Debug.Log(e.ToString());
            }

            Debug.Log("Closing the listener.");
        }

        public static void AcceptCallback(IAsyncResult ar)
        {
            // Signal the main thread to continue looking for new connections.
            Debug.Log("Socket connected.");
            allDone.Set();

            // Get the socket that handles the client request.  
            Socket listener = (Socket)ar.AsyncState;
            Socket handler = listener.EndAccept(ar);

            // Create the state object.  
            StateObject state = new StateObject();
            state.workSocket = handler;
            handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                new AsyncCallback(ReadCallback), state);
        }

        public static void ReadCallback(IAsyncResult ar)
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

                // Handle this message here
                if (msg.Contains("OPI_CLOSE"))
                {
                    Debug.Log("Received command to close socket.");
                    handler.Shutdown(SocketShutdown.Both);
                    handler.Close();
                    return;
                }

                // Tell Unity we have received a message.
                // Change this to an event handler?
                lock (UnityOPI.Messages)
                {
                    UnityOPI.Messages.Enqueue(msg);
                }

                // Keep watching for more messages.
                handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(ReadCallback), state);
            }
        }

        private static void Send(Socket handler, string data)
        {
            // Convert the string data to byte data using ASCII encoding.  
            byte[] byteData = Encoding.ASCII.GetBytes(data);

            // Begin sending the data to the remote device.  
            handler.BeginSend(byteData, 0, byteData.Length, 0,
                new AsyncCallback(SendCallback), handler);
        }

        private static void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.  
                Socket handler = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.  
                int bytesSent = handler.EndSend(ar);
                Debug.Log(string.Format("Sent {0} bytes to client.", bytesSent));

                handler.Shutdown(SocketShutdown.Both);
                handler.Close();
            }
            catch (Exception e)
            {
                Debug.Log(e.ToString());
            }
        }

        // Check if socket is still connected
        private static bool SocketConnected(Socket s)
        {
            bool part1 = s.Poll(1000, SelectMode.SelectRead);
            bool part2 = (s.Available == 0);
            if ((part1 && part2) || !s.Connected)
                return false;
            else
                return true;
        }
    }
}
