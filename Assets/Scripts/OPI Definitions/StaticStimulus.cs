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
        public double Duration { get; set; }

        /// <summary>
        /// Maximum time (>= 0) in milliseconds to wait for a response.
        /// </summary>
        public double ResponseWindow { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="stimulus"></param>
        /// <returns></returns>
        internal static bool CreateStaticStimulus(string[] parameters, out StaticStimulus stimulus)
        {
            stimulus = null;

            if (parameters.Length < 7)
            {
                Debug.LogError("Not enough parameters for OPI_PRESENT.");
                return false;
            }

            bool success;

            success = Conversions.ToEye(parameters[0], out Eye eye);
            if (!success) Debug.Log("Parameter for OPI_PRESENT Eye: " + eye + " was invalid.");

            success &= int.TryParse(parameters[1], out int x);
            if (!success) Debug.Log("Parameter for OPI_PRESENT X: " + x + " was invalid.");

            success &= int.TryParse(parameters[2], out int y);
            if (!success) Debug.Log("Parameter for OPI_PRESENT Y: " + y + " was invalid.");

            success &= Conversions.ToAlpha(parameters[3], out float level);
            if (!success) Debug.Log("Parameter for OPI_PRESENT Level: " + level + " was invalid.");

            success &= double.TryParse(parameters[4], out double size);
            if (!success) Debug.Log("Parameter for OPI_PRESENT Size: " + size + " was invalid.");

            success &= double.TryParse(parameters[5], out double duration);
            if (!success) Debug.Log("Parameter for OPI_PRESENT Duration: " + duration + " was invalid.");

            success &= double.TryParse(parameters[6], out double responseWindow);
            if (!success) Debug.Log("Parameter for OPI_PRESENT Response: " + responseWindow + " was invalid.");

            if (success)
            {
                stimulus = new StaticStimulus()
                {
                    Eye = eye,
                    X = x,
                    Y = y,
                    Level = level,
                    Size = size,
                    Duration = duration,
                    ResponseWindow = responseWindow
                };

                return true;
            }

            return false;
        }
    }
}
