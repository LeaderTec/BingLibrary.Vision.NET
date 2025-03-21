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

    public class CamFactory<T>
    {
        public CamFactory()
        { if (CameraList == null) CameraList = new List<ICamera<T>>(); }

        private static List<ICamera<T>> CameraList { get; set; } = new List<ICamera<T>>() { };

        /// <summary>
        /// 按相机品牌获取相近SN枚举
        /// </summary>
        /// <param name="brand"></param>
        /// <returns></returns>
        public static List<CameraInfo> GetDeviceEnum(CameraBrand brand)
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

                default: break;
            }
            return camera?.GetListEnum();
        }

        /// <summary>
        /// 按品牌创建相机
        /// </summary>
        /// <param name="brand"></param>
        /// <returns></returns>
        public static ICamera<T> CreatCamera(CameraBrand brand)
        {
            ICamera<T> returncamera = null;
            switch (brand)
            {
                case CameraBrand.HaiKang:
                    returncamera = new HaiKangCamera<T>();
                    break;

                case CameraBrand.DaHua:
                    returncamera = new DaHuaCamera<T>();
                    break;

                case CameraBrand.Basler:
                    returncamera = new BaslerCamera<T>();
                    break;

                case CameraBrand.DaHeng:
                    returncamera = new DaHengCamera<T>();
                    break;

                default: break;
            }
            CameraList.Add(returncamera);
            return returncamera;
        }

        ///// <summary>
        ///// 获取对应SN的相机实例
        ///// </summary>
        ///// <param name="CamSN"></param>
        ///// <returns></returns>
        //public static ICamera<T> GetItem(string CamSN)
        //{
        //    ICamera<T> cameraStandard = null;
        //    if (CameraList.Count < 1) return cameraStandard;

        //    foreach (var item in CameraList)
        //    {
        //        if ((item as BaseCamera<T>).SN.Equals(CamSN))
        //        {
        //            cameraStandard = item;
        //            break;
        //        }
        //    }
        //    return cameraStandard;
        //}

        /// <summary>
        /// 注销相机
        /// </summary>
        /// <param name="decamera"></param>
        public static void DestroyCamera(ICamera<T> decamera)
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