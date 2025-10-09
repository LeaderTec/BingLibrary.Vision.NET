using BingLibrary.Vision.Cameras;
using HalconDotNet;
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
    public interface ICamManager<T>
    {
        /// <summary>
        /// 软触发图像捕获事件
        /// </summary>
        event EventHandler<ImageCapturedEventArgs<T>> SoftTriggerImageCaptured;


        /// <summary>
        /// 软触发3D图像捕获事件 (用于3D相机, 输出HImage)
        /// </summary>
        event EventHandler<Image3DCapturedEventArgs<T>> SoftTrigger3DImageCaptured;

        /// <summary>
        /// 相机状态变化事件
        /// </summary>
        event EventHandler<CameraStatusChangedEventArgs> CameraStatusChanged;

        /// <summary>
        /// 相机发现事件
        /// </summary>
        event EventHandler<CameraDiscoveredEventArgs> CameraDiscovered;

        /// <summary>
        /// 当线扫相机采集完一张图像时触发
        /// </summary>
        public event EventHandler<LineScanImageAcquiredEventArgs> LineScanImageAcquired;

        /// <summary>
        /// 当线扫相机完成一批次扫描时触发
        /// </summary>
        public event EventHandler<LineScanBatchCompletedEventArgs<T>> LineScanBatchCompleted;

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
        /// 相机触发，最终目标是业务层只需要调用此方法即可完成触发和图像获取，无需关心触发方式
        /// </summary>
        /// <param name="cameraInfoInput"></param>
        /// <param name="triggerData"></param>
        /// <param name="exposureTime"></param>
        /// <param name="onFinished"></param>
        /// <returns></returns>
        Task<bool> ExecuteTrigger(CameraInfo cameraInfoInput, T triggerData, int exposureTime = 10000, Action? onFinished = null);

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

        /// <summary>
        /// 线扫相机硬件触发设置
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        Task<bool> SetupLineScanHardTriggerAsync(LineScanHardTriggerParameters<T> parameters);

    }




    /// <summary>
    /// [新增] 为3D相机图像捕获事件定义参数类。
    /// 包含深度图、亮度图和触发数据。
    /// </summary>
    /// <typeparam name="T">触发数据类型</typeparam>
    public class Image3DCapturedEventArgs<T> : EventArgs
    {
        /// <summary>
        /// 触发相机的序列号
        /// </summary>
        public string CameraSN { get; set; }
        /// <summary>
        /// 深度图像 (Halcon HImage)
        /// </summary>
        public HImage DepthImage { get; set; }
        /// <summary>
        /// 亮度图像/2D图像 (Halcon HImage)
        /// </summary>
        public HImage IntensityImage { get; set; }
        /// <summary>
        /// 伴随的触发数据
        /// </summary>
        public T TriggerData { get; set; }
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


    #region 线扫相机相关参数和事件 
    /// <summary>
    /// 用于配置线扫相机硬件触发的参数对象
    /// </summary>
    /// <typeparam name="T">与相机工厂一致的触发数据类型</typeparam>
    public class LineScanHardTriggerParameters<T>
    {
        /// <summary>
        /// 相机的序列号 (SN)
        /// </summary>
        public string CameraSN { get; set; }
        public string CameraName { get; set; }

        /// <summary>
        /// 硬触发源 (例如 Line0, Line1)
        /// </summary>
        public TriggerSource TriggerSource { get; set; } = TriggerSource.Line0;

        /// <summary>
        /// 一次完整扫描需要采集的图像数量
        /// </summary>
        public int ScanCount { get; set; }

        /// <summary>
        /// 每次扫描开始时，要附加的元数据
        /// </summary>
        public T InitialTriggerData { get; set; }

        /// <summary>
        /// (可选) 相机配置文件路径 (例如 .mfs 文件)
        /// </summary>
        public string ConfigFilePath { get; set; }
    }

    /// <summary>
    /// 线扫一批图像采集完成事件的参数
    /// </summary>
    /// <typeparam name="T">触发数据类型</typeparam>
    public class LineScanBatchCompletedEventArgs<T> : EventArgs
    {
        public string CameraSN { get; set; }
        public string CameraName { get; set; }

        /// <summary>
        /// 与这批图像关联的最终触发数据
        /// </summary>
        public T TriggerData { get; set; }

        /// <summary>
        /// 采集是否成功获取到了关联的触发数据
        /// </summary>
        public bool IsTriggerDataValid { get; set; }
    }

    /// <summary>
    /// 线扫单张图像采集完成事件的参数
    /// </summary>
    public class LineScanImageAcquiredEventArgs : EventArgs
    {
        public string CameraSN { get; set; }

        public string CameraName { get; set; }

        /// <summary>
        /// 采集到的图像 (通常是 HImage 或 Bitmap)
        /// </summary>
        public HImage AcquiredImage { get; set; }
    }

    #endregion
}
