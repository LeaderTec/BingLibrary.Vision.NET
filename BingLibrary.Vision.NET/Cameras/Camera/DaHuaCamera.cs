using Basler.Pylon;
using BingLibrary.Vision.Cameras.CameraSDK.HaiKang;
using BingLibrary.Vision.NET.Cameras.Camera;
using MVSDK_Net;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace BingLibrary.Vision.Cameras.Camera
{
    internal class DaHuaCamera<T> : BaseCamera<T>
    {
        public DaHuaCamera() : base()
        {
        }

        #region param

        private Bitmap m_bitmap = null;
        private static Object BufForDriverLock = new Object();
        private MyCamera cam = new MyCamera(); // 设备对象 | device object
        private List<IMVDefine.IMV_Frame> m_frameList = new List<IMVDefine.IMV_Frame>(); // 图像缓存列表
        private Thread renderThread = null; // 显示线程 | image display thread
        private bool m_bShowLoop = true; // 线程控制变量 | thread looping flag
        private Mutex m_mutex = new Mutex(); // 锁，保证多线程安全 | mutex
        private bool m_bGrabbing = false;

        private IntPtr m_pDstData;
        private int m_nDataLenth = 0;
        private string m_binSavePath = Environment.CurrentDirectory + @"\Bins";
        private string m_bitMapSavePath = Environment.CurrentDirectory + @"\BitMaps";

        // private IntPtr m_BufForDriver = IntPtr.Zero;
        private UInt32 m_nBufSizeForDriver = 0;

        // private IMVDefine.IMV_FrameInfo frameInfo = new IMVDefine.IMV_FrameInfo();

        private static IMVDefine.IMV_FrameCallBack pFrameCallBack;

        #endregion param

        #region operate

        public override List<CameraInfo> GetListEnum()
        {
            GC.Collect();
            List<CameraInfo> cameraInfos = new List<CameraInfo>();

            // 设备搜索
            // device search
            IMVDefine.IMV_DeviceList deviceList = new IMVDefine.IMV_DeviceList();
            IMVDefine.IMV_EInterfaceType interfaceTp = IMVDefine.IMV_EInterfaceType.interfaceTypeAll;
            int res = MyCamera.IMV_EnumDevices(ref deviceList, (uint)interfaceTp);

            // 添加设备信息
            // Add device info
            if (res == IMVDefine.IMV_OK && deviceList.nDevNum > 0)
            {
                for (int i = 0; i < deviceList.nDevNum; i++)
                {
                    IMVDefine.IMV_DeviceInfo deviceInfo =
                        (IMVDefine.IMV_DeviceInfo)
                            Marshal.PtrToStructure(
                                deviceList.pDevInfo + Marshal.SizeOf(typeof(IMVDefine.IMV_DeviceInfo)) * i,
                                typeof(IMVDefine.IMV_DeviceInfo));

                    cameraInfos.Add(new CameraInfo()
                    {
                        CameraName = deviceInfo.cameraName,
                        CameraSN = deviceInfo.serialNumber,
                        CameraBrand = CameraBrand.DaHua,
                        CameraType =
                        deviceInfo.nCameraType == IMVDefine.IMV_ECameraType.typeGigeCamera ? CameraType.Gige :
                        deviceInfo.nCameraType == IMVDefine.IMV_ECameraType.typeU3vCamera ? CameraType.USB :
                        CameraType.Gige,
                    });
                }
            }

            return cameraInfos;
        }

        public override bool InitDevice(CameraInfo cameraInfo)
        {
            IMVDefine.IMV_DeviceList deviceList = new IMVDefine.IMV_DeviceList();
            IMVDefine.IMV_EInterfaceType interfaceTp = IMVDefine.IMV_EInterfaceType.interfaceTypeAll;
            int res = MyCamera.IMV_EnumDevices(ref deviceList, (uint)interfaceTp);

            bool selectSNflag = false;

            if (!string.IsNullOrEmpty(cameraInfo.CameraName))
            {
                for (int i = 0; i < deviceList.nDevNum; i++)
                {
                    IMVDefine.IMV_DeviceInfo item =
                        (IMVDefine.IMV_DeviceInfo)
                            Marshal.PtrToStructure(
                                deviceList.pDevInfo + Marshal.SizeOf(typeof(IMVDefine.IMV_DeviceInfo)) * i,
                                typeof(IMVDefine.IMV_DeviceInfo));

                    if (item.cameraName.Equals(cameraInfo.CameraName))
                    {
                        // 创建设备句柄
                        // Create Device Handle
                        res = cam.IMV_CreateHandle(IMVDefine.IMV_ECreateHandleMode.modeByDeviceUserID, 0, cameraInfo.CameraName);

                        selectSNflag = true;
                        break;
                    }
                }
            }
            else if (!string.IsNullOrEmpty(cameraInfo.CameraSN))
            {
                for (int i = 0; i < deviceList.nDevNum; i++)
                {
                    IMVDefine.IMV_DeviceInfo item =
                        (IMVDefine.IMV_DeviceInfo)
                            Marshal.PtrToStructure(
                                deviceList.pDevInfo + Marshal.SizeOf(typeof(IMVDefine.IMV_DeviceInfo)) * i,
                                typeof(IMVDefine.IMV_DeviceInfo));

                    if (item.cameraName.Equals(cameraInfo.CameraName))
                    {
                        // 创建设备句柄
                        // Create Device Handle
                        res = cam.IMV_CreateHandle(IMVDefine.IMV_ECreateHandleMode.modeByCameraKey, 0, cameraInfo.CameraSN);

                        selectSNflag = true;
                        break;
                    }
                }
            }
            if (!selectSNflag) return false;

            // 打开设备
            // open device
            res = cam.IMV_Open();
            if (res != IMVDefine.IMV_OK)
            {
                return false;
            }

            // 设置缓存个数为8（默认值为16）
            // set buffer count to 8 (default 16)
            res = cam.IMV_SetBufferCount(8);

            pFrameCallBack = onGetFrame;
            res = cam.IMV_AttachGrabbing(pFrameCallBack, IntPtr.Zero);

            return true;
        }

        public override void CloseDevice()
        {
            if (cam != null)
            {
                cam.IMV_Close(); // 关闭相机 | close camera
            }
        }

        public override bool SoftTrigger(T tData)
        {
            if (cam.IMV_IsGrabbing())
            {
                AddTriggerData(tData);
                //发送一次触发命令
                //Send Trigger Command
                int res = IMVDefine.IMV_OK;
                res = cam.IMV_ExecuteCommandFeature("TriggerSoftware");
                if (IMVDefine.IMV_OK != res)
                {
                    return false;
                }
                return true;
            }
            return false;
        }

        #endregion operate

        #region SettingConfig

        public override bool SetTriggerMode(TriggerMode mode, TriggerSource triggerEnum = TriggerSource.Line0)
        {
            int rec;
            switch (mode)
            {
                case TriggerMode.Off:
                    rec = cam.IMV_SetEnumFeatureSymbol("TriggerMode", "Off");
                    break;

                case TriggerMode.On:
                    rec = cam.IMV_SetEnumFeatureSymbol("TriggerMode", "On");
                    break;

                default:
                    rec = cam.IMV_SetEnumFeatureSymbol("TriggerMode", "Off");
                    break;
            }
            bool flag1 = IMVDefine.IMV_OK == rec;
            switch (triggerEnum)
            {
                case TriggerSource.Software:
                    rec = cam.IMV_SetEnumFeatureSymbol("TriggerSource", "Software");
                    break;

                case TriggerSource.Line0:
                    rec = cam.IMV_SetEnumFeatureSymbol("TriggerSource", "Line0");
                    break;

                case TriggerSource.Line1:
                    rec = cam.IMV_SetEnumFeatureSymbol("TriggerSource", "Line1");
                    break;

                case TriggerSource.Line2:
                    rec = cam.IMV_SetEnumFeatureSymbol("TriggerSource", "Line2");
                    break;

                case TriggerSource.Line3:
                    rec = cam.IMV_SetEnumFeatureSymbol("TriggerSource", "Line3");
                    break;

                default:
                    rec = cam.IMV_SetEnumFeatureSymbol("TriggerSource", "Line0");
                    break;
            }
            bool flag2 = IMVDefine.IMV_OK == rec;
            return flag1 && flag2;
        }

        public override bool GetTriggerMode(out TriggerMode mode, out TriggerSource hardTriggerModel)
        {
            mode = TriggerMode.On;
            hardTriggerModel = TriggerSource.Line0;

            //获取当前相机的触发模式
            //get Trigger Mode
            IMVDefine.IMV_String triggerMode = new IMVDefine.IMV_String();
            int nRet = cam.IMV_GetEnumFeatureSymbol("TriggerMode", ref triggerMode);
            bool flag1 = IMVDefine.IMV_OK == nRet;
            if (nRet != IMVDefine.IMV_OK)
            {
                mode = TriggerMode.Off;
            }
            if (triggerMode.str == "Off")
            {
                mode = TriggerMode.Off;
            }
            else
            {
                mode = TriggerMode.On;
            }

            IMVDefine.IMV_String triggerSourcce = new IMVDefine.IMV_String();
            nRet = cam.IMV_GetEnumFeatureSymbol("TriggerSource", ref triggerSourcce);
            bool flag2 = IMVDefine.IMV_OK == nRet;
            if (triggerSourcce.str == "Software")
            {
                hardTriggerModel = TriggerSource.Software;
            }
            else if (triggerSourcce.str == "Line0")
            {
                hardTriggerModel = TriggerSource.Line0;
            }
            else if (triggerSourcce.str == "Line1")
            {
                hardTriggerModel = TriggerSource.Line1;
            }
            else if (triggerSourcce.str == "Line2")
            {
                hardTriggerModel = TriggerSource.Line2;
            }

            return flag1 && flag2;
        }

        public override bool SetExpouseTime(ulong value) => cam.IMV_SetDoubleFeatureValue("ExposureTime", value) == IMVDefine.IMV_OK;

        public override bool GetExpouseTime(out ulong value)
        {
            double expouseTime = 0;
            int nRet = cam.IMV_GetDoubleFeatureValue("ExposureTime", ref expouseTime);
            value = (ulong)expouseTime;
            return IMVDefine.IMV_OK == nRet;
        }

        public override bool GetFrameRate(out float value)
        {
            double frameRate = 0;
            int nRet = cam.IMV_GetDoubleFeatureValue("AcquisitionFrameRate", ref frameRate);
            value = (float)frameRate;
            return IMVDefine.IMV_OK == nRet;
        }

        public override bool SetGain(float gain) => cam.IMV_SetDoubleFeatureValue("GainRaw", gain) == IMVDefine.IMV_OK;

        public override bool GetGain(out float gain)
        {
            double gainValue = 0;
            int nRet = cam.IMV_GetDoubleFeatureValue("GainRaw", ref gainValue);
            gain = (ulong)gainValue;
            return IMVDefine.IMV_OK == nRet;
        }

        //1下降沿 0 上升沿
        public override bool SetTriggerPolarity(TriggerPolarity polarity)
            => cam.IMV_SetEnumFeatureSymbol("TriggerActivation", polarity.ToString()) == IMVDefine.IMV_OK;

        public override bool GetTriggerPolarity(out TriggerPolarity polarity)
        {
            polarity = TriggerPolarity.RisingEdge;

            IMVDefine.IMV_String polarityStr = new IMVDefine.IMV_String();
            int nRet = cam.IMV_GetEnumFeatureSymbol("TriggerActivation", ref polarityStr);

            if (polarityStr.str == "RisingEdge")
            {
                polarity = TriggerPolarity.RisingEdge;
            }
            else if (polarityStr.str == "FallingEdge")
            { //下降沿
                polarity = TriggerPolarity.FallingEdge;
            }
            return IMVDefine.IMV_OK == nRet;
        }

        public override bool SetTriggerFliter(ushort flitertime) => false;

        public override bool GetTriggerFliter(out ushort flitertime)
        {
            flitertime = 0;
            return false;
        }

        public override bool SetTriggerDelay(ushort delay) => false;

        public override bool GetTriggerDelay(out ushort delay)
        {
            delay = 0;
            return false;
        }

        public override bool SetLineMode(IOLines line, LineMode mode)
            => false;

        public override bool SetLineStatus(IOLines line, LineStatus linestatus)
              => false;

        public override bool GetLineStatus(IOLines line, out LineStatus linestatus)
        {
            linestatus = LineStatus.Low;
            return false;
        }

        public override bool AutoBalanceWhite() => false;

        #endregion SettingConfig

        #region helper

        /// <summary>
        ///  // Set default state after grabbing starts
        // Turn off real-time mode which is default
        // 0: real-time
        // 1: trigger
        /// </summary>
        /// <returns></returns>
        protected override bool StartGrabbing()
        {
            int ret = cam.IMV_StartGrabbing();
            return ret == IMVDefine.IMV_OK;
        }

        public override bool StopGrabbing() => cam.IMV_StopGrabbing() == IMVDefine.IMV_OK;

        private Bitmap ParseRawImageDatacallback(IntPtr pData, IMVDefine.IMV_FrameInfo stFrameInfo)
        {
            lock (BufForDriverLock)
            {
                ConvertToBitmap(pData, stFrameInfo, ref m_bitmap);
                cam.IMV_ClearFrameBuffer();
            }

            return m_bitmap;
        }

        private bool ConvertToBitmap(IntPtr pSrcData, IMVDefine.IMV_FrameInfo stFrameInfo, ref Bitmap bitmap)
        {
            IntPtr pDstRGB;
            BitmapData bmpData;
            Rectangle bitmapRect = new Rectangle();
            int ImgSize;

            if (null != bitmap)
            {
                bitmap.Dispose();
                bitmap = null;
            }

            if (stFrameInfo.pixelFormat == IMVDefine.IMV_EPixelType.gvspPixelMono8) //图像格式为Mono8时，无需转码，直接转成bitmap进行保存
            {
                // 用Mono8数据生成Bitmap
                bitmap = new Bitmap((int)stFrameInfo.width, (int)stFrameInfo.height, PixelFormat.Format8bppIndexed);
                ColorPalette colorPalette = bitmap.Palette;
                for (int i = 0; i != 256; ++i)
                {
                    colorPalette.Entries[i] = Color.FromArgb(i, i, i);
                }
                bitmap.Palette = colorPalette;

                bitmapRect.Height = bitmap.Height;
                bitmapRect.Width = bitmap.Width;
                bmpData = bitmap.LockBits(bitmapRect, ImageLockMode.ReadWrite, bitmap.PixelFormat);
                APublicStaticHelper.CopyMemory(bmpData.Scan0, pSrcData, (uint)(bmpData.Stride * bitmap.Height));
                bitmap.UnlockBits(bmpData);
            }
            else if (stFrameInfo.pixelFormat == IMVDefine.IMV_EPixelType.gvspPixelBGR8) //图像格式为BGR8时，无需转码，直接转成bitmap进行保存
            {
                // 用BGR24数据生成Bitmap
                bitmap = new Bitmap((int)stFrameInfo.width, (int)stFrameInfo.height, PixelFormat.Format24bppRgb);

                bitmapRect.Height = bitmap.Height;
                bitmapRect.Width = bitmap.Width;
                bmpData = bitmap.LockBits(bitmapRect, ImageLockMode.ReadWrite, bitmap.PixelFormat);
                APublicStaticHelper.CopyMemory(bmpData.Scan0, pSrcData, (uint)(bmpData.Stride * bitmap.Height));
                bitmap.UnlockBits(bmpData);
            }
            else //当图像格式为其它时，先转码为BGR24，然后转成bitmap进行保存
            {
                ImgSize = (int)stFrameInfo.width * (int)stFrameInfo.height * 3;

                try
                {
                    pDstRGB = Marshal.AllocHGlobal(ImgSize);
                }
                catch
                {
                    return false;
                }
                if (pDstRGB == IntPtr.Zero)
                {
                    return false;
                }

                IMVDefine.IMV_PixelConvertParam stPixelConvertParam = new IMVDefine.IMV_PixelConvertParam();
                int res = 0;
                // 转码参数
                stPixelConvertParam.nWidth = stFrameInfo.width;
                stPixelConvertParam.nHeight = stFrameInfo.height;
                stPixelConvertParam.ePixelFormat = stFrameInfo.pixelFormat;
                stPixelConvertParam.pSrcData = pSrcData;
                stPixelConvertParam.nSrcDataLen = stFrameInfo.size;
                stPixelConvertParam.nPaddingX = stFrameInfo.paddingX;
                stPixelConvertParam.nPaddingY = stFrameInfo.paddingY;
                stPixelConvertParam.eBayerDemosaic = IMVDefine.IMV_EBayerDemosaic.demosaicNearestNeighbor;
                stPixelConvertParam.eDstPixelFormat = IMVDefine.IMV_EPixelType.gvspPixelBGR8;
                stPixelConvertParam.pDstBuf = pDstRGB;
                stPixelConvertParam.nDstBufSize = (uint)ImgSize;

                res = cam.IMV_PixelConvert(ref stPixelConvertParam);
                if (IMVDefine.IMV_OK != res)
                {
                    return false;
                }

                // 用BGR24数据生成Bitmap
                bitmap = new Bitmap((int)stFrameInfo.width, (int)stFrameInfo.height, PixelFormat.Format24bppRgb);

                bitmapRect.Height = bitmap.Height;
                bitmapRect.Width = bitmap.Width;
                bmpData = bitmap.LockBits(bitmapRect, ImageLockMode.ReadWrite, bitmap.PixelFormat);
                APublicStaticHelper.CopyMemory(bmpData.Scan0, pDstRGB, (uint)(bmpData.Stride * bitmap.Height));
                bitmap.UnlockBits(bmpData);

                Marshal.FreeHGlobal(pDstRGB);
            }
            return true;
        }

        private void onGetFrame(ref IMVDefine.IMV_Frame frame, IntPtr pUser)
        {
            if (frame.frameHandle == IntPtr.Zero)
            {
                return;
            }

            var bitMap = ParseRawImageDatacallback(frame.pData, frame.frameInfo);
            if (bitMap == null) return;

            ActionGetImage?.Invoke(bitMap.Clone() as Bitmap);
        }

        public override bool LoadCamConfig(string filePath)
        {
            return false;
        }

        #endregion helper
    }
}