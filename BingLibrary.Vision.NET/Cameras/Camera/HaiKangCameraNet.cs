using BingLibrary.Vision.Cameras; 
using HalconDotNet;
using MvCameraControl;
using MySqlX.XDevAPI.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Navigation;

namespace BingLibrary.Vision.NET.Cameras.Camera
{
    public class HaiKangCameraNet<T> : BaseCamera<T>
    {
        public HaiKangCameraNet() : base()
        {
            SDKSystem.Initialize();
        }

        #region param
        readonly DeviceTLayerType enumTLayerType = DeviceTLayerType.MvGigEDevice | DeviceTLayerType.MvUsbDevice
            | DeviceTLayerType.MvGenTLGigEDevice | DeviceTLayerType.MvGenTLCXPDevice | DeviceTLayerType.MvGenTLCameraLinkDevice | DeviceTLayerType.MvGenTLXoFDevice;
         
        IDevice _myCamera = null;

        private static Object BufForDriverLock = new Object();
        private Bitmap m_bitmap = null;
        #endregion

        #region operate

        public override List<CameraInfo> GetListEnum(string manufacturerNameFilter = "")
        {

            GC.Collect();
            List<CameraInfo> cameraInfos = new List<CameraInfo>();
            List<IDeviceInfo> deviceInfoList = GetListInfoEnum(); 
             
            for (int i = 0; i < deviceInfoList.Count; i++)
            {
                IDeviceInfo deviceInfo = deviceInfoList[i];


                if (deviceInfo.TLayerType == DeviceTLayerType.MvGigEDevice)
                {
                    //厂商名字不等时加入
                    if (manufacturerNameFilter != deviceInfo.ManufacturerName)
                    {
                        cameraInfos.Add(new CameraInfo()
                        {
                            CameraName = deviceInfo.UserDefinedName,
                            ManufacturerName = deviceInfo.ManufacturerName,
                            CameraSN = deviceInfo.SerialNumber,
                            CameraBrand = CameraBrand.HaiKang,
                            CameraType = CameraType.Gige,
                        });
                    }
                }
                else if (deviceInfo.TLayerType == DeviceTLayerType.MvUsbDevice)
                {
                    //厂商名字不等时加入
                    if (manufacturerNameFilter != deviceInfo.ManufacturerName)
                    {
                        cameraInfos.Add(new CameraInfo()
                        {
                            CameraName = deviceInfo.UserDefinedName,
                            ManufacturerName = deviceInfo.ManufacturerName,
                            CameraSN = deviceInfo.SerialNumber,
                            CameraBrand = CameraBrand.HaiKang,
                            CameraType = CameraType.USB,
                        });
                    }
                }
               

            }

             
            return cameraInfos;
        }

        public override bool InitDevice(CameraInfo cameraInfo)
        {
            Info = cameraInfo; 
            var infolist = GetListInfoEnum();
            IDeviceInfo camerainfo = null;
            if (infolist.Count < 1) return false;

            bool selectSNflag = false;

            if (!string.IsNullOrEmpty(cameraInfo.CameraName))
            {
                foreach (var item in infolist)
                {
                    if (item.TLayerType == DeviceTLayerType.MvGigEDevice && cameraInfo.CameraType == CameraType.Gige)
                    { 
                             if (item.UserDefinedName.Equals(cameraInfo.CameraName))
                        {
                            camerainfo = item;
                            selectSNflag = true;
                            break;
                        }
                    }
                    if (item.TLayerType == DeviceTLayerType.MvUsbDevice && cameraInfo.CameraType == CameraType.USB)
                    {
                         if (item.UserDefinedName.Equals(cameraInfo.CameraName))
                        {
                            camerainfo = item;
                            selectSNflag = true;
                            break;
                        }
                    }
                }
            }
            else if (!string.IsNullOrEmpty(cameraInfo.CameraSN))
            {
                foreach (var item in infolist)
                {
                    if (item.TLayerType == DeviceTLayerType.MvGigEDevice && cameraInfo.CameraType == CameraType.Gige)
                    {
                         if (item.SerialNumber.Equals(cameraInfo.CameraSN))
                        {
                            camerainfo = item;
                            selectSNflag = true;
                            break;
                        }
                    }
                    if (item.TLayerType == DeviceTLayerType.MvUsbDevice && cameraInfo.CameraType == CameraType.USB)
                    {
                          if (item.SerialNumber.Equals(cameraInfo.CameraName))
                        {
                            camerainfo = item;
                            selectSNflag = true;
                            break;
                        }
                    }
                }
            }
            if (!selectSNflag) return false;



            // ch:打开设备 | en:Open device
            if (null == _myCamera)
            {
                try
                {
                    _myCamera = DeviceFactory.CreateDevice(camerainfo);
                }
                catch {
                    return false;
                }
                
            }

            int result = _myCamera.Open(); 
            if (result != MvError.MV_OK)
            { 
                return false;
            }

            //ch: 判断是否为gige设备 | en: Determine whether it is a GigE device
            if (_myCamera is IGigEDevice)
            {
                //ch: 转换为gigE设备 | en: Convert to Gige device
                IGigEDevice gigEDevice = _myCamera as IGigEDevice;

                // ch:探测网络最佳包大小(只对GigE相机有效) | en:Detection network optimal package size(It only works for the GigE camera)
                int optionPacketSize;
                result = gigEDevice.GetOptimalPacketSize(out optionPacketSize); 
            }

            //设置缓存节点数量
            _myCamera.StreamGrabber.SetImageNodeNum(5);

            // ch:注册回调函数 | en:Register image callback
            _myCamera.StreamGrabber.FrameGrabedEvent += ImageCallback;

 
            return true;
        }

        public override void CloseDevice()
        {
            StopGrabbing();
            if (_myCamera != null)
            { 
                _myCamera.Close();
                _myCamera.Dispose();
            }
        }

        public override bool SoftTrigger(T tData)
        {
            AddTriggerData(tData);
           return _myCamera.Parameters.SetCommandValue("TriggerSoftware")== MvError.MV_OK;
            
        }
        #endregion

        #region SettingConfig
        public override bool LoadCamConfig(string filePath)
        {
            try
            {
               int   nRet = _myCamera.Parameters.FeatureLoad(filePath); 
                if (MvError.MV_OK == nRet)
                    return true;
                else
                    return false;
            }
            catch { return false; }
        }
        protected override bool StartGrabbing() => _myCamera.StreamGrabber.StartGrabbing() == MvError.MV_OK;

        public override bool StopGrabbing() => _myCamera.StreamGrabber.StopGrabbing() == MvError.MV_OK;


        public override bool SetTriggerMode(TriggerMode mode, TriggerSource triggerEnum = TriggerSource.Line0)
        {
            int rec;
            switch (mode)
            {
                case TriggerMode.Off:
                    rec = _myCamera.Parameters.SetEnumValueByString("TriggerMode", "Off");
                    break;

                case TriggerMode.On:
                    rec = _myCamera.Parameters.SetEnumValueByString("TriggerMode", "On");
                    break;

                default:
                    rec = _myCamera.Parameters.SetEnumValueByString("TriggerMode", "On");
                    break;
            }
            bool flag1 = MvError.MV_OK == rec;
            switch (triggerEnum)
            {
                case TriggerSource.Software:
                    rec = _myCamera.Parameters.SetEnumValueByString("TriggerSource", "Software"); 
                    break;

                case TriggerSource.Line0:
                    rec = _myCamera.Parameters.SetEnumValueByString("TriggerSource", "Line0");
                    break;

                case TriggerSource.Line1:
                    rec = _myCamera.Parameters.SetEnumValueByString("TriggerSource", "Line1");
                    break;

                case TriggerSource.Line2:
                    rec = _myCamera.Parameters.SetEnumValueByString("TriggerSource", "Line2");
                    break;

                case TriggerSource.Line3:
                    rec = _myCamera.Parameters.SetEnumValueByString("TriggerSource", "Line3");
                    break;

                default:
                    rec = _myCamera.Parameters.SetEnumValueByString("TriggerSource", "Counter");
                    break;
            }
            bool flag2 = MvError.MV_OK == rec;
            return flag1 && flag2;
        }

        public override bool GetTriggerMode(out TriggerMode mode, out TriggerSource hardTriggerModel)
        {
            mode = TriggerMode.On;
            hardTriggerModel = TriggerSource.Line0;
          
            IEnumValue enumValue;
            int nRet = _myCamera.Parameters.GetEnumValue("TriggerMode", out enumValue);
            if (nRet == MvError.MV_OK)
            {
                if (enumValue.CurEnumEntry.Symbolic == "On")
                {
                    mode = TriggerMode.On;
                   
                }
                else
                {
                    mode = TriggerMode.Off; 
                }

                nRet = _myCamera.Parameters.GetEnumValue("TriggerSource", out enumValue);
                if (nRet == MvError.MV_OK)
                {
                    if (enumValue.CurEnumEntry.Symbolic == "TriggerSoftware")
                    {
                        hardTriggerModel = TriggerSource.Software;
                    }
                    else if (enumValue.CurEnumEntry.Symbolic == "Line0")
                    {
                        hardTriggerModel = TriggerSource.Line0;
                    }
                    else if (enumValue.CurEnumEntry.Symbolic == "Line1")
                    {
                        hardTriggerModel = TriggerSource.Line1;
                    }
                    else if (enumValue.CurEnumEntry.Symbolic == "Line2")
                    {
                        hardTriggerModel = TriggerSource.Line2;
                    }
                    else if (enumValue.CurEnumEntry.Symbolic == "Line3")
                    {
                        hardTriggerModel = TriggerSource.Line3;
                    }

                    return true;

                }
                else
                {
                    return false;
                }
                 

            }
            else
            { 
                return false;
            }
          
        }

        public override bool SetExpouseTime(ulong value) => _myCamera.Parameters.SetFloatValue("ExposureTime", (float)value)== MvError.MV_OK; 

        public override bool GetExpouseTime(out ulong value)
        {
            IFloatValue floatValue;
            int nRet = _myCamera.Parameters.GetFloatValue("ExposureTime", out floatValue);
            if (nRet == MvError.MV_OK)
            {
                value = (ulong)floatValue.CurValue;
            }
            else
                value = 0;  
            return MvError.MV_OK == nRet;
        }

        public override bool GetFrameRate(out float value)
        {
            IFloatValue floatValue;
            int nRet = _myCamera.Parameters.GetFloatValue("ResultingFrameRate", out floatValue);
            if (nRet == MvError.MV_OK)
            {
                value = floatValue.CurValue;
            }
            else
                value = 0;
            return MvError.MV_OK == nRet;
        }

        //1下降沿 0 上升沿
        public override bool SetTriggerPolarity(TriggerPolarity polarity)
            =>false;

        public override bool GetTriggerPolarity(out TriggerPolarity polarity)
        {
            polarity =  TriggerPolarity.FallingEdge;
            return false;
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

        public override bool SetGain(float gain) => _myCamera.Parameters.SetFloatValue("Gain", gain)== MvError.MV_OK;

        public override bool GetGain(out float gain)
        {
            IFloatValue floatValue;
            int nRet = _myCamera.Parameters.GetFloatValue("Gain", out floatValue);
            if (nRet == MvError.MV_OK)
            {
                gain = floatValue.CurValue;
            }
            else
                gain = 0;
            return MvError.MV_OK == nRet;
        }

        public override bool SetLineMode(IOLines line, LineMode mode)
            => false;

        public override bool SetLineStatus(IOLines line, LineStatus linestatus)
              => false;

        public override bool GetLineStatus(IOLines line, out LineStatus linestatus)
        {
            linestatus= LineStatus.Low;
            return false;
        }

        public override bool AutoBalanceWhite() => false;
        #endregion

        #region helper
        private List<IDeviceInfo> GetListInfoEnum()
        {
            List<IDeviceInfo> deviceInfoList = new List<IDeviceInfo>();
            int nRet = DeviceEnumerator.EnumDevices(enumTLayerType, out deviceInfoList);
            if (nRet != MvError.MV_OK)
            {
                return new List<IDeviceInfo>();
            }
            return deviceInfoList;  
           
        }

        private Bitmap ParseRawImageDatacallback(IFrameOut frameOut)
        {
            lock (BufForDriverLock)
            {
                int width = (int)frameOut.Image.Width;
                int height = (int)frameOut.Image.Height;
                int stride = width; // 每行字节数等于宽度 // 假设像素格式是 GrayScale (Mono8) 
                if (frameOut.Image.PixelType == MvGvspPixelType.PixelType_Gvsp_RGB8_Packed)
                {
                    stride = width * 3; // RGB 图像，每像素 3 字节
                }
                else if (frameOut.Image.PixelType == MvGvspPixelType.PixelType_Gvsp_Mono8)
                {
                    stride = width; // 灰度图，每像素 1 字节
                }
                 
                if (frameOut.Image.PixelType == MvGvspPixelType.PixelType_Gvsp_RGB8_Packed)
                {
                    m_bitmap = GetBmp(width, height, stride, frameOut.Image.PixelData, MvGvspPixelType.PixelType_Gvsp_RGB8_Packed); //转 bmp 
                }
                else if (frameOut.Image.PixelType == MvGvspPixelType.PixelType_Gvsp_Mono8)
                {
                    IntPtr pixelData = frameOut.Image.PixelDataPtr; // 托管指针
                    int imageSize = (int)frameOut.Image.ImageSize; 
                    // 将托管指针转换为字节数组
                    byte[] imageData = new byte[imageSize];
                    Marshal.Copy(pixelData, imageData, 0, imageSize);

                    m_bitmap = GetBmp(width, height, stride, imageData, MvGvspPixelType.PixelType_Gvsp_Mono8); //转bmp 


                }

            }
            return m_bitmap;
        }


        private void ImageCallback(object sender, FrameGrabbedEventArgs e)
        {
             var bitMap = ParseRawImageDatacallback(e.FrameOut);
            if (bitMap == null) return;

            ActionGetImage?.Invoke(bitMap.Clone() as Bitmap);
        }

        public   Bitmap GetBmp(int width, int height, int stride, byte[] imageData, MvGvspPixelType pixelType)
        {
            // 创建 Bitmap 对象
            Bitmap bitmap = null;

            if (pixelType == MvGvspPixelType.PixelType_Gvsp_Mono8)
            {
                // 灰度图
                bitmap = new Bitmap(width, height, PixelFormat.Format8bppIndexed);
            }
            else
            {
                // RGB 图
                bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            }

            // 锁定 Bitmap 数据
            Rectangle rect = new Rectangle(0, 0, width, height);
            BitmapData bitmapData = bitmap.LockBits(rect, ImageLockMode.WriteOnly, bitmap.PixelFormat);

            // 处理图像数据
            if (pixelType == MvGvspPixelType.PixelType_Gvsp_Mono8)
            {
                // 设置调色板
                ColorPalette palette = bitmap.Palette;
                for (int i = 0; i < 256; i++)
                {
                    palette.Entries[i] = Color.FromArgb(i, i, i);
                }
                bitmap.Palette = palette;

                // 复制灰度图数据
                Marshal.Copy(imageData, 0, bitmapData.Scan0, imageData.Length);
            }
            else
            {
                // 复制 RGB 数据
                Marshal.Copy(imageData, 0, bitmapData.Scan0, imageData.Length);
            }

            // 解锁 Bitmap 数据
            bitmap.UnlockBits(bitmapData);

            return bitmap;
        }




        #endregion


    }
}
