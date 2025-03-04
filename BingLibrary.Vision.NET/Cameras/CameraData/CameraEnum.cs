using System.ComponentModel;

namespace BingLibrary.Vision.Cameras
{
    public enum CameraBrand
    {
        [Description("海康相机")] HaiKang,
        [Description("大华相机")] DaHua,
        [Description("巴斯勒相机")] Basler,
        [Description("大恒相机")] DaHeng,
    }

    /// <summary>
    /// 触发源
    /// </summary>
    public enum TriggerSource
    {
        [Description("软触发")] Software,
        [Description("线路0")] Line0,
        [Description("线路1")] Line1,
        [Description("线路2")] Line2,
        [Description("线路3")] Line3,
    }

    public enum IOLines
    {
        Line0, Line1, Line2, Line3,
    }

    public enum LineMode
    {
        Input, Output
    }

    public enum LineStatus
    {
        Hight, Low
    }

    public enum TriggerMode
    {
        Off, On
    }

    public enum TriggerPolarity
    {
        RisingEdge, FallingEdge
    }
}