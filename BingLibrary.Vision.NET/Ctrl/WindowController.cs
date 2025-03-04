using BingLibrary.Extension;
using HalconDotNet;
using System.Drawing.Imaging;
using System.Windows.Media;

namespace BingLibrary.Vision
{
    /// <summary>
    /// 窗口显示控制器
    /// </summary>
    public class WindowController
    {
        public HWindowControlWPF hWindowControlWPF;

        public HImage image = new HImage();

        private string WaterString = "Powered by Leader";
        private int WaterFontSize = 36;

        private ROIController roiController;
        private MessageController messageController;
        private DispObjectController dispObjectController;

        //是否正在绘制
        public bool IsDrawing = false;

        //是否显示十字线
        public bool IsShowCross = false;

        //显示水印
        public bool IsShowWaterString = false;

        //是否编辑（移动）roi框
        public bool CanEdit = false;

        //显示模式
        public HalconShowing ShowMode = HalconShowing.margin;

        //仅显示图像
        public bool OnlyShowImage = false;

        //虚线
        public bool DotLine = false;

        //绘制结束方式
        public HalconDrawMode DrawFinishMode = HalconDrawMode.rightButton;

        //拖动模式
        private HalconMouseMode viewMode = HalconMouseMode.View_Move;

        //显示模式
        private HalconMouseMode dispROI = HalconMouseMode.Include_ROI;

        //缩放系数
        private double zoomWndFactor = 1.0;

        //鼠标按下时的坐标
        private double startX, startY;

        //字体
        private HTuple hv_Font = new HTuple();

        private HTuple hv_OS = new HTuple();

        /// <summary>
        /// 底图
        /// </summary>
        private HImage baseImage;

        /// <summary>
        /// 像素网格
        /// </summary>
        private int pixelGridSize = 30;

        public WindowController(HWindowControlWPF windowControlWPF)
        {
            hWindowControlWPF = windowControlWPF;
            zoomWndFactor = (double)imageWidth / hWindowControlWPF.ActualWidth;
            // 启用双缓冲
            RenderOptions.SetBitmapScalingMode(hWindowControlWPF, BitmapScalingMode.HighQuality);
            RenderOptions.SetEdgeMode(hWindowControlWPF, EdgeMode.Aliased);
            RenderOptions.SetCachingHint(hWindowControlWPF, CachingHint.Cache);

            hWindowControlWPF.HMouseDown += ViewPort_HMouseDown;
            hWindowControlWPF.HMouseUp += ViewPort_HMouseUp;
            hWindowControlWPF.HMouseMove += ViewPort_HMouseMove;
            hWindowControlWPF.HMouseWheel += ViewPort_HMouseWheel;
            hWindowControlWPF.SizeChanged += ViewPort_SizeChanged;

            // initFont(hWindowControlWPF.HalconWindow);
            ViewPort_SizeChanged(null, null);
        }

        /// <summary>
        /// 设置ROIController
        /// </summary>
        /// <param name="rc"></param>
        public void SetROIController(ROIController rc)
        {
            roiController = rc;
        }

        /// <summary>
        /// 设置DispObjectController
        /// </summary>
        /// <param name="dc"></param>
        public void SetDispObjectController(DispObjectController dc)
        {
            dispObjectController = dc;
        }

        /// <summary>
        /// 设置MessageController
        /// </summary>
        /// <param name="mc"></param>
        public void SetMessageController(MessageController mc)
        {
            messageController = mc;
        }

        private void ViewPort_HMouseDown(object sender, HMouseEventArgsWPF e)
        {
            //右键或者正在绘画，则不做处理
            if (e.Button == System.Windows.Input.MouseButton.Right || IsDrawing)
                return;

            startX = e.Column;
            startY = e.Row;

            if (roiController != null && dispROI == HalconMouseMode.Include_ROI && CanEdit)
            {
                try
                {
                    int mouse_X0, mouse_Y0;//用来记录按下鼠标时的坐标位置
                    int tempNum = 0;
                    this.hWindowControlWPF.HalconWindow.GetMposition(out mouse_X0, out mouse_Y0, out tempNum);
                    //判断是否在对应的ROI区域内
                    roiController.ActiveROIidx = roiController.MouseDownAction(mouse_Y0, mouse_X0);
                }
                catch { }
            }
        }

        private void ViewPort_HMouseUp(object sender, HMouseEventArgsWPF e)
        {
            if (DrawFinishMode == HalconDrawMode.directly)
                if (e.Button == System.Windows.Input.MouseButton.Left)
                    if (IsDrawing)
                        HalconMicroSoft.FinishDraw();
            // Repaint();
        }

        private void ViewPort_HMouseWheel(object sender, HMouseEventArgsWPF e)
        {
            zoomImage(e.Column, e.Row, e.Delta > 0 ? 0.9 : 1 / 0.9);
        }

        private void ViewPort_HMouseMove(object sender, HMouseEventArgsWPF e)
        {
            //右键或者正在绘画，则不做处理
            if (e.Button == System.Windows.Input.MouseButton.Right || IsDrawing)
                return;
            //
            if (roiController == null || dispROI != HalconMouseMode.Include_ROI)
                return;

            //仅在左键按下时候起作用
            if (e.Button != System.Windows.Input.MouseButton.Left)
            {
                return;
            }

            double motionX, motionY;

            roiController.MouseMoveROI(e.Column, e.Row);//鼠标经过的index
            //可以编辑roi
            if (roiController != null && (roiController.ActiveROIidx != -1) && dispROI == HalconMouseMode.Include_ROI && CanEdit)
            {
                motionX = e.Column - startX;
                motionY = e.Row - startY;

                if (((int)motionX != 0) || ((int)motionY != 0))
                {
                    roiController.MouseMoveAction(e.Column, e.Row, motionX, motionY);

                    startX = e.Column;
                    startY = e.Row;
                }
                Repaint();
            }
            //否则移动图像
            else if (viewMode == HalconMouseMode.View_Move)
            {
                if (startX == 0 && startY == 0)
                    return;

                motionX = e.Column - startX;
                motionY = e.Row - startY;

                if (((int)motionX != 0) || ((int)motionY != 0))
                {
                    moveImage(motionX, motionY);
                    startX = e.Column - motionX;
                    startY = e.Row - motionY;
                }
            }
        }

        //窗口大小变化是图像适应窗口
        //private async void ViewPort_SizeChanged(object sender, System.Windows.SizeChangedEventArgs e)
        //{
        //    await Task.Delay(1);
        //    {
        //        //创建基础位图
        //        Bitmap bitmap = createBaseBitmap((int)hWindowControlWPF.ActualWidth, (int)hWindowControlWPF.ActualHeight);
        //        //基础位图创建成功
        //        if (bitmap != null)
        //        {
        //            //将Bitmap类型转换成HObject类型
        //            bitmap2HObjectBpp24(bitmap, out HObject hObject);
        //            //转换成功
        //            if (hObject != null)
        //            {
        //                //实例化底图
        //                baseImage = new HImage(hObject);
        //            }
        //        }
        //    }

        //    FitImageToWindow();
        //    Repaint();
        //}

        private int imageWidth;
        private int imageHeight;

        private double ImgRow1, ImgCol1, ImgRow2, ImgCol2;

        /// <summary>
        /// 设置图像显示区域
        /// </summary>
        /// <param name="r1"></param>
        /// <param name="c1"></param>
        /// <param name="r2"></param>
        /// <param name="c2"></param>
        private void setImagePart(int r1, int c1, int r2, int c2)
        {
            ImgRow1 = r1;
            ImgCol1 = c1;
            ImgRow2 = imageHeight = r2;
            ImgCol2 = imageWidth = c2;

            System.Windows.Rect rect = hWindowControlWPF.ImagePart;
            rect.X = (int)ImgCol1;
            rect.Y = (int)ImgRow1;
            rect.Height = (int)imageHeight;
            rect.Width = (int)imageWidth;
            hWindowControlWPF.ImagePart = rect;
        }

        /// <summary>
        /// 适应图像
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="scale"></param>
        private void zoomImage(double x, double y, double scale)
        {
            double lengthC, lengthR;
            double percentC, percentR;
            int lenC, lenR;

            percentC = (x - ImgCol1) / (ImgCol2 - ImgCol1);
            percentR = (y - ImgRow1) / (ImgRow2 - ImgRow1);

            lengthC = (ImgCol2 - ImgCol1) * scale;
            lengthR = (ImgRow2 - ImgRow1) * scale;

            if (lengthC <= 16 || lengthR <= 16)
            {
                if (scale <= 1) return;
            }

            ImgCol1 = x - lengthC * percentC;
            ImgCol2 = x + lengthC * (1 - percentC);

            ImgRow1 = y - lengthR * percentR;
            ImgRow2 = y + lengthR * (1 - percentR);

            lenC = (int)Math.Round(lengthC);
            lenR = (int)Math.Round(lengthR);

            System.Windows.Rect rect = hWindowControlWPF.ImagePart;
            rect.X = (int)Math.Round(ImgCol1);
            rect.Y = (int)Math.Round(ImgRow1);
            rect.Width = (lenC > 0) ? lenC : 1;
            rect.Height = (lenR > 0) ? lenR : 1;
            hWindowControlWPF.ImagePart = rect;

            zoomWndFactor *= scale;
            Repaint();
        }

        /// <summary>
        /// 适应窗口
        /// </summary>
        /// <param name="scaleFactor"></param>
        private void zoomImage(double scaleFactor)
        {
            double midPointX, midPointY;

            if (((ImgRow2 - ImgRow1) == scaleFactor * imageHeight) &&
                ((ImgCol2 - ImgCol1) == scaleFactor * imageWidth))
            {
                Repaint();
                return;
            }

            ImgRow2 = ImgRow1 + imageHeight;
            ImgCol2 = ImgCol1 + imageWidth;

            midPointX = ImgCol1;
            midPointY = ImgRow1;

            zoomWndFactor = (double)imageWidth / hWindowControlWPF.ActualWidth;
            zoomImage(midPointX, midPointY, scaleFactor);
        }

        /// <summary>
        /// 移动图像
        /// </summary>
        /// <param name="motionX"></param>
        /// <param name="motionY"></param>
        private void moveImage(double motionX, double motionY)
        {
            ImgRow1 += -motionY;
            ImgRow2 += -motionY;

            ImgCol1 += -motionX;
            ImgCol2 += -motionX;

            System.Windows.Rect rect = hWindowControlWPF.ImagePart;
            rect.X = (int)Math.Round(ImgCol1);
            rect.Y = (int)Math.Round(ImgRow1);
            hWindowControlWPF.ImagePart = rect;

            Repaint();
        }

        private static SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1);

        /// <summary>
        /// 刷新
        /// </summary>
        public async void Repaint()
        {
            await semaphoreSlim.WaitAsync();
            repaint(hWindowControlWPF.HalconWindow);
            await semaphoreSlim.Release();
        }

        //private void repaint(HalconDotNet.HWindow window)
        //{
        //    try
        //    {
        //        if (image == null)
        //        {
        //            if (baseImage != null && baseImage.IsInitialized())
        //            {
        //                HOperatorSet.SetSystem("flush_graphic", "false");
        //                window.ClearWindow();
        //                adaptiveBaseImage();
        //                window.DispObj(baseImage);
        //                HOperatorSet.SetSystem("flush_graphic", "true");
        //                window.WriteString("");
        //            }
        //            return;
        //        }
        //        else if (!image.IsInitialized())
        //        {
        //            if (baseImage != null && baseImage.IsInitialized())
        //            {
        //                HOperatorSet.SetSystem("flush_graphic", "false");
        //                window.ClearWindow();
        //                adaptiveBaseImage();
        //                window.DispObj(baseImage);
        //                HOperatorSet.SetSystem("flush_graphic", "true");
        //                window.WriteString("");
        //            }
        //            return;
        //        }

        //        int h = imageHeight;
        //        if (window.IsInitialized() == false || hWindowControlWPF.HalconID.ToInt64() == -1 || hWindowControlWPF.ImagePart.Width <= 1 || hWindowControlWPF.ImagePart.Height <= 1)
        //            return;

        //        HOperatorSet.SetSystem("flush_graphic", "false");
        //        window.ClearWindow();
        //        //获取左上角和右下角坐标
        //        window.GetPart(out HTuple row1, out HTuple column1, out HTuple row2, out HTuple column2);

        //        if (baseImage != null)
        //        {
        //            adaptiveBaseImage();
        //            window.DispObj(baseImage);
        //        }

        //        window.SetPart(row1, column1, row2, column2);

        //        window.DispObj(image);

        //        try
        //        {
        //            if ((row2 - row1 <= pixelGridSize || column2 - column1 <= pixelGridSize))
        //            {
        //                //绘制像素网格
        //                drawPixelGrid(row1, column1, row2, column2);
        //            }

        //            //(row小于或等于设置的像素网格 || column小于或等于设置的像素网格) && 控件大小未更改 && 显示像素网格
        //            if ((row2 - row1 <= pixelGridSize || column2 - column1 <= pixelGridSize))
        //            {
        //                //显示像素网格
        //                window.SetColor(HalconColors.灰色.ToDescription());
        //                window.DispObj(pixelGrid);
        //            }
        //        }
        //        catch { }

        //        try
        //        {
        //            if (IsShowCross)
        //            {
        //                // window.SetColor(HalconColors.红色.ToDescription());

        //                HTuple s0, w0, h0;
        //                image.GetImagePointer1(out s0, out w0, out h0);
        //                //window.SetLineWidth(3);
        //                //window.DispLine(0, w0 / 2, h0, w0 / 2);
        //                //window.DispLine(h0 / 2, 0, h0 / 2, w0);
        //                //window.SetLineWidth(1);

        //                //显示十字线
        //                window.SetLineWidth(1);
        //                HXLDCont xldCross = new HXLDCont();
        //                window.SetColor(HalconColors.绿色.ToDescription());
        //                HRegion hRegion = new HRegion(0, 0, h0, w0);
        //                HOperatorSet.AreaCenter(
        //                    hRegion,
        //                    out HTuple _Area,
        //                    out HTuple _ROW,
        //                    out HTuple _COL
        //                );
        //                _ROW = h0 / 2;
        //                _COL = w0 / 2;
        //                //小十字
        //                window.DispLine(_ROW - 5, _COL, _ROW + 5, _COL);
        //                window.DispLine(_ROW, _COL - 5, _ROW, _COL + 5);
        //                //中心圆
        //                //window.DispCircle(_ROW, _COL, 35);
        //                //大十字-横
        //                window.DispLine(
        //                    (double)_ROW,
        //                    (double)_COL + 50,
        //                    (double)_ROW,
        //                    (double)_COL * 2
        //                );
        //                window.DispLine(
        //                    (double)_ROW,
        //                    0,
        //                    (double)_ROW,
        //                    (double)_COL - 50
        //                );
        //                //大十字-竖
        //                window.DispLine(
        //                    0,
        //                    (double)_COL,
        //                    (double)_ROW - 50,
        //                    (double)_COL
        //                );
        //                window.DispLine(
        //                    (double)_ROW + 50,
        //                    (double)_COL,
        //                    (double)_ROW * 2,
        //                    (double)_COL
        //                );
        //            }
        //        }
        //        catch { }

        //        if (OnlyShowImage)
        //        {
        //            HSystem.SetSystem("flush_graphic", "true");
        //            window.WriteString("");
        //            return;
        //        }
        //        if (roiController != null && (dispROI == HalconMouseMode.Include_ROI))
        //            roiController.PaintData(window, ShowMode);

        //        window.SetDraw(ShowMode.ToDescription());
        //        window.SetColor(HalconColors.黑色.ToDescription());
        //        window.DispLine(-100.0, -100.0, -101.0, -101.0);

        //        if (IsShowWaterString)
        //        {
        //            try
        //            {
        //                HOperatorSet.SetFont(window, "微软雅黑" + "-" + WaterFontSize.ToString());
        //                HOperatorSet.DispText(window, WaterString, HalconCoordinateSystem.window.ToDescription(), "top",
        //                    "right", "black", "box_color", "#ffffff77");
        //            }
        //            catch { }
        //        }

        //        var dispObjectList = dispObjectController.GetDispObjectList();
        //        for (int i = 0; i < dispObjectList.Count; i++)
        //        {
        //            if (dispObjectList[i].IsShowDotLine)
        //                window.SetLineStyle(new HTuple(10, 10));
        //            window.SetColor(dispObjectList[i].ShowColor.ToDescription());
        //            window.SetDraw(dispObjectList[i].ShowMode.ToDescription());
        //            window.DispObj(dispObjectList[i].ShowObject);

        //            window.SetLineStyle(new HTuple());
        //        }

        //        var messageList = messageController.GetMessageList();

        //        for (int i = 0; i < messageList.Count; i++)
        //        {
        //            try
        //            {
        //                //if ((int)(new HTuple(((hv_OS.TupleSubstr(0, 2))).TupleEqual("Win"))) != 0)
        //                //{
        //                //    using (HDevDisposeHelper dh = new HDevDisposeHelper())
        //                //    {
        //                //        HOperatorSet.SetFont(window, (hv_Font.TupleSelect(0)) + "-" + messageList[i].ShowFontSize.ToString());
        //                //    }
        //                //}
        //                //else
        //                //{
        //                //    using (HDevDisposeHelper dh = new HDevDisposeHelper())
        //                //    {
        //                //        HOperatorSet.SetFont(window, (hv_Font.TupleSelect(0)) + "-" + messageList[i].ShowFontSize.ToString());
        //                //    }
        //                //}
        //                HOperatorSet.SetFont(window, "微软雅黑" + "-" + messageList[i].ShowFontSize.ToString());
        //                HOperatorSet.DispText(window, messageList[i].ShowContent, messageList[i].ShowMode.ToDescription(), new HTuple(messageList[i].PositionX),
        //                  new HTuple(messageList[i].PositionY), messageList[i].ShowColor.ToDescription(), new HTuple("box", "box_color"),
        //                                              new HTuple(messageList[i].ShowBox ? "true" : "false", "#ffffff77"));
        //            }
        //            catch { }
        //        }
        //        try
        //        {
        //            hv_Font.Dispose();
        //            hv_OS.Dispose();
        //        }
        //        catch { }
        //    }
        //    catch { }
        //    HSystem.SetSystem("flush_graphic", "true");
        //    window.WriteString("");
        //}

        private void repaint(HalconDotNet.HWindow window)
        {
            try
            {
                if (image == null || !image.IsInitialized())
                {
                    if (baseImage != null && baseImage.IsInitialized())
                    {
                        HOperatorSet.SetSystem("flush_graphic", "false");
                        window.ClearWindow();
                        adaptiveBaseImage();
                        window.DispObj(baseImage);
                        HOperatorSet.SetSystem("flush_graphic", "true");
                        window.WriteString("");
                    }
                    return;
                }

                if (!window.IsInitialized() || hWindowControlWPF.HalconID.ToInt64() == -1 || hWindowControlWPF.ImagePart.Width <= 1 || hWindowControlWPF.ImagePart.Height <= 1)
                    return;

                HOperatorSet.SetSystem("flush_graphic", "false");
                window.ClearWindow();

                window.GetPart(out HTuple row1, out HTuple column1, out HTuple row2, out HTuple column2);

                if (baseImage != null)
                {
                    adaptiveBaseImage();
                    window.DispObj(baseImage);
                }

                window.SetPart(row1, column1, row2, column2);
                window.DispObj(image);

                drawPixelGridIfNecessary(window, row1, column1, row2, column2);

                drawCrossIfNecessary(window);

                displayWatermarkIfNecessary(window);

                displayRoiAndMessages(window);

                HSystem.SetSystem("flush_graphic", "true");
                window.WriteString("");
            }
            catch (Exception ex)
            {
                // Log the exception for debugging purposes
                Console.WriteLine($"Error in repaint: {ex.Message}");
            }
        }

        private void drawPixelGridIfNecessary(HalconDotNet.HWindow window, HTuple row1, HTuple column1, HTuple row2, HTuple column2)
        {
            if ((row2 - row1 <= pixelGridSize || column2 - column1 <= pixelGridSize))
            {
                drawPixelGrid(row1, column1, row2, column2);
                window.SetColor(HalconColors.灰色.ToDescription());
                window.DispObj(pixelGrid);
            }
        }

        private void drawCrossIfNecessary(HalconDotNet.HWindow window)
        {
            if (IsShowCross)
            {
                HTuple s0, w0, h0;
                image.GetImagePointer1(out s0, out w0, out h0);
                window.SetLineWidth(1);
                window.SetColor(HalconColors.绿色.ToDescription());

                double _ROW = h0 / 2;
                double _COL = w0 / 2;

                window.DispLine(_ROW - 5, _COL, _ROW + 5, _COL);
                window.DispLine(_ROW, _COL - 5, _ROW, _COL + 5);

                window.DispLine(
                    (double)_ROW,
                    (double)_COL + 50,
                    (double)_ROW,
                    (double)_COL * 2
                );
                window.DispLine(
                    (double)_ROW,
                    0,
                    (double)_ROW,
                    (double)_COL - 50
                );

                window.DispLine(
                    0,
                    (double)_COL,
                    (double)_ROW - 50,
                    (double)_COL
                );
                window.DispLine(
                    (double)_ROW + 50,
                    (double)_COL,
                    (double)_ROW * 2,
                    (double)_COL
                );
            }
        }

        private void displayWatermarkIfNecessary(HalconDotNet.HWindow window)
        {
            if (IsShowWaterString)
            {
                try
                {
                    HOperatorSet.SetFont(window, "微软雅黑" + "-" + WaterFontSize.ToString());
                    HOperatorSet.DispText(window, WaterString, HalconCoordinateSystem.window.ToDescription(), "top",
                        "right", "black", "box_color", "#ffffff77");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error displaying watermark: {ex.Message}");
                }
            }
        }

        private void displayRoiAndMessages(HalconDotNet.HWindow window)
        {
            if (roiController != null && dispROI == HalconMouseMode.Include_ROI)
                roiController.PaintData(window, ShowMode);

            var dispObjectList = dispObjectController.GetDispObjectList();
            foreach (var dispObject in dispObjectList)
            {
                if (dispObject.IsShowDotLine)
                    window.SetLineStyle(new HTuple(10, 10));

                window.SetColor(dispObject.ShowColor.ToDescription());
                window.SetDraw(dispObject.ShowMode.ToDescription());
                window.DispObj(dispObject.ShowObject);
                window.SetLineStyle(new HTuple());
            }

            var messageList = messageController.GetMessageList();
            foreach (var message in messageList)
            {
                try
                {
                    HOperatorSet.SetFont(window, "微软雅黑" + "-" + message.ShowFontSize.ToString());
                    HOperatorSet.DispText(window, message.ShowContent, message.ShowMode.ToDescription(), new HTuple(message.PositionX),
                        new HTuple(message.PositionY), message.ShowColor.ToDescription(), new HTuple("box", "box_color"),
                        new HTuple(message.ShowBox ? "true" : "false", "#ffffff77"));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error displaying message: {ex.Message}");
                }
            }
        }

        //private Bitmap createBaseBitmap(int width, int height)
        //{
        //    if (width <= 0 || height <= 0)
        //    {
        //        return null;
        //    }

        //    using (var bitmap = new Bitmap(width, height))
        //    using (var graphics = Graphics.FromImage(bitmap))
        //    using (var brush1 = new SolidBrush(System.Drawing.Color.FromArgb(60, 60, 60)))
        //    using (var brush2 = new SolidBrush(System.Drawing.Color.FromArgb(45, 45, 45)))
        //    {
        //        int size = 10;
        //        for (int row = 0; row < height / size; row++)
        //        {
        //            for (int col = 0; col < width / (size * 2); col++)
        //            {
        //                if (isEven(row))
        //                {
        //                    graphics.FillRectangle(brush1, new Rectangle(col * size * 2, row * size, size, size));
        //                    graphics.FillRectangle(brush2, new Rectangle(col * size * 2 + size, row * size, size, size));
        //                }
        //                else
        //                {
        //                    graphics.FillRectangle(brush1, new Rectangle(col * size * 2 + size, row * size, size, size));
        //                    graphics.FillRectangle(brush2, new Rectangle(col * size * 2, row * size, size, size));
        //                }
        //            }
        //        }
        //        return new Bitmap(bitmap);
        //    }
        //}

        private bool isEven(int num) => num % 2 == 0;

        private void bitmap2HObjectBpp24(Bitmap bmp, out HObject hObject)
        {
            BitmapData bmpData = null;
            try
            {
                bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                {
                    byte[] arrayR = new byte[bmpData.Width * bmpData.Height];
                    byte[] arrayG = new byte[bmpData.Width * bmpData.Height];
                    byte[] arrayB = new byte[bmpData.Width * bmpData.Height];

                    unsafe
                    {
                        byte* pBmp = (byte*)bmpData.Scan0;
                        for (int r = 0; r < bmpData.Height; r++)
                        {
                            for (int c = 0; c < bmpData.Width; c++)
                            {
                                byte* pBase = pBmp + bmpData.Stride * r + c * 3;
                                arrayR[r * bmpData.Width + c] = *(pBase + 2);
                                arrayG[r * bmpData.Width + c] = *(pBase + 1);
                                arrayB[r * bmpData.Width + c] = *(pBase);
                            }
                        }

                        fixed (byte* pR = arrayR, pG = arrayG, pB = arrayB)
                        {
                            HOperatorSet.GenImage3(out hObject, "byte", bmpData.Width, bmpData.Height, new IntPtr(pR), new IntPtr(pG), new IntPtr(pB));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error converting bitmap to HObject: {ex.Message}");
                hObject = null;
            }
            finally
            {
                if (bmpData != null && bmp != null)
                {
                    bmp.UnlockBits(bmpData);
                }
            }
        }

        private void ViewPort_SizeChanged(object sender, System.Windows.SizeChangedEventArgs e)
        {
            // 创建基础位图并适应窗口
            using (var bitmap = createBaseBitmap((int)hWindowControlWPF.ActualWidth, (int)hWindowControlWPF.ActualHeight))
            {
                if (bitmap != null)
                {
                    bitmap2HObjectBpp24(bitmap, out HObject hObject);
                    if (hObject != null)
                    {
                        baseImage = new HImage(hObject);
                    }
                }
            }

            FitImageToWindow();
            Repaint();
        }

        /// <summary>
        /// 显示水印
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="fontSize"></param>
        /// <param name="isShow"></param>
        public void ShowWaterStringToWindow(string msg, int fontSize = 36, bool isShow = true)
        {
            WaterString = msg;
            WaterFontSize = fontSize;
            IsShowWaterString = isShow;
        }

        private bool isFirstShowImage = true;

        /// <summary>
        /// 图像显示到窗口
        /// </summary>
        /// <param name="image"></param>
        public void ShowImageToWindow(HImage image)
        {
            this.image?.Dispose();
            this.image = image;

            if (isFirstShowImage)
            {
                isFirstShowImage = false;
                FitImageToWindow();
            }
            else
                Repaint();
        }

        /// <summary>
        /// 图像显示适应窗口
        /// </summary>
        public void FitImageToWindow()
        {
            try
            {
                if (image != null)
                {
                    if (!image.IsInitialized())
                        return;
                    int h, w;
                    string s;

                    image.GetImagePointer1(out s, out w, out h);

                    if ((h != imageHeight) || (w != imageWidth))
                    {
                        int _beginRow, _begin_Col, _endRow, _endCol;
                        double ratio_win = (double)hWindowControlWPF.ActualWidth / (double)hWindowControlWPF.ActualHeight;
                        double ratio_img = (double)w / (double)h;
                        imageHeight = h;
                        imageWidth = w;
                        if (ratio_win >= ratio_img)
                        {
                            _beginRow = 0;
                            _endRow = h - 1;
                            _begin_Col = (int)(-w * (ratio_win / ratio_img - 1d) / 2d);
                            _endCol = (int)(w + w * (ratio_win / ratio_img - 1d) / 2d);
                            zoomWndFactor = (double)h / hWindowControlWPF.ActualHeight;
                        }
                        else
                        {
                            _begin_Col = 0;
                            _endCol = w - 1;
                            _beginRow = (int)(-h * (ratio_img / ratio_win - 1d) / 2d);
                            _endRow = (int)(h + h * (ratio_img / ratio_win - 1d) / 2d);
                            zoomWndFactor = (double)w / hWindowControlWPF.ActualWidth;
                        }
                        setImagePart(_beginRow, _begin_Col, (int)hWindowControlWPF.ActualHeight, (int)hWindowControlWPF.ActualWidth);
                        zoomImage(zoomWndFactor);
                    }
                }
            }
            catch { }
        }

        #region 背景方格

        /// <summary>
        /// 创建基础位图
        /// </summary>
        /// <param name="width">图像宽</param>
        /// <param name="height">图像高</param>
        /// <returns>创建成功返回位图，创建失败返回null</returns>
        private Bitmap createBaseBitmap(int width, int height, int size = 10)
        {
            if (width <= 0 || height <= 0)
            {
                return null;
            }

            try
            {
                using (Bitmap bitmap = new Bitmap(width, height))
                using (Graphics graphics = Graphics.FromImage(bitmap))
                using (SolidBrush brush1 = new SolidBrush(System.Drawing.Color.FromArgb(60, 60, 60)))
                using (SolidBrush brush2 = new SolidBrush(System.Drawing.Color.FromArgb(45, 45, 45)))
                {
                    for (int row = 0; row * size < height; row++)
                    {
                        for (int column = 0; column * size * 2 < width; column++)
                        {
                            if (isEven(row))
                            {
                                graphics.FillRectangle(brush1, new Rectangle(column * size * 2, row * size, size, size));
                                graphics.FillRectangle(brush2, new Rectangle(column * size * 2 + size, row * size, size, size));
                            }
                            else
                            {
                                graphics.FillRectangle(brush1, new Rectangle(column * size * 2 + size, row * size, size, size));
                                graphics.FillRectangle(brush2, new Rectangle(column * size * 2, row * size, size, size));
                            }
                        }
                    }
                    return (Bitmap)bitmap.Clone();
                }
            }
            catch (Exception ex)
            {
                // Log the exception or handle it as needed
                Console.WriteLine($"An error occurred: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 自适应底图
        /// </summary>
        private void adaptiveBaseImage()
        {
            baseImage.GetImageSize(out int imageWide, out int imageHigh);
            hWindowControlWPF.HalconWindow.SetPart(0, 0, (int)(hWindowControlWPF.ActualHeight - 1), (int)(hWindowControlWPF.ActualWidth - 1));
        }

        #endregion 背景方格

        #region 绘制像素网格

        /// <summary>
        /// 像素网格
        /// </summary>
        private HXLDCont pixelGrid = new HXLDCont();

        /// <summary>
        /// 绘制像素网格
        /// </summary>
        /// <param name="row1">左上角row</param>
        /// <param name="column1">左上角column</param>
        /// <param name="row2">右下角row</param>
        /// <param name="column2">右下角column</param>
        private void drawPixelGrid(double row1, double column1, double row2, double column2)
        {
            // 输入参数验证
            if (row1 > row2 || column1 > column2)
            {
            }

            try
            {
                // 计算边界值，确保不会超出合理范围
                int startRow = (int)Math.Floor(row1 - 1);
                int startColumn = (int)Math.Floor(column1 - 1);
                int endRow = (int)Math.Ceiling(row2 + 1);
                int endColumn = (int)Math.Ceiling(column2 + 1);

                // 初始化空对象
                pixelGrid.GenEmptyObj();

                // 创建水平线段
                List<HXLDCont> horizontalSegments = new List<HXLDCont>();
                for (int i = startRow; i < endRow; i++)
                {
                    double beginRow = i + 0.5;
                    double beginCol = startColumn;
                    double endRowValue = i + 0.5;
                    double endCol = endColumn + 0.5;
                    horizontalSegments.Add(new HXLDCont(new HTuple(beginRow, endRowValue), new HTuple(beginCol, endCol)));
                }

                // 创建垂直线段
                List<HXLDCont> verticalSegments = new List<HXLDCont>();
                for (int i = startColumn; i < endColumn; i++)
                {
                    double beginRow = startRow;
                    double beginCol = i + 0.5;
                    double endRowValue = endRow + 0.5;
                    double endCol = i + 0.5;
                    verticalSegments.Add(new HXLDCont(new HTuple(beginRow, endRowValue), new HTuple(beginCol, endCol)));
                }

                // 拼接所有线段
                foreach (var segment in horizontalSegments)
                {
                    pixelGrid = pixelGrid.ConcatObj(segment);
                }
                foreach (var segment in verticalSegments)
                {
                    pixelGrid = pixelGrid.ConcatObj(segment);
                }
            }
            catch (Exception ex)
            {
                // 异常处理
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }

        #endregion 绘制像素网格
    }
}