using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HalconDotNet;

namespace BingLibrary.Vision.Calibrates
{
    /// <summary>
    /// 用于标定镜头畸变
    /// 1. 打印直线线条到纸上，用相机拍摄
    /// 2. 线标定相机畸变
    /// 3. 线标定矫正图像
    /// </summary>
    public static class LineCalibrate
    {
        /// <summary>
        /// 线标定相机畸变
        /// </summary>
        /// <param name="image"></param>
        /// <param name="camParamIn"></param>
        /// <param name="camParamOut"></param>
        public static void CalibrateCamera(HImage image, out HCamPar camParamIn, out HCamPar camParamOut)
        {
            camParamIn = new HCamPar();
            camParamOut = new HCamPar();
            try
            {
                HImage img = image.Rgb1ToGray();
                var edge = img.EdgesSubPix("canny", 1, 30, 50);
                var edges = edge.SegmentContoursXld("lines_circles", 5, 8, 4).SelectShapeXld("contlength", "and", 30, 999999999999999);
                HTuple w, h;
                image.GetImageSize(out w, out h);
                var eds = edges.RadialDistortionSelfCalibration(w, h, 0.08, 3, "division", "adaptive", 0, out camParamIn);
                camParamOut = camParamIn.ChangeRadialDistortionCamPar("adaptive", new HTuple(0));
            }
            catch (Exception ex) { }
        }

        /// <summary>
        /// 矫正图像
        /// </summary>
        /// <param name="image"></param>
        /// <param name="camParamIn"></param>
        /// <param name="camParamOut"></param>
        /// <returns></returns>
        public static HImage CalibrateImage(HImage image, HCamPar camParamIn, HCamPar camParamOut)
        {
            HImage resultImage = new HImage();
            try
            {
                HRegion domain = image.GetDomain();
                resultImage = image.ChangeRadialDistortionImage(domain, camParamIn, camParamOut);
            }
            catch (Exception ex) { }
            return resultImage;
        }

        /// <summary>
        /// 保存相机标定参数
        /// </summary>
        /// <param name="camParam"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static bool SaveCameraParam(HCamPar camParam, string fileName)
        {
            try
            {
                camParam.WriteCamPar(fileName);
                return true;
            }
            catch { return false; }
        }

        /// <summary>
        /// 读取相机标定参数
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static HCamPar LoadCameraParam(string fileName)
        {
            HCamPar camParam = new HCamPar();
            try
            {
                camParam.ReadCamPar(fileName);
                return camParam;
            }
            catch { }
            return camParam;
        }
    }
}