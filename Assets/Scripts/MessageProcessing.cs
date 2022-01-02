using Assets.Scripts.OPI_Definitions;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace FieldofVision
{
    public class MessageProcessing
    {
        internal bool WaitForResponse = true;
        private MainExecution Main;

        internal MessageProcessing(MainExecution main) 
        {
            Main = main;
        }

        internal void ProcessMessage(string message)
        {
            Debug.Log("Processing message: " + message);
            string[] commands = new string[] { "OPI_CLOSE", "OPI_SET_BGROUND", "OPI_QUERY_DEVICE", "OPI_PRESENT", "OPI_GET_RES", "OPI_SET_FOVY" };

            string currentCommand = commands.FirstOrDefault(s => message.Contains(s));

            if (string.IsNullOrEmpty(currentCommand))
            {
                Debug.LogError("OPI command not recognized: " + message);
                return;
            }

            // Signals SocketServer to wait for OPI_PRESENT coroutine
            WaitForResponse = currentCommand.Contains("OPI_PRESENT") ? true : false;

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
            Main.ExecuteOnMainThread.Enqueue(() => { Main.RunShutdown(); });
        }

        private void OPI_SET_BGROUND(string message)
        {
            Debug.Log("OPI_SET_BGROUND");
        }

        private void OPI_QUERY_DEVICE(string message)
        {
            Debug.Log("OPI_QUERY_DEVICE");
        }

        private void OPI_PRESENT(string message)
        {
            Debug.Log("OPI_PRESENT");
            Debug.Log("OPI_MONO_PRESENT");
            string[] parameters = message.Split(' ');
            if (parameters.Length < 7)
            {
                Debug.LogError("Not enough parameters for OPI_MONO_PRESENT.");
                return;
            }

            StaticStimulus stim = new StaticStimulus()
            {
                X = int.Parse(parameters[2]),
                Y = int.Parse(parameters[3]),
                Size = double.Parse(parameters[4]),
                Duration = double.Parse(parameters[5]),
                ResponseWindow = double.Parse(parameters[6]),
            };

            if (parameters[1][0] == 'L')
            {
                stim.Eye = Eye.Left;
            }
            else if (parameters[1][0] == 'R')
            {
                stim.Eye = Eye.Right;
            }
            else 
            {
                stim.Eye = Eye.Both;
            }

            Main.PresentationControl.Present(stim);
            // kick off something to wait for present to be done to send the response
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