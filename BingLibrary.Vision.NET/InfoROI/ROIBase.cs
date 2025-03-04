using HalconDotNet;

namespace BingLibrary.Vision
{
    [Serializable]
    public class ROIBase
    {
        public string ROIName { get; set; }

        public string ROIResult { set; get; }

        public HalconColors ROIColor { set; get; } = HalconColors.海军蓝色;

        public int ROIID { get; set; }
        public bool SizeEnable { set; get; } = true;

        public int smallregionwidth { set; get; } = 20;

        public bool ShowRect { set; get; } = true;

        public virtual void show()
        {
        }

        protected int numHandles;//可以调整（拖动）的句柄数

        protected int activeHandleIdx;

        public HTuple flagLineStyle = new HTuple();//halcon13修改=new htuple(),否则报null错误

        public ModeROI POSITIVE_FLAG = ModeROI.Roi_Positive;

        public ModeROI NEGATIVE_FLAG = ModeROI.Roi_Negative;

        public const int ROI_TYPE_LINE = 10;
        public const int ROI_TYPE_CIRCLE = 11;
        public const int ROI_TYPE_CIRCLEARC = 12;
        public const int ROI_TYPE_RECTANCLE1 = 13;
        public const int ROI_TYPE_RECTANGLE2 = 14;

        [NonSerialized]
        protected HTuple posOperation = new HTuple();

        [NonSerialized]
        protected HTuple negOperation = new HTuple(new int[] { 2, 2 });

        public ROIBase()
        {
        }

        public virtual void createROI(double midX, double midY)
        {
        }

        public virtual void createROICircle(double midX, double midY, double radius)
        {
        }

        public virtual void createROILine(double r1, double c1, double r2, double c2)
        {
        }

        public virtual void createROIRect1(double r1, double c1, double r2, double c2)
        {
        }

        public virtual void createROIRect2(double midX, double midY, double mphi, double mlength1, double mlength2)
        {
        }

        public virtual void draw(HalconDotNet.HWindow window)
        {
        }

        public virtual double distToClosestHandle(double x, double y)
        {
            return 0.0;
        }

        //判断鼠标是否在ROI内
        public virtual double distToClosestROI(double x, double y)
        {
            return -1.0;
        }

        public virtual void displayActive(HalconDotNet.HWindow window)
        {
        }

        public virtual void moveByHandle(double x, double y)
        {
        }

        public virtual HRegion getRegion()
        {
            return null;
        }

        public virtual double getDistanceFromStartPoint(double row, double col)
        {
            return 0.0;
        }

        public virtual HTuple getModelData()
        {
            return null;
        }

        public int getNumHandles()
        {
            return numHandles;
        }

        public int getActHandleIdx()
        {
            return activeHandleIdx;
        }

        public void setOperatorFlag(ModeROI flag)
        {
            switch (flag)
            {
                case ModeROI.Roi_Positive:
                    flagLineStyle = posOperation;
                    break;

                case ModeROI.Roi_Negative:
                    flagLineStyle = negOperation;
                    break;

                default:
                    flagLineStyle = posOperation;
                    break;
            }
        }
    }
}