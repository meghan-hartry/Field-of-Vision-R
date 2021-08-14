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
    }
}
