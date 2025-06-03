using Basler.Pylon;
using BingLibrary.Vision.Cameras.CameraSDK.DaHeng;
using BingLibrary.Vision.Cameras.CameraSDK.HaiKang;
using GxIAPINET;
using System.Diagnostics;

namespace BingLibrary.Vision.Cameras
{
    internal class DaHengCamera<T> : BaseCamera<T>
    {
        public DaHengCamera() : base()
        {
        }

        #region param

        private bool m_bIsOpen = false;                                     ///< ����򿪱�ʶ
        private bool m_bIsSnap = false;                                     ///< �����ʼ�ɼ���ʶ
        private bool m_bColorFilter = false;                                ///< ��ʶ�Ƿ�֧��Bayer��ʽ
        private bool m_bAwbLampHouse = false;                               ///< ��ʾ�Ƿ�֧�ֹ�Դѡ��
        private bool m_bWhiteAutoSelectedIndex = true;                      ///<��ƽ���б���ת����־
        private IGXFactory m_objIGXFactory = null;                          ///<Factory����
        private IGXDevice m_objIGXDevice = null;                            ///<�豸����
        private IGXStream m_objIGXStream = null;                            ///<������
        private IGXFeatureControl m_objIGXFeatureControl = null;            ///<Զ���豸���Կ���������
        private IGXFeatureControl m_objIGXStreamFeatureControl = null;      ///<�������Կ���������
        private IImageProcessConfig m_objCfg = null;                        ///<ͼ�����ò�������
        private GxBitmap m_objGxBitmap = null;                              ///<ͼ����ʾ�����
        private string m_strPixelColorFilter = null;                        ///<Bayer��ʽ
        private string m_strBalanceWhiteAutoValue = "Off";                  ///<�Զ���ƽ�⵱ǰ��ֵ
        private bool m_bEnableColorCorrect = false;                         ///<��ɫУ��ʹ�ܱ�־λ
        private bool m_bEnableGamma = false;                                ///<Gammaʹ�ܱ�־λ
        private bool m_bEnableSharpness = false;                            ///<��ʹ�ܱ�־λ
        private bool m_bEnableAutoWhite = false;                            ///<�Զ���ƽ��ʹ�ܱ�־λ
        private bool m_bEnableAwbLight = false;                             ///<�Զ���ƽ���Դʹ�ܱ�־λ
        private bool m_bEnableDenoise = false;                              ///<ͼ����ʹ�ܱ�־λ
        private bool m_bEnableSaturation = false;                           ///<���Ͷ�ʹ�ܱ�־λ
        private bool m_bEnumDevices = false;                                ///<�Ƿ�ö�ٵ��豸��־λ
        private List<IGXDeviceInfo> m_listGXDeviceInfo;                     ///<���ö�ٵ����豸������
        public IGXDeviceInfo GXDeviceInfo;
        private List<IGXDeviceInfo> listCameraInfo = new List<IGXDeviceInfo>();

        #endregion param

        #region Operate

        public override List<CameraInfo> GetListEnum()
        {
            //��ȡ����б�
            m_objIGXFactory = IGXFactory.GetInstance();
            m_objIGXFactory.Init();
            listCameraInfo.Clear();
            m_objIGXFactory.UpdateDeviceList(200, listCameraInfo);
            List<CameraInfo> cameraInfos = new List<CameraInfo>();
            if (listCameraInfo.Count < 1) return cameraInfos;

            foreach (var item in listCameraInfo)
            {
                cameraInfos.Add(new CameraInfo()
                {
                    CameraName = item.GetUserID(),
                    CameraSN = item.GetSN(),
                    CameraBrand = CameraBrand.DaHeng,
                    CameraType = CameraType.Gige,
                });
            }
            return cameraInfos;
        }

        public override bool InitDevice(CameraInfo cameraInfo)
        {
            Info = cameraInfo;
            GetListEnum();
            if (listCameraInfo.Count < 1) return false;

            bool selectSNflag = false;

            if (!string.IsNullOrEmpty(cameraInfo.CameraName))
            {
                foreach (var item in listCameraInfo)
                {
                    if (item.GetUserID().Equals(cameraInfo.CameraName))
                    {
                        GXDeviceInfo = item;
                        selectSNflag = true;
                        break;
                    }
                }
            }
            else if (!string.IsNullOrEmpty(cameraInfo.CameraSN))
            {
                foreach (var item in listCameraInfo)
                {
                    if (item.GetSN().Equals(cameraInfo.CameraSN))
                    {
                        GXDeviceInfo = item;
                        selectSNflag = true;
                        break;
                    }
                }
            }

            if (!selectSNflag) return false;

            if (GXDeviceInfo == null) return false;

            _StartInit();

            return true;
        }

        public override void CloseDevice()
        {
            // ֹͣ�ɼ��ر��豸���ر���
            __CloseAll();
            base.Dispose();
        }

        public override bool SoftTrigger(T tData)
        {
            try
            {
                //ÿ�η��ʹ�������֮ǰ��ղɼ��������
                //��ֹ���ڲ�����֡����ɱ���GXGetImage�õ���ͼ�����ϴη��ʹ����õ���ͼ
                if (null != m_objIGXStream)
                {
                    m_objIGXStream.FlushQueue();
                }

                //��������������
                if (null != m_objIGXFeatureControl)
                {
                    AddTriggerData(tData);
                    m_objIGXFeatureControl.GetCommandFeature("TriggerSoftware").Execute();
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion Operate

        #region SettingConfig

        public override bool SetExpouseTime(ulong value)
        {
            try
            {
                if (m_objIGXFeatureControl == null) return false;

                m_objIGXFeatureControl.GetFloatFeature("ExposureTime").SetValue(value);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public override bool GetExpouseTime(out ulong value)
        {
            value = 0;
            try
            {
                if (m_objIGXFeatureControl == null) return false;
                value = (ushort)(null != m_objIGXFeatureControl ? m_objIGXFeatureControl.GetFloatFeature("ExposureTime").GetValue() : 0);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public override bool GetFrameRate(out float value)
        {
            value = 0;
            return true;
        }

        public override bool SetTriggerMode(TriggerMode mode, TriggerSource triggerEnum = TriggerSource.Line0)
        {
            try
            {
                if (m_objIGXFeatureControl == null) return false;

                switch (mode)
                {
                    case TriggerMode.Off:
                        //m_objIGXFeatureControl.GetEnumFeature("TriggerSelector").SetValue("FrameStart");
                        m_objIGXFeatureControl?.GetEnumFeature("TriggerMode").SetValue("Off");
                        break;

                    case TriggerMode.On:
                        //m_objIGXFeatureControl.GetEnumFeature("TriggerSelector").SetValue("FrameStart");
                        m_objIGXFeatureControl?.GetEnumFeature("TriggerMode").SetValue("On");
                        m_objIGXFeatureControl?.GetEnumFeature("TriggerSource").SetValue(triggerEnum.ToString());
                        break;

                    default:
                        //m_objIGXFeatureControl.GetEnumFeature("TriggerSelector").SetValue("FrameStart");
                        m_objIGXFeatureControl?.GetEnumFeature("TriggerMode").SetValue("On");
                        m_objIGXFeatureControl?.GetEnumFeature("TriggerSource").SetValue(TriggerSource.Line0.ToString());
                        break;
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public override bool GetTriggerMode(out TriggerMode mode, out TriggerSource hardTriggerModel)
        {
            mode = TriggerMode.On;
            hardTriggerModel = TriggerSource.Line0;
            try
            {
                if (m_objIGXFeatureControl == null) return false;
                m_objIGXFeatureControl.GetEnumFeature("TriggerSelector").SetValue("FrameStart");
                string modelstr = m_objIGXFeatureControl.GetEnumFeature("TriggerMode").GetValue();
                string hadmodestr = m_objIGXFeatureControl.GetEnumFeature("TriggerSource").GetValue();

                switch (modelstr)
                {
                    case "On":
                        mode = TriggerMode.On;
                        break;

                    case "Off":
                        mode = TriggerMode.Off;
                        break;
                }

                switch (hadmodestr)
                {
                    case "Software":
                        hardTriggerModel = TriggerSource.Software;
                        break;

                    case "Line0":
                        hardTriggerModel = TriggerSource.Line0;
                        break;

                    case "Line1":
                        hardTriggerModel = TriggerSource.Line1;
                        break;

                    case "Line2":
                        hardTriggerModel = TriggerSource.Line2;
                        break;

                    case "Line3":
                        hardTriggerModel = TriggerSource.Line3;
                        break;
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public override bool SetTriggerPolarity(TriggerPolarity polarity)
        {
            try
            {
                if (null == m_objIGXFeatureControl) return false;
                m_objIGXFeatureControl.GetEnumFeature("TriggerSelector").SetValue("FrameStart");
                m_objIGXFeatureControl.GetEnumFeature("TriggerActivation").SetValue(polarity.ToString());
                return true;
            }
            catch
            {
                return false;
            }
        }

        public override bool GetTriggerPolarity(out TriggerPolarity polarity)
        {
            polarity = TriggerPolarity.RisingEdge;
            try
            {
                if (m_objIGXFeatureControl == null) return false;
                string polaritystr = m_objIGXFeatureControl.GetEnumFeature("TriggerActivation").GetValue();
                polarity = (TriggerPolarity)Enum.Parse(typeof(TriggerPolarity), polaritystr);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public override bool SetTriggerFliter(ushort flitertime)
        {
            try
            {
                if (m_objIGXFeatureControl == null) return false;
                m_objIGXFeatureControl.GetEnumFeature("RegionSelector").SetValue("Region0");
                m_objIGXFeatureControl.GetFloatFeature("TriggerFilterFallingEdge").SetValue(flitertime);//TriggerFilterRaisingEdge
                m_objIGXFeatureControl.GetFloatFeature("TriggerFilterRaisingEdge").SetValue(flitertime);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public override bool GetTriggerFliter(out ushort flitertime)
        {
            flitertime = 0;
            try
            {
                if (m_objIGXFeatureControl == null) return false;
                m_objIGXFeatureControl.GetEnumFeature("RegionSelector").SetValue("Region0");
                flitertime = (ushort)m_objIGXFeatureControl.GetFloatFeature("TriggerFilterFallingEdge").GetValue();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public override bool SetTriggerDelay(ushort delay)
        {
            try
            {
                if (m_objIGXFeatureControl == null) return false;

                m_objIGXFeatureControl.GetEnumFeature("TriggerSelector").SetValue("FrameStart");
                m_objIGXFeatureControl.GetFloatFeature("TriggerDelay").SetValue(delay);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public override bool GetTriggerDelay(out ushort delay)
        {
            delay = 0;
            try
            {
                if (m_objIGXFeatureControl == null) return false;
                m_objIGXFeatureControl.GetEnumFeature("TriggerSelector").SetValue("FrameStart");
                delay = (ushort)m_objIGXFeatureControl.GetFloatFeature("TriggerDelay").GetValue();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public override bool SetGain(float gain)
        {
            try
            {
                if (m_objIGXFeatureControl == null) return false;

                m_objIGXFeatureControl.GetEnumFeature("GainSelector").SetValue("AnalogAll");
                m_objIGXFeatureControl.GetFloatFeature("Gain").SetValue(gain);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public override bool GetGain(out float gain)
        {
            gain = 0;
            try
            {
                if (m_objIGXFeatureControl == null) return false;

                gain = (short)m_objIGXFeatureControl.GetFloatFeature("Gain").GetValue();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public override bool SetLineMode(IOLines line, LineMode mode)
        {
            throw new NotImplementedException();
        }

        public override bool SetLineStatus(IOLines line, LineStatus linestatus)
        {
            throw new NotImplementedException();
        }

        public override bool GetLineStatus(IOLines line, out LineStatus linestatus)
        {
            throw new NotImplementedException();
        }

        public override bool AutoBalanceWhite()
        {
            try
            {
                if (m_objIGXFeatureControl == null) return false;

                m_objIGXFeatureControl.GetEnumFeature("BalanceWhiteAuto").SetValue("Once");
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        #endregion SettingConfig

        #region private

        protected override bool StartGrabbing()
        {
            try
            {
                if (null != m_objIGXStreamFeatureControl)
                {
                    //��������Buffer����ģʽΪOldestFirst
                    m_objIGXStreamFeatureControl.GetEnumFeature("StreamBufferHandlingMode").SetValue("OldestFirst");
                }

                if (null != m_objIGXStream)
                {
                    //RegisterCaptureCallback��һ�����������û��Զ�����(���ͱ���Ϊ����
                    //����)�����û������������������ί�к����н���ʹ��
                    //m_objIGXStream.RegisterCaptureCallback(null, OnFrameCallbackFun);

                    //ע��ص�

                    m_objIGXStream.RegisterCaptureCallback(this, OnFrameCallbackFun);//  Delegate_Camera += new Action<Bitmap>(DelegateCallBack);
                                                                                     //��ʼ�ɼ�֮ǰ������buff����
                                                                                     //�����ɼ���ͨ��
                    m_objIGXStream.StartGrab();
                }

                //���Ϳ�������
                if (null != m_objIGXFeatureControl)
                {
                    m_objIGXFeatureControl.GetCommandFeature("AcquisitionStart").Execute();
                }
                m_bIsSnap = true;
                //m_bIsTrigValid = true;

                // ���½���UI
                // __UpdateUI();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public override bool StopGrabbing()
        {
            try
            {
                //����ͣ������ ----------------------
                if (null != m_objIGXFeatureControl)
                {
                    m_objIGXFeatureControl.GetCommandFeature("AcquisitionStop").Execute();
                    m_objIGXFeatureControl = null;
                }

                //�رղɼ���ͨ��
                if (null != m_objIGXStream)
                {
                    m_objIGXStream.StopGrab();
                    //ע���ɼ��ص�����
                    m_objIGXStream.UnregisterCaptureCallback();

                    m_objIGXStream.Close();
                    //m_objIGXStream = null;
                    //m_objIGXStreamFeatureControl = null;
                }

                m_bIsSnap = false;

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private void _StartInit()
        {
            try
            {
                //�ر���
                __CloseStream();
                // ����豸�Ѿ�����رգ���֤����ڳ�ʼ��������������ٴδ�
                __CloseDevice();

                //���б���һ���豸
                m_objIGXDevice = m_objIGXFactory.OpenDeviceBySN(GXDeviceInfo.GetSN(), GX_ACCESS_MODE.GX_ACCESS_EXCLUSIVE);
                m_objIGXFeatureControl = m_objIGXDevice.GetRemoteFeatureControl();

                //����
                if (null != m_objIGXDevice)
                {
                    m_objIGXStream = m_objIGXDevice.OpenStream(0);
                    m_objIGXStreamFeatureControl = m_objIGXStream.GetFeatureControl();
                }

                // �����û��ڴ��������֮�󣬸��ݵ�ǰ���绷�������������ͨ������ֵ��
                // �������������Ĳɼ�����,���÷����ο����´��롣
                GX_DEVICE_CLASS_LIST objDeviceClass = m_objIGXDevice.GetDeviceInfo().GetDeviceClass();
                if (GX_DEVICE_CLASS_LIST.GX_DEVICE_CLASS_GEV == objDeviceClass)
                {
                    // �ж��豸�Ƿ�֧����ͨ�����ݰ�����
                    if (true == m_objIGXFeatureControl.IsImplemented("GevSCPSPacketSize"))
                    {
                        // ��ȡ��ǰ���绷�������Ű���ֵ
                        uint nPacketSize = m_objIGXStream.GetOptimalPacketSize();
                        // �����Ű���ֵ����Ϊ��ǰ�豸����ͨ������ֵ
                        m_objIGXFeatureControl.GetIntFeature("GevSCPSPacketSize").SetValue(nPacketSize);
                    }
                }

                if (null != m_objIGXFeatureControl)
                {
                    //���òɼ�ģʽ�����ɼ�
                    m_objIGXFeatureControl.GetEnumFeature("AcquisitionMode").SetValue("Continuous");
                    if (GXDeviceInfo.GetDeviceClass() == GX_DEVICE_CLASS_LIST.GX_DEVICE_CLASS_GEV)
                    {
                        //����������ʱʱ��Ϊ1s
                        //���ǧ���������������Debugģʽ�µ�������ʱ�������������ʱʱ���Զ�����Ϊ5min��
                        //��������Ϊ�˲��������������ʱӰ�����ĵ��Ժ͵���ִ�У�ͬʱ��Ҳ��ζ���������5min���޷��Ͽ�������ʹ����ϵ����ϵ�
                        //Ϊ�˽�������������⣬�������������ʱʱ������Ϊ1s�����������ߺ������������
                        m_objIGXFeatureControl.GetIntFeature("GevHeartbeatTimeout").SetValue(1000);
                    }
                }

                m_objCfg = m_objIGXDevice.CreateImageProcessConfig();

                m_objGxBitmap = new GxBitmap(m_objIGXDevice);
                //Utilbitmap = new Util_Bitmap(m_objIGXDevice);

                // �����豸�򿪱�ʶ
                m_bIsOpen = true;
            }
            catch (Exception e)
            { MessageBox.Show(e.ToString()); }
        }

        /// <summary>
        ///  �ɼ��¼���ί�к���
        /// </summary>
        /// <param name="objUserParam">�û�˽�в���</param>
        /// <param name="objIFrameData">ͼ����Ϣ����</param>
        private void OnFrameCallbackFun(object objUserParam, IFrameData objIFrameData)
        {
            //�û�˽�в��� obj���û���ע��ص�������ʱ�������豸�����ڻص������ڲ����Խ���
            //������ԭΪ�û�˽�в���
            //IGXDevice objIGXDevice = objUserParam as IGXDevice;
            //if (objIFrameData.GetStatus() == GX_FRAME_STATUS_LIST.GX_FRAME_STATUS_SUCCESS)  //����֡
            //{
            // m_objGxBitmap = new GxBitmap(m_objIGXDevice);
            //Bitmap bmp = m_objGxBitmap.GetBmp(objIFrameData);
            GxBitmap bitmap = new GxBitmap(m_objIGXDevice);
            Bitmap bmp = bitmap.GetBmp(objIFrameData);
            //���ݸ���
            ActionGetImage?.Invoke(bmp.Clone() as Bitmap);
            //GC.Collect();
            //}
        }

        /// <summary>
        /// �ر���
        /// </summary>
        private void __CloseStream()
        {
            try
            {
                //�ر���
                if (null != m_objIGXStream)
                {
                    m_objIGXStream.Close();
                    m_objIGXStream = null;
                    m_objIGXStreamFeatureControl = null;
                }
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// �ر��豸
        /// </summary>
        private void __CloseDevice()
        {
            try
            {
                //�ر��豸
                if (null != m_objIGXDevice)
                {
                    m_objIGXDevice.Close();
                    m_objIGXDevice = null;
                }
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// ֹͣ�ɼ��ر��豸���ر���
        /// </summary>
        private void __CloseAll()
        {
            try
            {
                // ���δͣ������ֹͣ�ɼ�
                if (m_bIsSnap)
                {
                    if (null != m_objIGXFeatureControl)
                    {
                        m_objIGXFeatureControl.GetCommandFeature("AcquisitionStop").Execute();
                        m_objIGXFeatureControl = null;
                    }
                }
            }
            catch (Exception)
            {
            }
            m_bIsSnap = false;
            try
            {
                //ֹͣ��ͨ���͹ر���
                if (null != m_objIGXStream)
                {
                    m_objIGXStream.StopGrab();
                    //ע���ɼ��ص�����
                    m_objIGXStream.UnregisterCaptureCallback();
                    m_objIGXStream?.Close();
                    m_objIGXStream = null;
                    m_objIGXStreamFeatureControl = null;
                }
            }
            catch (Exception ee)
            {
                Trace.WriteLine("#######  " + ee);
            }

            //�ر��豸
            __CloseDevice();
            m_bIsOpen = false;
        }

        public override bool LoadCamConfig(string filePath)
        {
            return false;
        }

        #endregion private
    }
}