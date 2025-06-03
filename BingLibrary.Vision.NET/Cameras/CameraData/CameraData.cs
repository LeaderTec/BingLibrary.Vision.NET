using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;

namespace BingLibrary.Vision.Cameras
{
    public partial class CameraInfo : ObservableObject
    {
        public string CameraName { get; set; }
        [ObservableProperty]
        public string _cameraSN;
        public CameraBrand CameraBrand { get; set; }
        public CameraType CameraType { get; set; }
        public CameraStatus Status { get; set; }
        public TriggerSource TriggeSource { get; set; }
    }

    public class CameraData : ObservableObject
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