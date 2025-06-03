using BingLibrary.Vision.Cameras.Camera;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using Timer = System.Threading.Timer;

namespace BingLibrary.Vision.Cameras
{
    /*************************************************************************************
     *
     * 文 件 名:   CamFactory
     * 描    述:   相机工厂类，用于管理工业相机实例
     *
     * 版    本：  V2.0.0.0
     * 创 建 者：  Bing
     * 创建时间：  2025/3/4 10:40:21
     * ======================================
     * 历史更新记录
     * 版本：V2.0.0.0   修改时间：2025/5/16    修改人：GitHub Copilot
     * 修改内容：增加了异常处理、状态监控、事件通知和相机配置持久化等功能
     * ======================================
    *************************************************************************************/

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
        bool ExecuteSoftTrigger(CameraInfo cameraInfoInput, T triggerData, int exposureTime = 1000);

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
    /// 相机工厂类，用于管理不同品牌的相机实例
    /// </summary>
    /// <typeparam name="T">触发数据类型</typeparam>
    public class CamFactory<T> : ICamFactory<T>, IDisposable
    {
        #region 软触发相关事件和方法

        /// <summary>
        /// 软触发图像捕获事件
        /// </summary>
        public event EventHandler<ImageCapturedEventArgs<T>> SoftTriggerImageCaptured;

        /// <summary>
        /// 连接指定SN的相机并设置软触发出图事件
        /// </summary>
        /// <param name="cameraSN">相机序列号</param>
        /// <param name="config">可选的相机配置参数</param>
        /// <returns>是否成功连接并设置</returns>
        /// <exception cref="ArgumentException">相机序列号为空时抛出</exception>
        /// <exception cref="CameraFactoryException">无法找到或初始化相机时抛出</exception>
        public bool ConnectAndSetStartMode(CameraInfo cameraInfoInput)
        {
            string cameraSN = cameraInfoInput.CameraSN;
            if (string.IsNullOrEmpty(cameraSN))
                throw new ArgumentException("相机序列号不能为空", nameof(cameraSN));

            try
            {
                // 初始化相机工厂（如果尚未初始化）
                if (!IsInitialized())
                {
                    int count = InitializeAllCameras();
                    if (count == 0)
                    {
                        throw new CameraFactoryException("没有找到可用的相机", null, new List<string>());
                    }
                }

                var ststus = GetCameraStatus(cameraSN);
                if (ststus == CameraStatus.Connected)
                {
                    return true;
                }
                // 获取相机实例
                var camera = GetCameraBySN(cameraSN);
                if (camera == null)
                {
                    #region 保护逻辑，确保相机确实能被初始化
                    // 尝试查找相机信息
                    CameraInfo cameraInfo = null;
                    foreach (CameraBrand brand in Enum.GetValues(typeof(CameraBrand)))
                    {
                        var cameraInfos = GetDeviceEnum(brand);
                        cameraInfo = cameraInfos?.FirstOrDefault(c => c.CameraSN == cameraSN);
                        if (cameraInfo != null)
                            break;
                    }

                    if (cameraInfo == null)
                        throw new CameraFactoryException($"未找到序列号为 {cameraSN} 的相机", null, new List<string> { cameraSN });

                    // 创建相机实例
                    camera = CreateCamera(cameraInfo.CameraBrand);
                    if (camera == null)
                        throw new CameraFactoryException($"无法创建相机实例 (SN: {cameraSN})", null, new List<string> { cameraSN });

                    // 初始化相机
                    if (!camera.InitDevice(cameraInfo))
                    {
                        camera.Dispose();
                        throw new CameraFactoryException($"初始化相机失败 (SN: {cameraSN})", null, new List<string> { cameraSN });
                    }
                    #endregion

                    // 添加到字典和列表中
                    try
                    {
                        _lock.EnterWriteLock();
                        _cameraDict[cameraSN] = camera;
                        _cameraList.Add(camera);
                        _cameraStatuses[cameraSN] = CameraStatus.Connected;
                    }
                    finally
                    {
                        _lock.ExitWriteLock();
                    }

                    // 触发相机状态变化事件
                    OnCameraStatusChanged(cameraSN, CameraStatus.Connected, CameraStatus.Disconnected);
                }

                // 设置软触发模式并注册回调
                bool result = false;
                if (cameraInfoInput.TriggeSource == TriggerSource.Software)
                {
                    result = camera.StartWith_SoftTriggerModel(image =>
                    {
                        if (image != null)
                        {
                            // 触发图像捕获事件
                            OnSoftTriggerImageCaptured(cameraSN, image, default);
                        }
                    });
                }
                else
                {
                    //硬触发
                    result = camera.StartWith_HardTriggerModel(cameraInfoInput.TriggeSource, async image =>
                    {
                        if (image != null)
                        {
                            // 触发图像捕获事件
                            OnSoftTriggerImageCaptured(cameraSN, image, default);
                        }
                    });
                }

                if (result)
                {
                    // 更新相机状态
                    UpdateCameraStatus(cameraSN, CameraStatus.Connected);
                    return true;
                }
                else
                {
                    throw new CameraFactoryException($"启动相机软触发模式失败 (SN: {cameraSN})", null, new List<string> { cameraSN });
                }
            }
            catch (CameraFactoryException)
            {
                throw; // 直接重新抛出CameraFactoryException
            }
            catch (Exception ex)
            {
                throw new CameraFactoryException($"连接相机并设置软触发出图事件失败 (SN: {cameraSN})", ex, new List<string> { cameraSN });
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="cameraInfoInput"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public bool StartWithContinue(CameraInfo cameraInfoInput, T triggerData, int exposureTime = 10000)
        {
            string cameraSN = cameraInfoInput.CameraSN;
            if (string.IsNullOrEmpty(cameraSN))
                throw new ArgumentException("相机序列号不能为空", nameof(cameraSN));
            var camera = GetCameraBySN(cameraSN);
            if (camera == null)
                return false;
            try
            {
                // 设置相机状态为采集中
                UpdateCameraStatus(cameraSN, CameraStatus.Grabbing);
                Task.Run(() =>
                {
                    // 更新曝光时间
                    camera.SetExpouseTime((ulong)10000);
                });
                // 执行软触发
                bool result = camera.StartWith_Continue(image =>
                {
                    if (image != null)
                    {
                        // 触发图像捕获事件
                        OnSoftTriggerImageCaptured(cameraSN, image, default);
                    }
                });
                return result;
            }
            catch (Exception ex)
            {
                UpdateCameraStatus(cameraSN, CameraStatus.Error);
                return false;
            }
        }

        /// <summary>
        /// 对指定相机执行软触发并获取图像
        /// </summary>
        /// <param name="cameraSN">相机序列号</param>
        /// <param name="triggerData">触发数据</param>
        /// <param name="timeoutMs">超时时间(毫秒)</param>
        /// <returns>是否成功触发</returns>
        public bool ExecuteSoftTrigger(CameraInfo cameraInfoInput, T triggerData, int exposureTime = 10000)
        {
            string cameraSN = cameraInfoInput.CameraSN;
            if (string.IsNullOrEmpty(cameraSN))
                throw new ArgumentException("相机序列号不能为空", nameof(cameraSN));

            var camera = GetCameraBySN(cameraSN);
            if (camera == null)
                return false;

            try
            {
                // 设置相机状态为采集中
                UpdateCameraStatus(cameraSN, CameraStatus.Grabbing);
                Task.Run(() =>
                {
                    // 更新曝光时间
                    camera.SetExpouseTime((ulong)exposureTime);
                });
                camera.GetTriggerMode(out TriggerMode triggerMode, out TriggerSource triggerSource);
                if (triggerMode == TriggerMode.Off || triggerSource != cameraInfoInput.TriggeSource)
                {
                    if (cameraInfoInput.TriggeSource == TriggerSource.Software)
                    {
                        camera.StartWith_SoftTriggerModel(image =>
                        {
                            if (image != null)
                            {
                                // 触发图像捕获事件
                                OnSoftTriggerImageCaptured(cameraSN, image, default);
                            }
                        });
                    }
                    else
                    {
                        //硬触发
                        camera.StartWith_HardTriggerModel(cameraInfoInput.TriggeSource, async image =>
                        {
                            if (image != null)
                            {
                                // 触发图像捕获事件
                                OnSoftTriggerImageCaptured(cameraSN, image, default);
                            }
                        });
                    }
                }
                // 执行软触发
                bool result = camera.SoftTrigger(triggerData);
                UpdateCameraStatus(cameraSN, CameraStatus.Connected);
                return result;
            }
            catch (Exception ex)
            {
                UpdateCameraStatus(cameraSN, CameraStatus.Error);
                return false;
            }
        }

        /// <summary>
        /// 触发软触发图像捕获事件
        /// </summary>
        /// <param name="cameraSN">相机序列号</param>
        /// <param name="image">捕获的图像</param>
        /// <param name="triggerData">触发数据</param>
        protected virtual void OnSoftTriggerImageCaptured(string cameraSN, System.Drawing.Bitmap image, T triggerData)
        {
            SoftTriggerImageCaptured?.Invoke(this, new ImageCapturedEventArgs<T>
            {
                CameraSN = cameraSN,
                Image = image,
                TriggerData = triggerData
            });
        }

        #endregion


        #region 字段和属性

        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
        private readonly ConcurrentDictionary<string, ICamera<T>> _cameraDict = new ConcurrentDictionary<string, ICamera<T>>();
        private readonly List<ICamera<T>> _cameraList = new List<ICamera<T>>();
        private readonly Dictionary<string, CameraStatus> _cameraStatuses = new Dictionary<string, CameraStatus>();
        private readonly Dictionary<CameraBrand, ICamera<T>> _cameraPrototypes = new Dictionary<CameraBrand, ICamera<T>>();

        private static CamFactory<T> _instance;
        private static readonly object _instanceLock = new object();

        private bool _isInitialized = false;
        private Timer _healthCheckTimer;
        private Timer _discoveryTimer;
        private bool _isDiscoveryRunning = false;
        private bool _isDisposed = false;

        /// <summary>
        /// 单例实例
        /// </summary>
        public static CamFactory<T> Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_instanceLock)
                    {
                        if (_instance == null)
                        {
                            _instance = new CamFactory<T>();
                        }
                    }
                }
                return _instance;
            }
        }

        #endregion

        #region 事件

        /// <summary>
        /// 相机状态变化事件
        /// </summary>
        public event EventHandler<CameraStatusChangedEventArgs> CameraStatusChanged;

        /// <summary>
        /// 相机发现事件
        /// </summary>
        public event EventHandler<CameraDiscoveredEventArgs> CameraDiscovered;

        #endregion

        #region 构造函数和析构函数

        /// <summary>
        /// 构造函数
        /// </summary>
        public CamFactory()
        {
            // 构造函数
        }

        /// <summary>
        /// 析构函数
        /// </summary>
        ~CamFactory()
        {
            Dispose(false);
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        /// <param name="disposing">是否是显式释放</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    StopHealthCheck();
                    StopCameraDiscovery();
                    ReleaseAllCameras();
                    _lock?.Dispose();
                }

                _isDisposed = true;
            }
        }

        #endregion

        #region 初始化相关方法

        /// <summary>
        /// 判断相机工厂是否已经初始化
        /// </summary>
        /// <returns>是否已初始化</returns>
        public bool IsInitialized()
        {
            return _isInitialized;
        }

        /// <summary>
        /// 初始化并加载所有可用相机
        /// </summary>
        /// <returns>初始化成功的相机数量</returns>
        public int InitializeAllCameras()
        {
            int successCount = 0;
            List<string> failedCameras = new List<string>();

            // 清除现有相机
            ReleaseAllCameras();

            // 重置初始化状态
            _isInitialized = false;

            try
            {
                // 枚举所有品牌的相机
                foreach (CameraBrand brand in Enum.GetValues(typeof(CameraBrand)))
                {
                    try
                    {
                        int brandSuccessCount = InitializeCamerasByBrand(brand);
                        successCount += brandSuccessCount;
                    }
                    catch (Exception ex)
                    {
                        // 记录异常但继续处理其他品牌
                        failedCameras.Add($"{brand}相机初始化失败: {ex.Message}");
                    }
                }

                // 只要有相机初始化成功，就认为初始化完成
                _isInitialized = successCount > 0;

                // 如果有失败的相机但也有成功的，记录警告
                if (failedCameras.Count > 0 && successCount > 0)
                {
                    // 这里可以记录日志
                    System.Diagnostics.Debug.WriteLine($"部分相机初始化失败: {string.Join(", ", failedCameras)}");
                }
                else if (failedCameras.Count > 0)
                {
                    throw new CameraFactoryException("所有相机初始化失败", null, failedCameras);
                }
            }
            catch (Exception ex)
            {
                _isInitialized = false;
                throw new CameraFactoryException("初始化相机工厂失败", ex, failedCameras);
            }

            return successCount;
        }

        /// <summary>
        /// 初始化特定品牌的相机
        /// </summary>
        /// <param name="brand">相机品牌</param>
        /// <returns>初始化成功的相机数量</returns>
        public int InitializeCamerasByBrand(CameraBrand brand)
        {
            int successCount = 0;
            List<string> failedCameras = new List<string>();

            try
            {
                var cameraInfos = GetDeviceEnum(brand);
                if (cameraInfos != null && cameraInfos.Count > 0)
                {
                    foreach (var cameraInfo in cameraInfos)
                    {
                        try
                        {
                            // 创建相机实例
                            var camera = CreateCamera(brand);
                            if (camera != null)
                            {
                                // 初始化相机
                                if (camera.InitDevice(cameraInfo))
                                {
                                    // 添加到字典和列表中
                                    try
                                    {
                                        _lock.EnterWriteLock();

                                        _cameraDict[cameraInfo.CameraSN] = camera;
                                        _cameraList.Add(camera);
                                        _cameraStatuses[cameraInfo.CameraSN] = CameraStatus.Disconnected;

                                        successCount++;

                                        // 触发相机状态变化事件
                                        //OnCameraStatusChanged(cameraInfo.CameraSN, CameraStatus.Disconnected, CameraStatus.Disconnected);
                                    }
                                    finally
                                    {
                                        _lock.ExitWriteLock();
                                    }
                                }
                                else
                                {
                                    // 初始化失败，记录并释放资源
                                    failedCameras.Add($"{cameraInfo.CameraSN} ({cameraInfo.CameraName})");
                                    camera.Dispose();
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            failedCameras.Add($"{cameraInfo.CameraSN} ({cameraInfo.CameraName}): {ex.Message}");
                        }
                    }
                }

                // 如果有失败的相机，记录警告
                if (failedCameras.Count > 0)
                {
                    // 这里可以记录日志
                    System.Diagnostics.Debug.WriteLine($"{brand}品牌的部分相机初始化失败: {string.Join(", ", failedCameras)}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"初始化{brand}品牌相机失败", ex);
            }

            return successCount;
        }

        #endregion

        #region 相机获取和管理方法

        /// <summary>
        /// 根据相机SN获取相机实例
        /// </summary>
        /// <param name="cameraSN">相机序列号</param>
        /// <returns>相机实例，未找到返回null</returns>
        public ICamera<T> GetCameraBySN(string cameraSN)
        {
            if (string.IsNullOrEmpty(cameraSN))
                return null;

            try
            {
                _lock.EnterReadLock();
                if (_cameraDict.TryGetValue(cameraSN, out var camera))
                    return camera;
            }
            finally
            {
                _lock.ExitReadLock();
            }

            return null;
        }

        /// <summary>
        /// 获取所有已加载的相机
        /// </summary>
        /// <returns>相机列表</returns>
        public List<ICamera<T>> GetAllCameras()
        {
            try
            {
                _lock.EnterReadLock();
                return _cameraList.ToList(); // 返回副本，防止外部修改
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        /// <summary>
        /// 获取特定品牌的所有相机
        /// </summary>
        /// <param name="brand">相机品牌</param>
        /// <returns>相机列表</returns>
        public List<ICamera<T>> GetCamerasByBrand(CameraBrand brand)
        {
            try
            {
                _lock.EnterReadLock();

                // 筛选特定品牌的相机
                return _cameraList.Where(c =>
                {
                    // 假设相机实现了某种方式可以获取品牌信息
                    var cameraInfo = new CameraInfo();
                    if (c is BaseCamera<T> baseCamera)
                    {
                        return baseCamera.Info.CameraBrand == brand;
                    }
                    return false;
                }).ToList();
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        /// <summary>
        /// 获取特定类型的所有相机
        /// </summary>
        /// <param name="type">相机类型</param>
        /// <returns>相机列表</returns>
        public List<ICamera<T>> GetCamerasByType(CameraType type)
        {
            try
            {
                _lock.EnterReadLock();

                // 筛选特定类型的相机
                return _cameraList.Where(c =>
                {
                    // 假设相机实现了某种方式可以获取类型信息
                    if (c is BaseCamera<T> baseCamera)
                    {
                        return baseCamera.Info.CameraType == type;
                    }
                    return false;
                }).ToList();
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        /// <summary>
        /// 释放所有相机资源
        /// </summary>
        public void ReleaseAllCameras()
        {
            List<ICamera<T>> camerasToRelease = new List<ICamera<T>>();

            try
            {
                _lock.EnterWriteLock();

                // 获取所有相机的副本
                camerasToRelease.AddRange(_cameraList);

                // 清空集合
                _cameraList.Clear();
                _cameraDict.Clear();
                _cameraStatuses.Clear();
                _isInitialized = false;
            }
            finally
            {
                _lock.ExitWriteLock();
            }

            // 在锁外释放资源，避免长时间持有锁
            foreach (var camera in camerasToRelease)
            {
                try
                {
                    camera?.CloseDevice();
                    camera?.Dispose();
                }
                catch (Exception ex)
                {
                    // 忽略释放过程中的异常，但可以记录日志
                    System.Diagnostics.Debug.WriteLine($"释放相机资源时出错: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 注销指定相机
        /// </summary>
        /// <param name="cameraSN">相机序列号</param>
        /// <returns>是否成功</returns>
        public bool ReleaseCamera(string cameraSN)
        {
            if (string.IsNullOrEmpty(cameraSN))
                return false;

            ICamera<T> camera = null;

            try
            {
                _lock.EnterWriteLock();

                if (_cameraDict.TryGetValue(cameraSN, out camera))
                {
                    _cameraList.Remove(camera);
                    _cameraDict.TryRemove(cameraSN, out _);
                    _cameraStatuses.Remove(cameraSN);

                    // 如果没有相机了，更新初始化状态
                    if (_cameraList.Count == 0)
                    {
                        _isInitialized = false;
                    }
                }
                else
                {
                    return false;
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }

            // 在锁外释放资源
            if (camera != null)
            {
                try
                {
                    camera.CloseDevice();
                    camera.Dispose();

                    // 触发事件
                    OnCameraStatusChanged(cameraSN, CameraStatus.Disconnected, CameraStatus.Connected);

                    return true;
                }
                catch (Exception ex)
                {
                    // 记录异常但不抛出
                    System.Diagnostics.Debug.WriteLine($"释放相机 {cameraSN} 时出错: {ex.Message}");
                    return false;
                }
            }

            return false;
        }

        #endregion

        #region 相机创建和枚举方法

        /// <summary>
        /// 按相机品牌获取可用相机枚举
        /// </summary>
        /// <param name="brand">相机品牌</param>
        /// <returns>相机信息列表</returns>
        public static List<CameraInfo> GetDeviceEnum(CameraBrand brand)
        {
            ICamera<T> camera = null;
            List<CameraInfo> result = null;

            try
            {
                camera = CreateCamera(brand);
                result = camera?.GetListEnum();
            }
            finally
            {
                // 如果只是用来枚举，使用完就释放
                if (camera != null && result != null)
                {
                    camera.Dispose();
                }
            }

            return result ?? new List<CameraInfo>();
        }

        /// <summary>
        /// 按品牌创建相机实例
        /// </summary>
        /// <param name="brand">相机品牌</param>
        /// <returns>相机实例</returns>
        public static ICamera<T> CreateCamera(CameraBrand brand)
        {
            ICamera<T> camera = null;

            switch (brand)
            {
                case CameraBrand.HaiKang:
                    camera = new HaiKangCamera<T>();
                    break;

                case CameraBrand.DaHua:
                    camera = new DaHuaCamera<T>();
                    break;

                case CameraBrand.Basler:
                    camera = new BaslerCamera<T>();
                    break;

                case CameraBrand.DaHeng:
                    camera = new DaHengCamera<T>();
                    break;

                default:
                    break;
            }

            return camera;
        }

        #endregion

        #region 状态监控方法

        /// <summary>
        /// 获取所有相机状态
        /// </summary>
        /// <returns>相机状态字典</returns>
        public Dictionary<string, CameraStatus> GetAllCamerasStatus()
        {
            try
            {
                _lock.EnterReadLock();
                return new Dictionary<string, CameraStatus>(_cameraStatuses);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        /// <summary>
        /// 获取特定相机状态
        /// </summary>
        /// <param name="cameraSN">相机序列号</param>
        /// <returns>相机状态</returns>
        public CameraStatus GetCameraStatus(string cameraSN)
        {
            if (string.IsNullOrEmpty(cameraSN))
                return CameraStatus.Disconnected;

            try
            {
                _lock.EnterReadLock();
                if (_cameraStatuses.TryGetValue(cameraSN, out var status))
                    return status;

                return CameraStatus.Disconnected;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        /// <summary>
        /// 更新相机状态
        /// </summary>
        /// <param name="cameraSN">相机序列号</param>
        /// <param name="newStatus">新状态</param>
        public void UpdateCameraStatus(string cameraSN, CameraStatus newStatus)
        {
            if (string.IsNullOrEmpty(cameraSN))
                return;

            CameraStatus oldStatus = CameraStatus.Disconnected;

            try
            {
                _lock.EnterWriteLock();

                if (_cameraStatuses.TryGetValue(cameraSN, out oldStatus))
                {
                    if (oldStatus != newStatus)
                    {
                        _cameraStatuses[cameraSN] = newStatus;

                        // 在锁内更新状态后记录旧状态和新状态
                        var currentOldStatus = oldStatus;
                        var currentNewStatus = newStatus;

                        // 但在锁外触发事件，避免死锁
                        Task.Run(() => OnCameraStatusChanged(cameraSN, currentNewStatus, currentOldStatus));
                    }
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <summary>
        /// 触发相机状态变化事件
        /// </summary>
        /// <param name="cameraSN">相机序列号</param>
        /// <param name="newStatus">新状态</param>
        /// <param name="oldStatus">旧状态</param>
        protected virtual void OnCameraStatusChanged(string cameraSN, CameraStatus newStatus, CameraStatus oldStatus)
        {
            CameraStatusChanged?.Invoke(this, new CameraStatusChangedEventArgs
            {
                CameraSN = cameraSN,
                NewStatus = newStatus,
                OldStatus = oldStatus
            });
        }

        /// <summary>
        /// 触发相机发现事件
        /// </summary>
        /// <param name="cameraInfo">相机信息</param>
        protected virtual void OnCameraDiscovered(CameraInfo cameraInfo)
        {
            CameraDiscovered?.Invoke(this, new CameraDiscoveredEventArgs
            {
                CameraInfo = cameraInfo
            });
        }

        #endregion

        #region 配置管理方法

        /// <summary>
        /// 按指定配置初始化相机
        /// </summary>
        /// <param name="cameraSN">相机序列号</param>
        /// <param name="config">相机配置</param>
        /// <returns>初始化是否成功</returns>
        public bool InitializeCameraWithConfig(string cameraSN, CameraData config)
        {
            var camera = GetCameraBySN(cameraSN);
            if (camera == null)
                return false;

            try
            {
                // 应用配置
                camera.SetCamConfig(config);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"配置相机 {cameraSN} 失败: {ex.Message}");
                UpdateCameraStatus(cameraSN, CameraStatus.Error);
                return false;
            }
        }

        /// <summary>
        /// 保存所有相机配置
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>是否成功</returns>
        public bool SaveAllCamerasConfig(string filePath)
        {
            try
            {
                var config = new Dictionary<string, CameraData>();

                foreach (var cameraSN in _cameraDict.Keys)
                {
                    var camera = GetCameraBySN(cameraSN);
                    if (camera != null)
                    {
                        CameraData cameraData = new CameraData();
                        camera.GetCamConfig(out cameraData);
                        config[cameraSN] = cameraData;
                    }
                }

                // 确保目录存在
                string directory = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // 序列化并保存
                string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(filePath, json);

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存相机配置失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 加载相机配置
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>是否成功</returns>
        public bool LoadAllCamerasConfig(string filePath)
        {
            if (!File.Exists(filePath))
                return false;

            try
            {
                string json = File.ReadAllText(filePath);
                var config = JsonSerializer.Deserialize<Dictionary<string, CameraData>>(json);

                bool allSuccess = true;

                foreach (var pair in config)
                {
                    if (!InitializeCameraWithConfig(pair.Key, pair.Value))
                    {
                        allSuccess = false;
                    }
                }

                return allSuccess;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载相机配置失败: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region 相机发现方法

        /// <summary>
        /// 开始监听新相机连接
        /// </summary>
        public void StartCameraDiscovery()
        {
            if (_isDiscoveryRunning)
                return;

            _isDiscoveryRunning = true;

            // 创建定时器，定期检查新相机
            _discoveryTimer = new Timer(_ => CheckForNewCameras(), null, TimeSpan.Zero, TimeSpan.FromSeconds(5));
        }

        /// <summary>
        /// 停止监听新相机连接
        /// </summary>
        public void StopCameraDiscovery()
        {
            _isDiscoveryRunning = false;
            _discoveryTimer?.Dispose();
            _discoveryTimer = null;
        }

        /// <summary>
        /// 检查新相机
        /// </summary>
        private void CheckForNewCameras()
        {
            if (!_isDiscoveryRunning)
                return;

            try
            {
                // 获取所有已知的相机序列号
                HashSet<string> knownCameras = new HashSet<string>();

                try
                {
                    _lock.EnterReadLock();
                    foreach (var sn in _cameraDict.Keys)
                    {
                        knownCameras.Add(sn);
                    }
                }
                finally
                {
                    _lock.ExitReadLock();
                }

                // 枚举所有品牌的相机
                foreach (CameraBrand brand in Enum.GetValues(typeof(CameraBrand)))
                {
                    var cameraInfos = GetDeviceEnum(brand);
                    if (cameraInfos != null)
                    {
                        foreach (var info in cameraInfos)
                        {
                            // 如果是新相机，触发发现事件
                            if (!knownCameras.Contains(info.CameraSN))
                            {
                                OnCameraDiscovered(info);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"检查新相机时出错: {ex.Message}");
            }
        }

        #endregion

        #region 健康检查方法

        /// <summary>
        /// 开始相机健康检查
        /// </summary>
        /// <param name="interval">检查间隔</param>
        public void StartHealthCheck(TimeSpan interval)
        {
            // 停止现有的健康检查
            StopHealthCheck();

            // 创建新的定时器，定期检查相机健康状况
            _healthCheckTimer = new Timer(_ => CheckAllCamerasHealth(), null, TimeSpan.Zero, interval);
        }

        /// <summary>
        /// 停止相机健康检查
        /// </summary>
        public void StopHealthCheck()
        {
            _healthCheckTimer?.Dispose();
            _healthCheckTimer = null;
        }

        /// <summary>
        /// 检查所有相机的健康状况
        /// </summary>
        private void CheckAllCamerasHealth()
        {
            // 获取所有相机的副本，避免在遍历过程中集合被修改
            Dictionary<string, ICamera<T>> cameras = new Dictionary<string, ICamera<T>>();

            try
            {
                _lock.EnterReadLock();
                foreach (var pair in _cameraDict)
                {
                    cameras.Add(pair.Key, pair.Value);
                }
            }
            finally
            {
                _lock.ExitReadLock();
            }

            // 检查每个相机的健康状态
            foreach (var pair in cameras)
            {
                string cameraSN = pair.Key;
                ICamera<T> camera = pair.Value;

                try
                {
                    // 这里实现检查相机健康的逻辑
                    // 例如，检查相机是否仍然可以通信
                    bool isHealthy = CheckCameraHealth(camera);

                    // 更新状态
                    if (isHealthy)
                    {
                        // 保持现有状态或恢复为Connected
                        var currentStatus = GetCameraStatus(cameraSN);
                        if (currentStatus == CameraStatus.Error)
                        {
                            UpdateCameraStatus(cameraSN, CameraStatus.Connected);
                        }
                    }
                    else
                    {
                        UpdateCameraStatus(cameraSN, CameraStatus.Error);
                        System.Diagnostics.Debug.WriteLine($"相机 {cameraSN} 健康检查失败");

                        // 可以尝试恢复相机
                        TryRecoverCamera(cameraSN);
                    }
                }
                catch (Exception ex)
                {
                    UpdateCameraStatus(cameraSN, CameraStatus.Error);
                    System.Diagnostics.Debug.WriteLine($"检查相机 {cameraSN} 健康状态时出错: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 检查单个相机的健康状态
        /// </summary>
        /// <param name="camera">相机实例</param>
        /// <returns>是否健康</returns>
        private bool CheckCameraHealth(ICamera<T> camera)
        {
            // 这里实现具体的相机健康检查逻辑
            // 例如，可以尝试获取相机参数或状态
            try
            {
                // 简单的健康检查示例
                camera.GetExpouseTime(out _);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 尝试恢复异常相机
        /// </summary>
        /// <param name="cameraSN">相机序列号</param>
        private void TryRecoverCamera(string cameraSN)
        {
            try
            {
                // 获取相机实例
                var camera = GetCameraBySN(cameraSN);
                if (camera == null)
                    return;

                // 记录当前配置
                CameraData config = new CameraData();
                try
                {
                    camera.GetCamConfig(out config);
                }
                catch { }

                // 获取相机信息
                CameraInfo cameraInfo = null;
                if (camera is BaseCamera<T> baseCamera)
                {
                    cameraInfo = baseCamera.Info;
                }

                // 释放相机资源
                ReleaseCamera(cameraSN);

                // 如果有相机信息，尝试重新初始化
                if (cameraInfo != null)
                {
                    var newCamera = CreateCamera(cameraInfo.CameraBrand);
                    if (newCamera != null && newCamera.InitDevice(cameraInfo))
                    {
                        // 添加到字典和列表
                        try
                        {
                            _lock.EnterWriteLock();
                            _cameraDict[cameraInfo.CameraSN] = newCamera;
                            _cameraList.Add(newCamera);
                            _cameraStatuses[cameraInfo.CameraSN] = CameraStatus.Connected;
                        }
                        finally
                        {
                            _lock.ExitWriteLock();
                        }

                        // 应用之前的配置
                        try
                        {
                            newCamera.SetCamConfig(config);
                        }
                        catch { }

                        // 触发状态变化事件
                        OnCameraStatusChanged(cameraSN, CameraStatus.Connected, CameraStatus.Error);

                        System.Diagnostics.Debug.WriteLine($"相机 {cameraSN} 恢复成功");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"相机 {cameraSN} 恢复失败");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"尝试恢复相机 {cameraSN} 时出错: {ex.Message}");
            }
        }

        #endregion
    }
}
