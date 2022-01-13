using System;
using UnityEngine;

namespace Assets.Scripts.OPI_Definitions
{
    /// <summary>
    /// Possible eyes to test.
    /// </summary>
    public enum Eye
    {
        Left,
        Right,
        Both
    }

    /// <summary>
    /// Class definition of a response object.
    /// </summary>
    public class Response
    {
        /// <summary>
        /// Constructor requires Seen parameter to be set.
        /// Time can also be set, but defaults to 0.
        /// </summary>
        public Response(bool seen, int time = 0)
        {
            this.Seen = seen;
            this.Time = time;
        }

        /// <summary>
        /// Response was detected in the allowed ResponseWindow.
        /// </summary>
        public bool Seen { get; set; }

        /// <summary>
        /// The time in milliseconds from the onset (or offset) of the presentation until the response from the subject. 
        /// Value should be 0 if <see cref="Seen"/> is false.
        /// </summary>
        public int Time { get; set; }
    }

    /// <summary>
    /// Defines conversion methods for relevant units.
    /// </summary>
    public static class Conversions
    {
        /// <summary>
        /// Try to parse a string value into an Eye (Left, Right, or Both)
        /// </summary>
        /// <param name="value">String value to be converted</param>
        /// <param name="eye">Eye result</param>
        /// <returns>Success</returns>
        public static bool ToEye(string value, out Eye eye)
        {
            value = value.Trim().ToUpper();

            switch (value) 
            {
                case "L":
                case "LEFT":
                    eye = Eye.Left;
                    return true;

                case "R":
                case "RIGHT":
                    eye = Eye.Right;
                    return true;

                case "B":
                case "BOTH":
                    eye = Eye.Both;
                    return true;

                default:
                    eye = Eye.Both;
                    return false;
            }
        }

        /// <summary>
        /// Try to parse a string value into a color value.
        /// </summary>
        /// <param name="value">Name of a color</param>
        /// <param name="color">Color result</param>
        /// <returns>Success</returns>
        public static bool ToColor(string value, out Color color) => ToColor(new[] { value }, out color);

        /// <summary>
        /// Try to parse a string array into a color value.
        /// </summary>
        /// <param name="value">String aray with name of a color or RBG triplet</param>
        /// <param name="color">Color result</param>
        /// <returns>Success</returns>
        public static bool ToColor(string[] value, out Color color)
        {
            if (value.Length == 1)
            {
                try
                {
                    color = (Color)typeof(Color).GetProperty(value[0].ToLowerInvariant()).GetValue(null, null);
                    return true;
                }
                catch
                {
                    color = Color.white;
                    return false;
                }
            }
            else if (value.Length == 3)
            {
                var redSuccess = RGBToColorValue(value[0], out float red);
                var greenSuccess = RGBToColorValue(value[1], out float green);
                var blueSuccess = RGBToColorValue(value[2], out float blue);
                var success = redSuccess && greenSuccess && blueSuccess;

                if (success)
                {
                    color = new Color(red, green, blue);
                    return true;
                }
            }

            color = Color.white;
            return false;
        }

        /// <summary>
        /// Try to convert an RBG value (0-255), to its UnityEngine.Color value (0-1) equivalent.
        /// Override method that converts a string value to a float.
        /// </summary>
        /// <param name="value">RBG value to be converted</param>
        /// <param name="colorValue">Color value result</param>
        /// <returns>Success</returns>
        private static bool RGBToColorValue(string value, out float percent) 
        {
            // If the float parse is successful
            if (float.TryParse(value, out percent))
            {
                // Convert to ColorValue and return success
                return RGBToColorValue(ref percent);
            }
            return false;
        }

        /// <summary>
        /// Try to convert an RBG value (0-255), to its UnityEngine.Color value (0-1) equivalent.
        /// </summary>
        /// <param name="value">RBG value to be converted</param>
        /// <param name="colorValue">Color value result</param>
        /// <returns>Success</returns>
        private static bool RGBToColorValue(ref float colorValue)
        {
            if (colorValue > 255)
            {
                colorValue = 255;
                return false;
            }
            else if (colorValue < 0)
            {
                colorValue = 0;
                return false;
            }
            else
            {
                colorValue /= 255F;
                return true;
            }
        }

        /// <summary>
        /// Try to convert a value (0-100), to its alpha (0-1) equivalent.
        /// Overload method that converts a string value to a float.
        /// </summary>
        /// <param name="value">Value to be converted</param>
        /// <param name="alpha">Alpha result</param>
        /// <returns>Success</returns>
        public static bool ToAlpha(string value, out float alpha)
        {
            // If the float parse is successful
            if (float.TryParse(value, out alpha)) 
            {
                // Convert to alpha and return success
                return ToAlpha(ref alpha); 
            }
            return false;
        }

        /// <summary>
        /// Try to convert a value (0-100), to its alpha (0-1) equivalent.
        /// </summary>
        /// <param name="value">Value to be converted</param>
        /// <param name="alpha">Alpha result/param>
        /// <returns>Success</returns>
        public static bool ToAlpha(ref float alpha)
        {
            if (alpha > 100)
            {
                alpha = 100;
                return false;
            }
            else if (alpha < 0)
            {
                alpha = 0;
                return false;
            }
            else
            {
                alpha /= 100F;
                return true;
            }
        }
    }
}
