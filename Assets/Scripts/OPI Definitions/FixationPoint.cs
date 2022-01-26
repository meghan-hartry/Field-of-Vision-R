using System;
using UnityEngine;

namespace Assets.Scripts.OPI_Definitions
{
    public class FixationPoint
    {
        /// <summary>
        /// The eye to present to. Default is "Both".
        /// </summary>
        public Eye Eye { get; set; } = Eye.Both;

        /// <summary>
        /// Coordinate X of the center of fixation point.
        /// </summary>
        public float X { get; set; }

        /// <summary>
        /// Coordinate Y of the center of fixation point.
        /// </summary>
        public float Y { get; set; }

        /// <summary>
        /// Horizontal scale of the fixation point.
        /// </summary>
        public float SizeX { get; set; }

        /// <summary>
        /// Vertical scale of the fixation point.
        /// </summary>
        public float SizeY { get; set; }

        /// <summary>
        /// The color to use for the fixation point. Default is black.
        /// </summary>
        public Color Color { get; set; } = Color.black;

        /// <summary>
        /// The alpha (0-1) to use for the fixation point. Default is 1.
        /// </summary>
        public float Alpha { get; set; } = 1;

        /// <summary>
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
        /// </summary>
        /// <param name="parameters">String parameters to parse</param>
        /// <param name="fixationPoint">Resulting FixationPoint object</param>
        /// <returns>Error Message</returns>
        internal static string CreateFixationPoint(string[] parameters, out FixationPoint fixationPoint)
        {
            fixationPoint = null;
            string errorMessage = string.Empty;

            if (parameters.Length != 11)
            {
                errorMessage += "Not the correct number of parameters for a fixation point.\n";
                return errorMessage;
            }

            var success = Conversions.ToEye(parameters[0], out Eye eye);
            if (!success) errorMessage += "Parameter for OPI_SET_BGROUND Eye: " + parameters[5] + " was invalid.\n";

            success &= float.TryParse(parameters[2], out float x);
            if (!success) errorMessage += "Parameter for OPI_SET_BGROUND Fixation X: " + parameters[0] + " was invalid.\n";

            success &= float.TryParse(parameters[3], out float y);
            if (!success) errorMessage += "Parameter for OPI_SET_BGROUND Fixation Y: " + parameters[1] + " was invalid.\n";

            success &= float.TryParse(parameters[4], out float sizeX);
            if (!success) errorMessage += "Parameter for OPI_SET_BGROUND Fixation Horizontal Size: " + parameters[2] + " was invalid.\n";

            success &= float.TryParse(parameters[5], out float sizeY);
            if (!success) errorMessage += "Parameter for OPI_SET_BGROUND Fixation Veritical Size: " + parameters[3] + " was invalid.\n";

            success &= Conversions.ToAlpha(parameters[6], out float alpha);
            if (!success) errorMessage += "Parameter for OPI_SET_BGROUND Fixation Alpha: " + parameters[6] + " was invalid.\n";

            string[] colorParameters = new string[4];
            Array.Copy(parameters, 7, colorParameters, 0, 4);
            success &= Conversions.ToColor(colorParameters, out Color color);
            if (!success) errorMessage += "Parameters for OPI_SET_BGROUND Fixation Color: " + string.Join(' ', colorParameters) + " were invalid.\n";

            if (success)
            {
                fixationPoint = new FixationPoint()
                {
                    Eye = eye,
                    X = x,
                    Y = y,
                    SizeX = sizeX,
                    SizeY = sizeY,
                    Color = color,
                    Alpha = alpha
                };
            }

            return errorMessage;
        }
    }
}
