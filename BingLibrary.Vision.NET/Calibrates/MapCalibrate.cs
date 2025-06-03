using HalconDotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BingLibrary.Vision.Calibrates
{
    /// <summary>
    /// 标定映射，两套坐标系统建议映射关系，至少需要3个点
    /// </summary>
    public static class MapCalibrate
    {
        /// <summary>
        /// 标定映射,从源坐标系映射到目标坐标系
        /// </summary>
        /// <param name="sourceX"></param>
        /// <param name="sourceY"></param>
        /// <param name="targetX"></param>
        /// <param name="targetY"></param>
        /// <returns></returns>
        public static HHomMat2D CalibrateMap(List<double> sourceX, List<double> sourceY, List<double> targetX, List<double> targetY)
        {
            HHomMat2D hHomMat2D = new HHomMat2D();
            try
            {
                hHomMat2D.VectorToHomMat2d(new HTuple(sourceX), new HTuple(sourceY), new HTuple(targetX), new HTuple(targetY));
            }
            catch { }
            return hHomMat2D;
        }
    }
}