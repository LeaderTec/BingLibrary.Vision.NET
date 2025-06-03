using HalconDotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BingLibrary.Vision.Calibrates
{
    /// <summary>
    /// XY轴分离标定
    /// </summary>
    public static class OneAxisCalibrate
    {
        /// <summary>
        /// 按顺序移动三个点，间距相等
        /// </summary>
        /// <param name="pixelX"></param>
        /// <param name="pixelY"></param>
        /// <param name="worldX"></param>
        /// <param name="worldY"></param>
        /// <returns></returns>
        public static HHomMat2D CalibrateThree(List<double> pixelX, List<double> pixelY, List<double> worldX, List<double> worldY)
        {
            HHomMat2D hHomMat2D = new HHomMat2D();
            try
            {
                //虚拟两点，形成十字五点，标定
                HHomMat2D tempHomMat2d = new HHomMat2D();
                tempHomMat2d.VectorAngleToRigid(pixelX[1], pixelY[1], 0, pixelX[1], pixelY[1], (new HTuple(-90)).TupleRad().D);
                tempHomMat2d.AffineTransPixel(pixelX[2], pixelY[2], out double tempPixelX, out double tempPixelY);
                pixelX.Add(tempPixelX);
                pixelY.Add(tempPixelY);
                worldX.Add(worldX[1] + Math.Abs(worldX[0] - worldX[1]));
                worldY.Add(worldY[1]);

                tempHomMat2d.VectorAngleToRigid(pixelX[1], pixelY[1], 0, pixelX[1], pixelY[1], (new HTuple(90)).TupleRad().D);
                tempHomMat2d.AffineTransPixel(pixelX[2], pixelY[2], out tempPixelX, out tempPixelY);
                pixelX.Add(tempPixelX);
                pixelY.Add(tempPixelY);
                worldX.Add(worldX[1] - Math.Abs(worldX[0] - worldX[1]));
                worldY.Add(worldY[1]);

                hHomMat2D.VectorToHomMat2d(new HTuple(pixelX), new HTuple(pixelY), new HTuple(worldX), new HTuple(worldY));
            }
            catch { }
            return hHomMat2D;
        }

        /// <summary>
        /// 按顺序移动三个点，间距相等，拟合圆心
        /// </summary>
        /// <param name="pixelX"></param>
        /// <param name="pixelY"></param>
        /// <param name="worldX"></param>
        /// <param name="worldY"></param>
        /// <param name="pixelXC"></param>
        /// <param name="pixelYC"></param>
        /// <returns></returns>
        public static HHomMat2D CalibrateThreeWithRotate(List<double> pixelX, List<double> pixelY, List<double> worldX, List<double> worldY, List<double> pixelXC, List<double> pixelYC)
        {
            HHomMat2D hHomMat2D = new HHomMat2D();
            try
            {
                //虚拟两点，形成十字五点，标定
                HHomMat2D tempHomMat2d = new HHomMat2D();
                tempHomMat2d.VectorAngleToRigid(pixelX[1], pixelY[1], 0, pixelX[1], pixelY[1], (new HTuple(-90)).TupleRad().D);
                tempHomMat2d.AffineTransPixel(pixelX[2], pixelY[2], out double tempPixelX, out double tempPixelY);
                pixelX.Add(tempPixelX);
                pixelY.Add(tempPixelY);
                worldX.Add(worldX[1] + Math.Abs(worldX[0] - worldX[1]));
                worldY.Add(worldY[1]);

                tempHomMat2d.VectorAngleToRigid(pixelX[1], pixelY[1], 0, pixelX[1], pixelY[1], (new HTuple(90)).TupleRad().D);
                tempHomMat2d.AffineTransPixel(pixelX[2], pixelY[2], out tempPixelX, out tempPixelY);
                pixelX.Add(tempPixelX);
                pixelY.Add(tempPixelY);
                worldX.Add(worldX[1] - Math.Abs(worldX[0] - worldX[1]));
                worldY.Add(worldY[1]);

                hHomMat2D.VectorToHomMat2d(new HTuple(pixelX), new HTuple(pixelY), new HTuple(worldX), new HTuple(worldY));

                (double x, double y, double r) circle = FitCircleToPoints(pixelXC, pixelYC);
                double cX = hHomMat2D.AffineTransPoint2d(circle.x, circle.y, out double cY);

                double bx = worldX[0] - cX;
                double by = worldY[0] - cY;

                List<double> newWorldX = new List<double>();
                List<double> newWorldY = new List<double>();
                for (int i = 0; i < worldX.Count; i++)
                {
                    newWorldX.Add(worldX[i] + bx);
                    newWorldY.Add(worldY[i] + by);
                }
                hHomMat2D.VectorToHomMat2d(new HTuple(pixelX), new HTuple(pixelY), new HTuple(newWorldX), new HTuple(newWorldY));
            }
            catch { }
            return hHomMat2D;
        }

        /// <summary>
        /// 多点拟合计算圆心坐标
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        private static (double a, double b, double r) FitCircleToPoints(List<double> x, List<double> y)
        {
            if (x.Count != y.Count || x.Count < 3)
                throw new ArgumentException("至少需要 3 个点才能拟合圆");

            //构造矩阵 A = [X, Y, 1], B = [X² + Y²]
            //3列：X, Y, 1
            HMatrix matrixA = new HMatrix();
            matrixA.CreateMatrix(x.Count, 3, 1.0);

            HMatrix matrixXCol = new HMatrix();
            matrixXCol.CreateMatrix(x.Count, 1, new HTuple(x.ToArray()));

            //第0列 = X
            matrixA.SetSubMatrix(matrixXCol, 0, 0);
            HMatrix matrixYCol = new HMatrix();
            matrixYCol.CreateMatrix(x.Count, 1, new HTuple(y.ToArray()));

            //第1列 = Y
            matrixA.SetSubMatrix(matrixYCol, 0, 1);

            //构造 B = X² + Y²
            HMatrix matrixB = new HMatrix();
            matrixB.CreateMatrix(x.Count, 1, (new HTuple(x.ToArray())) * (new HTuple(x.ToArray())) + (new HTuple(y.ToArray())) * (new HTuple(y.ToArray())));

            //解方程 A * [C; D; E] = B
            HMatrix matrixX = new HMatrix();
            matrixX = matrixA.SolveMatrix("general", 0, matrixB);
            //提取 C, D, E
            var c = matrixX.GetValueMatrix(0, 0);
            var d = matrixX.GetValueMatrix(1, 0);
            var e = matrixX.GetValueMatrix(2, 0);

            //计算圆心 (a, b) 和半径 r
            var a = c / 2;
            var b = d / 2;
            var r = Math.Sqrt(Math.Pow(a, 2) + Math.Pow(b, 2) + e);

            return (a, b, r);
        }
    }
}