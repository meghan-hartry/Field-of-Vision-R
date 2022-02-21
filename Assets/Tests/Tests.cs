using NUnit.Framework;
using UnityEngine;
using FieldofVision;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System;
using System.Text;

public class Tests
{
    // A Test behaves as an ordinary method
    [Test]
    public void StartListeningStartsTask()
    {
        // Arrange
        var server = new TCPServer();

        try
        {   // Act
            server.StartListening();

            // Assert
            Assert.AreEqual(TaskStatus.WaitingForActivation, server.ListeningTask.Status);
        }
        finally 
        {
            // Cleanup
            if (server.ListeningTask != null)
            {
                server.StopListening().Wait();
            }
        }
    }

    // A Test behaves as an ordinary method
    [Test]
    public void StopListeningStopsTask()
    {
        var server = new TCPServer();

        try
        {
            // Arrange
            server.StartListening();

            // Act
            server.StopListening().Wait();

            // Assert
            Assert.AreEqual(TaskStatus.RanToCompletion, server.ListeningTask.Status);
        }
        finally
        {
            // Cleanup
            if (server.ListeningTask != null && !server.ListeningTask.IsCompleted)
            {
                server.StopListening().Wait();
            }
        }
    }

    // A Test behaves as an ordinary method
    [Test]
    public void CanConnectClient()
    {
        var server = new TCPServer();
        var mockClient = new MockClient();

        try
        {
            // Arrange
            server.StartListening();

            // Act
            mockClient.Connect();
            server.WaitForClientConnection();

            // Assert
            Assert.AreEqual(true, TCPServer.IsConnected(mockClient.Socket));
        }
        finally
        {
            // Cleanup
            if (mockClient != null)
            {
                mockClient.Disconnect(); // Disconnect before closing listener.
            }
            if (server.ListeningTask != null)
            {
                server.StopListening().Wait();
            }
        }
    }

    [Test]
    public void ConnectingTwoClientsClosesFirst()
    {
        var server = new TCPServer();
        var mockClient = new MockClient();
        var mockClient2 = new MockClient();
        try
        {   // Arrange
            server.StartListening();
            mockClient.Connect();

            // Act
            mockClient2.Connect();
            server.WaitForClientConnection();
            System.Threading.Thread.Sleep(100); // Give first socket time to close

            // Assert
            Assert.AreEqual(false, TCPServer.IsConnected(mockClient.Socket));
            Assert.AreEqual(true, TCPServer.IsConnected(mockClient2.Socket));
        }
        finally
        {
            // Cleanup
            if (mockClient != null)
            {
                mockClient.Disconnect(); // Disconnect before closing listener.
            }
            if (mockClient2 != null)
            {
                mockClient2.Disconnect();
            }
            if (server.ListeningTask != null)
            {
                server.StopListening().Wait();
            }
        }
    }

    [Test]
    public void CanDisconnectClient()
    {
        var server = new TCPServer();
        var mockClient = new MockClient();

        try
        {
            // Arrange
            server.StartListening();
            mockClient.Connect();
            server.WaitForClientConnection();

            // Act
            mockClient.Disconnect();

            // Assert
            Assert.AreEqual(false, TCPServer.IsConnected(mockClient.Socket));
        }
        finally
        {
            // Cleanup
            if (mockClient != null)
            {
                mockClient.Disconnect(); // Disconnect before closing listener.
            }
            if (server.ListeningTask != null)
            {
                server.StopListening().Wait();
            }
        }
    }

    [Test]
    public void CanSendMessage()
    {
        var server = new TCPServer();
        var mockClient = new MockClient();

        try
        {
            // Arrange
            server.StartListening();
            mockClient.Connect();

            // Act
            byte[] data = Encoding.ASCII.GetBytes("true");
            server.Write(data).Wait();

            // Assert
            Assert.AreEqual("true", mockClient.ReceiveMessage());
        }
        finally
        {
            // Cleanup
            if (mockClient != null)
            {
                mockClient.Disconnect(); // Disconnect before closing listener.
            }
            if (server.ListeningTask != null)
            {
                server.StopListening().Wait();
            }
        }
    }

    [Test]
    public void MessageSendsToNewestClient()
    {
        var server = new TCPServer();
        var mockClient = new MockClient();
        var mockClient2 = new MockClient();

        try
        {
            // Arrange
            server.StartListening();
            mockClient.Connect();
            mockClient2.Connect();
            System.Threading.Thread.Sleep(100); // Give first socket time to close

            // Act        
            byte[] data = Encoding.ASCII.GetBytes("true");
            server.Write(data);

            // Assert
            Assert.AreEqual(string.Empty, mockClient.ReceiveMessage());
            Assert.AreEqual("true", mockClient2.ReceiveMessage());
        }
        finally
        {
            // Cleanup
            if (mockClient != null)
            {
                mockClient.Disconnect(); // Disconnect before closing listener.
            }
            if (mockClient2 != null)
            {
                mockClient2.Disconnect(); 
            }
            if (server.ListeningTask != null)
            {
                server.StopListening().Wait();
            }
        }
    }

    [Test]
    public void CanStartReading()
    {
        var server = new TCPServer();
        var mockClient = new MockClient();

        try
        {
            // Arrange
            server.StartListening();
            mockClient.Connect();

            // Act
            server.WaitForClientConnection();
            server.StartReading();

            System.Threading.Thread.Sleep(100); // Give the reading task time to start

            // Assert
            Assert.AreEqual(true, server.ReadingTask.Status == TaskStatus.Running);
        }
        finally
        {
            // Cleanup
            if (mockClient != null)
            {
                mockClient.Disconnect(); // Disconnect before closing listener.
            }
            if (server.ListeningTask != null)
            {
                server.StopListening().Wait();
            }
        }
    }

    [Test]
    public void CanStopReading()
    {
        var server = new TCPServer();
        var mockClient = new MockClient();

        try
        {
            // Arrange
            server.StartListening();
            mockClient.Connect();

            // Act
            server.StartReading();
            server.StopReading();

            // Assert
            Assert.AreEqual(true, server.ReadingTask.Status == TaskStatus.RanToCompletion);
        }
        finally
        {
            // Cleanup
            if (mockClient != null)
            {
                mockClient.Disconnect(); // Disconnect before closing listener.
            }
            if (server.ListeningTask != null)
            {
                server.StopListening().Wait();
            }
        }
    }

    [Test]
    public void StopListeningStopsReading()
    {
        var server = new TCPServer();
        var mockClient = new MockClient();

        try
        {
            // Arrange
            server.StartListening();
            mockClient.Connect();
            server.StartReading();

            // Act
            server.StopListening().Wait();

            // Assert
            Assert.AreEqual(true, server.ReadingTask.Status == TaskStatus.RanToCompletion);
        }
        finally
        {
            // Cleanup
            if (mockClient != null)
            {
                mockClient.Disconnect(); // Disconnect before closing listener.
            }
        }
    }

    [Test]
    public void CanReceiveMessage()
    {
        var server = new TCPServer();
        var mockClient = new MockClient();

        try
        {
            // Arrange
            server.StartListening();
            mockClient.Connect();
            server.StartReading();

            // Act
            mockClient.SendMessage("true");
            System.Threading.Thread.Sleep(1000); // Give the reading task time to start

            // Assert
            Assert.AreEqual("true", server.Buffer.ToString());
        }
        finally
        {
            // Cleanup
            server.StopReading();
            if (mockClient != null)
            {
                mockClient.Disconnect(); // Disconnect before closing listener.
            }
            if (server.ListeningTask != null)
            {
                server.StopListening().Wait();
            }
        }
    }

    // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
    // `yield return null;` to skip a frame.
    /*[UnityTest]
    public IEnumerator TestsWithEnumeratorPasses()
    {
        // Use the Assert class to test conditions.
        // Use yield to skip a frame.
        yield return null;
    }*/
}

public class MockClient
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
            Socket.ReceiveTimeout = 200;
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
}
