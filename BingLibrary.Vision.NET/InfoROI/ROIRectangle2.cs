using HalconDotNet;

namespace BingLibrary.Vision
{
    public class ROIRectangle2 : ROIBase
    {
        public double length1;

        public double length2;

        public double midR;

        public double midC;

        public double phi;

        private HTuple rowsInit;

        private HTuple colsInit;
        private HTuple rows;
        private HTuple cols;

        private HHomMat2D hom2D, tmp;

        public ROIRectangle2()
        {
            numHandles = 6; // 4 corners +  1 midpoint + 1 rotationpoint
            activeHandleIdx = 4;
        }

        public override void createROI(double midX, double midY)
        {
            midR = midY;
            midC = midX;

            length1 = 100;
            length2 = 50;

            phi = 0.0;

            rowsInit = new HTuple(new double[] {-1.0, -1.0, 1.0,
                                                   1.0,  0.0, 0.0 });
            colsInit = new HTuple(new double[] {-1.0, 1.0,  1.0,
                                                  -1.0, 0.0, 0.6 });
            //order        ul ,  ur,   lr,  ll,   mp, arrowMidpoint
            hom2D = new HHomMat2D();
            tmp = new HHomMat2D();

            updateHandlePos();
        }

        public override void createROIRect2(double midX, double midY, double mphi, double mlength1, double mlength2)
        {
            midR = midY;
            midC = midX;

            length1 = mlength1;
            length2 = mlength2;

            phi = mphi;

            rowsInit = new HTuple(new double[] {-1.0, -1.0, 1.0,
                                                   1.0,  0.0, 0.0 });
            colsInit = new HTuple(new double[] {-1.0, 1.0,  1.0,
                                                  -1.0, 0.0, 0.6 });
            //order        ul ,  ur,   lr,  ll,   mp, arrowMidpoint
            hom2D = new HHomMat2D();
            tmp = new HHomMat2D();

            updateHandlePos();
        }

        public override void draw(HalconDotNet.HWindow window)
        {
            window.DispRectangle2(midR, midC, -phi, length1, length2);
            if (SizeEnable && ShowRect)
            {
                for (int i = 0; i < numHandles; i++)
                    window.DispRectangle2(rows[i].D, cols[i].D, -phi, smallregionwidth, smallregionwidth);

                window.DispArrow(midR, midC, midR + (Math.Sin(phi) * length1 * 1.2),
                    midC + (Math.Cos(phi) * length1 * 1.2), 2.0);
            }
        }

        public override double distToClosestHandle(double x, double y)
        {
            double max = 10000;
            double[] val = new double[numHandles];

            for (int i = 0; i < numHandles; i++)
                val[i] = HMisc.DistancePp(y, x, rows[i].D, cols[i].D);

            for (int i = 0; i < numHandles; i++)
            {
                if (val[i] < max)
                {
                    max = val[i];
                    activeHandleIdx = i;
                }
            }
            return val[activeHandleIdx];
        }

        public override double distToClosestROI(double x, double y)
        {
            HTuple dismax, dismin = 0;
            HOperatorSet.DistancePr(getRegion(), y, x, out dismin, out dismax);
            //System.Diagnostics.Debug.Print(dismin + "," + dismax);
            return dismin;
        }

        public override void displayActive(HalconDotNet.HWindow window)
        {
            if (!SizeEnable || !ShowRect)
                return;
            window.DispRectangle2(rows[activeHandleIdx].D,
                                  cols[activeHandleIdx].D,
                                  -phi, smallregionwidth, smallregionwidth);

            if (activeHandleIdx == 5)
                window.DispArrow(midR, midC,
                                 midR + (Math.Sin(phi) * length1 * 1.2),
                                 midC + (Math.Cos(phi) * length1 * 1.2),
                                 smallregionwidth);
        }

        public override HRegion getRegion()
        {
            HRegion region = new HRegion();
            region.GenRectangle2(midR, midC, -phi, length1, length2);
            return region;
        }

        public override HTuple getModelData()
        {
            return new HTuple(new double[] { midR, midC, phi, length1, length2 });
        }

        public override void moveByHandle(double newX, double newY)
        {
            if (SizeEnable == false)
                return;
            double vX, vY, x = 0, y = 0;

            switch (activeHandleIdx)
            {
                case 0:
                case 1:
                case 2:
                case 3:
                    tmp = hom2D.HomMat2dInvert();
                    x = tmp.AffineTransPoint2d(newX, newY, out y);

                    length2 = Math.Abs(y);
                    length1 = Math.Abs(x);

                    checkForRange(x, y);
                    break;

                case 4:
                    midC = newX;
                    midR = newY;
                    break;

                case 5:
                    vY = newY - rows[4].D;
                    vX = newX - cols[4].D;
                    phi = Math.Atan2(vY, vX);
                    break;
            }
            updateHandlePos();
        }//end of method

        private void updateHandlePos()
        {
            hom2D.HomMat2dIdentity();
            hom2D = hom2D.HomMat2dTranslate(midC, midR);
            hom2D = hom2D.HomMat2dRotateLocal(phi);
            tmp = hom2D.HomMat2dScaleLocal(length1, length2);
            cols = tmp.AffineTransPoint2d(colsInit, rowsInit, out rows);
        }

        private void checkForRange(double x, double y)
        {
            switch (activeHandleIdx)
            {
                case 0:
                    if ((x < 0) && (y < 0))
                        return;
                    if (x >= 0) length1 = 0.01;
                    if (y >= 0) length2 = 0.01;
                    break;

                case 1:
                    if ((x > 0) && (y < 0))
                        return;
                    if (x <= 0) length1 = 0.01;
                    if (y >= 0) length2 = 0.01;
                    break;

                case 2:
                    if ((x > 0) && (y > 0))
                        return;
                    if (x <= 0) length1 = 0.01;
                    if (y <= 0) length2 = 0.01;
                    break;

                case 3:
                    if ((x < 0) && (y > 0))
                        return;
                    if (x >= 0) length1 = 0.01;
                    if (y <= 0) length2 = 0.01;
                    break;

                default:
                    break;
            }
        }
    }
}