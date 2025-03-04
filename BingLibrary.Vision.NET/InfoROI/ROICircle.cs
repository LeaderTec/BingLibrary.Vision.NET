using HalconDotNet;

namespace BingLibrary.Vision
{
    public class ROICircle : ROIBase
    {
        public double radius;
        public double row1, col1;  // first handle
        public double midR, midC;  // second handle

        public ROICircle()
        {
            numHandles = 2; // one at corner of circle + midpoint
            activeHandleIdx = 1;
        }

        public override void createROI(double midX, double midY)
        {
            midR = midY;
            midC = midX;

            radius = 100;

            row1 = midR;
            col1 = midC + radius;
        }

        public override void createROICircle(double midX, double midY, double mradius)
        {
            midR = midY;
            midC = midX;
            radius = mradius;
            row1 = midR;
            col1 = midC + radius;
        }

        public override void draw(HalconDotNet.HWindow window)
        {
            window.DispCircle(midR, midC, radius);
            if (SizeEnable && ShowRect)
            {
                window.DispRectangle2(row1, col1, 0, smallregionwidth, smallregionwidth);
                window.DispRectangle2(midR, midC, 0, smallregionwidth, smallregionwidth);
            }
        }

        public override double distToClosestHandle(double x, double y)
        {
            double max = 10000;
            double[] val = new double[numHandles];

            val[0] = HMisc.DistancePp(y, x, row1, col1); // border handle
            val[1] = HMisc.DistancePp(y, x, midR, midC); // midpoint
            for (int i = 0; i < numHandles; i++)
            {
                if (val[i] < max)
                {
                    max = val[i];
                    activeHandleIdx = i;
                }
            }// end of for
            return val[activeHandleIdx];
        }

        public override double distToClosestROI(double x, double y)
        {
            HTuple dismax, dismin = 0;
            HOperatorSet.DistancePr(getRegion(), y, x, out dismin, out dismax);
            return dismin;
        }

        public override void displayActive(HalconDotNet.HWindow window)
        {
            if (!SizeEnable || !ShowRect)
                return;
            switch (activeHandleIdx)
            {
                case 0:
                    window.DispRectangle2(row1, col1, 0, smallregionwidth, smallregionwidth);
                    break;

                case 1:
                    window.DispRectangle2(midR, midC, 0, smallregionwidth, smallregionwidth);
                    break;
            }
        }

        public override HRegion getRegion()
        {
            HRegion region = new HRegion();
            region.GenCircle(midR, midC, radius);
            return region;
        }

        public override double getDistanceFromStartPoint(double row, double col)
        {
            double sRow = midR; // assumption: we have an angle starting at 0.0
            double sCol = midC + 1 * radius;

            double angle = HMisc.AngleLl(midR, midC, sRow, sCol, midR, midC, row, col);

            if (angle < 0)
                angle += 2 * Math.PI;

            return (radius * angle);
        }

        public override HTuple getModelData()
        {
            return new HTuple(new double[] { midR, midC, radius });
        }

        public override void moveByHandle(double newX, double newY)
        {
            if (SizeEnable == false)
                return;
            HTuple distance;
            double shiftX, shiftY;

            switch (activeHandleIdx)
            {
                case 0: // handle at circle border

                    row1 = newY;
                    col1 = newX;
                    HOperatorSet.DistancePp(new HTuple(row1), new HTuple(col1),
                                            new HTuple(midR), new HTuple(midC),
                                            out distance);

                    radius = distance[0].D;
                    break;

                case 1: // midpoint

                    shiftY = midR - newY;
                    shiftX = midC - newX;

                    midR = newY;
                    midC = newX;

                    row1 -= shiftY;
                    col1 -= shiftX;
                    break;
            }
        }
    }
}