namespace Assets.Scripts.OPI_Definitions
{
    /// <summary>
    /// List containing stimulus parameters with an S3 class attribute of TemporalStimulus.
    /// </summary>
    public class TemporalStimulus : Stimulus
    {
        /// <summary>
        /// Frequency with which lut is processed in Hz.
        /// </summary>
        public double Rate { get; set; }

        /// <summary>
        /// Total length of stimulus flash in milliseconds. There is no guarantee that duration \%\% length(lut)/rate == 0. 
        /// That is, the onus is on the user to ensure the duration is a multiple of the period of the stimuli.
        /// </summary>
        public double Duration { get; set; }

        /// <summary>
        /// Maximum time (>= 0) in milliseconds to wait for a response from the onset of the stimulus presentation.
        /// </summary>
        public double ResponseWindow { get; set; }
    }
}
