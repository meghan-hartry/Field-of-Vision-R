namespace Assets.Scripts.OPI_Definitions
{
    /// <summary>
    /// An implementation of a KineticStimulus will be a collection of KinecticStimulus to show movement.
    /// </summary>
    public class KineticStimulus : Stimulus
    {
        /// <summary>
        /// The speed (degrees per second) for the stimulus to traverse the <see cref="Coordinate"/>s.
        /// </summary>
        public double Speeds { get; set; }
    }
}
