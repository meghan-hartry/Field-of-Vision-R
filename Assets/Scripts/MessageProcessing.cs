using Assets.Scripts.OPI_Definitions;
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace FieldofVision
{
    public class MessageProcessing
    {
        internal static bool Exited = false;
        internal IEnumerator BeginProcessing()
        {
            // handle messages
            while (!MainExecution.Shutdown)
            {
                if (!MainExecution.Messages.IsEmpty)
                {
                    Debug.Log("Dequeueing message.");
                    var success = MainExecution.Messages.TryDequeue(out var msg);
                    if (success)
                    {
                        ProcessMessage(msg);
                    }
                    else
                    {
                        Debug.LogWarning("Failed to dequeue message.");
                    }
                }
                yield return null; // wait until next frame
            }
            Exited = true;
            Debug.Log("Shutting down Message Processing.");
        }

        private void ProcessMessage(string message)
        {
            Debug.Log("Processing message: " + message);
            string[] commands = new string[] { "OPI_CLOSE", "OPI_GET_RES", "OPI_IMAGE", "OPI_PRESENT", "OPI_SET_BGROUND", "OPI_BIN_FIXATION", "OPI_BIN_PRESENT", "OPI_MONO_BG_ADD", "OPI_MONO_SET_BG", "OPI_MONO_PRESENT", "OPI_SET_FOVY" };

            string currentCommand = commands.FirstOrDefault(s => message.Contains(s));

            if (string.IsNullOrEmpty(currentCommand))
            {
                Debug.LogError("OPI command not recognized: " + message);
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
            MainExecution.RunShutdown();
        }

        /// <summary>
        /// Return the resolution of the VR device.
        /// </summary>
        private void OPI_GET_RES(string message)
        {
            Debug.Log("OPI_GET_RES");
        }

        private void OPI_IMAGE(string message)
        {
            Debug.Log("OPI_IMAGE");
        }

        private void OPI_PRESENT(string message)
        {
            Debug.Log("OPI_PRESENT");
        }

        private void OPI_SET_BGROUND(string message)
        {
            Debug.Log("OPI_SET_BGROUND");
        }

        private void OPI_BIN_FIXATION(string message)
        {
            Debug.Log("OPI_BIN_FIXATION");
        }

        private void OPI_BIN_PRESENT(string message)
        {
            Debug.Log("OPI_BIN_PRESENT");
        }

        private void OPI_MONO_BG_ADD(string message)
        {
            Debug.Log("OPI_MONO_BG_ADD");
        }

        private void OPI_MONO_SET_BG(string message)
        {
            Debug.Log("OPI_MONO_SET_BG");
        }

        private void OPI_MONO_PRESENT(string message) 
        {
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

            var response = MainExecution.PresentationControl.Present(stim);
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