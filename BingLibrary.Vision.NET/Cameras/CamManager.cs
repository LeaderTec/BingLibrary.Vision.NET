using BingLibrary.Vision.Cameras;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using HalconDotNet; // [新增] 引入Halcon库以使用HImage
using Timer = System.Threading.Timer;
using Serilog;
using ZstdSharp.Unsafe;

namespace BingLibrary.Vision.Cameras
{
    /*************************************************************************************
     *
     * 文 件 名:   CamManager
     * 描    述:   相机工厂类，用于管理工业相机实例，同时支持2D和3D相机。
     *
     * 版    本：  V2.1.0.0
     * 创 建 者：  Bing
     * 创建时间：  2025/3/4 10:40:21
     * ======================================
     * 历史更新记录
     * 版本：V2.0.0.0   修改时间：2025/5/16
     * 修改内容：增加了异常处理、状态监控、事件通知和相机配置持久化等功能。
     * ======================================
     * 版本：V2.1.0.0   修改时间：2025/8/4
     * 修改内容：增加了对3D相机的适配，通过新增3D图像事件和在工厂方法中判断相机类型实现，
     *            使其能够处理来自3D相机的HImage数据流。
     * ======================================
    *************************************************************************************/


    /// <summary>
    /// 相机工厂类，用于管理不同品牌的2D和3D相机实例。
    /// </summary>
    /// <typeparam name="T">触发数据类型</typeparam>
    public class CamManager<T> : ICamManager<T>, IDisposable
    {

        ILogger _logger = Log.ForContext<CamManager<T>>();
        #region 软触发相关事件和方法

        /// <summary>
        /// 软触发图像捕获事件 (用于2D相机, 输出Bitmap)
        /// </summary>
        public event EventHandler<ImageCapturedEventArgs<T>> SoftTriggerImageCaptured;

        /// <summary>
        /// [新增] 软触发3D图像捕获事件 (用于3D相机, 输出HImage)
        /// </summary>
        public event EventHandler<Image3DCapturedEventArgs<T>> SoftTrigger3DImageCaptured;

        /// <summary>
        /// [修改] 连接指定SN的相机并设置触发模式。
        /// 此方法现在可以自动识别2D和3D相机，并为它们设置正确的回调和事件。
        /// </summary>
        /// <param name="cameraInfoInput">要连接的相机信息</param>
        /// <returns>是否成功连接并设置</returns>
        /// <exception cref="ArgumentException">相机序列号为空时抛出</exception>
        /// <exception cref="CameraFactoryException">无法找到、初始化或设置相机时抛出</exception>
        public bool ConnectAndSetStartMode(CameraInfo cameraInfoInput)
        {
            string cameraSN = cameraInfoInput.CameraSN;
            if (string.IsNullOrEmpty(cameraSN))
                throw new ArgumentException("相机序列号不能为空", nameof(cameraSN));

            try
            {
                // 如果工厂未初始化，则先初始化所有相机
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

                // 获取或创建相机实例
                var camera = GetCameraBySN(cameraSN);
                if (camera == null)
                {
                    #region 保护逻辑，确保相机确实能被初始化
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

                    camera = CreateCamera(cameraInfo.CameraBrand);
                    if (camera == null)
                        throw new CameraFactoryException($"无法创建相机实例 (SN: {cameraSN})", null, new List<string> { cameraSN });

                    if (!camera.InitDevice(cameraInfo))
                    {
                        camera.Dispose();
                        throw new CameraFactoryException($"初始化相机失败 (SN: {cameraSN})", null, new List<string> { cameraSN });
                    }
                    #endregion

                    // 将新创建的相机添加到管理列表
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
                    OnCameraStatusChanged(cameraSN, CameraStatus.Connected, CameraStatus.Disconnected);
                }

                // [修改] 核心逻辑：根据相机类型（品牌）选择不同的启动模式和回调
                if (camera is BaseCamera<T> baseCamera && cameraInfoInput.CameraBrand == CameraBrand.HaiKang3D)
                {
                    // --- 3D 相机处理逻辑 ---
                    bool result3D = false;
                    // 根据触发源选择软触发或硬触发模式
                    if (cameraInfoInput.TriggeSource == TriggerSource.Software)
                    {
                        result3D = baseCamera.StartWith_SoftTriggerModel3D((depthImage, intensityImage) =>
                        {
                            if (baseCamera.TryGetNextTriggerData(out T myTriggerData) && depthImage != null)
                            {
                                // 触发3D图像事件
                                OnSoftTrigger3DImageCaptured(cameraSN, depthImage, intensityImage, myTriggerData);
                            }
                        });
                    }
                    else // 硬触发
                    {
                        result3D = baseCamera.StartWith_HardTriggerModel3D((depthImage, intensityImage) =>
                        {
                            if (baseCamera.TryGetNextTriggerData(out T myTriggerData) && depthImage != null)
                            {
                                // 触发3D图像事件
                                OnSoftTrigger3DImageCaptured(cameraSN, depthImage, intensityImage, myTriggerData);
                            }
                        });
                    }

                    if (result3D)
                    {
                        UpdateCameraStatus(cameraSN, CameraStatus.Connected);
                        return true;
                    }
                    else
                    {
                        throw new CameraFactoryException($"启动3D相机触发模式失败 (SN: {cameraSN})", null, new List<string> { cameraSN });
                    }
                }
                else
                {
                    // --- 原有的 2D 相机处理逻辑 ---
                    bool result2D = false;
                    if (cameraInfoInput.TriggeSource == TriggerSource.Software)
                    {
                        result2D = camera.StartWith_SoftTriggerModel(image =>
                        {
                            if (camera.TryGetNextTriggerData(out T myTriggerData) && image != null)
                            {
                                // 触发2D图像事件
                                OnSoftTriggerImageCaptured(cameraSN, image, myTriggerData);
                            }
                        });
                    }
                    else //硬触发
                    {
                        result2D = camera.StartWith_HardTriggerModel(cameraInfoInput.TriggeSource, image =>
                        {
                            if (camera.TryGetNextTriggerData(out T myTriggerData) && image != null)
                            {
                                // 触发2D图像事件
                                OnSoftTriggerImageCaptured(cameraSN, image, myTriggerData);
                            }
                        });
                    }

                    if (result2D)
                    {
                        UpdateCameraStatus(cameraSN, CameraStatus.Connected);
                        return true;
                    }
                    else
                    {
                        throw new CameraFactoryException($"启动2D相机触发模式失败 (SN: {cameraSN})", null, new List<string> { cameraSN });
                    }
                }
            }
            catch (CameraFactoryException)
            {
                throw; // 直接重新抛出已知的工厂异常
            }
            catch (Exception ex)
            {
                // 将其他未知异常包装为工厂异常
                throw new CameraFactoryException($"连接相机并设置触发模式失败 (SN: {cameraSN})", ex, new List<string> { cameraSN });
            }
        }

        /// <summary>
        /// 以连续采集模式启动相机（主要用于2D相机）。
        /// </summary>
        public bool StartWithContinue(CameraInfo cameraInfoInput, T triggerData, int exposureTime = 10000)
        {
            string cameraSN = cameraInfoInput.CameraSN;
            if (string.IsNullOrEmpty(cameraSN))
                throw new ArgumentException("相机序列号不能为空", nameof(cameraSN));
            var camera = GetCameraBySN(cameraSN);
            if (camera == null)
                return false;

            // 3D相机的连续采集模式可能需要特殊处理，此处仅为2D相机保留
            if (cameraInfoInput.CameraBrand == CameraBrand.HaiKang3D)
            {
                System.Diagnostics.Debug.WriteLine($"警告: 3D相机 (SN: {cameraSN}) 的连续采集模式 StartWithContinue 未在此处实现。");
                return false;
            }

            try
            {
                UpdateCameraStatus(cameraSN, CameraStatus.Grabbing);
                Task.Run(() =>
                {
                    camera.SetExpouseTime((ulong)10000);
                });

                bool result = camera.StartWith_Continue(image =>
                {
                    if (image != null)
                    {
                        OnSoftTriggerImageCaptured(cameraSN, image, default);
                    }
                });
                return result;
            }
            catch (Exception ex)
            {
                UpdateCameraStatus(cameraSN, CameraStatus.Error);
                System.Diagnostics.Debug.WriteLine($"启动连续采集失败 (SN: {cameraSN}): {ex.Message}");
                return false;            }
        }

        /// <summary>
        /// [修改] 对指定相机执行一次软触发并获取图像。
        /// 此方法现在可以自动适配2D和3D相机。
        /// </summary>
        public async Task<bool> ExecuteTrigger(CameraInfo cameraInfoInput, T triggerData, int exposureTime = 10000, Action? onFinished = null)
        {
            if(cameraInfoInput.TriggeSource == TriggerSource.Software)
            {
                return ExecuteSoftTrigger(cameraInfoInput, triggerData, exposureTime, onFinished);
            }
            else if(cameraInfoInput.Usage == CameraUsageType.LineScan2D)
            {
                string cameraSN = cameraInfoInput.CameraSN;
                ICamera<T>? camera = GetCameraBySN(cameraSN);
                if (camera == null)
                {
                    //重连逻辑等
                    _logger.Error("【ExecuteTrigger】相机未连接");
                    throw new Exception("相机未连接");
                }

                try
                {
                    _logger.Information("添加触发数据");
                    camera.AddTriggerData(triggerData);
                }
                catch(Exception ex)
                {
                    _logger.Error(ex, "ExecuteTrigger,AddTriggerData失败");
                    return false;
                }

                return true;

                //TOCHECK 线扫硬触发处理逻辑是否正确
                //LineScanHardTriggerParameters<T> lineScanParams = new LineScanHardTriggerParameters<T>
                //{
                //    CameraSN = cameraInfoInput.CameraSN,
                //    TriggerSource = cameraInfoInput.TriggeSource,
                //    ScanCount = cameraInfoInput.ScanCount,
                //    //ConfigFilePath = Directory.GetFiles(Path.Combine(ProjectPath, runProject.ProjectName), "*.mfs").FirstOrDefault(),
                //    //ConfigFilePath = Directory.GetFiles(Path.Combine(_configurationService.ProjectRootPath, runProject.ProjectName), "*.mfs").FirstOrDefault(),
                //    ConfigFilePath = cameraInfoInput.ConfigFilePath,
                //    InitialTriggerData = triggerData
                //};
                //return await SetupLineScanHardTriggerAsync(lineScanParams);
            }
            else
            {
                throw new NotImplementedException("不支持的触发源或相机类型");
            }
        }

        /// <summary>
        /// [修改] 对指定相机执行一次软触发并获取图像。
        /// 此方法现在可以自动适配2D和3D相机。
        /// </summary>
        public bool ExecuteSoftTrigger(CameraInfo cameraInfoInput, T triggerData, int exposureTime = 10000, Action? onFinished = null)
        {
            string cameraSN = cameraInfoInput.CameraSN;
            if (string.IsNullOrEmpty(cameraSN))
                throw new ArgumentException("相机序列号不能为空", nameof(cameraSN));

            var camera = GetCameraBySN(cameraSN);
            if (camera == null)
                return false;

            // [修改] 根据相机类型执行不同逻辑
            if (camera is BaseCamera<T> baseCamera && cameraInfoInput.CameraBrand == CameraBrand.HaiKang3D)
            {
                // --- 3D 相机触发逻辑 ---
                try
                {
                    UpdateCameraStatus(cameraSN, CameraStatus.Grabbing);

                    // 检查并设置触发模式
                    baseCamera.GetTriggerMode(out TriggerMode triggerMode, out TriggerSource triggerSource);
                    if (triggerMode == TriggerMode.Off || triggerSource != cameraInfoInput.TriggeSource)
                    {
                        if (cameraInfoInput.TriggeSource == TriggerSource.Software)
                        {
                            baseCamera.StartWith_SoftTriggerModel3D((depth, intensity) => {
                                if (depth != null) { OnSoftTrigger3DImageCaptured(cameraSN, depth, intensity, default); }
                            });
                        }
                        else // 硬触发
                        {
                            baseCamera.StartWith_HardTriggerModel3D((depth, intensity) => {
                                if (depth != null) { OnSoftTrigger3DImageCaptured(cameraSN, depth, intensity, default); }
                            });
                        }
                    }

                    // 执行软触发
                    bool result = baseCamera.SoftTrigger(triggerData);
                    UpdateCameraStatus(cameraSN, CameraStatus.Connected);
                    onFinished?.Invoke();
                    return result;
                }
                catch (Exception ex)
                {
                    UpdateCameraStatus(cameraSN, CameraStatus.Error);
                    System.Diagnostics.Debug.WriteLine($"3D相机软触发失败 (SN: {cameraSN}): {ex.Message}");
                    return false;
                }
            }
            else
            {
                // --- 原有2D相机触发逻辑 ---
                try
                {
                    UpdateCameraStatus(cameraSN, CameraStatus.Grabbing);
                    Task.Run(() =>
                    {
                        camera.SetExpouseTime((ulong)exposureTime);
                    });

                    // 检查并设置触发模式
                    camera.GetTriggerMode(out TriggerMode triggerMode, out TriggerSource triggerSource);
                    if (triggerMode == TriggerMode.Off || triggerSource != cameraInfoInput.TriggeSource)
                    {
                        if (cameraInfoInput.TriggeSource == TriggerSource.Software)
                        {
                            camera.StartWith_SoftTriggerModel(image =>
                            {
                                if (image != null) { OnSoftTriggerImageCaptured(cameraSN, image, default); }
                            });
                        }
                        else //硬触发
                        {
                            camera.StartWith_HardTriggerModel(cameraInfoInput.TriggeSource, image =>
                            {
                                if (image != null) { OnSoftTriggerImageCaptured(cameraSN, image, default); }
                            });
                        }
                    }

                    // 执行软触发
                    bool result = camera.SoftTrigger(triggerData);
                    UpdateCameraStatus(cameraSN, CameraStatus.Connected);
                    onFinished?.Invoke();
                    return result;
                }
                catch (Exception ex)
                {
                    UpdateCameraStatus(cameraSN, CameraStatus.Error);
                    System.Diagnostics.Debug.WriteLine($"2D相机软触发失败 (SN: {cameraSN}): {ex.Message}");
                    return false;
                }
            }
        }

        /// <summary>
        /// 触发2D图像捕获事件的内部辅助方法。
        /// </summary>
        /// <param name="cameraSN">相机序列号</param>
        /// <param name="image">捕获的图像 (Bitmap)</param>
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

        /// <summary>
        /// [新增] 触发3D图像捕获事件的内部辅助方法。
        /// </summary>
        /// <param name="cameraSN">相机序列号</param>
        /// <param name="depthImage">捕获的深度图 (HImage)</param>
        /// <param name="intensityImage">捕获的亮度图 (HImage)</param>
        /// <param name="triggerData">触发数据</param>
        protected virtual void OnSoftTrigger3DImageCaptured(string cameraSN, HImage depthImage, HImage intensityImage, T triggerData)
        {
            SoftTrigger3DImageCaptured?.Invoke(this, new Image3DCapturedEventArgs<T>
            {
                CameraSN = cameraSN,
                DepthImage = depthImage,
                IntensityImage = intensityImage,
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

        private static CamManager<T> _instance;
        private static readonly object _instanceLock = new object();

        private bool _isInitialized = false;
        private Timer _healthCheckTimer;
        private Timer _discoveryTimer;
        private bool _isDiscoveryRunning = false;
        private bool _isDisposed = false;

        /// <summary>
        /// 获取相机工厂的单例实例
        /// </summary>
        public static CamManager<T> Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_instanceLock)
                    {
                        if (_instance == null)
                        {
                            _instance = new CamManager<T>();
                        }
                    }
                }
                return _instance;
            }
        }

        #endregion

        #region 事件

        /// <summary>
        /// 相机状态（如连接、断开、错误）发生变化时触发的事件
        /// </summary>
        public event EventHandler<CameraStatusChangedEventArgs> CameraStatusChanged;

        /// <summary>
        /// 当发现网络中存在新的、未被管理的相机时触发的事件
        /// </summary>
        public event EventHandler<CameraDiscoveredEventArgs> CameraDiscovered;

        #endregion

        #region 构造函数和析构函数

        /// <summary>
        /// 私有构造函数，确保通过单例模式创建
        /// </summary>
        public CamManager() { }

        /// <summary>
        /// 析构函数，用于释放非托管资源
        /// </summary>
        ~CamManager() { Dispose(false); }

        /// <summary>
        /// 实现IDisposable接口，释放托管和非托管资源
        /// </summary>
        public void Dispose() { Dispose(true); GC.SuppressFinalize(this); }

        /// <summary>
        /// 核心释放逻辑
        /// </summary>
        /// <param name="disposing">是否由用户代码显式调用</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    // 释放托管资源（如定时器）
                    StopHealthCheck();
                    StopCameraDiscovery();
                    ReleaseAllCameras();
                    _lock?.Dispose();
                }
                // 可以在此释放非托管资源
                _isDisposed = true;
            }
        }
        #endregion

        #region 初始化相关方法

        /// <summary>
        /// 判断相机工厂是否已经初始化（即至少有一台相机被加载）
        /// </summary>
        public bool IsInitialized() => _isInitialized;

        /// <summary>
        /// 初始化并加载所有可用品牌的相机
        /// </summary>
        public int InitializeAllCameras()
        {
            int successCount = 0;
            List<string> failedCameras = new List<string>();
            ReleaseAllCameras(); // 先释放旧的相机
            _isInitialized = false;

            try
            {
                // 遍历所有支持的相机品牌并尝试初始化
                foreach (CameraBrand brand in Enum.GetValues(typeof(CameraBrand)))
                {
                    try
                    {
                        successCount += InitializeCamerasByBrand(brand);
                    }
                    catch (Exception ex)
                    {
                        failedCameras.Add($"{brand}相机初始化失败: {ex.Message}");
                    }
                }

                _isInitialized = successCount > 0;

                if (failedCameras.Count > 0 && successCount > 0)
                {
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
        public int InitializeCamerasByBrand(CameraBrand brand)
        {
            int successCount = 0;
            List<string> failedCameras = new List<string>();
            try
            {
                var cameraInfos = GetDeviceEnum(brand); // 枚举该品牌下的所有设备
                if (cameraInfos != null && cameraInfos.Count > 0)
                {
                    foreach (var cameraInfo in cameraInfos)
                    {
                        try
                        {
                            var camera = CreateCamera(brand);
                            if (camera != null)
                            {
                                if (camera.InitDevice(cameraInfo)) // 尝试初始化设备
                                {
                                    try
                                    {
                                        _lock.EnterWriteLock();
                                        _cameraDict[cameraInfo.CameraSN] = camera;
                                        _cameraList.Add(camera);
                                        _cameraStatuses[cameraInfo.CameraSN] = CameraStatus.Disconnected; // 初始状态为断开
                                        successCount++;
                                    }
                                    finally
                                    {
                                        _lock.ExitWriteLock();
                                    }
                                }
                                else
                                {
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

                if (failedCameras.Count > 0)
                {
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
        /// 根据相机SN（序列号）获取相机实例
        /// </summary>
        public ICamera<T> GetCameraBySN(string cameraSN)
        {
            if (string.IsNullOrEmpty(cameraSN)) return null;
            try
            {
                _lock.EnterReadLock();
                if (_cameraDict.TryGetValue(cameraSN, out var camera)) return camera;
            }
            finally { _lock.ExitReadLock(); }
            return null;
        }

        /// <summary>
        /// 获取所有已加载的相机实例列表
        /// </summary>
        public List<ICamera<T>> GetAllCameras()
        {
            try
            {
                _lock.EnterReadLock();
                return _cameraList.ToList(); // 返回副本以防外部修改
            }
            finally { _lock.ExitReadLock(); }
        }

        /// <summary>
        /// 获取特定品牌的所有相机
        /// </summary>
        public List<ICamera<T>> GetCamerasByBrand(CameraBrand brand)
        {
            try
            {
                _lock.EnterReadLock();
                return _cameraList.Where(c => {
                    if (c is BaseCamera<T> baseCamera) return baseCamera.Info.CameraBrand == brand;
                    return false;
                }).ToList();
            }
            finally { _lock.ExitReadLock(); }
        }

        /// <summary>
        /// 获取特定类型（如Gige, USB）的所有相机
        /// </summary>
        public List<ICamera<T>> GetCamerasByType(CameraType type)
        {
            try
            {
                _lock.EnterReadLock();
                return _cameraList.Where(c => {
                    if (c is BaseCamera<T> baseCamera) return baseCamera.Info.CameraType == type;
                    return false;
                }).ToList();
            }
            finally { _lock.ExitReadLock(); }
        }

        /// <summary>
        /// 释放所有已加载的相机资源
        /// </summary>
        public void ReleaseAllCameras()
        {
            List<ICamera<T>> camerasToRelease = new List<ICamera<T>>();
            try
            {
                _lock.EnterWriteLock();
                camerasToRelease.AddRange(_cameraList);
                _cameraList.Clear();
                _cameraDict.Clear();
                _cameraStatuses.Clear();
                _isInitialized = false;
            }
            finally { _lock.ExitWriteLock(); }

            // 在锁外释放资源，避免长时间阻塞
            foreach (var camera in camerasToRelease)
            {
                try
                {
                    camera?.CloseDevice();
                    camera?.Dispose();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"释放相机资源时出错: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 注销并释放指定的相机
        /// </summary>
        public bool ReleaseCamera(string cameraSN)
        {
            if (string.IsNullOrEmpty(cameraSN)) return false;
            ICamera<T> camera = null;
            try
            {
                _lock.EnterWriteLock();
                if (_cameraDict.TryGetValue(cameraSN, out camera))
                {
                    _cameraList.Remove(camera);
                    _cameraDict.TryRemove(cameraSN, out _);
                    _cameraStatuses.Remove(cameraSN);
                    if (_cameraList.Count == 0) _isInitialized = false;
                }
                else { return false; }
            }
            finally { _lock.ExitWriteLock(); }

            if (camera != null)
            {
                try
                {
                    camera.CloseDevice();
                    camera.Dispose();
                    OnCameraStatusChanged(cameraSN, CameraStatus.Disconnected, CameraStatus.Connected);
                    return true;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"释放相机 {cameraSN} 时出错: {ex.Message}");
                    return false;
                }
            }
            return false;
        }
        #endregion

        #region 相机创建和枚举方法

        /// <summary>
        /// 静态方法，用于枚举指定品牌下所有可用的相机设备信息。
        /// </summary>
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
                // 创建的临时相机实例仅用于枚举，使用后即释放
                if (camera != null && result != null) camera.Dispose();
            }
            return result ?? new List<CameraInfo>();
        }

        /// <summary>
        /// 静态方法，根据相机品牌创建对应的相机实例。
        /// </summary>
        public static ICamera<T> CreateCamera(CameraBrand brand)
        {
            switch (brand)
            {
                case CameraBrand.HaiKang: return new HaiKangCamera<T>();
                case CameraBrand.HaiKang3D: return new HaiKangCamera3D<T>();
                case CameraBrand.DaHua: return new DaHuaCamera<T>();
                case CameraBrand.Basler: return new BaslerCamera<T>();
                case CameraBrand.DaHeng: return new DaHengCamera<T>();
                default: return null;
            }
        }
        #endregion

        #region 状态监控方法

        public Dictionary<string, CameraStatus> GetAllCamerasStatus()
        {
            try
            {
                _lock.EnterReadLock();
                return new Dictionary<string, CameraStatus>(_cameraStatuses);
            }
            finally { _lock.ExitReadLock(); }
        }

        public CameraStatus GetCameraStatus(string cameraSN)
        {
            if (string.IsNullOrEmpty(cameraSN)) return CameraStatus.Disconnected;
            try
            {
                _lock.EnterReadLock();
                if (_cameraStatuses.TryGetValue(cameraSN, out var status)) return status;
                return CameraStatus.Disconnected;
            }
            finally { _lock.ExitReadLock(); }
        }

        public void UpdateCameraStatus(string cameraSN, CameraStatus newStatus)
        {
            if (string.IsNullOrEmpty(cameraSN)) return;
            CameraStatus oldStatus = CameraStatus.Disconnected;
            try
            {
                _lock.EnterWriteLock();
                if (_cameraStatuses.TryGetValue(cameraSN, out oldStatus))
                {
                    if (oldStatus != newStatus)
                    {
                        _cameraStatuses[cameraSN] = newStatus;
                        var currentOldStatus = oldStatus;
                        var currentNewStatus = newStatus;
                        // 异步触发事件以避免在写锁中执行外部代码，防止死锁
                        Task.Run(() => OnCameraStatusChanged(cameraSN, currentNewStatus, currentOldStatus));
                    }
                }
            }
            finally { _lock.ExitWriteLock(); }
        }

        protected virtual void OnCameraStatusChanged(string cameraSN, CameraStatus newStatus, CameraStatus oldStatus)
        {
            CameraStatusChanged?.Invoke(this, new CameraStatusChangedEventArgs { CameraSN = cameraSN, NewStatus = newStatus, OldStatus = oldStatus });
        }

        protected virtual void OnCameraDiscovered(CameraInfo cameraInfo)
        {
            CameraDiscovered?.Invoke(this, new CameraDiscoveredEventArgs { CameraInfo = cameraInfo });
        }
        #endregion

        #region 配置管理方法

        public bool InitializeCameraWithConfig(string cameraSN, CameraData config)
        {
            var camera = GetCameraBySN(cameraSN);
            if (camera == null) return false;
            try
            {
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
                        camera.GetCamConfig(out CameraData cameraData);
                        config[cameraSN] = cameraData;
                    }
                }
                string directory = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
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

        public bool LoadAllCamerasConfig(string filePath)
        {
            if (!File.Exists(filePath)) return false;
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
        /// 开始后台任务，定期扫描新连接的相机设备
        /// </summary>
        public void StartCameraDiscovery()
        {
            if (_isDiscoveryRunning) return;
            _isDiscoveryRunning = true;
            _discoveryTimer = new Timer(_ => CheckForNewCameras(), null, TimeSpan.Zero, TimeSpan.FromSeconds(5));
        }

        public void StopCameraDiscovery()
        {
            _isDiscoveryRunning = false;
            _discoveryTimer?.Dispose();
            _discoveryTimer = null;
        }

        private void CheckForNewCameras()
        {
            if (!_isDiscoveryRunning) return;
            try
            {
                HashSet<string> knownCameras = new HashSet<string>();
                try
                {
                    _lock.EnterReadLock();
                    foreach (var sn in _cameraDict.Keys) knownCameras.Add(sn);
                }
                finally { _lock.ExitReadLock(); }

                foreach (CameraBrand brand in Enum.GetValues(typeof(CameraBrand)))
                {
                    var cameraInfos = GetDeviceEnum(brand);
                    if (cameraInfos != null)
                    {
                        foreach (var info in cameraInfos)
                        {
                            if (!knownCameras.Contains(info.CameraSN)) OnCameraDiscovered(info);
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
        /// 开始后台任务，定期检查已连接相机的健康状态
        /// </summary>
        /// <param name="interval">检查间隔</param>
        public void StartHealthCheck(TimeSpan interval)
        {
            StopHealthCheck();
            _healthCheckTimer = new Timer(_ => CheckAllCamerasHealth(), null, TimeSpan.Zero, interval);
        }

        public void StopHealthCheck()
        {
            _healthCheckTimer?.Dispose();
            _healthCheckTimer = null;
        }

        private void CheckAllCamerasHealth()
        {
            Dictionary<string, ICamera<T>> cameras = new Dictionary<string, ICamera<T>>();
            try
            {
                _lock.EnterReadLock();
                foreach (var pair in _cameraDict) cameras.Add(pair.Key, pair.Value);
            }
            finally { _lock.ExitReadLock(); }

            foreach (var pair in cameras)
            {
                string cameraSN = pair.Key;
                ICamera<T> camera = pair.Value;
                try
                {
                    bool isHealthy = CheckCameraHealth(camera);
                    if (isHealthy)
                    {
                        var currentStatus = GetCameraStatus(cameraSN);
                        if (currentStatus == CameraStatus.Error) UpdateCameraStatus(cameraSN, CameraStatus.Connected);
                    }
                    else
                    {
                        UpdateCameraStatus(cameraSN, CameraStatus.Error);
                        System.Diagnostics.Debug.WriteLine($"相机 {cameraSN} 健康检查失败");
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

        private bool CheckCameraHealth(ICamera<T> camera)
        {
            try
            {
                // 一个简单的健康检查：尝试获取曝光时间。如果失败则认为不健康。
                camera.GetExpouseTime(out _);
                return true;
            }
            catch { return false; }
        }

        /// <summary>
        /// 尝试自动恢复处于错误状态的相机
        /// </summary>
        private void TryRecoverCamera(string cameraSN)
        {
            try
            {
                var camera = GetCameraBySN(cameraSN);
                if (camera == null) return;

                CameraData config = new CameraData();
                try { camera.GetCamConfig(out config); } catch { }

                CameraInfo cameraInfo = null;
                if (camera is BaseCamera<T> baseCamera) cameraInfo = baseCamera.Info;

                // 先释放掉出错的相机
                ReleaseCamera(cameraSN);

                // 尝试用旧的信息和配置重新初始化
                if (cameraInfo != null)
                {
                    var newCamera = CreateCamera(cameraInfo.CameraBrand);
                    if (newCamera != null && newCamera.InitDevice(cameraInfo))
                    {
                        try
                        {
                            _lock.EnterWriteLock();
                            _cameraDict[cameraInfo.CameraSN] = newCamera;
                            _cameraList.Add(newCamera);
                            _cameraStatuses[cameraInfo.CameraSN] = CameraStatus.Connected;
                        }
                        finally { _lock.ExitWriteLock(); }

                        try { newCamera.SetCamConfig(config); } catch { }

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


        #region 线扫相机专用逻辑

        /// <summary>
        /// 当线扫相机采集完一张图像时触发
        /// </summary>
        public event EventHandler<LineScanImageAcquiredEventArgs> LineScanImageAcquired;

        /// <summary>
        /// 当线扫相机完成一批次扫描时触发
        /// </summary>
        public event EventHandler<LineScanBatchCompletedEventArgs<T>> LineScanBatchCompleted;

        // 用于在工厂内部跟踪每个线扫相机的当前计数值
        private readonly ConcurrentDictionary<string, int> _lineScanCounters = new ConcurrentDictionary<string, int>();


        /// <summary>
        /// 初始化并设置线扫相机的硬件触发模式。
        /// 此方法现在完全由工厂内部管理批处理状态。
        /// </summary>
        /// <param name="parameters">包含所有必要配置的参数对象</param>
        /// <returns>如果成功启动，则返回 true；否则返回 false。</returns>
        public async Task<bool> SetupLineScanHardTriggerAsync(LineScanHardTriggerParameters<T> parameters)
        {
            string cameraSN = parameters.CameraSN;
            var camera = GetCameraBySN(cameraSN);
            if (camera == null)
            {
                // ... (如果相机未初始化，尝试连接它的逻辑保持不变)
                var cameraInfo = GetDeviceEnum(CameraBrand.HaiKang).FirstOrDefault(c => c.CameraSN == cameraSN);
                if (cameraInfo == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[Factory] 错误: 找不到SN为 {cameraSN} 的线扫相机。");
                    return false;
                }
                // 注意: ConnectAndSetStartMode可能需要根据需求调整，或直接在这里处理初始化
                var baseCamera = CreateCamera(cameraInfo.CameraBrand);
                if (baseCamera == null || !baseCamera.InitDevice(cameraInfo))
                {
                    System.Diagnostics.Debug.WriteLine($"[Factory] 错误: 初始化SN为 {cameraSN} 的相机失败。");
                    return false;
                }
                // 将新创建的相机添加到管理列表
                try
                {
                    _lock.EnterWriteLock();
                    _cameraDict[cameraSN] = baseCamera;
                    _cameraList.Add(baseCamera);
                }
                finally { _lock.ExitWriteLock(); }
                camera = baseCamera;
            }

            // 初始化或重置该相机的计数器
            _lineScanCounters[cameraSN] = 0;

            // 尝试3次初始化和启动
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    // 注意：频繁Close/Init可能不是最佳实践
                    camera.CloseDevice();
                    await Task.Delay(500);

                    var cameraInfo = (camera as BaseCamera<T>)?.Info;
                    if (!camera.InitDevice(cameraInfo))
                    {
                        await Task.Delay( 500);
                        continue;
                    }

                    // 加载配置文件
                    if (!string.IsNullOrEmpty(parameters.ConfigFilePath) && File.Exists(parameters.ConfigFilePath))
                    {
                        camera.LoadCamConfig(parameters.ConfigFilePath);
                    }

                    //TODO 这里的数量不应该由相机回调函数维护而应该由外层维护
                    // 启动硬触发模式，并提供工厂内部的回调逻辑
                    bool started = camera.StartWith_HardTriggerModel(parameters.TriggerSource, image =>
                    {
                        // 使用 using 确保 HImage 被正确转换和释放
                        using var hImage = new HImage(TransToHimage.ConvertBitmapToHImage(image));

                        // 1. 触发单张图像采集事件，让应用层可以接收并处理图像
                        LineScanImageAcquired?.Invoke(this, new LineScanImageAcquiredEventArgs { CameraSN = cameraSN, AcquiredImage = new HImage(hImage) });

                        // 2. 递增内部计数器
                        int currentCount = _lineScanCounters.AddOrUpdate(cameraSN, 1, (sn, count) => count + 1);

                        // 3. 如果是第一张图，将元数据存入工厂的字典中
                        if (currentCount == 1)
                        {
                            //_lineScanTriggerDataStore[cameraSN] = parameters.InitialTriggerData;
                            //camera.AddTriggerData(parameters.InitialTriggerData);
                        }

                        // 4. 如果达到扫描总数，完成批处理
                        if (currentCount >= parameters.ScanCount)
                        {
                            // 从工厂字典中原子性地移除并获取数据
                            //bool isValid = _lineScanTriggerDataStore.TryRemove(cameraSN, out T finalTriggerData);
                            bool isValid = camera.TryGetNextTriggerData(out T finalTriggerData);

                            // 触发批处理完成事件
                            LineScanBatchCompleted?.Invoke(this, new LineScanBatchCompletedEventArgs<T>
                            {
                                CameraSN = cameraSN,
                                TriggerData = finalTriggerData,
                                IsTriggerDataValid = isValid
                            });

                            // 重置计数器，为下一次扫描做准备
                            _lineScanCounters[cameraSN] = 0;
                        }
                    });

                    if (started)
                    {
                        System.Diagnostics.Debug.WriteLine($"[Factory] 线扫相机 {cameraSN} 硬触发模式启动成功。");
                        return true; // 成功启动，退出方法
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Factory] 设置线扫相机时发生异常 (尝试 {i + 1}/3): {ex.Message}");
                    await Task.Delay(500);
                }
            }

            System.Diagnostics.Debug.WriteLine($"[Factory] 错误: 经过3次尝试后，线扫相机 {cameraSN} 启动失败。");
            return false; // 3次尝试都失败了
        }

        #endregion
    }
}