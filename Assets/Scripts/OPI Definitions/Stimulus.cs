using UnityEngine;

namespace Assets.Scripts.OPI_Definitions
{
    /// <summary>
    /// Properties shared by all types of stimulus.
    /// </summary>
    public abstract class Stimulus
    {
        /// <summary>
        /// The eye to present to. Default is "Both".
        /// </summary>
        public Eye Eye { get; set; } = Eye.Both;

        /// <summary>
        /// Coordinate X of the center of stimulus in degrees relative to fixation.
        /// </summary>
        public float X { get; set; }

        /// <summary>
        /// Coordinate Y of the center of stimulus in degrees relative to fixation.
        /// </summary>
        public float Y { get; set; }

        /// <summary>
        /// The stimulus level as a percentage of opacity (0-100) 
        /// </summary>
        public float Level { get; set; }

        /// <summary>
        /// The size of stimulus (diameter in degrees), or a scaling factor for <see cref="Image"/>.
        /// </summary>
        public float Size { get; set; }

        /// <summary>
        /// The color to use for the stimuli. Default is black.
        /// </summary>
        public Color Color { get; set; } = Color.black;
    }
}
