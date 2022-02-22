using System;
using UnityEngine;

namespace Assets.Scripts.OPI_Definitions
{
    /// <summary>
    /// A single, motionless static stimulus.
    /// </summary>
    public class StaticStimulus : Stimulus
    {
        /// <summary>
        /// Total stimulus duration in milliseconds (>= 0).
        /// </summary>
        public int Duration { get; set; }

        /// <summary>
        /// Maximum time (>= 0) in milliseconds to wait for a response.
        /// </summary>
        public int ResponseWindow { get; set; }

        /// <summary>
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
        /// <param name="parameters">String parameters to parse</param>
        /// <param name="stimulus">Resulting Stimulus object</param>
        /// <returns>Error Message</returns>
        internal static string CreateStaticStimulus(string[] parameters, out StaticStimulus stimulus)
        {
            stimulus = null;
            string errorMessage = string.Empty;

            if (parameters.Length != 11)
            {
                errorMessage += "Not the correct number of parameters for a static stimulus.\n";
                return errorMessage;
            }

            var success = Conversions.ToEye(parameters[0], out Eye eye);
            if (!success) errorMessage += "Parameter for OPI_PRESENT Eye: " + parameters[0] + " was invalid.\n";

            success = float.TryParse(parameters[1], out float x);
            if (!success) errorMessage += "Parameter for OPI_PRESENT X: " + parameters[1] + " was invalid.\n";

            success = float.TryParse(parameters[2], out float y);
            if (!success) errorMessage += "Parameter for OPI_PRESENT Y: " + parameters[2] + " was invalid.\n";

            success = Conversions.ToAlpha(parameters[3], out float alpha);
            if (!success) errorMessage += "Parameter for OPI_PRESENT Level: " + parameters[3] + " was invalid.\n";

            success = float.TryParse(parameters[4], out float size);
            if (!success) errorMessage += "Parameter for OPI_PRESENT Size: " + parameters[4] + " was invalid.\n";

            success = int.TryParse(parameters[5], out int duration);
            if (!success) errorMessage += "Parameter for OPI_PRESENT Duration: " + parameters[5] + " was invalid.\n";

            success = int.TryParse(parameters[6], out int responseWindow);
            if (!success) errorMessage += "Parameter for OPI_PRESENT Response: " + parameters[6] + " was invalid.\n";

            string[] colorParameters = new string[4];
            Array.Copy(parameters, 7, colorParameters, 0, 4);
            success = Conversions.ToColor(colorParameters, out Color color);
            if (!success) errorMessage += "Parameter for OPI_PRESENT Color: " + string.Join(' ', colorParameters) + " were invalid.\n";

            if (errorMessage == string.Empty)
            {
                stimulus = new StaticStimulus()
                {
                    Eye = eye,
                    X = x,
                    Y = y,
                    Level = alpha,
                    Size = size,
                    Duration = duration,
                    ResponseWindow = responseWindow,
                    Color = color
                };
            }

            return errorMessage;
        }
    }
}
