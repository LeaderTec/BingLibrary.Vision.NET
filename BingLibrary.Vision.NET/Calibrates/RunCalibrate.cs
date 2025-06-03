using HalconDotNet;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BingLibrary.Vision.NET.Calibrates
{
    /// <summary>
    /// 运行标定
    /// </summary>
    public static class RunCalibrate
    {
        /// <summary>
        /// 获取转换的坐标点，坐标转换或映射转换
        /// </summary>
        /// <param name="homMat2D"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="qx"></param>
        /// <param name="qy"></param>
        public static void GetCalibratePoint(HHomMat2D homMat2D, double x, double y, out double qx, out double qy)
        {
            qx = homMat2D.AffineTransPoint2d(x, y, out qy);
        }

        /// <summary>
        /// 获取对位后的机械手坐标点，角度是度
        /// </summary>
        /// <param name="sourceX"></param>
        /// <param name="sourceY"></param>
        /// <param name="sourceDeg"></param>
        /// <param name="targetX"></param>
        /// <param name="targetY"></param>
        /// <param name="targetDeg"></param>
        /// <param name="tx"></param>
        /// <param name="ty"></param>
        /// <param name="qx"></param>
        /// <param name="qy"></param>
        public static void GetMatrixPoint(double sourceX, double sourceY, double sourceDeg, double targetX, double targetY, double targetDeg, double tx, double ty, out double qx, out double qy)
        {
            HHomMat2D hHomMat2D = new HHomMat2D();
            hHomMat2D.VectorAngleToRigid(sourceX, sourceY, new HTuple(sourceDeg).TupleRad().D, targetX, targetY, new HTuple(targetDeg).TupleRad().D);
            qx = hHomMat2D.AffineTransPoint2d(tx, ty, out qy);
        }
    }
}