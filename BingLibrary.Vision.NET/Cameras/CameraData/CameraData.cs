namespace BingLibrary.Vision.Cameras
{
    public class CameraInfo
    {
        public string CameraName { get; set; }
        public string CameraSN { get; set; }
        public CameraBrand CameraBrand { get; set; }
        public CameraType CameraType { get; set; }
    }

    public class CameraData
    {
        public TriggerMode triggerMode { get; set; }

        public TriggerSource triggeSource { get; set; }

        public TriggerPolarity triggerPolarity { get; set; }

        public ulong ExpouseTime { get; set; }

        public ushort TriggerFilter { get; set; }

        public ushort TriggerDelay { get; set; }

        public float Gain { get; set; }
    }
}