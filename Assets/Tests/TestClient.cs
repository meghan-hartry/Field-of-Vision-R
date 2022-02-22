using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System;
using System.Text;

namespace FieldofVisionTests
{
    public class TestClient
    {
        private readonly IPAddress IP = IPAddress.Parse("127.0.0.1");
        private const int PORT = 50008;
        private const int BufferSize = 1024;

        public Socket Socket = null;
        public NetworkStream Stream = null;

        public void Connect()
        {
            try
            {
                // Create a TcpClient.
                // Note, for this client to work you need to have a TcpServer
                // connected to the same address as specified by the server, port
                // combination.
                IPEndPoint ipe = new IPEndPoint(IP, PORT);
                Socket = new Socket(ipe.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                Socket.Connect(ipe);
                Socket.LingerState = new LingerOption(enable: true, seconds: 0);
                Socket.SendTimeout = 1000;
                Socket.ReceiveTimeout = 1000;
            }
            catch (Exception e)
            {
                Debug.Log("Error: " + e.Message);
                Disconnect();
            }
        }

        public void Disconnect()
        {
            if (Socket != null)
            {
                Socket.Close();
                Socket.Dispose();
            }
        }

        public void SendMessage(string message)
        {
            // Translate the passed message into ASCII and store it as a Byte array.
            byte[] data = Encoding.ASCII.GetBytes(message);

            // Send the message to the connected TcpServer.
            Socket.Send(data, data.Length, 0);
            Debug.Log("MockClient Sent: " + message);
        }

        public string ReceiveMessage()
        {
            // Buffer to store the response bytes.
            var buffer = new byte[BufferSize];

            // Read the first batch of the TcpServer response bytes.
            int bytes = Socket.Receive(buffer, buffer.Length, 0);
            var responseData = Encoding.ASCII.GetString(buffer, 0, bytes);
            Debug.Log("MockClient Received: " + responseData);
            return responseData;
        }

        public bool IsConnected()
        {
            // The socket believes it's connected
            if (Socket != null && Socket.Connected)
            {
                byte[] buff = new byte[1];

                // The socket is CONNECTED when Poll is FALSE.
                if (!Socket.Poll(0, SelectMode.SelectRead))
                {
                    return true;
                }

                // The socket is CONNECTED if data is available when Poll is TRUE.
                var timeoutSetting = Socket.ReceiveTimeout; // save timeout setting
                Socket.ReceiveTimeout = 200; // temporarily set 200ms receive timeout.
                if (Socket.Receive(buff, SocketFlags.Peek) != 0)
                {
                    return true;
                }
                Socket.ReceiveTimeout = timeoutSetting; // reset timeout setting

            }
            return false;
        }
    }
}
