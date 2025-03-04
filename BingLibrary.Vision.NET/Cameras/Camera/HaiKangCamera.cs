using BingLibrary.Vision.Cameras.CameraSDK.HaiKang;
using MVSDK_Net;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace BingLibrary.Vision.Cameras
{
    internal class HaiKangCamera : BaseCamera
    {
        public HaiKangCamera() : base()
        {
        }

        [DllImport("kernel32.dll", EntryPoint = "RtlMoveMemory", SetLastError = false)]
        public static extern void CopyMemory(IntPtr dest, IntPtr src, uint count);

        #region param

        private HKCameraCtrl _myCamera = new HKCameraCtrl();
        private HKCameraCtrl.cbOutputExdelegate _imageCallbackDelegate = null;

        private static Object BufForDriverLock = new Object();
        private UInt32 m_nBufSizeForDriver = 0;
        private Bitmap m_bitmap = null;
        private PixelFormat m_bitmapPixelFormat = PixelFormat.DontCare;

        // IntPtr m_BufForDriver = IntPtr.Zero;
        private IntPtr m_ConvertDstBuf = IntPtr.Zero;

        private UInt32 m_nConvertDstBufLen = 0;

        #endregion param

        #region operate

        public override List<string> GetListEnum()
        {
            GC.Collect();
            List<string> listsn = new List<string>();
            var m_stDeviceList = new HKCameraCtrl.MV_CC_DEVICE_INFO_LIST();
            List<HKCameraCtrl.MV_CC_DEVICE_INFO> deviceList = new List<HKCameraCtrl.MV_CC_DEVICE_INFO>();
            m_stDeviceList.nDeviceNum = 0;
            HKCameraCtrl.MV_CC_EnumDevices_NET(HKCameraCtrl.MV_GIGE_DEVICE | HKCameraCtrl.MV_USB_DEVICE, ref m_stDeviceList);
            for (int i = 0; i < m_stDeviceList.nDeviceNum; i++)
            {
                HKCameraCtrl.MV_CC_DEVICE_INFO device = (HKCameraCtrl.MV_CC_DEVICE_INFO)Marshal.PtrToStructure(m_stDeviceList.pDeviceInfo[i], typeof(HKCameraCtrl.MV_CC_DEVICE_INFO));

                if (device.nTLayerType == HKCameraCtrl.MV_GIGE_DEVICE)
                {
                    HKCameraCtrl.MV_GIGE_DEVICE_INFO gigeInfo = (HKCameraCtrl.MV_GIGE_DEVICE_INFO)HKCameraCtrl.ByteToStruct(device.SpecialInfo.stGigEInfo, typeof(HKCameraCtrl.MV_GIGE_DEVICE_INFO));
                    listsn.Add(gigeInfo.chSerialNumber);
                }
                else if (device.nTLayerType == HKCameraCtrl.MV_USB_DEVICE)
                {
                    HKCameraCtrl.MV_USB3_DEVICE_INFO usbInfo = (HKCameraCtrl.MV_USB3_DEVICE_INFO)HKCameraCtrl.ByteToStruct(device.SpecialInfo.stUsb3VInfo, typeof(HKCameraCtrl.MV_USB3_DEVICE_INFO));
                    listsn.Add(usbInfo.chSerialNumber);
                }
            }

            return listsn;
        }

        public override bool InitDevice(string CamSN)
        {
            if (string.IsNullOrEmpty(CamSN)) return false;
            HKCameraCtrl.MV_CC_DEVICE_INFO camerainfo = new HKCameraCtrl.MV_CC_DEVICE_INFO();
            var infolist = GetListInfoEnum();
            if (infolist.Count < 1) return false;

            bool selectSNflag = false;
            foreach (var item in infolist)
            {
                if (item.nTLayerType == HKCameraCtrl.MV_GIGE_DEVICE)
                {
                    HKCameraCtrl.MV_GIGE_DEVICE_INFO gigeInfo = (HKCameraCtrl.MV_GIGE_DEVICE_INFO)HKCameraCtrl.ByteToStruct(item.SpecialInfo.stGigEInfo, typeof(HKCameraCtrl.MV_GIGE_DEVICE_INFO));
                    if (gigeInfo.chSerialNumber.Equals(CamSN))
                    {
                        camerainfo = item;
                        selectSNflag = true;
                        break;
                    }
                }
                else if (item.nTLayerType == HKCameraCtrl.MV_USB_DEVICE)
                {
                    HKCameraCtrl.MV_USB3_DEVICE_INFO usbInfo = (HKCameraCtrl.MV_USB3_DEVICE_INFO)HKCameraCtrl.ByteToStruct(item.SpecialInfo.stUsb3VInfo, typeof(HKCameraCtrl.MV_USB3_DEVICE_INFO));
                    if (usbInfo.chSerialNumber.Equals(CamSN))
                    {
                        camerainfo = item;
                        selectSNflag = true;
                        break;
                    }
                }
            }

            if (!selectSNflag) return false;

            // ch:打开设备 | en:Open device
            if (null == _myCamera)
            {
                _myCamera = new HKCameraCtrl();
                if (null == _myCamera)
                {
                    Debug.WriteLine("Applying resource fail!", HKCameraCtrl.MV_E_RESOURCE);
                    return false;
                }
            }

            int nRet = _myCamera.MV_CC_CreateDevice_NET(ref camerainfo);
            if (HKCameraCtrl.MV_OK != nRet)
            {
                Debug.WriteLine("Create device fail!", nRet);
                return false;
            }

            nRet = _myCamera.MV_CC_OpenDevice_NET();
            if (HKCameraCtrl.MV_OK != nRet)
            {
                _myCamera.MV_CC_DestroyDevice_NET();
                Debug.WriteLine("Device open fail!", nRet);
                return false;
            }

            // Register image acquisition call back
            _imageCallbackDelegate = ImageCallback;
            nRet = _myCamera.MV_CC_RegisterImageCallBackEx_NET(_imageCallbackDelegate, IntPtr.Zero);
            if (nRet != 0)
            {
                Debug.WriteLine("Register image acquisition call back failed");
                _myCamera.MV_CC_DestroyDevice_NET();
                return false;
            }

            // ch:探测网络最佳包大小(只对GigE相机有效) | en:Detection network optimal package size(It only works for the GigE camera)
            if (camerainfo.nTLayerType == HKCameraCtrl.MV_GIGE_DEVICE)
            {
                int nPacketSize = _myCamera.MV_CC_GetOptimalPacketSize_NET();
                if (nPacketSize > 0)
                {
                    nRet = _myCamera.MV_CC_SetIntValueEx_NET("GevSCPSPacketSize", nPacketSize);
                    if (nRet != HKCameraCtrl.MV_OK)
                    {
                        Debug.WriteLine("Set Packet Size failed!", nRet);
                    }
                }
                else
                {
                    Debug.WriteLine("Get Packet Size failed!", nPacketSize);
                }

                //设置心跳时间1000ms
                nRet = _myCamera.MV_CC_SetHeartBeatTimeout_NET(1000);
                if (nRet != HKCameraCtrl.MV_OK)
                {
                    Debug.WriteLine("Set HeartBeatTimeout  failed!", nRet);
                }
            }

            //更新图像Buff大小；
            NecessaryOperBeforeGrab();
            SN = CamSN;

            return true;
        }

        public override void CloseDevice()
        {
            StopGrabbing();
            var nRet = _myCamera.MV_CC_CloseDevice_NET();
            if (HKCameraCtrl.MV_OK != nRet) return;
            nRet = _myCamera.MV_CC_DestroyDevice_NET();
            //try
            //{
            //    if (IntPtr.Zero != m_ConvertDstBuf)
            //    {
            //        Marshal.Release(m_ConvertDstBuf);
            //        m_ConvertDstBuf = IntPtr.Zero;
            //    }
            //}
            //catch { }
         
            if (HKCameraCtrl.MV_OK != nRet) return;
        }

        public override bool SoftTrigger() => _myCamera.MV_CC_SetCommandValue_NET("TriggerSoftware") == HKCameraCtrl.MV_OK;

        #endregion operate

        #region SettingConfig

        public override bool SetTriggerMode(TriggerMode mode, TriggerSource triggerEnum = TriggerSource.Line0)
        {
            int rec;
            switch (mode)
            {
                case TriggerMode.Off:
                    rec = _myCamera.MV_CC_SetEnumValue_NET("TriggerMode", (uint)HKCameraCtrl.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_OFF);
                    break;

                case TriggerMode.On:
                    rec = _myCamera.MV_CC_SetEnumValue_NET("TriggerMode", (uint)HKCameraCtrl.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_ON);
                    break;

                default:
                    rec = _myCamera.MV_CC_SetEnumValue_NET("TriggerMode", (uint)HKCameraCtrl.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_ON);
                    break;
            }
            bool flag1 = HKCameraCtrl.MV_OK == rec;
            switch (triggerEnum)
            {
                case TriggerSource.Software:
                    rec = _myCamera.MV_CC_SetEnumValue_NET("TriggerSource", (uint)HKCameraCtrl.MV_CAM_TRIGGER_SOURCE.MV_TRIGGER_SOURCE_SOFTWARE);
                    break;

                case TriggerSource.Line0:
                    rec = _myCamera.MV_CC_SetEnumValue_NET("TriggerSource", (uint)HKCameraCtrl.MV_CAM_TRIGGER_SOURCE.MV_TRIGGER_SOURCE_LINE0);
                    break;

                case TriggerSource.Line1:
                    rec = _myCamera.MV_CC_SetEnumValue_NET("TriggerSource", (uint)HKCameraCtrl.MV_CAM_TRIGGER_SOURCE.MV_TRIGGER_SOURCE_LINE1);
                    break;

                case TriggerSource.Line2:
                    rec = _myCamera.MV_CC_SetEnumValue_NET("TriggerSource", (uint)HKCameraCtrl.MV_CAM_TRIGGER_SOURCE.MV_TRIGGER_SOURCE_LINE2);
                    break;

                case TriggerSource.Line3:
                    rec = _myCamera.MV_CC_SetEnumValue_NET("TriggerSource", (uint)HKCameraCtrl.MV_CAM_TRIGGER_SOURCE.MV_TRIGGER_SOURCE_LINE3);
                    break;

                default:
                    rec = _myCamera.MV_CC_SetEnumValue_NET("TriggerSource", (uint)HKCameraCtrl.MV_CAM_TRIGGER_SOURCE.MV_TRIGGER_SOURCE_LINE0);
                    break;
            }
            bool flag2 = HKCameraCtrl.MV_OK == rec;
            return flag1 && flag2;
        }

        public override bool GetTriggerMode(out TriggerMode mode, out TriggerSource hardTriggerModel)
        {
            mode = TriggerMode.On;
            hardTriggerModel = TriggerSource.Line0;
            HKCameraCtrl.MVCC_ENUMVALUE stParam = new HKCameraCtrl.MVCC_ENUMVALUE();

            int nRet = _myCamera.MV_CC_GetEnumValue_NET("TriggerMode", ref stParam);
            HKCameraCtrl.MV_CAM_TRIGGER_MODE Mode = (HKCameraCtrl.MV_CAM_TRIGGER_MODE)stParam.nCurValue;
            bool flag1 = HKCameraCtrl.MV_OK == nRet;

            switch (Mode)
            {
                case HKCameraCtrl.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_OFF:
                    mode = TriggerMode.Off;
                    break;

                case HKCameraCtrl.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_ON:
                    mode = TriggerMode.On;
                    break;

                default:
                    mode = TriggerMode.On;
                    break;
            }

            nRet = _myCamera.MV_CC_GetEnumValue_NET("TriggerSource", ref stParam);
            HKCameraCtrl.MV_CAM_TRIGGER_SOURCE Source = (HKCameraCtrl.MV_CAM_TRIGGER_SOURCE)stParam.nCurValue;
            bool flag2 = HKCameraCtrl.MV_OK == nRet;
            switch (Source)
            {
                case HKCameraCtrl.MV_CAM_TRIGGER_SOURCE.MV_TRIGGER_SOURCE_LINE0:
                    hardTriggerModel = TriggerSource.Line0;
                    break;

                case HKCameraCtrl.MV_CAM_TRIGGER_SOURCE.MV_TRIGGER_SOURCE_LINE1:
                    hardTriggerModel = TriggerSource.Line1;
                    break;

                case HKCameraCtrl.MV_CAM_TRIGGER_SOURCE.MV_TRIGGER_SOURCE_LINE2:
                    hardTriggerModel = TriggerSource.Line2;
                    break;

                case HKCameraCtrl.MV_CAM_TRIGGER_SOURCE.MV_TRIGGER_SOURCE_SOFTWARE:
                    hardTriggerModel = TriggerSource.Software;
                    break;

                default:
                    hardTriggerModel = TriggerSource.Line0;
                    break;
            }

            return flag1 && flag2;
        }

        public override bool SetExpouseTime(ulong value) => _myCamera.MV_CC_SetFloatValue_NET("ExposureTime", value) == HKCameraCtrl.MV_OK;

        public override bool GetExpouseTime(out ulong value)
        {
            HKCameraCtrl.MVCC_FLOATVALUE stParam = new HKCameraCtrl.MVCC_FLOATVALUE();
            int nRet = _myCamera.MV_CC_GetFloatValue_NET("ExposureTime", ref stParam);
            value = (ulong)stParam.fCurValue;
            return HKCameraCtrl.MV_OK == nRet;
        }

        public override bool GetFrameRate(out float value)
        {
            value = 0;
            HKCameraCtrl.MVCC_FLOATVALUE stParam = new HKCameraCtrl.MVCC_FLOATVALUE();
            int nRet = _myCamera.MV_CC_GetFloatValue_NET("ResultingFrameRate", ref stParam);
            value = (float)stParam.fCurValue;
            return HKCameraCtrl.MV_OK == nRet;
        }

        //1下降沿 0 上升沿
        public override bool SetTriggerPolarity(TriggerPolarity polarity)
            => _myCamera.MV_CC_SetEnumValueByString_NET("TriggerActivation", polarity.ToString()) == HKCameraCtrl.MV_OK;

        public override bool GetTriggerPolarity(out TriggerPolarity polarity)
        {
            polarity = TriggerPolarity.RisingEdge;
            HKCameraCtrl.MVCC_ENUMVALUE stParam = new HKCameraCtrl.MVCC_ENUMVALUE();
            int nRet = _myCamera.MV_CC_GetEnumValue_NET("TriggerActivation", ref stParam);

            ushort activate = (ushort)stParam.nCurValue;
            //1下降沿 0 上升沿
            if (activate == 0)
            { //上升沿
                polarity = TriggerPolarity.RisingEdge;
            }
            else if (activate == 1)
            { //下降沿
                polarity = TriggerPolarity.FallingEdge;
            }
            return HKCameraCtrl.MV_OK == nRet;
        }

        public override bool SetTriggerFliter(ushort flitertime) => _myCamera.MV_CC_SetIntValue_NET("LineDebouncerTime", flitertime) == HKCameraCtrl.MV_OK;

        public override bool GetTriggerFliter(out ushort flitertime)
        {
            flitertime = 1000;
            HKCameraCtrl.MVCC_INTVALUE stParam = new HKCameraCtrl.MVCC_INTVALUE();
            int nRet = _myCamera.MV_CC_GetIntValue_NET("LineDebouncerTime", ref stParam);
            flitertime = (ushort)stParam.nCurValue;
            return HKCameraCtrl.MV_OK == nRet;
        }

        public override bool SetTriggerDelay(ushort delay) => _myCamera.MV_CC_SetFloatValue_NET("TriggerDelay", delay) == HKCameraCtrl.MV_OK;

        public override bool GetTriggerDelay(out ushort delay)
        {
            delay = 0;
            HKCameraCtrl.MVCC_FLOATVALUE stParam = new HKCameraCtrl.MVCC_FLOATVALUE();
            int nRet = _myCamera.MV_CC_GetFloatValue_NET("TriggerDelay", ref stParam);
            delay = (ushort)stParam.fCurValue;
            return HKCameraCtrl.MV_OK == nRet;
        }

        public override bool SetGain(float gain) => _myCamera.MV_CC_SetFloatValue_NET("Gain", gain) == HKCameraCtrl.MV_OK;

        public override bool GetGain(out float gain)
        {
            HKCameraCtrl.MVCC_FLOATVALUE stParam = new HKCameraCtrl.MVCC_FLOATVALUE();
            int nRet = _myCamera.MV_CC_GetFloatValue_NET("Gain", ref stParam);
            gain = stParam.fCurValue;
            return HKCameraCtrl.MV_OK == nRet;
        }

        public override bool SetLineMode(IOLines line, LineMode mode)
            => _myCamera.MV_CC_SetEnumValueByString_NET(line.ToString(), mode.ToString()) == HKCameraCtrl.MV_OK;

        public override bool SetLineStatus(IOLines line, LineStatus linestatus)
              => _myCamera.MV_CC_SetBoolValue_NET(line.ToString(), linestatus.Equals(LineStatus.Hight)) == HKCameraCtrl.MV_OK;

        public override bool GetLineStatus(IOLines line, out LineStatus linestatus)
        {
            bool resultsignal = false;
            int nRet = _myCamera.MV_CC_GetBoolValue_NET(line.ToString(), ref resultsignal);
            linestatus = resultsignal ? LineStatus.Hight : LineStatus.Low;
            return HKCameraCtrl.MV_OK == nRet;
        }

        public override bool AutoBalanceWhite() => _myCamera.MV_CC_SetEnumValueByString_NET("BalanceWhiteAuto", "Once") == HKCameraCtrl.MV_OK;

        #endregion SettingConfig

        #region helper

        /// <summary>
        ///  // Set default state after grabbing starts
        // Turn off real-time mode which is default
        // 0: real-time
        // 1: trigger
        /// </summary>
        /// <returns></returns>
        protected override bool StartGrabbing() => _myCamera.MV_CC_StartGrabbing_NET() == HKCameraCtrl.MV_OK;

        public override bool StopGrabbing() => _myCamera.MV_CC_StopGrabbing_NET() == HKCameraCtrl.MV_OK;

        private List<HKCameraCtrl.MV_CC_DEVICE_INFO> GetListInfoEnum()
        {
            List<string> listsn = new List<string>();
            HKCameraCtrl.MV_CC_DEVICE_INFO_LIST m_stDeviceList = new HKCameraCtrl.MV_CC_DEVICE_INFO_LIST();
            List<HKCameraCtrl.MV_CC_DEVICE_INFO> deviceList = new List<HKCameraCtrl.MV_CC_DEVICE_INFO>();
            m_stDeviceList.nDeviceNum = 0;
            HKCameraCtrl.MV_CC_EnumDevices_NET(HKCameraCtrl.MV_GIGE_DEVICE | HKCameraCtrl.MV_USB_DEVICE, ref m_stDeviceList);
            for (int i = 0; i < m_stDeviceList.nDeviceNum; i++)
            {
                HKCameraCtrl.MV_CC_DEVICE_INFO device = (HKCameraCtrl.MV_CC_DEVICE_INFO)Marshal.PtrToStructure(m_stDeviceList.pDeviceInfo[i], typeof(HKCameraCtrl.MV_CC_DEVICE_INFO));
                deviceList.Add(device);
            }
            return deviceList;
        }

        //private Bitmap ParseRawImageDatacallback(IntPtr pData, HKCameraCtrl.MV_FRAME_OUT_INFO_EX stFrameInfo)
        //{
        //    lock (BufForDriverLock)
        //    {
        //        ///  // ch:转换像素格式 | en:Convert Pixel Format
        //        HKCameraCtrl.MV_PIXEL_CONVERT_PARAM stConvertInfo = new HKCameraCtrl.MV_PIXEL_CONVERT_PARAM();
        //        stConvertInfo.nWidth = stFrameInfo.nWidth;
        //        stConvertInfo.nHeight = stFrameInfo.nHeight;
        //        stConvertInfo.enSrcPixelType = stFrameInfo.enPixelType;
        //        stConvertInfo.pSrcData = pData;
        //        stConvertInfo.nSrcDataLen = stFrameInfo.nFrameLen;
        //        stConvertInfo.pDstBuffer = m_ConvertDstBuf;
        //        stConvertInfo.nDstBufferSize = m_nConvertDstBufLen;

        //        if (PixelFormat.Format8bppIndexed == m_bitmap.PixelFormat)
        //        {
        //            stConvertInfo.enDstPixelType = HKCameraCtrl.MvGvspPixelType.PixelType_Gvsp_Mono8;
        //            _myCamera.MV_CC_ConvertPixelType_NET(ref stConvertInfo);
        //        }
        //        else
        //        {
        //            stConvertInfo.enDstPixelType = HKCameraCtrl.MvGvspPixelType.PixelType_Gvsp_BGR8_Packed;
        //            _myCamera.MV_CC_ConvertPixelType_NET(ref stConvertInfo);
        //        }

        //        // ch:保存Bitmap数据 | en:Save Bitmap Data
        //        BitmapData bitmapData = m_bitmap.LockBits(new Rectangle(0, 0, stConvertInfo.nWidth, stConvertInfo.nHeight), ImageLockMode.ReadWrite, m_bitmap.PixelFormat);
        //        CopyMemory(bitmapData.Scan0, stConvertInfo.pDstBuffer, (UInt32)(bitmapData.Stride * m_bitmap.Height));

        //        m_bitmap.UnlockBits(bitmapData);

        //        _myCamera.MV_CC_ClearImageBuffer_NET();
        //    }

        //    return m_bitmap;
        //}

        private Bitmap ParseRawImageDatacallback(IntPtr pData, HKCameraCtrl.MV_FRAME_OUT_INFO_EX stFrameInfo)
        {
            lock (BufForDriverLock)
            {
                // 确保目标转换缓冲区足够大
                int requiredDstBufferSize = CalculateDstBufferSize(stFrameInfo);
                if (m_ConvertDstBuf == IntPtr.Zero || requiredDstBufferSize > m_nConvertDstBufLen)
                {
                    if (m_ConvertDstBuf != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(m_ConvertDstBuf);
                    }
                    m_ConvertDstBuf = Marshal.AllocHGlobal(requiredDstBufferSize);
                    m_nConvertDstBufLen = (UInt32)requiredDstBufferSize;
                }

                // 配置像素转换参数
                HKCameraCtrl.MV_PIXEL_CONVERT_PARAM stConvertInfo = new HKCameraCtrl.MV_PIXEL_CONVERT_PARAM
                {
                    nWidth = stFrameInfo.nWidth,
                    nHeight = stFrameInfo.nHeight,
                    enSrcPixelType = stFrameInfo.enPixelType,
                    pSrcData = pData,
                    nSrcDataLen = stFrameInfo.nFrameLen,
                    pDstBuffer = m_ConvertDstBuf,
                    nDstBufferSize = m_nConvertDstBufLen,
                    enDstPixelType = (m_bitmap.PixelFormat == PixelFormat.Format8bppIndexed)
                        ? HKCameraCtrl.MvGvspPixelType.PixelType_Gvsp_Mono8
                        : HKCameraCtrl.MvGvspPixelType.PixelType_Gvsp_BGR8_Packed
                };

                // 执行像素格式转换
                _myCamera.MV_CC_ConvertPixelType_NET(ref stConvertInfo);

                // 更新或创建Bitmap对象
                if (m_bitmap == null || m_bitmap.Width != stConvertInfo.nWidth || m_bitmap.Height != stConvertInfo.nHeight)
                {
                    m_bitmap?.Dispose();
                    m_bitmap = new Bitmap(
                        stConvertInfo.nWidth,
                        stConvertInfo.nHeight,
                        (stConvertInfo.enDstPixelType == HKCameraCtrl.MvGvspPixelType.PixelType_Gvsp_Mono8)
                            ? PixelFormat.Format8bppIndexed
                            : PixelFormat.Format24bppRgb
                    );
                    // 若为8位灰度图，设置调色板
                    if (m_bitmap.PixelFormat == PixelFormat.Format8bppIndexed)
                    {
                        ColorPalette palette = m_bitmap.Palette;
                        for (int i = 0; i < 256; i++)
                        {
                            palette.Entries[i] = Color.FromArgb(i, i, i);
                        }
                        m_bitmap.Palette = palette;
                    }
                }

                // 拷贝数据到Bitmap
                BitmapData bitmapData = m_bitmap.LockBits(
                    new Rectangle(0, 0, m_bitmap.Width, m_bitmap.Height),
                    ImageLockMode.WriteOnly,
                    m_bitmap.PixelFormat
                );
                CopyMemory(bitmapData.Scan0, m_ConvertDstBuf, (uint)(bitmapData.Stride * m_bitmap.Height));
                m_bitmap.UnlockBits(bitmapData);

                _myCamera.MV_CC_ClearImageBuffer_NET();
            }
            return m_bitmap;
        }

        // 计算目标缓冲区所需大小
        private int CalculateDstBufferSize(HKCameraCtrl.MV_FRAME_OUT_INFO_EX stFrameInfo)
        {
            int bytesPerPixel = (m_bitmap?.PixelFormat == PixelFormat.Format8bppIndexed) ? 1 : 3;
            return stFrameInfo.nWidth * stFrameInfo.nHeight * bytesPerPixel;
        }

        /// <summary>
        /// ch:取图前的必要操作步骤 | en:Necessary operation before grab
        /// </summary>
        /// <returns></returns>
        private Int32 NecessaryOperBeforeGrab()
        {
            // ch:取图像宽 | en:Get Iamge Width
            HKCameraCtrl.MVCC_INTVALUE_EX stWidth = new HKCameraCtrl.MVCC_INTVALUE_EX();
            int nRet = _myCamera.MV_CC_GetIntValueEx_NET("Width", ref stWidth);
            if (HKCameraCtrl.MV_OK != nRet)
            {
                return nRet;
            }
            // ch:取图像高 | en:Get Iamge Height
            HKCameraCtrl.MVCC_INTVALUE_EX stHeight = new HKCameraCtrl.MVCC_INTVALUE_EX();
            nRet = _myCamera.MV_CC_GetIntValueEx_NET("Height", ref stHeight);
            if (HKCameraCtrl.MV_OK != nRet)
            {
                return nRet;
            }
            // ch:取像素格式 | en:Get Pixel Format
            HKCameraCtrl.MVCC_ENUMVALUE stPixelFormat = new HKCameraCtrl.MVCC_ENUMVALUE();
            nRet = _myCamera.MV_CC_GetEnumValue_NET("PixelFormat", ref stPixelFormat);
            if (HKCameraCtrl.MV_OK != nRet)
            {
                return nRet;
            }

            // ch:设置bitmap像素格式，申请相应大小内存 | en:Set Bitmap Pixel Format, alloc memory
            if ((Int32)HKCameraCtrl.MvGvspPixelType.PixelType_Gvsp_Undefined == (Int32)stPixelFormat.nCurValue)
            {
                return HKCameraCtrl.MV_E_UNKNOW;
            }
            else if (IsMonoData((HKCameraCtrl.MvGvspPixelType)stPixelFormat.nCurValue))
            {
                m_bitmapPixelFormat = PixelFormat.Format8bppIndexed;
                //m_bitmapPixelFormat = PixelFormat.Format16bppGrayScale;
                if (IntPtr.Zero != m_ConvertDstBuf)
                {
                    Marshal.Release(m_ConvertDstBuf);
                    m_ConvertDstBuf = IntPtr.Zero;
                }
                m_nConvertDstBufLen = (UInt32)(stWidth.nCurValue * stHeight.nCurValue);
                m_ConvertDstBuf = Marshal.AllocHGlobal((Int32)m_nConvertDstBufLen);
                if (IntPtr.Zero == m_ConvertDstBuf)
                {
                    Debug.WriteLine("Malloc Memory Fail!");
                    return HKCameraCtrl.MV_E_RESOURCE;
                }
            }
            else
            {
                m_bitmapPixelFormat = PixelFormat.Format24bppRgb;
                if (IntPtr.Zero != m_ConvertDstBuf)
                {
                    Marshal.FreeHGlobal(m_ConvertDstBuf);
                    m_ConvertDstBuf = IntPtr.Zero;
                }

                // RGB为三通道
                m_nConvertDstBufLen = (UInt32)(3 * stWidth.nCurValue * stHeight.nCurValue);
                m_ConvertDstBuf = Marshal.AllocHGlobal((Int32)m_nConvertDstBufLen);
                if (IntPtr.Zero == m_ConvertDstBuf)
                {
                    Debug.WriteLine("Malloc Memory Fail!");
                    return HKCameraCtrl.MV_E_RESOURCE;
                }
            }

            // 确保释放保存了旧图像数据的bitmap实例，用新图像宽高等信息new一个新的bitmap实例
            if (null != m_bitmap)
            {
                m_bitmap.Dispose();
                m_bitmap = null;
            }
            m_bitmap = new Bitmap((Int32)stWidth.nCurValue, (Int32)stHeight.nCurValue, m_bitmapPixelFormat);

            // ch:Mono8格式，设置为标准调色板 | en:Set Standard Palette in Mono8 Format
            if (PixelFormat.Format8bppIndexed == m_bitmapPixelFormat)
            {
                ColorPalette palette = m_bitmap.Palette;
                for (int i = 0; i < palette.Entries.Length; i++)
                {
                    palette.Entries[i] = Color.FromArgb(i, i, i);
                }
                m_bitmap.Palette = palette;
            }

            return HKCameraCtrl.MV_OK;
        }

        private bool IsColorData(HKCameraCtrl.MvGvspPixelType enGvspPixelType)
        {
            switch (enGvspPixelType)
            {
                case HKCameraCtrl.MvGvspPixelType.PixelType_Gvsp_BayerGR8:
                case HKCameraCtrl.MvGvspPixelType.PixelType_Gvsp_BayerRG8:
                case HKCameraCtrl.MvGvspPixelType.PixelType_Gvsp_BayerGB8:
                case HKCameraCtrl.MvGvspPixelType.PixelType_Gvsp_BayerBG8:
                case HKCameraCtrl.MvGvspPixelType.PixelType_Gvsp_BayerGR10:
                case HKCameraCtrl.MvGvspPixelType.PixelType_Gvsp_BayerRG10:
                case HKCameraCtrl.MvGvspPixelType.PixelType_Gvsp_BayerGB10:
                case HKCameraCtrl.MvGvspPixelType.PixelType_Gvsp_BayerBG10:
                case HKCameraCtrl.MvGvspPixelType.PixelType_Gvsp_BayerGR12:
                case HKCameraCtrl.MvGvspPixelType.PixelType_Gvsp_BayerRG12:
                case HKCameraCtrl.MvGvspPixelType.PixelType_Gvsp_BayerGB12:
                case HKCameraCtrl.MvGvspPixelType.PixelType_Gvsp_BayerBG12:
                case HKCameraCtrl.MvGvspPixelType.PixelType_Gvsp_BayerGR10_Packed:
                case HKCameraCtrl.MvGvspPixelType.PixelType_Gvsp_BayerRG10_Packed:
                case HKCameraCtrl.MvGvspPixelType.PixelType_Gvsp_BayerGB10_Packed:
                case HKCameraCtrl.MvGvspPixelType.PixelType_Gvsp_BayerBG10_Packed:
                case HKCameraCtrl.MvGvspPixelType.PixelType_Gvsp_BayerGR12_Packed:
                case HKCameraCtrl.MvGvspPixelType.PixelType_Gvsp_BayerRG12_Packed:
                case HKCameraCtrl.MvGvspPixelType.PixelType_Gvsp_BayerGB12_Packed:
                case HKCameraCtrl.MvGvspPixelType.PixelType_Gvsp_BayerBG12_Packed:
                case HKCameraCtrl.MvGvspPixelType.PixelType_Gvsp_RGB8_Packed:
                case HKCameraCtrl.MvGvspPixelType.PixelType_Gvsp_YUV422_Packed:
                case HKCameraCtrl.MvGvspPixelType.PixelType_Gvsp_YUV422_YUYV_Packed:
                case HKCameraCtrl.MvGvspPixelType.PixelType_Gvsp_YCBCR411_8_CBYYCRYY:
                    return true;

                default:
                    return false;
            }
        }

        private bool IsMonoData(HKCameraCtrl.MvGvspPixelType enGvspPixelType)
        {
            switch (enGvspPixelType)
            {
                case HKCameraCtrl.MvGvspPixelType.PixelType_Gvsp_Mono1p:
                case HKCameraCtrl.MvGvspPixelType.PixelType_Gvsp_Mono2p:
                case HKCameraCtrl.MvGvspPixelType.PixelType_Gvsp_Mono4p:
                case HKCameraCtrl.MvGvspPixelType.PixelType_Gvsp_Mono8:
                case HKCameraCtrl.MvGvspPixelType.PixelType_Gvsp_Mono8_Signed:
                case HKCameraCtrl.MvGvspPixelType.PixelType_Gvsp_Mono10:
                case HKCameraCtrl.MvGvspPixelType.PixelType_Gvsp_Mono10_Packed:
                case HKCameraCtrl.MvGvspPixelType.PixelType_Gvsp_Mono12:
                case HKCameraCtrl.MvGvspPixelType.PixelType_Gvsp_Mono12_Packed:
                case HKCameraCtrl.MvGvspPixelType.PixelType_Gvsp_Mono14:
                case HKCameraCtrl.MvGvspPixelType.PixelType_Gvsp_Mono16:
                    return true;

                default:
                    return false;
            }
        }

        private void ImageCallback(IntPtr pdata, ref HKCameraCtrl.MV_FRAME_OUT_INFO_EX pframeinfo, IntPtr puser)
        {
            var bitMap = ParseRawImageDatacallback(pdata, pframeinfo);
            if (bitMap == null) return;

            ActionGetImage?.Invoke(bitMap.Clone() as Bitmap);
        }

        #endregion helper
    }
}