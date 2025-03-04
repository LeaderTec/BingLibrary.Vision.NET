using BingLibrary.Vision.Cameras.Camera;

namespace BingLibrary.Vision.Cameras
{
    /*************************************************************************************
     *
     * 文 件 名:   CamFactory
     * 描    述:
     *
     * 版    本：  V1.0.0.0
     * 创 建 者：  Bing
     * 创建时间：  2025/3/4 10:40:21
     * ======================================
     * 历史更新记录
     * 版本：V          修改时间：         修改人：
     * 修改内容：
     * ======================================
    *************************************************************************************/

    public class CamFactory
    {
        public CamFactory()
        { if (CameraList == null) CameraList = new List<ICamera>(); }

        private static List<ICamera> CameraList { get; set; } = new List<ICamera>() { };

        /// <summary>
        /// 按相机品牌获取相近SN枚举
        /// </summary>
        /// <param name="brand"></param>
        /// <returns></returns>
        public static List<string> GetDeviceEnum(CameraBrand brand)
        {
            ICamera camera = null;
            switch (brand)
            {
                case CameraBrand.HaiKang:
                    camera = new HaiKangCamera();
                    break;

                case CameraBrand.DaHua:
                    camera = new DaHuaCamera();
                    break;

                case CameraBrand.Basler:
                    camera = new BaslerCamera();
                    break;

                case CameraBrand.DaHeng:
                    camera = new DaHengCamera();
                    break;

                default: break;
            }
            return camera?.GetListEnum();
        }

        /// <summary>
        /// 按品牌创建相机
        /// </summary>
        /// <param name="brand"></param>
        /// <returns></returns>
        public static ICamera CreatCamera(CameraBrand brand)
        {
            ICamera returncamera = null;
            switch (brand)
            {
                case CameraBrand.HaiKang:
                    returncamera = new HaiKangCamera();
                    break;

                case CameraBrand.DaHua:
                    returncamera = new DaHuaCamera();
                    break;

                case CameraBrand.Basler:
                    returncamera = new BaslerCamera();
                    break;

                case CameraBrand.DaHeng:
                    returncamera = new DaHengCamera();
                    break;

                default: break;
            }
            CameraList.Add(returncamera);
            return returncamera;
        }

        /// <summary>
        /// 获取对应SN的相机实例
        /// </summary>
        /// <param name="CamSN"></param>
        /// <returns></returns>
        public static ICamera GetItem(string CamSN)
        {
            ICamera cameraStandard = null;
            if (CameraList.Count < 1) return cameraStandard;

            foreach (var item in CameraList)
            {
                if ((item as BaseCamera).SN.Equals(CamSN))
                {
                    cameraStandard = item;
                    break;
                }
            }
            return cameraStandard;
        }

        /// <summary>
        /// 注销相机
        /// </summary>
        /// <param name="decamera"></param>
        public static void DestroyCamera(ICamera decamera)
        {
            CameraList?.Remove(decamera);
            decamera?.CloseDevice();
        }

        /// <summary>
        /// 注销所有相机
        /// </summary>
        public static void DestroyAll()
        {
            if (CameraList.Count < 1) return;
            foreach (var camereaitem in CameraList)
            {
                camereaitem?.CloseDevice();
            }
            CameraList?.Clear();
        }
    }
}