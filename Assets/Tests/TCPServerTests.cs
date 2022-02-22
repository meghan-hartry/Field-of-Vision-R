using NUnit.Framework;
using FieldofVision;
using System.Threading.Tasks;
using System.Text;
using System.Collections;
using UnityEngine.TestTools;
using System.Threading;
using System;
using UnityEngine;

namespace FieldofVisionTests
{
    public class Tests
    {
        [UnityTest]
        public IEnumerator StartListeningStartsTask()
        {
            var server = new TCPServer();
            try
            {   // Act
                server.StartListening();

                // Assert
                Assert.AreEqual(TaskStatus.WaitingForActivation, server.ListeningTaskStatus);
            }
            finally
            {
                // Cleanup
                Debug.Log("Cleanup:");
                server.StopListening();
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator StopListeningStopsTask()
        {
            var server = new TCPServer();
            try
            {
                // Arrange
                var signal = new AutoResetEvent(false);
                server.ListeningStopped.AddListener(()=> 
                { 
                    signal.Set();
                });
                server.StartListening();

                // Act
                server.StopListening();

                // Assert
                signal.WaitOne(timeout: TimeSpan.FromMilliseconds(2000));
                new AutoResetEvent(false).WaitOne(timeout: TimeSpan.FromMilliseconds(20)); // Ensure time to update status.
                Assert.AreEqual(true, server.ListeningTaskStatus == TaskStatus.RanToCompletion);
            }
            finally
            {
                // Cleanup
                Debug.Log("Cleanup:");
                server.StopListening();
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator CanConnectClient()
        {
            var server = new TCPServer();
            var mockClient = new TestClient();
            try
            {
                // Arrange
                var signal = new AutoResetEvent(false);
                server.ClientConnected.AddListener(() =>
                {
                    signal.Set();
                });
                server.StartListening();

                // Act
                mockClient.Connect();

                // Assert
                signal.WaitOne(timeout: TimeSpan.FromMilliseconds(2000));
                Assert.AreEqual(true, mockClient.IsConnected());
            }
            finally
            {
                // Cleanup
                Debug.Log("Cleanup:");
                mockClient.Disconnect();
                server.StopListening();
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator ConnectingTwoClientsClosesFirst()
        {
            var server = new TCPServer();
            var mockClient = new TestClient();
            var mockClient2 = new TestClient();
            try
            {   // Arrange
                var signal = new AutoResetEvent(false);
                server.ClientConnected.AddListener(() =>
                {
                    signal.Set();
                });
                server.StartListening();
                mockClient.Connect();
                signal.WaitOne(timeout: TimeSpan.FromMilliseconds(2000));
                signal = new AutoResetEvent(false);

                // Act
                mockClient2.Connect();

                // Assert
                signal.WaitOne(timeout: TimeSpan.FromMilliseconds(2000));
                Assert.AreEqual(false, mockClient.IsConnected());
                Assert.AreEqual(true, mockClient2.IsConnected());
            }
            finally
            {
                // Cleanup
                Debug.Log("Cleanup:");
                mockClient.Disconnect();
                mockClient2.Disconnect();
                server.StopListening();
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator CanDisconnectClient()
        {
            var server = new TCPServer();
            var mockClient = new TestClient();
            try
            {
                // Arrange
                var signal = new AutoResetEvent(false);
                server.ClientConnected.AddListener(() =>
                {
                    signal.Set();
                });
                server.StartListening();
                mockClient.Connect();
                signal.WaitOne(timeout: TimeSpan.FromMilliseconds(2000));

                // Act
                mockClient.Disconnect();

                // Assert
                Assert.AreEqual(false, mockClient.IsConnected());
            }
            finally
            {
                // Cleanup
                Debug.Log("Cleanup:");
                mockClient.Disconnect();
                server.StopListening();
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator CanWriteMessage()
        {
            var server = new TCPServer();
            var mockClient = new TestClient();
            try
            {
                // Arrange
                var writtenSignal = new AutoResetEvent(false);
                var connectedSignal = new AutoResetEvent(false);
                server.ClientConnected.AddListener(() =>
                {
                    connectedSignal.Set();
                });
                server.MessageWritten.AddListener(() =>
                {
                    writtenSignal.Set();
                });
                server.StartListening();
                mockClient.Connect();
                connectedSignal.WaitOne(timeout: TimeSpan.FromMilliseconds(2000));

                // Act
                server.Write(Encoding.ASCII.GetBytes("true"));

                // Assert
                writtenSignal.WaitOne(timeout: TimeSpan.FromMilliseconds(2000));
                Assert.AreEqual("true", mockClient.ReceiveMessage());
            }
            finally
            {
                // Cleanup
                Debug.Log("Cleanup:");
                mockClient.Disconnect();
                server.StopListening();
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator MessageWritesToNewestClient()
        {
            var server = new TCPServer();
            var mockClient = new TestClient();
            var mockClient2 = new TestClient();
            try
            {
                // Arrange
                var connectedSignal = new AutoResetEvent(false);
                var writtenSignal = new AutoResetEvent(false);
                server.MessageWritten.AddListener(() =>
                {
                    writtenSignal.Set();
                });
                server.ClientConnected.AddListener(() =>
                {
                    connectedSignal.Set();
                });
                server.StartListening();
                mockClient.Connect(); 
                connectedSignal.WaitOne(timeout: TimeSpan.FromMilliseconds(2000)); // Connect first
                connectedSignal = new AutoResetEvent(false);

                mockClient2.Connect();
                connectedSignal.WaitOne(timeout: TimeSpan.FromMilliseconds(2000)); // Connect second

                new AutoResetEvent(false).WaitOne(timeout: TimeSpan.FromMilliseconds(20)); // Give client time to read.

                // Act        
                byte[] data = Encoding.ASCII.GetBytes("true");
                server.Write(data);

                // Assert
                writtenSignal.WaitOne(timeout: TimeSpan.FromMilliseconds(2000));
                Assert.AreEqual(string.Empty, mockClient.ReceiveMessage());
                Assert.AreEqual("true", mockClient2.ReceiveMessage());
            }
            finally
            {
                // Cleanup
                Debug.Log("Cleanup:");
                mockClient2.Disconnect();
                server.StopListening();
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator CanStartReading()
        {
            var server = new TCPServer();
            var mockClient = new TestClient();
            try
            {
                // Arrange
                var signal = new AutoResetEvent(false);
                server.ClientConnected.AddListener(() =>
                {
                    signal.Set();
                });
                server.StartListening();

                // Act
                mockClient.Connect();   // Reading starts when a client connects

                // Assert
                signal.WaitOne(timeout: TimeSpan.FromMilliseconds(2000));
                new AutoResetEvent(false).WaitOne(timeout: TimeSpan.FromMilliseconds(20)); // Ensure time to start reading after client connects.
                Assert.AreEqual(true, server.ReadingTaskStatus == TaskStatus.Running);
            }
            finally
            {
                // Cleanup
                Debug.Log("Cleanup:");
                mockClient.Disconnect();
                server.StopListening();
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator CanStopReading()
        {
            var server = new TCPServer();
            var mockClient = new TestClient();
            try
            {
                // Arrange
                var readingSignal = new AutoResetEvent(false);
                var connectedSignal = new AutoResetEvent(false);
                server.ReadingStopped.AddListener(() =>
                {
                    readingSignal.Set();
                });
                server.ClientConnected.AddListener(() => 
                {
                    connectedSignal.Set();
                });
                server.StartListening();
                mockClient.Connect(); // Reading starts when a client connects
                connectedSignal.WaitOne(timeout: TimeSpan.FromMilliseconds(2000)); // Wait for client to connect to ensure reading starts 
                new AutoResetEvent(false).WaitOne(timeout: TimeSpan.FromMilliseconds(20)); // Ensure time to start reading

                // Act
                server.StopReading();

                // Assert
                readingSignal.WaitOne(timeout: TimeSpan.FromMilliseconds(2000));
                new AutoResetEvent(false).WaitOne(timeout: TimeSpan.FromMilliseconds(20)); // Ensure time to update event
                Assert.AreEqual(true, server.ReadingTaskStatus == TaskStatus.RanToCompletion);
            }
            finally
            {
                // Cleanup
                Debug.Log("Cleanup:");
                mockClient.Disconnect();
                server.StopListening();
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator StopListeningStopsReading()
        {
            var server = new TCPServer();
            var mockClient = new TestClient();
            try
            {
                // Arrange
                var readingSignal = new AutoResetEvent(false);
                var connectedSignal = new AutoResetEvent(false);
                server.ReadingStopped.AddListener(() =>
                {
                    readingSignal.Set();
                });
                server.ClientConnected.AddListener(() =>
                {
                    connectedSignal.Set();
                });
                server.StartListening();
                mockClient.Connect(); // Reading starts when a client connects
                connectedSignal.WaitOne(timeout: TimeSpan.FromMilliseconds(2000)); // Wait for client to connect to ensure reading starts 

                // Act
                server.StopListening();

                // Assert
                readingSignal.WaitOne(timeout: TimeSpan.FromMilliseconds(2000));
                Assert.AreEqual(true, server.ReadingTaskStatus == TaskStatus.RanToCompletion);
            }
            finally
            {
                // Cleanup
                Debug.Log("Cleanup:");
                mockClient.Disconnect();
                server.StopListening();
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator CanReceiveMessage()
        {
            var server = new TCPServer();
            var mockClient = new TestClient();
            try
            {
                // Arrange
                var receivedSignal = new AutoResetEvent(false);
                var connectedSignal = new AutoResetEvent(false);
                server.MessageReceived.AddListener(() =>
                {
                    receivedSignal.Set();
                });
                server.ClientConnected.AddListener(() =>
                {
                    connectedSignal.Set();
                });
                server.StartListening();
                mockClient.Connect();

                // Act
                mockClient.SendMessage("true");

                // Assert
                receivedSignal.WaitOne(timeout: TimeSpan.FromMilliseconds(2000));
                server.Buffer.TryDequeue(out var result);
                Assert.AreEqual("true", result);
            }
            finally
            {
                // Cleanup
                Debug.Log("Cleanup:");
                mockClient.Disconnect();
                server.StopListening();
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator CanReceiveMultipleMessage()
        {
            var server = new TCPServer();
            var mockClient = new TestClient();
            try
            {
                // Arrange
                var receivedSignal = new AutoResetEvent(false);
                var connectedSignal = new AutoResetEvent(false);
                server.MessageReceived.AddListener(() =>
                {
                    receivedSignal.Set();
                });
                server.ClientConnected.AddListener(() =>
                {
                    connectedSignal.Set();
                });
                server.StartListening();
                mockClient.Connect();
                mockClient.SendMessage("true");
                receivedSignal.WaitOne(timeout: TimeSpan.FromMilliseconds(2000));
                server.Buffer.TryDequeue(out var result);
                Assert.AreEqual("true", result);
                receivedSignal = new AutoResetEvent(false);

                // Act
                mockClient.SendMessage("false");

                // Assert
                receivedSignal.WaitOne(timeout: TimeSpan.FromMilliseconds(2000));
                server.Buffer.TryDequeue(out result);
                Assert.AreEqual("false", result);
            }
            finally
            {
                // Cleanup
                Debug.Log("Cleanup:");
                mockClient.Disconnect();
                server.StopListening();
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator CanReadMessageAfterWriting()
        {
            var server = new TCPServer();
            var mockClient = new TestClient();
            try
            {
                // Arrange
                var receivedSignal = new AutoResetEvent(false);
                var writtenSignal = new AutoResetEvent(false);
                var connectedSignal = new AutoResetEvent(false);
                server.ClientConnected.AddListener(() =>
                {
                    connectedSignal.Set();
                });
                server.MessageReceived.AddListener(() =>
                {
                    receivedSignal.Set();
                });
                server.MessageWritten.AddListener(() =>
                {
                    writtenSignal.Set();
                });
                server.StartListening();
                mockClient.Connect();
                connectedSignal.WaitOne(timeout: TimeSpan.FromMilliseconds(2000));
                server.Write(Encoding.ASCII.GetBytes("true"));
                writtenSignal.WaitOne(timeout: TimeSpan.FromMilliseconds(2000));
                new AutoResetEvent(false).WaitOne(timeout: TimeSpan.FromMilliseconds(20)); // Ensure time for client to read.

                // Act
                mockClient.SendMessage("true");

                // Assert
                receivedSignal.WaitOne(timeout: TimeSpan.FromMilliseconds(2000));
                server.Buffer.TryDequeue(out var result);
                Assert.AreEqual("true", result);
            }
            finally
            {
                // Cleanup
                Debug.Log("Cleanup:");
                mockClient.Disconnect();
                server.StopListening();
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator CanWriteMessageAfterReading()
        {
            var server = new TCPServer();
            var mockClient = new TestClient();
            try
            {
                // Arrange
                var receivedSignal = new AutoResetEvent(false);
                var writtenSignal = new AutoResetEvent(false);
                var connectedSignal = new AutoResetEvent(false);
                server.ClientConnected.AddListener(() =>
                {
                    connectedSignal.Set();
                });
                server.MessageReceived.AddListener(() =>
                {
                    receivedSignal.Set();
                });
                server.MessageWritten.AddListener(() =>
                {
                    writtenSignal.Set();
                });
                server.StartListening();
                mockClient.Connect();
                connectedSignal.WaitOne(timeout: TimeSpan.FromMilliseconds(2000));
                mockClient.SendMessage("false");
                receivedSignal.WaitOne(timeout: TimeSpan.FromMilliseconds(2000));
                server.Buffer.TryDequeue(out var result);
                Assert.AreEqual("false", result);

                // Act
                server.Write(Encoding.ASCII.GetBytes("true"));

                // Assert
                writtenSignal.WaitOne(timeout: TimeSpan.FromMilliseconds(2000));
                Assert.AreEqual("true", mockClient.ReceiveMessage());
            }
            finally
            {
                // Cleanup
                Debug.Log("Cleanup:");
                mockClient.Disconnect();
                server.StopListening();
            }
            yield return null;
        }
    }
}