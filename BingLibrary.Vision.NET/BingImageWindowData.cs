using HalconDotNet;

namespace BingLibrary.Vision
{
    public class BingImageWindowData
    {
        #region 控件相关

        /// <summary>
        /// 显示控件
        /// </summary>
        [Obsolete("不推荐直接使用此属性，请使用该类中其它方法。", false)]
        public WindowController WindowCtrl { set; get; }

        /// <summary>
        /// ROI相关
        /// </summary>
        [Obsolete("不推荐直接使用此属性，请使用该类中其它方法。", false)]
        public ROIController ROICtrl { set; get; }

        /// <summary>
        /// 显示消息相关
        /// </summary>
        [Obsolete("不推荐直接使用此属性，请使用该类中其它方法。", false)]
        public MessageController MessageCtrl { set; get; }

        /// <summary>
        /// 其余需要显示在窗口的
        /// </summary>
        [Obsolete("不推荐直接使用此属性，请使用该类中其它方法。", false)]
        public DispObjectController DispObjectCtrl { set; get; }

        #endregion 控件相关

#pragma warning disable CS0618 // 禁用“类型或成员已过时”的警告

        /// <summary>
        /// 初始化窗口
        /// </summary>
        /// <param name="hWin"></param>
        internal void Init(HWindowControlWPF hwcw)
        {
            ROICtrl = new ROIController();
            MessageCtrl = new MessageController();
            DispObjectCtrl = new DispObjectController();
            WindowCtrl = new WindowController(hwcw);

            WindowCtrl.SetROIController(ROICtrl);
            WindowCtrl.SetMessageController(MessageCtrl);
            WindowCtrl.SetDispObjectController(DispObjectCtrl);
        }

        #region 操作

        /// <summary>
        /// 显示图像到窗口
        /// </summary>
        /// <param name="image"></param>
        public void DisplayImage(HImage image) => this.WindowCtrl.ShowImageToWindow(image);

        /// <summary>
        /// 适应图像到窗口
        /// </summary>
        public void FitImage() => this.WindowCtrl.FitImageToWindow();

        /// <summary>
        /// 刷新窗口显示
        /// </summary>
        public void RefreshWindow() => this.WindowCtrl.Repaint();

        /// <summary>
        /// 清空窗口
        /// </summary>
        public void ClearWindow()
        {
            this.DispObjectCtrl.Clear();
            this.MessageCtrl.Clear();
            this.ROICtrl.Clear();
            this.WindowCtrl.ShowImageToWindow(null);
        }

        /// <summary>
        /// 绘制区域
        /// </summary>
        /// <param name="color"></param>
        /// <param name="region"></param>
        public void DrawRegion(HalconColors color, out HRegion region)
        {
            region = new HRegion();
            region.GenEmptyRegion();

            this.WindowCtrl.IsDrawing = true;
            this.WindowCtrl.hWindowControlWPF.HalconWindow.SetColor(color.ToDescriptionOrString());
            region = this.WindowCtrl.hWindowControlWPF.HalconWindow.DrawRegion();

            this.WindowCtrl.IsDrawing = false;
        }

        /// <summary>
        /// 绘制矩形
        /// </summary>
        /// <param name="color"></param>
        /// <param name="row1"></param>
        /// <param name="column1"></param>
        /// <param name="row2"></param>
        /// <param name="column2"></param>
        public void DrawRectangle1(HalconColors color, out double row1, out double column1, out double row2, out double column2)
        {
            this.WindowCtrl.IsDrawing = true;
            this.WindowCtrl.hWindowControlWPF.HalconWindow.SetColor(color.ToDescriptionOrString());
            this.WindowCtrl.hWindowControlWPF.HalconWindow.DrawRectangle1(out row1, out column1, out row2, out column2);

            this.WindowCtrl.IsDrawing = false;
        }

        /// <summary>
        /// 绘制矩形
        /// </summary>
        /// <param name="color"></param>
        /// <param name="row1"></param>
        /// <param name="column1"></param>
        /// <param name="row2"></param>
        /// <param name="column2"></param>
        public void DrawRectangleMod1(HalconColors color, double r1, double c1, double r2, double c2, out double row1, out double column1, out double row2, out double column2)
        {
            this.WindowCtrl.IsDrawing = true;
            this.WindowCtrl.hWindowControlWPF.HalconWindow.SetColor(color.ToDescriptionOrString());
            this.WindowCtrl.hWindowControlWPF.HalconWindow.DrawRectangle1Mod(r1, c1, r2, c2, out row1, out column1, out row2, out column2);
            this.WindowCtrl.IsDrawing = false;
        }

        /// <summary>
        /// 绘制可旋转矩形
        /// </summary>
        /// <param name="color"></param>
        /// <param name="row"></param>
        /// <param name="column"></param>
        /// <param name="phi"></param>
        /// <param name="length1"></param>
        /// <param name="length2"></param>
        public void DrawRectangle2(HalconColors color, out double row, out double column, out double phi, out double length1, out double length2)
        {
            this.WindowCtrl.IsDrawing = true;
            this.WindowCtrl.hWindowControlWPF.HalconWindow.SetColor(color.ToDescriptionOrString());
            this.WindowCtrl.hWindowControlWPF.HalconWindow.DrawRectangle2(out row, out column, out phi, out length1, out length2);

            this.WindowCtrl.IsDrawing = false;
        }

        /// <summary>
        /// 绘制圆
        /// </summary>
        /// <param name="color"></param>
        /// <param name="row"></param>
        /// <param name="column"></param>
        /// <param name="radius"></param>
        public void DrawCircle(HalconColors color, out double row, out double column, out double radius)
        {
            this.WindowCtrl.IsDrawing = true;
            this.WindowCtrl.hWindowControlWPF.HalconWindow.SetColor(color.ToDescriptionOrString());
            this.WindowCtrl.hWindowControlWPF.HalconWindow.DrawCircle(out row, out column, out radius);

            this.WindowCtrl.IsDrawing = false;
        }

        /// <summary>
        /// 设置水印
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="fontSize"></param>
        /// <param name="isShow"></param>
        public void SetWaterString(string msg, int fontSize = 36, bool isShow = true) => this.WindowCtrl.ShowWaterStringToWindow(msg, fontSize, isShow);

        /// <summary>
        /// 显示文字
        /// </summary>
        /// <param name="message"></param>
        /// <param name="row"></param>
        /// <param name="column"></param>
        /// <param name="fontsize"></param>
        /// <param name="color"></param>
        /// <param name="mode"></param>
        public void DisplayMessage(string message, int row, int column, int fontsize = 12, HalconColors color = HalconColors.绿色, bool showBox = true, HalconCoordinateSystem mode = HalconCoordinateSystem.image) => this.MessageCtrl.AddMessageVar(message, row, column, fontsize, color, showBox, mode);

        /// <summary>
        /// 清空所有显示文字
        /// </summary>
        public void ClearMessages() => this.MessageCtrl.Clear();

        /// <summary>
        /// 显示区域
        /// </summary>
        /// <param name="showObject"></param>
        /// <param name="color"></param>
        public void DisplayObject(HObject showObject, HalconColors color = HalconColors.绿色, HalconShowing showMode = HalconShowing.margin, bool isShowDotLine = false) => this.DispObjectCtrl.AddDispObjectVar(showObject, color, showMode, isShowDotLine);

        /// <summary>
        /// 清空所有显示区域
        /// </summary>
        public void ClearObject() => this.DispObjectCtrl.Clear();

        /// <summary>
        /// 设置绘制释放模式，直接释放或右键释放
        /// </summary>
        /// <param name="drawFinishMode"></param>
        public void SetDrawFinishMode(HalconDrawMode drawFinishMode) => this.WindowCtrl.DrawFinishMode = drawFinishMode;

        #endregion 操作

        #region 设置

        /// <summary>
        /// 设置默认显示颜色
        /// </summary>
        /// <param name="color"></param>
        public void SetColor(HalconColors color) => this.WindowCtrl.hWindowControlWPF.HalconWindow.SetColor(color.ToDescriptionOrString());

        /// <summary>
        /// 显示模式
        /// </summary>
        /// <param name="showMode"></param>
        public void SetShowMode(HalconShowing showMode) => this.WindowCtrl.ShowMode = showMode;

        /// <summary>
        /// 绘制模式，直接释放或右键释放
        /// </summary>
        /// <param name="halconDrawMode"></param>
        public void SetDrawMode(HalconDrawMode halconDrawMode) => this.WindowCtrl.DrawFinishMode = halconDrawMode;

        #endregion 设置

        /// <summary>
        /// 运行拖拽编辑ROI
        /// </summary>
        public void EnableEdit() => this.WindowCtrl.CanEdit = true;

        /// <summary>
        /// 不允许拖拽编辑ROI
        /// </summary>
        public void DisableEdit() => this.WindowCtrl.CanEdit = false;

        #region 绘制ROI

        /// <summary>
        /// 绘制区域
        /// </summary>
        /// <param name="color"></param>
        /// <param name="region"></param>
        public void DrawROIRegion(HalconColors color, out HRegion region)
        {
            region = new HRegion();
            region.GenEmptyRegion();

            this.WindowCtrl.IsDrawing = true;
            this.WindowCtrl.hWindowControlWPF.HalconWindow.SetColor(color.ToDescriptionOrString());
            region = this.WindowCtrl.hWindowControlWPF.HalconWindow.DrawRegion();
            this.ROICtrl.AddROI(new ROIRegion(region));

            this.WindowCtrl.IsDrawing = false;
        }

        /// <summary>
        /// 绘制矩形
        /// </summary>
        /// <param name="color"></param>
        /// <param name="row1"></param>
        /// <param name="column1"></param>
        /// <param name="row2"></param>
        /// <param name="column2"></param>
        public void DrawROIRectangle1(HalconColors color, out double row1, out double column1, out double row2, out double column2)
        {
            this.WindowCtrl.IsDrawing = true;
            this.WindowCtrl.hWindowControlWPF.HalconWindow.SetColor(color.ToDescriptionOrString());
            this.WindowCtrl.hWindowControlWPF.HalconWindow.DrawRectangle1(out row1, out column1, out row2, out column2);

            ROIRectangle1 roiRectangle1 = new ROIRectangle1();
            roiRectangle1.createROIRect1(row1, column1, row2, column2);
            this.ROICtrl.AddROI(roiRectangle1);

            this.WindowCtrl.IsDrawing = false;
        }

        /// <summary>
        /// 绘制可旋转矩形
        /// </summary>
        /// <param name="color"></param>
        /// <param name="row"></param>
        /// <param name="column"></param>
        /// <param name="phi"></param>
        /// <param name="length1"></param>
        /// <param name="length2"></param>
        public void DrawROIRectangle2(HalconColors color, out double row, out double column, out double phi, out double length1, out double length2)
        {
            this.WindowCtrl.IsDrawing = true;
            this.WindowCtrl.hWindowControlWPF.HalconWindow.SetColor(color.ToDescriptionOrString());
            this.WindowCtrl.hWindowControlWPF.HalconWindow.DrawRectangle2(out row, out column, out phi, out length1, out length2);
            ROIRectangle2 roiRectangle2 = new ROIRectangle2();
            roiRectangle2.createROIRect2(row, column, phi, length1, length2);
            this.ROICtrl.AddROI(roiRectangle2);

            this.WindowCtrl.IsDrawing = false;
        }

        /// <summary>
        /// 绘制圆
        /// </summary>
        /// <param name="color"></param>
        /// <param name="row"></param>
        /// <param name="column"></param>
        /// <param name="radius"></param>
        public void DrawROICircle(HalconColors color, out double row, out double column, out double radius)
        {
            this.WindowCtrl.IsDrawing = true;
            this.WindowCtrl.hWindowControlWPF.HalconWindow.SetColor(color.ToDescriptionOrString());
            this.WindowCtrl.hWindowControlWPF.HalconWindow.DrawCircle(out row, out column, out radius);
            ROICircle roiCircle = new ROICircle();
            roiCircle.createROICircle(column, row, radius);
            this.ROICtrl.AddROI(roiCircle);

            this.WindowCtrl.IsDrawing = false;
        }

        /// <summary>
        /// 删除ROI
        /// </summary>
        public void DeleteROI() => this.ROICtrl.RemoveActiveRoi();

        #endregion 绘制ROI

#pragma warning restore CS0618 // 恢复警告
    }
}