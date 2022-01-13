using Assets.Scripts;
using Assets.Scripts.OPI_Definitions;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace FieldofVision
{
    public class MessageProcessing
    {
        private MainExecution Main => MainExecution.MainInstance;

        internal void ProcessMessage(string message)
        {
            Debug.Log("Processing message: " + message);
            string[] commands = new string[] { "OPI_CLOSE", "OPI_SET_BGROUND", "OPI_QUERY_DEVICE", "OPI_PRESENT", "OPI_GET_RES", "OPI_SET_FOVY" };

            string currentCommand = commands.FirstOrDefault(s => message.Contains(s));

            if (string.IsNullOrEmpty(currentCommand))
            {
                Main.ErrorOccurred.Invoke("OPI command not recognized: " + message);
                return;
            }

            // Call method with the same string name as the command
            Type thisType = this.GetType();
            MethodInfo theMethod = thisType
                .GetMethod(currentCommand, BindingFlags.NonPublic | BindingFlags.Instance);
            theMethod.Invoke(this, new[] { message });
        }

        /// <summary>
        /// Close socket connection and exit program.
        /// </summary>
        private void OPI_CLOSE(string message)
        {
            Debug.Log("OPI_CLOSE");
            Main.DoInMainThread(() => { Main.RunShutdown(); });
        }

        private void OPI_SET_BGROUND(string message)
        {
            Debug.Log("OPI_SET_BGROUND");

            string[] parameters = message.Trim().Split(' ').Skip(1).ToArray();
            var success = Conversions.ToColor(parameters, out Color color);
            if (!success) 
            {
                Debug.Log("Parameters for OPI_SET_BGROUND were invalid.");
                Main.ErrorOccurred.Invoke("Parameters for OPI_SET_BGROUND were invalid.");
                TCPServer.Write(BitConverter.GetBytes(false));
                return;
            }

            Main.DoInMainThread(() => { Main.PresentationControl.SetBackground(color); });
            TCPServer.Write(BitConverter.GetBytes(true));
        }

        private void OPI_QUERY_DEVICE(string message)
        {
            Debug.Log("OPI_QUERY_DEVICE");
            var connected = TCPServer.Connected();
            TCPServer.Write(BitConverter.GetBytes(connected));
        }

        private void OPI_PRESENT(string message)
        {
            Debug.Log("OPI_PRESENT");
            string[] parameters = message.Trim().Split(' ').Skip(1).ToArray();

            var success = StaticStimulus.CreateStaticStimulus(parameters, out StaticStimulus stimulus);
            if (!success)
            {
                Main.ErrorOccurred.Invoke("Parameters for OPI_PRESENT were invalid: " + message);
                return;
            }

            Main.PresentationControl.Present(stimulus);
        }

        /// <summary>
        /// Return the resolution of the VR device.
        /// </summary>
        private void OPI_GET_RES(string message)
        {
            Debug.Log("OPI_GET_RES");
        }

        /// <summary>
        ///  Set field of view in the y-axis in degrees of visual angle
        /// </summary>
        private void OPI_SET_FOVY(string message)
        {
            Debug.Log("OPI_SET_FOVY");
        }
    }
}