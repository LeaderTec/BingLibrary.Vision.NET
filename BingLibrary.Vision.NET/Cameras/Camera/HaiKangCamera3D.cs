using BingLibrary.Extension;
using BingLibrary.Vision.Cameras.CameraSDK.HaiKang;
using BingLibrary.Vision.NET.Cameras.Camera;
using HalconDotNet;
using MVSDK_Net;
using Org.BouncyCastle.Asn1.Tsp;
using System;
using System.Collections.Generic;
using System.Collections.Generic; 
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text; 
using System.Threading.Tasks;

using STC_DataSet = System.IntPtr;
namespace BingLibrary.Vision.Cameras
{
    public class HaiKangCamera3D<T> : BaseCamera<T>
    {
        public HaiKangCamera3D() : base()
        {
            pImageDataCallBack=new ImageDataCallBackHandle<T>(this);
        }
      
        #region param

        UInt32 m_nDevNum = 0; 
        MV3D_LP_DEVICE_INFO_VECTOR m_stVector= null;
        private STC_DataSet m_DevHandle = IntPtr.Zero;
        private Mv3dLpImageMode m_nImgMode = Mv3dLpImageMode.MV3D_LP_Range_Image; 
        private ImageDataCallBackHandle<T> pImageDataCallBack ;
        #endregion param


        #region operate
        public async Task<(HImage, HImage)> Grabe3DImages(int timeout = 25000, int internalTime = 20)
        {
            try
            { 
                int c = timeout / internalTime;
                for (int i = 0; i < c; i++)
                {
                    await internalTime;
                    if (pImageDataCallBack.m_hImageLoaded) break;
                }
                 
                StopGrabbing();
                return (pImageDataCallBack.realImageFinal, pImageDataCallBack.lightImageFinal);//这里考虑copyimage
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                StopGrabbing();
                return (new HImage(), new HImage());
            }
        }


        public override List<CameraInfo> GetListEnum()
        {
            GC.Collect();
            List<CameraInfo> cameraInfos = new List<CameraInfo>();
 
              m_nDevNum = 0; 
            int nRet = Mv3dLpSDK.MV3D_LP_GetDeviceNumber(ref m_nDevNum);
            m_stVector = new MV3D_LP_DEVICE_INFO_VECTOR((int)m_nDevNum);
            if (m_nDevNum == 0)
            {
                return cameraInfos;
            }

            for (UInt32 i = 0; i < m_nDevNum; i++)
            {
                m_stVector.Add(new MV3D_LP_DEVICE_INFO());
            }
            // 获取网络中设备信息 | Get Devices Infomation
            nRet = Mv3dLpSDK.MV3D_LP_GetDeviceList(m_stVector[0], m_nDevNum, ref m_nDevNum);
            if ((int)Mv3dLpSDK.MV3D_LP_OK != nRet)
            {

                return cameraInfos;
            }

            // 在窗体列表中显示设备名 | Display device name in the form list
            for (int i = 0; i < m_nDevNum; i++)
            {
                string strSerialNumber = m_stVector[i].chSerialNumber;
                strSerialNumber = strSerialNumber.TrimEnd('\0');
                string strModelName = m_stVector[i].chModelName;
                strModelName = strModelName.TrimEnd('\0');
                string strCurrentIp = m_stVector[i].chCurrentIp;
                strCurrentIp = strCurrentIp.TrimEnd('\0'); 

                cameraInfos.Add(new CameraInfo()
                {
                    CameraName = strModelName,
                    CameraSN = strSerialNumber,
                    CameraBrand = CameraBrand.HaiKang3D,
                    CameraType = CameraType.Gige,
                    CameraIP = strCurrentIp

                });
            }

          

            return cameraInfos;
        }

        public override bool InitDevice(CameraInfo cameraInfo)
        {
            try {
                // 打开设备 | Open device
                int nRet = Mv3dLpSDK.MV3D_LP_OpenDeviceBySN(ref m_DevHandle,cameraInfo.CameraSN);
                if ((int)Mv3dLpSDK.MV3D_LP_OK != nRet)
                {
                  
                    return false;
                }

                //设置深度图模式 
                MV3D_LP_PARAM devParam = new MV3D_LP_PARAM();
                MV3D_LP_ENUMPARAM enumParam = new MV3D_LP_ENUMPARAM();
                devParam.set_enumparam(enumParam);
                nRet = Mv3dLpSDK.MV3D_LP_GetParam(m_DevHandle, "ImageMode", devParam);
                if ((int)Mv3dLpSDK.MV3D_LP_OK != nRet)
                { 
                    return false;
                }
                enumParam = devParam.get_enumparam();
                uint nImgMode = (uint)enumParam.nCurValue;
                m_nImgMode = Mv3dLpImageMode.MV3D_LP_Range_Image;
                pImageDataCallBack.Register(m_DevHandle); //注册了一个回调函数
                 

                return true;
            } catch { return false; }

           
        }

        public override void CloseDevice()
        {
            StopGrabbing();

            try
            {
                Mv3dLpSDK.MV3D_LP_StopMeasure(m_DevHandle);
                Mv3dLpSDK.MV3D_LP_CloseDevice(ref m_DevHandle);
            }
            catch { }
         
        }

        public override bool SoftTrigger(T tData)
        {
            AddTriggerData(tData); 
            int nRet = (int)Mv3dLpSDK.MV3D_LP_OK;
            nRet = Mv3dLpSDK.MV3D_LP_SoftTrigger(m_DevHandle);  
            return nRet == Mv3dLpSDK.MV3D_LP_OK;
        }

        #endregion operate
        #region SettingConfig

        public override bool SetTriggerMode(TriggerMode mode, TriggerSource triggerEnum = TriggerSource.Line0)
        {
            
            return true;
        }

        public override bool GetTriggerMode(out TriggerMode mode, out TriggerSource hardTriggerModel)
        {
            mode = TriggerMode.Off;hardTriggerModel = TriggerSource.Line0;
            return false;
        }

        public override bool SetExpouseTime(ulong value) 
        {
            return false;
        }
        public override bool GetExpouseTime(out ulong value)
        {
            value = 0; 
            return false;    
        }

        public override bool GetFrameRate(out float value)
        {
            value = 0; 
            return false;
        }

        //1下降沿 0 上升沿
        public override bool SetTriggerPolarity(TriggerPolarity polarity) { return false; }
        public override bool GetTriggerPolarity(out TriggerPolarity polarity)
        {
            polarity = TriggerPolarity.RisingEdge;
            return false;
        }

        public override bool SetTriggerFliter(ushort flitertime) { return false; }
        public override bool GetTriggerFliter(out ushort flitertime)
        {
            flitertime = 0;
            return false;
        }

        public override bool SetTriggerDelay(ushort delay) { return false; }
        public override bool GetTriggerDelay(out ushort delay)
        {
            delay = 0;
           return false;
        }

        public override bool SetGain(float gain) {
            return false;
        }
        public override bool GetGain(out float gain)
        {
          gain = 0;
            return false;
        }

        public override bool SetLineMode(IOLines line, LineMode mode) {
            return false;
        }
        public override bool SetLineStatus(IOLines line, LineStatus linestatus)
        { return false; }
        public override bool GetLineStatus(IOLines line, out LineStatus linestatus)
        {
            linestatus = LineStatus.Low;
            return false;
        }

        public override bool AutoBalanceWhite() { 
         return false;
        }
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
            try
            {
                pImageDataCallBack.m_hImageLoaded = false;
              int  nRet = Mv3dLpSDK.MV3D_LP_StartMeasure(m_DevHandle);

                if ((int)Mv3dLpSDK.MV3D_LP_OK != nRet)
                { 
                    return false;
                }
                return true;
            }
            catch { return false; }
        }
        public override bool StopGrabbing() 
        {
            try
            { 
                int nRet = Mv3dLpSDK.MV3D_LP_StopMeasure(m_DevHandle);

                if ((int)Mv3dLpSDK.MV3D_LP_OK != nRet)
                 
                {
                    return false;
                }
                return true;
            }
            catch { return false; }
          

        }
      
        
        public override bool LoadCamConfig(string filePath)
        {
            return false;
        }

        #endregion helper
    }



    public static class LockObjects
    {
        public static readonly object Grab3DLockObject = new object();
    }


    public class ImageDataCallBackHandle<T> : ImageDataCallBack
    {
        public delegate void cbOutputExdelegate(MV3D_LP_IMAGE_DATA pData, ref MV3D_LP_IMAGE_DATA pstImageData, IntPtr pUser);

        private static readonly object Lock = new object();
        private static UInt32 m_MaxImageSize = 1024 * 1024 * 30;
        private static byte[] m_pcDataBuf = new byte[m_MaxImageSize];
        private static byte[] m_pcDataBufLight = new byte[m_MaxImageSize];
        public HImage realImageOriginal = new HImage();
        public HImage realImageFinal = new HImage(); 
        public HImage lightImageOriginal = new HImage();
        public HImage lightImageFinal = new HImage();
        public bool m_hImageLoaded = false;
        private HaiKangCamera3D<T> _haiKangCamera3D;

        public ImageDataCallBackHandle(HaiKangCamera3D<T> haiKangCamera3D)
        {
            _haiKangCamera3D= haiKangCamera3D;
        }

        public override void run(MV3D_LP_IMAGE_DATA pstImageData)
        {
            lock (LockObjects.Grab3DLockObject)
            {
                try
                {
                    realImageOriginal?.Dispose();
                    realImageFinal?.Dispose();
                    lightImageOriginal?.Dispose();
                    lightImageFinal?.Dispose();

                    m_hImageLoaded = false;
                    Monitor.Enter(Lock);
                    var _nWidth = (int)pstImageData.nWidth;
                    var _nHeight = (int)pstImageData.nHeight;
                    var _nDataLen = pstImageData.nDataLen;
                    var _nFrameNum = pstImageData.nFrameNum;



                    if (m_MaxImageSize < (int)pstImageData.nDataLen)
                    {
                        m_pcDataBuf = new byte[pstImageData.nDataLen];
                        m_pcDataBufLight = new byte[pstImageData.nDataLen];
                        m_MaxImageSize = pstImageData.nDataLen;

                    }

                    var Rece = pstImageData;
                   

                    Parallel.Invoke(
                        () => {

                            Marshal.Copy(pstImageData.pData, m_pcDataBuf, 0, (int)pstImageData.nDataLen);  //获取深度图
                        },
                         () => {
                             System.Threading.Thread.Sleep(30);
                             Marshal.Copy(Rece.pIntensityData, m_pcDataBufLight, 0, (int)Rece.nIntensityDataLen); //获取亮度图
                         }
                        );

                    Monitor.Exit(Lock);

                    float[] buff1 = new float[(int)pstImageData.nDataLen / 2];
                    //  double[] buffLight = new double[(int)pstImageData.nIntensityDataLen];
                    ////深度图
                    //for (int i = 0; i < buff1.Length; i++)
                    //{
                    //    //3D相机的高度的范围为-2500到+2500  --这句话加2500就是表示高度范围变成0到5000
                    //    int value = (Convert.ToInt16($"{m_pcDataBuf[i * 2 + 1]:X2}{m_pcDataBuf[i * 2]:X2}", 16) + 2500);
                    //    if (value < 0) value = 0;
                    //    buff1[i] = value * 0.01;    //10倍软件上固定的，/1000是换算成毫米
                    //}

                    //深度图
                    for (int i = 0; i < buff1.Length; i++)
                    {
                        short rawValue = (short)((m_pcDataBuf[i * 2 + 1] << 8) | m_pcDataBuf[i * 2]);
                        int value = rawValue + 2500 * 2;
                        if (value < 0) value = 0;
                        buff1[i] = value * 0.01F * 0.58F;    //10倍软件上固定的，/1000是换算成毫米
                    }
                     
                    IntPtr pData = Marshal.UnsafeAddrOfPinnedArrayElement(buff1, 0);
                    realImageOriginal = new HImage();
                    realImageOriginal.GenImage1("real", _nWidth, _nHeight, pData); 
                    realImageFinal = realImageOriginal.RotateImage(90.0, "constant");
                   
                    //亮度图
                    GCHandle hBuf = GCHandle.Alloc(m_pcDataBufLight, GCHandleType.Pinned);
                    IntPtr ptr = hBuf.AddrOfPinnedObject();
                    lightImageOriginal = new HImage();
                    lightImageOriginal.GenImage1("byte",(int) Rece.nWidth, (int)Rece.nHeight, ptr);
                    lightImageFinal= lightImageOriginal.RotateImage(90.0, "constant");


                    _haiKangCamera3D.ActionGet3DImages?.Invoke(new HImage( realImageFinal),new HImage( lightImageFinal)); //回调函数，传递图像数据
                    //?.Invoke(bitMap.Clone() as Bitmap);

                    m_hImageLoaded = true;
                }
                catch (Exception ex)
                {
                    
                }
            }

        }


         
    }

    internal enum Mv3dLpImageMode
    {
        MV3D_LP_Origin_Image = 1,
        MV3D_LP_Point_Cloud_Image = 4,
        MV3D_LP_Range_Image = 7,
        MV3D_LP_Intensity_Image = 10,
    };
}
