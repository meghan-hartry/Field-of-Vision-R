using Assets.Scripts.OPI_Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace FieldofVision
{
    /// <summary>
    /// Defines the messages sent and received with the OPI R package.
    /// </summary>
    internal class InputProcessing : MonoBehaviour
    {
        #region Properties and Fields

        private MainExecution Main => MainExecution.MainInstance;

        #endregion

        #region MonoBehavior Override Methods

        void Update()
        {
            for (int i=0; i < Main.Server.Buffer.Count; i++)
            {
                Main.Server.Buffer.TryDequeue(out var message);
                ProcessMessage(message);
            }
        }
        #endregion

        #region Private Methods

        /// <summary>
        /// Parse which method to execute from message string.
        /// </summary>
        private void ProcessMessage(string message)
        {
            Debug.Log("Processing message: " + message);
            string[] commands = new string[] { "OPI_QUERY_DEVICE",  "OPI_SET_BGROUND", "OPI_PRESENT", "OPI_CLOSE" };

            string currentCommand = commands.FirstOrDefault(s => message.Contains(s));

            if (string.IsNullOrEmpty(currentCommand))
            {
                Debug.LogError("OPI command not recognized: " + message);
                return;
            }

            // Call method with the same string name as the command
            MethodInfo theMethod = GetType().GetMethod(currentCommand, BindingFlags.NonPublic | BindingFlags.Instance);
            theMethod.Invoke(this, new[] { message });                                                                                                                                                                                                                                                                          
        }

        /// <summary>
        /// Responds with true if a client is connected to the server.
        /// </summary>
        private void OPI_QUERY_DEVICE(string message)
        {
            Debug.Log("OPI_QUERY_DEVICE");

            Main.Server.Write(BitConverter.GetBytes(Main.Server.IsConnected()));
        }

        /// <summary>
        ///  Sets the background and fixation point.
        ///  parameter 0:   fixation type (currently ignored)
        ///  parameter 1:   fixation X
        ///  parameter 2:   fixation Y
        ///  parameter 3:   fixation horizontal size
        ///  parameter 4:   fixation vertical size
        ///  parameter 5:   fixation alpha
        ///  parameter 6:   fixation color (string color name)
        ///  parameter 7:   fixation color (float RGB red value)
        ///  parameter 8:   fixation color (float RGB green value)
        ///  parameter 9:   fixation color (float RGB blue value)
        ///  parameter 10:  background alpha (currently ignored)
        ///  parameter 11:  background color (string color name)
        ///  parameter 12:  background color (float RGB red value)
        ///  parameter 13:  background color (float RGB green value)
        ///  parameter 14:  background color (float RGB blue value)
        ///  parameter 15:  eye (active background configuration)
        /// </summary>
        private void OPI_SET_BGROUND(string message)
        {
            Debug.Log("OPI_SET_BGROUND");
            string[] parameters = message.Trim().Split(' ').Skip(1).ToArray();
            string errorMessage = string.Empty;
            if (parameters.Length != 16) errorMessage += "Not the correct number of parameters for OPI_SET_BGROUND: " + parameters.Length + ". Should be 16.";

            // Get Fixation
            string[] fixationParameters = new string[10];
            Array.Copy(parameters, 0, fixationParameters, 0, 10);
            errorMessage += FixationPoint.CreateFixationPoint(fixationParameters, out FixationPoint fixationPoint);

            // Get Background
            var success = Conversions.ToAlpha(parameters[10], out float alpha); // todo: property ignored as background opacity works differently.
            if (!success) errorMessage += "Parameter for OPI_SET_BGROUND Background Alpha: " + parameters[10] + " was invalid.\n";

            string[] colorParameters = new string[4];
            Array.Copy(parameters, 11, colorParameters, 0, 4);
            success = Conversions.ToColor(colorParameters, out Color bgColor);
            if (!success) errorMessage += "Parameters for OPI_SET_BGROUND Color: " + string.Join(" ", colorParameters) + " were invalid.\n";

            // Get eye
            success = Conversions.ToEye(parameters[15], out Eye eye);
            if (!success) errorMessage += "Parameter for OPI_SET_BGROUND Eye: " + parameters[15] + " was invalid.\n";

            // Check for errors
            if (errorMessage != string.Empty)
            {
                Debug.LogError(errorMessage);
                Main.Server.Write(BitConverter.GetBytes(false));
                return;
            }

            // Set Background, Fixation, and Active Eye
            Main.DoInMainThread(() => { Main.Draw.SetBackground(bgColor, fixationPoint, eye); });

            Main.Server.Write(BitConverter.GetBytes(true));
        }

        /// <summary>
        /// Present a stimulus.
        ///  parameter 0:   eye to present to (default is both)
        ///  parameter 1:   X position
        ///  parameter 2:   Y position
        ///  parameter 3:   alpha
        ///  parameter 4:   size
        ///  parameter 5:   duration
        ///  parameter 6:   response window
        ///  parameter 7:   color (string color name)
        ///  parameter 8:   color (float RGB red value)
        ///  parameter 9:   color (float RGB green value)
        ///  parameter 10:  color (float RGB blue value)
        /// </summary>
        private void OPI_PRESENT(string message)
        {
            Debug.Log("OPI_PRESENT");
            string[] parameters = message.Trim().Split(' ').Skip(1).ToArray();
            string errorMessage = string.Empty;
            if (parameters.Length != 11) errorMessage += "Not the correct number of parameters for OPI_PRESENT: " + parameters.Length + ". Should be 11.";

            // Get stimulus
            errorMessage += StaticStimulus.CreateStaticStimulus(parameters, out StaticStimulus stimulus);

            // Check for errors
            if (errorMessage != string.Empty)
            {
                Debug.LogError(errorMessage);

                // Create a message indicating error.  
                List<byte> byteData = new List<byte>();
                byteData.AddRange(BitConverter.GetBytes(0));
                byteData.AddRange(BitConverter.GetBytes(3));

                Main.Server.Write(byteData.ToArray());
                return;
            }

            Main.Draw.Present(stimulus);
        }

        /// <summary>
        /// Close socket connection and exits program.
        /// </summary>
        private void OPI_CLOSE(string message)
        {
            Debug.Log("OPI_CLOSE");
            Main.Server.Write(BitConverter.GetBytes(true));
            Main.Shutdown();
        }

        #endregion
    }
}