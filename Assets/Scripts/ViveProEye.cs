namespace FieldofVision
{
    /// <summary>
    /// This class defines the OPI implementation on a VIVE device using Unity.
    /// </summary>
    public class ViveProEye
    {
        ///<inheritdoc />
        public double MinStimulus { get; set; } = 10;

        ///<inheritdoc />
        public double MaxStimulus { get; set; } = 143;

        ///<inheritdoc />
        public double VerticalResolution { get; set; } = 1440;

        ///<inheritdoc />
        public double HorizontalResolution { get; set; } = 1600;

        ///<inheritdoc />
        public double ToAlpha(double cd)
        {
            // Unity already does Gamma correction. Just need to convert to a value between 0-1 based off percentage of max display brightness.
            if (cd < this.MinStimulus) { cd = this.MinStimulus; }
            if (cd > this.MaxStimulus) { cd = this.MaxStimulus; }

            return cd / this.MaxStimulus;
        }

        ///<inheritdoc />
        public float[] ToVector(double x, double y)
        {
            return new[] { (float)x, (float)y };
        }
    }
}