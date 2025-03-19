using HalconDotNet;
using Newtonsoft.Json;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using Button = System.Windows.Controls.Button;
using CheckBox = System.Windows.Controls.CheckBox;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using Point = System.Windows.Point;
using Size = System.Windows.Size;
using UserControl = System.Windows.Controls.UserControl;

namespace BingLibrary.Vision
{
    /// <summary>
    /// BingImageWindow.xaml 的交互逻辑
    /// </summary>
    public partial class BingImageWindow : UserControl
    {
        public BingImageWindowData windowData = new BingImageWindowData();

        private Config config = new Config();

        public BingImageWindow()
        {
            InitializeComponent();
            Loaded += BingImageWindow_Loaded;
        }

        public static readonly DependencyProperty WindowDataProperty =
             DependencyProperty.Register("WindowData", //属性名字
             typeof(BingImageWindowData), //属性类型
             typeof(BingImageWindow),//属性所属，及属性所有者
             new PropertyMetadata(new PropertyChangedCallback((d, e) =>
             {
                 var window = (BingImageWindow)d;
                 // 手动更新绑定或执行其他逻辑
                 window.DataContext = e.NewValue; // 如果 DataContext 依赖于此属性
             })));//属性默认值

        public BingImageWindowData WindowData
        {
            get
            {
                return (BingImageWindowData)GetValue(WindowDataProperty);
            }
            set { SetValue(WindowDataProperty, value); }
        }

        private void BingImageWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // 获取父窗口
            var parentWindow = Window.GetWindow(this);
            if (parentWindow != null)
            {
                // 订阅父窗口的位置和大小变化事件
                parentWindow.LocationChanged += parentWindow_LocationChanged;
                parentWindow.SizeChanged += parentWindow_SizeChanged;
            }
        }

        private void parentWindow_LocationChanged(object sender, EventArgs e)
        {
            updatePopupPosition();
        }

        private async void parentWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            updatePopupPosition();
            await Task.Delay(10);
            try
            {
                windowData.FitImage();
                windowData.RefreshWindow();
            }
            catch { }
        }

        private void updatePopupPosition()
        {
            var targetRect = grid.TransformToAncestor(this).TransformBounds(new Rect(0, 0, grid.ActualWidth, grid.ActualHeight));

            double left = targetRect.Left;  // 目标左边缘
            double top = targetRect.Bottom;  // 目标底部
        }

        private string OpenImageDialog()
        {
            System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog();
            openFileDialog.Title = "选择文件";
            openFileDialog.Filter = "所有文件|*.*|Tiff文件|*.tif|BMP文件|*.bmp|Jpeg文件|*.jpg";
            openFileDialog.FileName = string.Empty;
            openFileDialog.FilterIndex = 1;
            openFileDialog.RestoreDirectory = true;
            openFileDialog.DereferenceLinks = false;
            openFileDialog.AutoUpgradeEnabled = true;
            System.Windows.Forms.DialogResult result = openFileDialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.Cancel)
            {
                return "";
            }
            string fileName = openFileDialog.FileName;
            return fileName;
        }

        private string SaveImageDialog()
        {
            System.Windows.Forms.SaveFileDialog saveFileDialog = new System.Windows.Forms.SaveFileDialog();
            saveFileDialog.Title = "选择文件";
            saveFileDialog.Filter = "Tiff文件|*.tif|BMP文件|*.bmp|Jpeg文件|*.jpg|所有文件|*.*";
            saveFileDialog.FileName = string.Empty;
            saveFileDialog.FilterIndex = 1;
            saveFileDialog.RestoreDirectory = true;
            saveFileDialog.FileName = $"BV_{System.DateTime.Now.ToString("yyyy-MM-dd_HH_mm_ss_ffff")}";
            System.Windows.Forms.DialogResult result = saveFileDialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.Cancel)
            {
                return "";
            }
            string fileName = saveFileDialog.FileName;
            return fileName;
        }

        private void iwin_HInitWindow(object sender, EventArgs e)
        {
            try
            {
                config = Serialize.ReadJson<Config>(System.AppDomain.CurrentDomain.BaseDirectory + this.Name + ".Config");
            }
            catch { config = new Config(); }

            HOperatorSet.ResetObjDb(4096, 4096, 1);
            HOperatorSet.SetSystem("clip_region", "false");
            HOperatorSet.SetSystem("store_empty_region", "true");
            iwin.HMouseUp += (sender0, e0) => { if (e0.Button == MouseButton.Right && windowData.WindowCtrl.IsDrawing == false) CM.IsOpen = true; };

            windowData.Init(iwin);
            windowData.WindowCtrl.IsShowWaterString = config.IsShowWaterString;
            windowData.WindowCtrl.ShowMode = config.IsShowMargin ? HalconShowing.margin : HalconShowing.fill;
            windowData.WindowCtrl.CanEdit = config.IsEdit;
            windowData.WindowCtrl.DrawFinishMode = config.IsDrawFinish ? HalconDrawMode.rightButton : HalconDrawMode.directly;
            windowData.WindowCtrl.IsShowCross = config.IsShowCross;
            windowData.WindowCtrl.OnlyShowImage = config.OnlyShowImage;
            windowData.WindowCtrl.DotLine = config.DotLine;
            HOperatorSet.ClearWindow(iwin.HalconWindow);
            windowData.WindowCtrl.Repaint();

            //HOperatorSet.SetWindowParam(iwin.HalconWindow, "background_color",
            //    config.ColorIndex == 0 ? "white"
            //   : config.ColorIndex == 1 ? "black"
            //   : config.ColorIndex == 2 ? "gray"
            //   : config.ColorIndex == 3 ? "orange"
            //   : config.ColorIndex == 4 ? "coral"
            //   : config.ColorIndex == 5 ? "red"
            //   : config.ColorIndex == 6 ? "spring green"
            //   : config.ColorIndex == 7 ? "cadet blue"
            //   : config.ColorIndex == 8 ? "indian red"
            //   : config.ColorIndex == 9 ? "dark olive green"

            //   : "black");
            //HOperatorSet.SetWindowParam(iwin.HalconWindow, "background_color", "#1e1e1e");

            c1.IsChecked = config.IsShowWaterString;
            c2.IsChecked = config.IsShowMargin;
            c4.IsChecked = config.IsEdit;
            c5.IsChecked = config.IsDrawFinish;
            c10.IsChecked = config.IsShowCross;
            c20.IsChecked = config.OnlyShowImage;
            c21.IsChecked = config.DotLine;
            colorSet.SelectedIndex = config.ColorIndex;

            m9.IsChecked = config.IsShowMargin;
            m10.IsChecked = config.IsShowWaterString;
            m11.IsChecked = config.IsShowCross;

            Pop.CustomPopupPlacementCallback = new CustomPopupPlacementCallback(placePopup);
            updatePopupPosition();
            WindowData = windowData;
        }

        //public BackGroundColor WindowBackgroud
        //{
        //    get { return (BackGroundColor)GetValue(WindowBackgroudProperty); }
        //    set { SetValue(WindowBackgroudProperty, value); }
        //}

        //public static readonly DependencyProperty WindowBackgroudProperty =
        // DependencyProperty.Register("WindowBackgroud", //属性名字
        // typeof(BackGroundColor), //属性类型
        // typeof(BingImageWindow));//属性所属，及属性所有者

        private void Read_Image(object sender, RoutedEventArgs e)
        {
            try
            {
                var rst = OpenImageDialog();
                if (rst != "")
                {
                    windowData.DisplayImage(new HImage(rst));
                    windowData.RefreshWindow();
                }
            }
            catch { }
        }

        private void Write_Image(object sender, RoutedEventArgs e)
        {
            try
            {
                var rst = SaveImageDialog();
                if (rst != "")
                {
                    string extension = Path.GetExtension(rst);
                    if (rst.Contains("tif"))
                        windowData.WindowCtrl.image.WriteImage("tiff", new HTuple(0), new HTuple(rst));
                    else if (rst.Contains("bmp"))
                        windowData.WindowCtrl.image.WriteImage("bmp", new HTuple(0), new HTuple(rst));
                    else if (rst.Contains("jpg"))
                        windowData.WindowCtrl.image.WriteImage("jpge", new HTuple(0), new HTuple(rst));
                }
            }
            catch { }
        }

        private void Write_Window_Image(object sender, RoutedEventArgs e)
        {
            try
            {
                var rst = SaveImageDialog();
                if (rst != "")
                {
                    string extension = Path.GetExtension(rst);
                    if (rst.Contains("tif"))
                        windowData.WindowCtrl.hWindowControlWPF.HalconWindow.DumpWindowImage().WriteImage("tiff", new HTuple(0), new HTuple(rst));
                    else if (rst.Contains("bmp"))
                        windowData.WindowCtrl.hWindowControlWPF.HalconWindow.DumpWindowImage().WriteImage("bmp", new HTuple(0), new HTuple(rst));
                    else if (rst.Contains("jpg"))
                        windowData.WindowCtrl.hWindowControlWPF.HalconWindow.DumpWindowImage().WriteImage("jpge", new HTuple(0), new HTuple(rst));
                }
            }
            catch { }
        }

        private void Fit_Window(object sender, RoutedEventArgs e)
        {
            try
            {
                windowData.FitImage();
                windowData.RefreshWindow();
            }
            catch { }
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            HOperatorSet.SetWindowParam(iwin.HalconWindow, "background_color", "white");
            HOperatorSet.ClearWindow(iwin.HalconWindow);
            windowData.WindowCtrl.Repaint();
        }

        private void MenuItem_Click_1(object sender, RoutedEventArgs e)
        {
            HOperatorSet.SetWindowParam(iwin.HalconWindow, "background_color", "black");
            HOperatorSet.ClearWindow(iwin.HalconWindow);
            windowData.WindowCtrl.Repaint();
        }

        private void MenuItem_Click_2(object sender, RoutedEventArgs e)
        {
            HOperatorSet.SetWindowParam(iwin.HalconWindow, "background_color", "gray");
            HOperatorSet.ClearWindow(iwin.HalconWindow);
            windowData.WindowCtrl.Repaint();
        }

        private void MenuItem_Click_3(object sender, RoutedEventArgs e)
        {
            HOperatorSet.SetWindowParam(iwin.HalconWindow, "background_color", "orange");
            HOperatorSet.ClearWindow(iwin.HalconWindow);
            windowData.WindowCtrl.Repaint();
        }

        private void MenuItem_Click_4(object sender, RoutedEventArgs e)
        {
            HOperatorSet.SetWindowParam(iwin.HalconWindow, "background_color", "coral");
            HOperatorSet.ClearWindow(iwin.HalconWindow);
            windowData.WindowCtrl.Repaint();
        }

        private void MenuItem_Click_5(object sender, RoutedEventArgs e)
        {
            HOperatorSet.SetWindowParam(iwin.HalconWindow, "background_color", "red");
            HOperatorSet.ClearWindow(iwin.HalconWindow);
            windowData.WindowCtrl.Repaint();
        }

        private void MenuItem_Click_6(object sender, RoutedEventArgs e)
        {
            HOperatorSet.SetWindowParam(iwin.HalconWindow, "background_color", "spring green");
            HOperatorSet.ClearWindow(iwin.HalconWindow);
            windowData.WindowCtrl.Repaint();
        }

        private void MenuItem_Click_7(object sender, RoutedEventArgs e)
        {
            HOperatorSet.SetWindowParam(iwin.HalconWindow, "background_color", "cadet blue");
            HOperatorSet.ClearWindow(iwin.HalconWindow);
            windowData.WindowCtrl.Repaint();
        }

        private void MenuItem_Click_8(object sender, RoutedEventArgs e)
        {
            HOperatorSet.SetWindowParam(iwin.HalconWindow, "background_color", "indian red");
            HOperatorSet.ClearWindow(iwin.HalconWindow);
            windowData.WindowCtrl.Repaint();
        }

        private void MenuItem_Click_9(object sender, RoutedEventArgs e)
        {
            windowData.WindowCtrl.ShowMode = (sender as MenuItem).IsChecked == true ? HalconShowing.margin : HalconShowing.fill;
            windowData.WindowCtrl.Repaint();

            config.IsShowMargin = (sender as MenuItem).IsChecked == true;
            Serialize.WriteJson(config, System.AppDomain.CurrentDomain.BaseDirectory + this.Name + ".Config");
        }

        private void MenuItem_Click_10(object sender, RoutedEventArgs e)
        {
            windowData.WindowCtrl.IsShowWaterString = (sender as MenuItem).IsChecked;
            windowData.WindowCtrl.Repaint();
            config.IsShowWaterString = windowData.WindowCtrl.IsShowWaterString;
            Serialize.WriteJson(config, System.AppDomain.CurrentDomain.BaseDirectory + this.Name + ".Config");
        }

        private int mouse_X0_old = 0, mouse_Y0_old = 0;

        private System.Threading.SemaphoreSlim showPopSS = new System.Threading.SemaphoreSlim(1);

        private void iwin_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            mouse_X0_old = 0; mouse_Y0_old = 0;
            Pop.IsOpen = false;
            isPressed = false;
        }

        private bool isPressed = false;

        private async void iwin_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            isPressed = true;
            if (await showPopSS.WaitAsync(0))
            {
                while (isPressed == true)
                {
                    await System.Threading.Tasks.Task.Delay(20);
                    int mouse_X0 = 0, mouse_Y0 = 0;//用来记录按下鼠标时的坐标位置
                    if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                    {
                        Pop.IsOpen = true;
                        try
                        {
                            int tempNum = 0;
                            windowData.WindowCtrl.hWindowControlWPF.HalconWindow.GetMposition(out mouse_X0, out mouse_Y0, out tempNum);

                            if (mouse_X0_old == mouse_X0 && mouse_Y0_old == mouse_Y0)
                            {
                                //showPopSS.Release();
                                //return;
                            }
                            else
                            {
                                mouse_X0_old = mouse_X0; mouse_Y0_old = mouse_Y0;
                                var gray = windowData.WindowCtrl.image.GetGrayval(mouse_X0, mouse_Y0);
                                HTuple w, h;
                                windowData.WindowCtrl.image.GetImageSize(out w, out h);
                                zb.Text = "坐标：" + mouse_X0 + " , " + mouse_Y0;
                                try
                                {
                                    hd.Text = "灰度：" + gray[0] + " , " + gray[1] + " , " + gray[2];
                                }
                                catch
                                {
                                    hd.Text = "灰度：" + gray[0];
                                }

                                try
                                {
                                    HTuple hv_R = new HTuple(), hv_G = new HTuple();
                                    HTuple hv_B = new HTuple(), hv_Min = new HTuple(), hv_Max = new HTuple();
                                    HTuple hv_V = new HTuple(), hv_S = new HTuple(), hv_H = new HTuple();
                                    // Initialize local and output iconic variables
                                    hv_R.Dispose();
                                    hv_R = (int)gray[0];
                                    hv_G.Dispose();
                                    hv_G = (int)gray[1];
                                    hv_B.Dispose();
                                    hv_B = (int)gray[2];

                                    hv_Min.Dispose();
                                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                                    {
                                        hv_Min = ((((hv_R.TupleConcat(
                                            hv_G))).TupleConcat(hv_B))).TupleMin();
                                    }
                                    hv_Max.Dispose();
                                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                                    {
                                        hv_Max = ((((hv_R.TupleConcat(
                                            hv_G))).TupleConcat(hv_B))).TupleMax();
                                    }
                                    hv_V.Dispose();
                                    hv_V = new HTuple(hv_Max);
                                    if ((int)(new HTuple(hv_Max.TupleEqual(hv_Min))) != 0)
                                    {
                                        hv_S.Dispose();
                                        hv_S = 0;
                                        hv_H.Dispose();
                                        hv_H = 0;
                                    }
                                    else
                                    {
                                        hv_S.Dispose();
                                        using (HDevDisposeHelper dh = new HDevDisposeHelper())
                                        {
                                            hv_S = (hv_Max.D - hv_Min.D) / (1.0 * hv_Max.D);
                                        }
                                        if ((int)(new HTuple(hv_R.TupleEqual(hv_Max))) != 0)
                                        {
                                            hv_H.Dispose();
                                            using (HDevDisposeHelper dh = new HDevDisposeHelper())
                                            {
                                                hv_H = ((hv_G.D - hv_B.D) / (hv_Max.D - hv_Min.D)) * ((new HTuple(60)).TupleRad()
                                                    );
                                            }
                                        }
                                        else if ((int)(new HTuple(hv_G.TupleEqual(hv_Max))) != 0)
                                        {
                                            hv_H.Dispose();
                                            using (HDevDisposeHelper dh = new HDevDisposeHelper())
                                            {
                                                hv_H = (2 + ((hv_B.D - hv_R.D) / (hv_Max.D - hv_Min.D))) * ((new HTuple(60)).TupleRad()
                                                    );
                                            }
                                        }
                                        else if ((int)(new HTuple(hv_B.TupleEqual(hv_Max))) != 0)
                                        {
                                            hv_H.Dispose();
                                            using (HDevDisposeHelper dh = new HDevDisposeHelper())
                                            {
                                                hv_H = (4 + ((hv_R.D - hv_G.D) / (hv_Max.D - hv_Min.D))) * ((new HTuple(60)).TupleRad()
                                                    );
                                            }
                                        }
                                    }

                                    hv_H = (int)(hv_H.D / 2.0 / Math.PI * 255);
                                    if (hv_H < 0)
                                    {
                                        hv_H = 255 + hv_H;
                                    }
                                    hv_S = (int)(hv_S.D * 255);

                                    hsv.Text = "HSV：" + hv_H + " , " + hv_S + " , " + hv_V;
                                }
                                catch
                                {
                                    hsv.Text = "HSV：" + gray[0];
                                }

                                sd.Text = "尺寸：" + w.D + " * " + h.D;
                            }
                        }
                        catch
                        {
                            sd.Text = "请确认鼠标位置";
                            zb.Text = "";
                            hd.Text = "";
                            hsv.Text = "";
                        }
                    }

                    try
                    {
                        windowData.WindowCtrl.hWindowControlWPF.HalconWindow.GetPart(out HTuple row1, out HTuple column1, out HTuple row2, out HTuple column2);

                        //获取可见部分最大像素
                        double partMaxPixels = row2 - row1 > column2 - column1 ? row2 - row1 : column2 - column1;
                        //计算十字架大小
                        HTuple size = new HTuple(partMaxPixels / 24);
                        if (size[0].D < 0.2)
                        {
                            size[0].D = 0.2;
                        }

                        HXLDCont rectangle2 = new HXLDCont();
                        rectangle2.GenRectangle2ContourXld(mouse_X0, mouse_Y0, 0, size, size);
                        windowData.RefreshWindow();

                        windowData.WindowCtrl.hWindowControlWPF.HalconWindow.SetColor("#22b15c");
                        windowData.WindowCtrl.hWindowControlWPF.HalconWindow.SetLineWidth(4);
                        windowData.WindowCtrl.hWindowControlWPF.HalconWindow.DispLine(mouse_X0 - size * 20, mouse_Y0, mouse_X0 - size, mouse_Y0);
                        windowData.WindowCtrl.hWindowControlWPF.HalconWindow.DispLine(mouse_X0 + size, mouse_Y0, mouse_X0 + size * 20, mouse_Y0);
                        windowData.WindowCtrl.hWindowControlWPF.HalconWindow.DispLine(mouse_X0, mouse_Y0 - size * 20, mouse_X0, mouse_Y0 - size);
                        windowData.WindowCtrl.hWindowControlWPF.HalconWindow.DispLine(mouse_X0, mouse_Y0 + size, mouse_X0, mouse_Y0 + size * 20);

                        windowData.WindowCtrl.hWindowControlWPF.HalconWindow.DispObj(rectangle2);
                        windowData.WindowCtrl.hWindowControlWPF.HalconWindow.SetLineWidth(4);
                        windowData.WindowCtrl.hWindowControlWPF.HalconWindow.SetColor("red");
                        windowData.WindowCtrl.hWindowControlWPF.HalconWindow.DispLine(mouse_X0 - size / 2, mouse_Y0, mouse_X0 + size / 2, mouse_Y0);
                        windowData.WindowCtrl.hWindowControlWPF.HalconWindow.DispLine(mouse_X0, mouse_Y0 - size / 2, mouse_X0, mouse_Y0 + size / 2);
                        windowData.WindowCtrl.hWindowControlWPF.HalconWindow.SetLineWidth(1);
                        if (mouse_X0 == 0 && mouse_Y0 == 0)
                        {
                            windowData.RefreshWindow();
                        }
                    }
                    catch { }
                }

                showPopSS.Release();
            }
        }

        public CustomPopupPlacement[] placePopup(Size popupSize,
                                           Size targetSize,
                                           Point offset)
        {
            CustomPopupPlacement placement1 =
               new CustomPopupPlacement(new Point(10, 10), PopupPrimaryAxis.Vertical);

            CustomPopupPlacement placement2 =
                new CustomPopupPlacement(new Point(10, 10), PopupPrimaryAxis.Horizontal);

            CustomPopupPlacement[] ttplaces =
                    new CustomPopupPlacement[] { placement1, placement2 };
            return ttplaces;
        }

        private void MenuItem_Click_11(object sender, RoutedEventArgs e)
        {
            config.IsShowCross = (sender as MenuItem).IsChecked;
            windowData.WindowCtrl.IsShowCross = config.IsShowCross;
            Serialize.WriteJson(config, System.AppDomain.CurrentDomain.BaseDirectory + this.Name + ".Config");
            windowData.WindowCtrl.Repaint();
        }

        private void MenuItem_Click_12(object sender, RoutedEventArgs e)
        {
            bool rst = (sender as MenuItem).IsChecked;
            if (rst)
                more.Width = 200;
            else
                more.Width = 0;
        }

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            windowData.WindowCtrl.IsShowWaterString = (sender as CheckBox).IsChecked == true;
            windowData.WindowCtrl.Repaint();
            config.IsShowWaterString = windowData.WindowCtrl.IsShowWaterString;
            Serialize.WriteJson(config, System.AppDomain.CurrentDomain.BaseDirectory + this.Name + ".Config");
        }

        private void CheckBox_Click_1(object sender, RoutedEventArgs e)
        {
            windowData.WindowCtrl.ShowMode = (sender as CheckBox).IsChecked == true ? HalconShowing.margin : HalconShowing.fill;
            windowData.WindowCtrl.Repaint();
            config.IsShowMargin = (sender as CheckBox).IsChecked == true;
            Serialize.WriteJson(config, System.AppDomain.CurrentDomain.BaseDirectory + this.Name + ".Config");
        }

        private void CheckBox_Click_2(object sender, RoutedEventArgs e)
        {
        }

        private void colorSet_DropDownClosed(object sender, EventArgs e)
        {
            HOperatorSet.SetWindowParam(iwin.HalconWindow, "background_color",
                colorSet.SelectedIndex == 0 ? "white"
                : colorSet.SelectedIndex == 1 ? "black"
                : colorSet.SelectedIndex == 2 ? "gray"
                : colorSet.SelectedIndex == 3 ? "orange"
                : colorSet.SelectedIndex == 4 ? "coral"
                : colorSet.SelectedIndex == 5 ? "red"
                : colorSet.SelectedIndex == 6 ? "spring green"
                : colorSet.SelectedIndex == 7 ? "cadet blue"
                : colorSet.SelectedIndex == 8 ? "indian red"
                : colorSet.SelectedIndex == 9 ? "dark olive green"

                : "black");
            HOperatorSet.ClearWindow(iwin.HalconWindow);
            windowData.WindowCtrl.Repaint();
            config.ColorIndex = colorSet.SelectedIndex;
            Serialize.WriteJson(config, System.AppDomain.CurrentDomain.BaseDirectory + this.Name + ".Config");
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            (sender as System.Windows.Controls.Button).IsEnabled = false;

            windowData.WindowCtrl.IsDrawing = true;

            if (cb.SelectedIndex == 0)
            {
                ROIRegion rr0 = new ROIRegion(windowData.WindowCtrl.hWindowControlWPF.HalconWindow.DrawRegion());
                rr0.draw(iwin.HalconWindow);
                rr0.ROIName = getRoiName();
                windowData.ROICtrl.ROIList.Add(rr0);
            }
            else if (cb.SelectedIndex == 1)
            {
                ROILine rl1 = new ROILine();
                double r1, c1, r2, c2;
                windowData.WindowCtrl.hWindowControlWPF.HalconWindow.DrawLine(out r1, out c1, out r2, out c2);
                rl1.createROILine(r1, c1, r2, c2);
                rl1.draw(iwin.HalconWindow);
                rl1.ROIName = getRoiName();
                windowData.ROICtrl.ROIList.Add(rl1);
            }
            else if (cb.SelectedIndex == 2)
            {
                ROICircle rc2 = new ROICircle();
                double r1, c1, r;
                windowData.WindowCtrl.hWindowControlWPF.HalconWindow.DrawCircle(out r1, out c1, out r);
                rc2.createROICircle(c1, r1, r);
                rc2.draw(iwin.HalconWindow);
                rc2.ROIName = getRoiName();
                windowData.ROICtrl.ROIList.Add(rc2);
            }
            else if (cb.SelectedIndex == 3)
            {
                ROIRectangle1 rr3 = new ROIRectangle1();
                double r1, c1, r2, c2;
                windowData.WindowCtrl.hWindowControlWPF.HalconWindow.DrawRectangle1(out r1, out c1, out r2, out c2);
                rr3.createROIRect1(r1, c1, r2, c2);
                rr3.draw(iwin.HalconWindow);
                rr3.ROIName = getRoiName();
                windowData.ROICtrl.ROIList.Add(rr3);
            }
            else if (cb.SelectedIndex == 4)
            {
                ROIRectangle2 rr4 = new ROIRectangle2();
                double r1, c1, p, l1, l2;
                windowData.WindowCtrl.hWindowControlWPF.HalconWindow.DrawRectangle2(out c1, out r1, out p, out l1, out l2);
                rr4.createROIRect2(r1, c1, -p, l1, l2);
                rr4.draw(iwin.HalconWindow);
                rr4.ROIName = getRoiName();
                windowData.ROICtrl.ROIList.Add(rr4);
            }

            windowData.WindowCtrl.IsDrawing = false;
            (sender as Button).IsEnabled = true;
        }

        private string getRoiName()
        {
            string name = "0";
            int k = 0;
            for (int j = 0; j < 1024; j++)
            {
                for (int i = 0; i < windowData.ROICtrl.ROIList.Count; i++)
                {
                    if (windowData.ROICtrl.ROIList[i].ROIName == k.ToString())
                    { k++; break; }
                }
            }
            return name = k.ToString();
        }

        private void c4_Click(object sender, RoutedEventArgs e)
        {
            windowData.WindowCtrl.CanEdit = (sender as CheckBox).IsChecked == true;
            windowData.WindowCtrl.Repaint();
            config.IsEdit = (sender as CheckBox).IsChecked == true;
            Serialize.WriteJson(config, System.AppDomain.CurrentDomain.BaseDirectory + this.Name + ".Config");
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            windowData.ROICtrl.RemoveActiveRoi();
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            (sender as Button).IsEnabled = false;
            windowData.WindowCtrl.IsDrawing = true;

            HRegion region = windowData.WindowCtrl.hWindowControlWPF.HalconWindow.DrawRegion();
            ROIRegion roi = new ROIRegion(region);

            roi.draw(iwin.HalconWindow);
            roi.ROIName = getRoiName();
            windowData.ROICtrl.ROIList.Add(roi);

            windowData.WindowCtrl.IsDrawing = false;
            (sender as Button).IsEnabled = true;
        }

        private void c5_Click(object sender, RoutedEventArgs e)
        {
            windowData.WindowCtrl.DrawFinishMode = ((sender as CheckBox).IsChecked == true) ? HalconDrawMode.rightButton : HalconDrawMode.directly;
            config.IsDrawFinish = (sender as CheckBox).IsChecked == true;
            Serialize.WriteJson(config, System.AppDomain.CurrentDomain.BaseDirectory + this.Name + ".Config");
        }

        private void c20_Click(object sender, RoutedEventArgs e)
        {
            windowData.WindowCtrl.OnlyShowImage = ((sender as CheckBox).IsChecked == true);
            config.OnlyShowImage = (sender as CheckBox).IsChecked == true;
            Serialize.WriteJson(config, System.AppDomain.CurrentDomain.BaseDirectory + this.Name + ".Config");
            windowData.WindowCtrl.Repaint();
        }

        private void c21_Click(object sender, RoutedEventArgs e)
        {
            windowData.WindowCtrl.DotLine = ((sender as CheckBox).IsChecked == true);
            config.DotLine = (sender as CheckBox).IsChecked == true;
            Serialize.WriteJson(config, System.AppDomain.CurrentDomain.BaseDirectory + this.Name + ".Config");

            windowData.WindowCtrl.Repaint();
        }

        private void iwin_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            windowData.WindowCtrl.Repaint();
        }

        private void CheckBox_Click_10(object sender, RoutedEventArgs e)
        {
            config.IsShowCross = (sender as CheckBox).IsChecked == true;
            windowData.WindowCtrl.IsShowCross = config.IsShowCross;
            Serialize.WriteJson(config, System.AppDomain.CurrentDomain.BaseDirectory + this.Name + ".Config");
            windowData.WindowCtrl.Repaint();
        }
    }

    //public enum BackGroundColor
    //{
    //    white,
    //    black,
    //    gray,
    //    orange,
    //    coral,
    //    red,
    //}

    public class Config
    {
        public bool IsDrawFinish { set; get; } = false;
        public bool IsShowMargin { set; get; } = true;
        public bool IsShowCross { set; get; } = false;
        public bool IsShowWaterString { set; get; } = false;
        public bool IsEdit { set; get; } = false;
        public int ColorIndex { set; get; } = 1;

        public bool OnlyShowImage { set; get; } = false;
        public bool DotLine { set; get; } = false;
    }

    public class Serialize
    {
        /// <summary>
        /// 序列化读取
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="jsonFileName"></param>
        /// <returns></returns>
        /// <exception cref="FileNotFoundException"></exception>
        public static T ReadJson<T>(string jsonFileName)
        {
            FileInfo fileInfo = new FileInfo(jsonFileName);
            if (fileInfo.Exists)
            {
                using (var fs = new FileStream(jsonFileName, FileMode.Open, FileAccess.Read))
                {
                    using (var sr = new StreamReader(fs))
                    {
                        var content = sr.ReadToEnd();
                        var rslt = JsonConvert.DeserializeObject<T>(content);
                        return rslt;
                    }
                }
            }
            else
                throw new FileNotFoundException();
        }

        /// <summary>
        /// 序列化保存
        /// </summary>
        /// <param name="objectToSerialize"></param>
        /// <param name="jsonFileName"></param>
        public static void WriteJson(object objectToSerialize, string jsonFileName)
        {
            using (var fs = new FileStream(jsonFileName, FileMode.OpenOrCreate, FileAccess.Write))
            {
                fs.SetLength(0L);
                using (var sw = new StreamWriter(fs))
                {
                    var jsonStr = JsonConvert.SerializeObject(objectToSerialize, Formatting.Indented);
                    sw.Write(jsonStr);
                }
            }
        }

        /// <summary>
        /// 当常规序列化失败时可以使用
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="jsonFileName"></param>
        /// <returns></returns>
        public static T ReadJsonV2<T>(string jsonFileName)
        {
            try
            {
                var setting = new JsonSerializerSettings();
                setting.PreserveReferencesHandling = PreserveReferencesHandling.All;
                setting.ReferenceLoopHandling = ReferenceLoopHandling.Serialize;
                setting.TypeNameHandling = TypeNameHandling.All;

                FileInfo fileInfo = new FileInfo(jsonFileName);
                if (fileInfo.Exists)
                {
                    using (var fs = new FileStream(jsonFileName, FileMode.Open, FileAccess.Read))
                    {
                        using (var sr = new StreamReader(fs))
                        {
                            var content = sr.ReadToEnd();
                            var rslt = JsonConvert.DeserializeObject<T>(content, setting);
                            return rslt;
                        }
                    }
                }
                else
                    throw new FileNotFoundException();
            }
            catch { return default(T); }
        }

        /// <summary>
        /// 当常规序列化失败时可以使用
        /// </summary>
        /// <param name="objectToSerialize"></param>
        /// <param name="jsonFileName"></param>
        public static void WriteJsonV2(object objectToSerialize, string jsonFileName)
        {
            var setting = new JsonSerializerSettings();
            setting.PreserveReferencesHandling = PreserveReferencesHandling.All;
            setting.ReferenceLoopHandling = ReferenceLoopHandling.Serialize;
            setting.TypeNameHandling = TypeNameHandling.All;
            setting.Formatting = Formatting.Indented;
            using (var fs = new FileStream(jsonFileName, FileMode.OpenOrCreate, FileAccess.Write))
            {
                fs.SetLength(0L);
                using (var sw = new StreamWriter(fs))
                {
                    var jsonStr = JsonConvert.SerializeObject(objectToSerialize, setting);
                    sw.Write(jsonStr);
                }
            }
        }
    }

    public class PopupPlus : Popup
    {
        public static DependencyProperty TopmostProperty = Window.TopmostProperty.AddOwner(typeof(PopupPlus), new FrameworkPropertyMetadata(false, OnTopmostChanged));

        public bool Topmost
        {
            get { return (bool)GetValue(TopmostProperty); }
            set { SetValue(TopmostProperty, value); }
        }

        private static void OnTopmostChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            (obj as PopupPlus).UpdateWindow();
        }

        protected override void OnOpened(EventArgs e)
        {
            UpdateWindow();
        }

        private void UpdateWindow()
        {
            var hwnd = ((HwndSource)PresentationSource.FromVisual(this.Child)).Handle;
            RECT rect;
            if (GetWindowRect(hwnd, out rect))
            {
                SetWindowPos(hwnd, Topmost ? -1 : -2, rect.Left, rect.Top, (int)this.Width, (int)this.Height, 0);
            }
        }

        #region P/Invoke imports & definitions

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32", EntryPoint = "SetWindowPos")]
        private static extern int SetWindowPos(IntPtr hWnd, int hwndInsertAfter, int x, int y, int cx, int cy, int wFlags);

        #endregion P/Invoke imports & definitions
    }
}