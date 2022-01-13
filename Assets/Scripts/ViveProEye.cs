using Assets.Scripts.OPI_Definitions;

namespace FieldofVision
{
    /// <summary>
    /// This class defines the OPI implementation on a VIVE device using Unity.
    /// </summary>
    public class ViveProEye
    {
        public double MinStimulus { get; set; } = 10;

        public double MaxStimulus { get; set; } = 143;

        public double VerticalResolution { get; set; } = 1440;

        public double HorizontalResolution { get; set; } = 1600;
    }
}