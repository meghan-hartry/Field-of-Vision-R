namespace Assets.Scripts.OPI_Definitions
{
    /// <summary>
    /// Properties shared by all types of stimulus.
    /// </summary>
    public class Stimulus
    {
        /// <summary>
        /// The color to use for the stimuli. Default is "White".
        /// </summary>
        public Eye Eye { get; set; } = Eye.Left;

        /// <summary>
        /// Coordinate X of the center of stimulus in degrees relative to fixation.
        /// </summary>
        public double X { get; set; }

        /// <summary>
        /// Coordinate Y of the center of stimulus in degrees relative to fixation.
        /// </summary>
        public double Y { get; set; }

        /// <summary>
        /// The stimulus level in cd/m^2. 
        /// </summary>
        public double Level { get; set; }

        /// <summary>
        /// The image to display. Can be null.
        /// </summary>
        public string Image { get; set; }

        /// <summary>
        /// The size of stimulus (diameter in degrees), or a scaling factor for <see cref="Image"/>.
        /// </summary>
        public double Size { get; set; }

        /// <summary>
        /// The color to use for the stimuli. Default is "White".
        /// </summary>
        public string Color { get; set; } = "White";
    }
}
