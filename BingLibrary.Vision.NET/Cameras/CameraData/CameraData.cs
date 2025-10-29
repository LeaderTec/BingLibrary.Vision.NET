using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;

namespace BingLibrary.Vision.Cameras
{

    public abstract class BaseTriggerData
        {
            public int TriggerId { get; set; }
            public string TriggerGuid { get; set; }
         }
    public partial class CameraInfo : ObservableObject
    {

        public string CameraName { get; set; }
        [ObservableProperty]
        public string _cameraSN=""; 
        public string ManufacturerName { set; get; }
        public string CameraIP { get; set; }
        public CameraBrand CameraBrand { get; set; }
        public CameraType CameraType { get; set; }

        //public CameraStatus Status { get; set; }
        //public TriggerSource TriggerSource { get; set; }

        [ObservableProperty]
        private CameraStatus _status;
        [ObservableProperty]
        private TriggerSource _triggerSource;

        //相机用途类型
        /// <summary>
        /// 定义相机的用途，例如通用、线扫、PIN检测等。
        /// 这将决定在运行时如何初始化和使用该相机。
        /// </summary>
        [ObservableProperty]
        private CameraDeviceType _usage;

        // 线扫相机专用参数
        /// <summary>
        /// 如果相机用途是线扫(LineScan)，此属性定义一次完整扫描需要采集的图像数量。
        /// 对于其他类型的相机，此值将被忽略。
        /// </summary>
        [ObservableProperty]
        private int _scanCount = 2; // 给一个默认值

        [ObservableProperty]
        private string _configFilePath;

    }

   


    public class CameraData : ObservableObject
    {
        public TriggerMode triggerMode { get; set; }
        public TriggerSource triggerSource { get; set; }
        public TriggerPolarity triggerPolarity { get; set; }
        public ulong ExpouseTime { get; set; }
        public ushort TriggerFilter { get; set; }
        public ushort TriggerDelay { get; set; }
        public float Gain { get; set; }
    }

    //一个辅助类，用于在XAML中绑定枚举值列表
    public static class CameraUsageTypeHelper
    {
        public static Array CameraUsageTypeValues => Enum.GetValues(typeof(CameraDeviceType));
    }
}