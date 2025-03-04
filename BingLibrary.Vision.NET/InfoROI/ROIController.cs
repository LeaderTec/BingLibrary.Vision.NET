using BingLibrary.Extension;
using HalconDotNet;
using System.Collections.ObjectModel;

namespace BingLibrary.Vision
{
    public enum ModeROI
    {
        Roi_None,

        //箭头方向如直线等
        Roi_Positive,

        Roi_Negative,
    }

    /// <summary>
    /// ROI控制器
    /// </summary>
    public class ROIController
    {
        public ObservableCollection<ROIBase> ROIList;

        private HalconColors activeCol = HalconColors.绿色;//"green,cyan";
        private HalconColors activeHdlCol = HalconColors.紫色;
        private HalconColors inactiveCol = HalconColors.蓝色;//"magenta";//"yellow";

        public ROIBase roi;

        /// <summary>
        /// 构造函数
        /// </summary>
        public ROIController()
        {
            ROIList = new ObservableCollection<ROIBase>();
            ActiveROIidx = -1;
            currX = currY = -1;
        }

        private double currX, currY;

        /// <summary>
        /// 活动的ROI index
        /// </summary>
        public int ActiveROIidx { set; get; }

        /// <summary>
        /// 获取ROI列表
        /// </summary>
        /// <returns></returns>
        public ObservableCollection<ROIBase> GetROIList()
        {
            return ROIList;
        }

        /// <summary>
        /// 获取当前活动的ROI
        /// </summary>
        /// <returns></returns>
        public ROIBase GetActiveROI()
        {
            if (ActiveROIidx != -1)
                return ((ROIBase)ROIList[ActiveROIidx]);

            return null;
        }

        /// <summary>
        /// 获取选中的ROI索引
        /// </summary>
        /// <returns></returns>
        public int GetActiveROIIdx()
        {
            return ActiveROIidx;
        }

        /// <summary>
        /// 设置活动的ROI索引
        /// </summary>
        /// <param name="active"></param>
        public void SetActiveROIIdx(int active)
        {
            ActiveROIidx = active;
        }

        /// <summary>
        /// 增加ROI
        /// </summary>
        /// <param name="r"></param>
        public void AddROI(ROIBase r)
        {
            ROIList.Add(r);
            ActiveROIidx = -1;
        }

        /// <summary>
        /// 移除roi
        /// </summary>
        /// <param name="idx"></param>
        public void RemoveROI(int idx)
        {
            try
            {
                ROIList.RemoveAt(idx);
                ActiveROIidx = -1;
                GC.Collect();//垃圾回收
            }
            catch { }
        }

        /// <summary>
        /// 移除活动的ROI
        /// </summary>
        public void RemoveActiveRoi()
        {
            try
            {
                if (ActiveROIidx != -1)
                {
                    ROIList.RemoveAt(ActiveROIidx);
                    ActiveROIidx = -1;
                }
            }
            catch { }
        }

        public void Clear()
        {
            ROIList.Clear();
            ActiveROIidx = -1;
            roi = null;
            GC.Collect();
        }

        public void ResetROI()
        {
            ActiveROIidx = -1;
            roi = null;
        }

        /// <summary>
        /// 显示ROI
        /// </summary>
        /// <param name="window"></param>
        /// <param name="drawMode"></param>
        public void PaintData(HalconDotNet.HWindow window, HalconShowing drawMode)
        {
            window.SetDraw(drawMode.ToDescription());
            window.SetLineWidth(2);

            if (ROIList.Count > 0)
            {
                window.SetColor(inactiveCol.ToDescription());

                for (int i = 0; i < ROIList.Count; i++)
                {
                    if (string.IsNullOrEmpty(ROIList[i].ROIColor.ToDescription()))
                        window.SetColor(inactiveCol.ToDescription());
                    else
                        window.SetColor(ROIList[i].ROIColor.ToDescription());
                    window.SetLineStyle(((ROIBase)ROIList[i]).flagLineStyle);
                    ROIList[i].draw(window);
                }

                if (ActiveROIidx != -1)
                {
                    window.SetColor(activeCol.ToDescription());
                    window.SetLineStyle(((ROIBase)ROIList[ActiveROIidx]).flagLineStyle);

                    ROIList[ActiveROIidx].draw(window);

                    window.SetColor(activeHdlCol.ToDescription());
                    ROIList[ActiveROIidx].displayActive(window);
                }
            }
        }

        //用于鼠标显示可拖动的区域
        public int MouseMoveROI(double imgX, double imgY)
        {
            int idxROI = -1;
            double dist = -1.0;
            if (ROIList.Count > 0)
            {
                for (int i = 0; i < ROIList.Count; i++)
                {
                    if (ROIList[i].SizeEnable == false)//SizeEnable flase例外
                        continue;
                    dist = ROIList[i].distToClosestROI(imgX, imgY);
                    ROIList[i].ShowRect = true;
                    if (dist == 0.0)
                    {
                        idxROI = i;
                        break;
                    }
                }//end of for

                if (idxROI >= 0)
                {
                    ROIList[idxROI].ShowRect = true;
                    //System.Diagnostics.Debug.Print(idxROI.ToString());
                }
            }

            return idxROI;
        }

        /// <summary>
        /// 判断是否在区域内
        /// </summary>
        /// <param name="imgX"></param>
        /// <param name="imgY"></param>
        /// <returns></returns>
        public int MouseDownAction(double imgX, double imgY)
        {
            int idxROI = -1;
            int idxSizeEnableROI = -1;
            double max = 10000, dist = 0;
            //maximal shortest distance to one of
            //the handles

            if (roi != null)             //either a new ROI object is created
            {
                roi.createROI(imgX, imgY);
                ROIList.Add(roi);
                roi = null;
                ActiveROIidx = ROIList.Count - 1;
            }
            else if (ROIList.Count > 0)     // ... or an existing one is manipulated
            {
                ActiveROIidx = -1;

                for (int i = 0; i < ROIList.Count; i++)
                {
                    double epsilon = ROIList[i].smallregionwidth;  //矩形移动点击生效的区域
                    if (ROIList[i] is ROIRegion)//ROIRegion例外
                    {
                        HRegion tempRegion = new HRegion();//创建一个局部变量并实例化，用来保存鼠标点击的区域
                        tempRegion.GenRegionPoints(imgY, imgX);//根据鼠标点击的位置创建一个Point区域
                        var r = ((ROIRegion)ROIList[i]).mCurHRegion.Intersection(tempRegion);
                        if (r.Area.TupleMax() >= 1)
                        {
                            idxSizeEnableROI = i;
                            break;
                        }
                    }
                    else
                    {
                        dist = ((ROIBase)ROIList[i]).distToClosestHandle(imgX, imgY);
                        if ((dist <= max) && (dist < epsilon))
                        {
                            max = dist;
                            idxROI = i;
                            if (((ROIBase)ROIList[i]).SizeEnable == true)
                                idxSizeEnableROI = i;
                        }
                    }
                }//end of for
                if (idxROI != idxSizeEnableROI && idxSizeEnableROI >= 0)//优先SizeEnable的
                    idxROI = idxSizeEnableROI;
                if (idxROI >= 0)
                {
                    ActiveROIidx = idxROI;
                }
            }
            return ActiveROIidx;
        }

        /// <summary>
        /// 如果可以被拖动，则显示出来，Hregion直接拖动
        /// </summary>
        /// <param name="newX"></param>
        /// <param name="newY"></param>
        /// <param name="motionX"></param>
        /// <param name="motionY"></param>
        public void MouseMoveAction(double newX, double newY, double motionX = 0, double motionY = 0)
        {
            if ((newX == currX) && (newY == currY))
                return;
            try
            {
                if (ROIList[ActiveROIidx] is ROIRegion)//ROIRegion
                {
                    ((ROIRegion)ROIList[ActiveROIidx]).mCurHRegion = ((ROIRegion)ROIList[ActiveROIidx]).mCurHRegion.MoveRegion((int)motionY, (int)motionX);
                }
                else
                {
                    ROIList[ActiveROIidx].moveByHandle(newX, newY);
                }

                currX = newX;
                currY = newY;
            }
            catch { }
        }
    }
}