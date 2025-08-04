using HalconDotNet;
using MySqlX.XDevAPI.Common;
using System.Collections.Concurrent;

namespace BingLibrary.Vision.Cameras
{
    public abstract class BaseCamera<T> : ICamera<T>
    {
        protected BaseCamera()
        { 
            ActionGetImage += ResetActionImageSignal; 
            ActionGet3DImages += ResetAction3DImageSignal;
        }

        #region 触发Data

        private readonly ConcurrentQueue<T> _pendingTriggerData = new ConcurrentQueue<T>();
        public IReadOnlyCollection<T> PendingTriggerData => _pendingTriggerData.ToList().AsReadOnly();

        public void ClearTriggerData()
        {
            while (TryGetNextTriggerData(out T result))
            {
            }
        }

        // 添加触发数据（线程安全入队）
        public void AddTriggerData(T data)
        {
            if (data != null)
                _pendingTriggerData.Enqueue(data);
        }

        public bool TryGetNextTriggerData(out T result)
        {
            return _pendingTriggerData.TryDequeue(out result);
        }

        // 清空队列（可选）
        public void ClearPendingData()
        {
            while (_pendingTriggerData.TryDequeue(out _)) { }
        }

        #endregion 触发Data

        #region Parm

        public string SN { get; set; } = string.Empty;
        public CameraInfo Info { get; set; } = new CameraInfo();

        /// <summary>
        /// 回调委托，获取图像数据，+= 赋值,子类要添加到回调中
        /// </summary>
        protected Action<Bitmap> ActionGetImage { get; set; }
       
        protected Bitmap CallBaclImg { get; set; }

        protected AutoResetEvent ResetGetImageSignal = new AutoResetEvent(false);

        /// <summary>
        /// 海康3D相机获取图像回调委托，+= 赋值,子类要添加到回调中
        /// </summary>
        public Action<HImage, HImage> ActionGet3DImages { get; set; }
        protected HImage CallBaclDeepImg { get; set; }
        protected HImage CallBaclLightImg { get; set; }
        protected AutoResetEvent ResetGet3DImageSignal = new AutoResetEvent(false);


        #endregion Parm

        #region operate

        public abstract void CloseDevice();

        public abstract List<CameraInfo> GetListEnum();

        public abstract bool InitDevice(CameraInfo cameraInfo);

        List<Action<Bitmap>> handles = new List<Action<Bitmap>>();
        public bool StartWith_Continue(Action<Bitmap> callbackfunc)
        {
            try
            {
                foreach (Action<Bitmap> handle in ActionGetImage.GetInvocationList())
                    foreach (var tempHandle in handles)
                        if (tempHandle == handle)
                            ActionGetImage -= handle;
                handles.Clear();
                handles.Add(callbackfunc);
            }
            catch { }
            SetTriggerMode(TriggerMode.Off);
            if (callbackfunc != null && !ActionGetImage.GetInvocationList().Contains(callbackfunc)) ActionGetImage += callbackfunc;
            return StartGrabbing();
        }

        public bool StartWith_HardTriggerModel(TriggerSource hardsource, Action<Bitmap> callbackfunc = null)
        {
            try
            {
                foreach (Action<Bitmap> handle in ActionGetImage.GetInvocationList())
                    foreach (var tempHandle in handles)
                        if (tempHandle == handle)
                            ActionGetImage -= handle;
                handles.Clear();
                handles.Add(callbackfunc);
            }
            catch { }
            // if (hardsource == TriggerSource.Software) hardsource = TriggerSource.Line0;
            SetTriggerMode(TriggerMode.On, hardsource);
            if (callbackfunc != null && !ActionGetImage.GetInvocationList().Contains(callbackfunc)) ActionGetImage += callbackfunc;
            return StartGrabbing();
        }

        public bool StartWith_SoftTriggerModel(Action<Bitmap> callbackfunc = null)
        {
            try
            {
                foreach (Action<Bitmap> handle in ActionGetImage.GetInvocationList())
                    foreach (var tempHandle in handles)
                        if (tempHandle == handle)
                            ActionGetImage -= handle;
                handles.Clear();
                handles.Add(callbackfunc);
            }
            catch { }
            SetTriggerMode(TriggerMode.On, TriggerSource.Software);

            if (callbackfunc != null && !ActionGetImage.GetInvocationList().Contains(callbackfunc)) ActionGetImage += callbackfunc;
            return StartGrabbing();
        }


        List<Action<HImage,HImage>> handles3D = new List<Action<HImage,HImage>>();
        public bool StartWith_HardTriggerModel3D( Action<HImage,HImage> callbackfunc = null)
        {
            try
            {
                foreach (Action<HImage, HImage> handle in ActionGet3DImages.GetInvocationList())
                    foreach (var tempHandle in handles3D)
                        if (tempHandle == handle)
                            ActionGet3DImages -= handle;
                handles3D.Clear();
                handles3D.Add(callbackfunc);
            }
            catch { }
           
            if (callbackfunc != null && !ActionGet3DImages.GetInvocationList().Contains(callbackfunc)) ActionGet3DImages += callbackfunc;
            return StartGrabbing();
        }

        public bool StartWith_SoftTriggerModel3D(Action<HImage, HImage> callbackfunc = null)
        {
            try
            {
                foreach (Action<HImage, HImage> handle in ActionGet3DImages.GetInvocationList())
                    foreach (var tempHandle in handles3D)
                        if (tempHandle == handle)
                            ActionGet3DImages -= handle;
                handles3D.Clear();
                handles3D.Add(callbackfunc);
            }
            catch { }

            if (callbackfunc != null && !ActionGet3DImages.GetInvocationList().Contains(callbackfunc)) ActionGet3DImages += callbackfunc;
            return StartGrabbing();
        }




        /// <summary>
        /// 等待硬触发获取图像
        /// </summary>
        /// <param name="bitmap"></param>
        /// <param name="outtime"></param>
        /// <returns></returns>
        public bool GrabImage(out Bitmap bitmap, int outtime = 3000)
        {
            bitmap = null;
            if (ResetGetImageSignal.WaitOne(outtime))
            {
                bitmap = CallBaclImg.Clone() as Bitmap;
                CallBaclImg?.Dispose();
                return true;
            }
            CallBaclImg?.Dispose();
            return false;
        }

        /// <summary>
        /// 海康3D相机获取图像
        /// </summary>
        /// <param name="deepImage"></param>
        /// <param name="lightImage"></param>
        /// <param name="outtime"></param>
        /// <returns></returns>
        public bool Grab3DImage(out HImage deepImage,out HImage lightImage, int outtime = 30000)
        {
            deepImage = null;
            lightImage = null;
            if (ResetGet3DImageSignal.WaitOne(outtime))
            {
                deepImage =new HImage( CallBaclDeepImg);
                lightImage = new HImage(CallBaclLightImg);
                CallBaclDeepImg?.Dispose();
                CallBaclLightImg?.Dispose();
                return true;
            }
            CallBaclDeepImg?.Dispose();
            CallBaclLightImg?.Dispose();
            return false;
        }

        /// <summary>
        /// 软触发获取图像
        /// </summary>
        /// <param name="bitmap"></param>
        /// <param name="outtime"></param>
        /// <returns></returns>
        public bool GrabImageWithSoftTrigger(out Bitmap bitmap, int outtime = 3000)
        {
            bitmap = null;
            if (!SoftTrigger(default)) return false;

            if (ResetGetImageSignal.WaitOne(outtime))
            {
                //Debug.WriteLine("software get img");
                bitmap = CallBaclImg.Clone() as Bitmap;
                CallBaclImg?.Dispose();
                return true;
            }
            CallBaclImg?.Dispose();
            return false;
        }

        /// <summary>
        /// 海康3D软触发获取图像
        /// </summary>
        /// <param name="bitmap"></param>
        /// <param name="outtime"></param>
        /// <returns></returns>
        public bool Grab3DImageWithSoftTrigger(out HImage deepImage, out HImage lightImage, int outtime = 30000)
        {
            deepImage = null;
            lightImage = null;
            if (!SoftTrigger(default)) return false;

            if (ResetGet3DImageSignal.WaitOne(outtime))
            {
                deepImage = new HImage(CallBaclDeepImg);
                lightImage = new HImage(CallBaclLightImg);
                CallBaclDeepImg?.Dispose();
                CallBaclLightImg?.Dispose();
                return true;
            }
            CallBaclDeepImg?.Dispose();
            CallBaclLightImg?.Dispose();
            return false;
        }


        /// <summary>
        /// 软触发
        /// </summary>
        /// <returns></returns>
        public abstract bool SoftTrigger(T tData);

        #endregion operate

        #region SettingConfig
        public abstract bool LoadCamConfig(string filePath);
        public void SetCamConfig(CameraData config)
        {
            if (config == null) return;
            SetExpouseTime(config.ExpouseTime);
            SetTriggerMode(config.triggerMode, config.triggeSource);
            SetTriggerPolarity(config.triggerPolarity);
            SetTriggerFliter(config.TriggerFilter);
            SetGain(config.Gain);
            SetTriggerDelay(config.TriggerDelay);
        }

        public void GetCamConfig(out CameraData config)
        {
            GetExpouseTime(out ulong expouseTime);
            GetTriggerMode(out TriggerMode triggerMode, out TriggerSource hardwareTriggerModel);
            GetTriggerPolarity(out TriggerPolarity triggerPolarity);
            GetTriggerFliter(out ushort triggerfilter);
            GetGain(out float gain);
            GetTriggerDelay(out ushort triggerdelay);

            config = new CameraData()
            {
                triggerMode = triggerMode,
                triggeSource = hardwareTriggerModel,
                triggerPolarity = triggerPolarity,
                TriggerFilter = triggerfilter,
                TriggerDelay = triggerdelay,
                ExpouseTime = expouseTime,
                Gain = gain
            };
        }

        /// <summary>
        /// 设置触发模式及触发源
        /// </summary>
        /// <param name="mode"></param>
        /// <param name="triggerEnum"></param>
        /// <returns></returns>
        public abstract bool SetTriggerMode(TriggerMode mode, TriggerSource triggerEnum = TriggerSource.Line0);

        public abstract bool GetTriggerMode(out TriggerMode mode, out TriggerSource hardTriggerModel);

        public abstract bool SetExpouseTime(ulong value);

        public abstract bool GetExpouseTime(out ulong value);

        public abstract bool GetFrameRate(out float value);

        public abstract bool SetTriggerPolarity(TriggerPolarity polarity);

        public abstract bool GetTriggerPolarity(out TriggerPolarity polarity);

        /// <summary>
        /// 设置触发滤波时间 （us）
        /// </summary>
        /// <param name="flitertime"></param>
        /// <returns></returns>
        public abstract bool SetTriggerFliter(ushort flitertime);

        /// <summary>
        /// 获取触发参数时间 （us）
        /// </summary>
        /// <param name="flitertime"></param>
        /// <returns></returns>
        public abstract bool GetTriggerFliter(out ushort flitertime);

        public abstract bool SetTriggerDelay(ushort delay);

        public abstract bool GetTriggerDelay(out ushort delay);

        public abstract bool SetGain(float gain);

        public abstract bool GetGain(out float gain);

        public abstract bool SetLineMode(IOLines line, LineMode mode);

        public abstract bool SetLineStatus(IOLines line, LineStatus linestatus);

        public abstract bool GetLineStatus(IOLines line, out LineStatus lineStatus);

        public abstract bool AutoBalanceWhite();

        #endregion SettingConfig

        #region protected abstract

        /// <summary>
        /// 开始采图
        /// </summary>
        /// <returns></returns>
        protected abstract bool StartGrabbing();

        /// <summary>
        /// 停止采图
        /// </summary>
        /// <returns></returns>
        public abstract bool StopGrabbing();

        private void ResetActionImageSignal(Bitmap bitmap)
        {
            CallBaclImg?.Dispose();
            CallBaclImg = bitmap;
            ResetGetImageSignal.Set();
            // Debug.WriteLine("reset get img");
        }

        private void ResetAction3DImageSignal(HImage deepImage,HImage lightImage)
        {
            CallBaclDeepImg?.Dispose();
            CallBaclLightImg?.Dispose();
            CallBaclDeepImg = deepImage;
            CallBaclLightImg = lightImage;
            ResetGet3DImageSignal.Set();
            // Debug.WriteLine("reset get img");
        }

        public void Dispose()
        {
            CallBaclImg?.Dispose();
            CallBaclDeepImg?.Dispose();
            CallBaclLightImg?.Dispose();
        }

        #endregion protected abstract
    }
}