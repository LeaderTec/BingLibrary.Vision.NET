using BingLibrary.Vision.Cameras;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BingLibrary.Vision.Cameras
{
    /// <summary>
    /// 相机工厂接口，用于创建和管理不同品牌的相机实例
    /// </summary>
    public interface ICamFactory<T>
    {
        /// <summary>
        /// 软触发图像捕获事件
        /// </summary>
        event EventHandler<ImageCapturedEventArgs<T>> SoftTriggerImageCaptured;

        /// <summary>
        /// 相机状态变化事件
        /// </summary>
        event EventHandler<CameraStatusChangedEventArgs> CameraStatusChanged;

        /// <summary>
        /// 相机发现事件
        /// </summary>
        event EventHandler<CameraDiscoveredEventArgs> CameraDiscovered;


        /// <summary>
        /// 连接指定SN的相机并设置实时触发出图事件
        /// </summary>
        /// <param name="cameraInfoInput">相机信息</param>
        /// <param name="config">可选的相机配置参数</param>
        /// <returns>是否成功连接并设置</returns>
        bool StartWithContinue(CameraInfo cameraInfoInput, T triggerData, int exposureTime = 10000);

        /// <summary>
        /// 连接指定SN的相机并设置软触发出图事件
        /// </summary>
        /// <param name="cameraInfoInput">相机信息</param>
        /// <param name="config">可选的相机配置参数</param>
        /// <returns>是否成功连接并设置</returns>
        bool ConnectAndSetStartMode(CameraInfo cameraInfoInput);

        /// <summary>
        /// 对指定相机执行软触发并获取图像
        /// </summary>
        /// <param name="cameraSN">相机序列号</param>
        /// <param name="triggerData">触发数据</param>
        /// <param name="timeoutMs">超时时间(毫秒)</param>
        /// <returns>是否成功触发</returns>
        bool ExecuteSoftTrigger(CameraInfo cameraInfoInput, T triggerData, int exposureTime = 1000, Action? onFinished = null);

        /// <summary>
        /// 初始化并加载所有可用相机
        /// </summary>
        /// <returns>初始化成功的相机数量</returns>
        int InitializeAllCameras();

        /// <summary>
        /// 初始化特定品牌的相机
        /// </summary>
        /// <param name="brand">相机品牌</param>
        /// <returns>初始化成功的相机数量</returns>
        int InitializeCamerasByBrand(CameraBrand brand);

        /// <summary>
        /// 根据相机SN获取相机实例
        /// </summary>
        /// <param name="cameraSN">相机序列号</param>
        /// <returns>相机实例</returns>
        ICamera<T> GetCameraBySN(string cameraSN);

        /// <summary>
        /// 获取所有已加载的相机
        /// </summary>
        /// <returns>相机列表</returns>
        List<ICamera<T>> GetAllCameras();

        /// <summary>
        /// 获取特定品牌的所有相机
        /// </summary>
        /// <param name="brand">相机品牌</param>
        /// <returns>相机列表</returns>
        List<ICamera<T>> GetCamerasByBrand(CameraBrand brand);

        /// <summary>
        /// 获取特定类型的所有相机
        /// </summary>
        /// <param name="type">相机类型</param>
        /// <returns>相机列表</returns>
        List<ICamera<T>> GetCamerasByType(CameraType type);

        /// <summary>
        /// 释放所有相机资源
        /// </summary>
        void ReleaseAllCameras();

        /// <summary>
        /// 注销特定相机
        /// </summary>
        /// <param name="cameraSN">相机序列号</param>
        /// <returns>是否成功</returns>
        bool ReleaseCamera(string cameraSN);

        /// <summary>
        /// 获取是否已初始化
        /// </summary>
        /// <returns>是否已初始化</returns>
        bool IsInitialized();

        /// <summary>
        /// 获取所有相机的状态
        /// </summary>
        /// <returns>相机状态字典</returns>
        Dictionary<string, CameraStatus> GetAllCamerasStatus();

        /// <summary>
        /// 获取特定相机的状态
        /// </summary>
        /// <param name="cameraSN">相机序列号</param>
        /// <returns>相机状态</returns>
        CameraStatus GetCameraStatus(string cameraSN);

        /// <summary>
        /// 保存所有相机配置
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>是否成功</returns>
        bool SaveAllCamerasConfig(string filePath);

        /// <summary>
        /// 加载相机配置
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>是否成功</returns>
        bool LoadAllCamerasConfig(string filePath);

        /// <summary>
        /// 按指定配置初始化相机
        /// </summary>
        /// <param name="cameraSN">相机序列号</param>
        /// <param name="config">相机配置</param>
        /// <returns>是否成功</returns>
        bool InitializeCameraWithConfig(string cameraSN, CameraData config);

        /// <summary>
        /// 开始监听新相机
        /// </summary>
        void StartCameraDiscovery();

        /// <summary>
        /// 停止监听新相机
        /// </summary>
        void StopCameraDiscovery();

        /// <summary>
        /// 开始相机健康检查
        /// </summary>
        /// <param name="interval">检查间隔</param>
        void StartHealthCheck(TimeSpan interval);

        /// <summary>
        /// 停止相机健康检查
        /// </summary>
        void StopHealthCheck();
    }


    /// <summary>
    /// 相机状态枚举
    /// </summary>
    public enum CameraStatus
    {
        [Description("未连接")] Disconnected,
        [Description("已连接")] Connected,
        [Description("正在采集")] Grabbing,
        [Description("错误状态")] Error,
    }
    /// <summary>
    /// 图像捕获事件参数
    /// </summary>
    public class ImageCapturedEventArgs<T> : EventArgs
    {
        /// <summary>
        /// 相机序列号
        /// </summary>
        public string CameraSN { get; set; }

        /// <summary>
        /// 捕获的图像
        /// </summary>
        public System.Drawing.Bitmap Image { get; set; }

        /// <summary>
        /// 触发数据
        /// </summary>
        public T TriggerData { get; set; }
    }


    /// <summary>
    /// 相机状态变化事件参数
    /// </summary>
    public class CameraStatusChangedEventArgs : EventArgs
    {
        /// <summary>
        /// 相机序列号
        /// </summary>
        public string CameraSN { get; set; }

        /// <summary>
        /// 新状态
        /// </summary>
        public CameraStatus NewStatus { get; set; }

        /// <summary>
        /// 旧状态
        /// </summary>
        public CameraStatus OldStatus { get; set; }
    }

    /// <summary>
    /// 相机发现事件参数
    /// </summary>
    public class CameraDiscoveredEventArgs : EventArgs
    {
        /// <summary>
        /// 相机信息
        /// </summary>
        public CameraInfo CameraInfo { get; set; }
    }

    /// <summary>
    /// 相机工厂异常类
    /// </summary>
    public class CameraFactoryException : Exception
    {
        /// <summary>
        /// 失败的相机列表
        /// </summary>
        public List<string> FailedCameras { get; private set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="message">异常消息</param>
        /// <param name="innerException">内部异常</param>
        /// <param name="failedCameras">失败的相机列表</param>
        public CameraFactoryException(string message, Exception innerException, List<string> failedCameras)
            : base(message, innerException)
        {
            FailedCameras = failedCameras ?? new List<string>();
        }
    }
}
