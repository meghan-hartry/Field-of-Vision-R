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
            string[] commands = new string[] { "OPI_QUERY_DEVICE",  "OPI_SET_BGROUND", "OPI_SET_FOVY", "OPI_PRESENT", "OPI_CLOSE" };

            string currentCommand = commands.FirstOrDefault(s => message.Contains(s));

            if (string.IsNullOrEmpty(currentCommand))
            {
                Main.ErrorOccurred.Invoke("OPI command not recognized: " + message);
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
            //var connected = TCPServer.Connected();
            TCPServer.Write(BitConverter.GetBytes(true));
        }

        /// <summary>
        ///  Sets the background and fixation point.
        ///  parameter 0:   eye to set (default is both)
        ///  parameter 1:   fixation type (currently ignored)
        ///  parameter 2:   fixation X
        ///  parameter 3:   fixation Y
        ///  parameter 4:   fixation horizontal size
        ///  parameter 5:   fixation vertical size
        ///  parameter 6:   fixation alpha
        ///  parameter 7:   fixation color (string color name)
        ///  parameter 8:   fixation color (float RGB red value)
        ///  parameter 9:   fixation color (float RGB green value)
        ///  parameter 10:  fixation color (float RGB blue value)
        ///  parameter 11:  background color (string color name)
        ///  parameter 12:  background color (float RGB red value)
        ///  parameter 13:  background color (float RGB green value)
        ///  parameter 14:  background color (float RGB blue value)
        /// </summary>
        private void OPI_SET_BGROUND(string message)
        {
            Debug.Log("OPI_SET_BGROUND");
            string[] parameters = message.Trim().Split(' ').Skip(1).ToArray();

            if (parameters.Length != 15)
            {
                Main.ErrorOccurred.Invoke("Not the correct number of parameters for OPI_SET_BGROUND: " + parameters.Length + ". Should be 15.");
                //TCPServer.Write(BitConverter.GetBytes(false));
                return;
            }

            // Set Fixation
            string[] fixationParameters = new string[11];
            Array.Copy(parameters, 0, fixationParameters, 0, 11);

            var errorMessage = FixationPoint.CreateFixationPoint(fixationParameters, out FixationPoint fixationPoint);
            if (errorMessage != string.Empty)
            {
                Main.ErrorOccurred.Invoke(errorMessage);
                return;
            }

            Main.DoInMainThread(() => { Main.PresentationControl.SetFixation(fixationPoint); });

            // Set Background
            string[] colorParameters = new string[4];
            Array.Copy(parameters, 11, colorParameters, 0, 4);

            var success = Conversions.ToColor(colorParameters, out Color bgColor);
            if (!success) 
            {
                Main.ErrorOccurred.Invoke("Parameters for OPI_SET_BGROUND were invalid.");
                //TCPServer.Write(BitConverter.GetBytes(false));
                return;
            }

            Main.DoInMainThread(() => { Main.PresentationControl.SetBackground(bgColor); });

            //TCPServer.Write(BitConverter.GetBytes(true));
        }

        /// <summary>
        ///  Set field of view in the y-axis in degrees of visual angle.
        /// </summary>
        private void OPI_SET_FOVY(string message)
        {
            Debug.Log("OPI_SET_FOVY");
            // todo
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

            if (parameters.Length != 11)
            {
                Main.ErrorOccurred.Invoke("Not the correct number of parameters for OPI_PRESENT: " + parameters.Length + ". Should be 11.");
                return;
            }

            var errorMessage = StaticStimulus.CreateStaticStimulus(parameters, out StaticStimulus stimulus);
            if (errorMessage != string.Empty)
            {
                Main.ErrorOccurred.Invoke(errorMessage);
                return;
            }

            Main.PresentationControl.Present(stimulus);
        }

        /// <summary>
        /// Close socket connection and exits program.
        /// </summary>
        private void OPI_CLOSE(string message)
        {
            Debug.Log("OPI_CLOSE");
            //Main.DoInMainThread(() => { Main.RunShutdown(); });
        }
    }
}