using BingLibrary.Vision.Models;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace BingLibrary.Vision.Controls
{
    [TemplatePart(Name = "PART_Canvas", Type = typeof(Canvas))]
    public class WindowConfigurationControl : System.Windows.Controls.Control
    {
        #region 构造函数和初始化

        static WindowConfigurationControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(WindowConfigurationControl),
                new FrameworkPropertyMetadata(typeof(WindowConfigurationControl)));
        }

        public WindowConfigurationControl()
        {
            // 初始化命令
            AddWindowCommand = new RelayCommand(AddWindow);
            DeleteWindowCommand = new RelayCommand<WindowItem>(DeleteWindow);
            DeleteSelectedWindowCommand = new RelayCommand(DeleteSelectedWindow);
            GenerateWindowsCommand = new RelayCommand(GenerateWindows);
            BringToFrontCommand = new RelayCommand(BringToFront);
            SendToBackCommand = new RelayCommand(SendToBack);
            SaveLayoutCommand = new RelayCommand(SaveLayout);
            LoadLayoutPresetCommand = new RelayCommand(LoadLayoutPreset);
            ApplyConfigurationCommand = new RelayCommand(ApplyConfiguration);
            CancelCommand = new RelayCommand(CancelConfiguration);
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            // 获取布局画布
            _canvas = GetTemplateChild("PART_Canvas") as Canvas;
            if (_canvas != null)
            {
                _canvas.SizeChanged += Canvas_SizeChanged;
            }
        }

        #endregion

        #region 依赖属性

        // WindowItems 集合 - 用户将直接绑定此属性
        public static readonly DependencyProperty WindowItemsProperty =
            DependencyProperty.Register("WindowItems", typeof(ObservableCollection<WindowItem>),
                typeof(WindowConfigurationControl),
                new PropertyMetadata(new ObservableCollection<WindowItem>()));

        public ObservableCollection<WindowItem> WindowItems
        {
            get { return (ObservableCollection<WindowItem>)GetValue(WindowItemsProperty); }
            set { SetValue(WindowItemsProperty, value); }
        }

        // 当前选中的窗口项
        public static readonly DependencyProperty SelectedWindowItemProperty =
            DependencyProperty.Register("SelectedWindowItem", typeof(WindowItem),
                typeof(WindowConfigurationControl),
                new PropertyMetadata(null));

        public WindowItem SelectedWindowItem
        {
            get { return (WindowItem)GetValue(SelectedWindowItemProperty); }
            set { SetValue(SelectedWindowItemProperty, value); }
        }

        // 选中的窗口数量 (用于生成布局)
        public static readonly DependencyProperty SelectedWindowCountProperty =
            DependencyProperty.Register("SelectedWindowCount", typeof(int),
                typeof(WindowConfigurationControl),
                new PropertyMetadata(4));

        public int SelectedWindowCount
        {
            get { return (int)GetValue(SelectedWindowCountProperty); }
            set { SetValue(SelectedWindowCountProperty, value); }
        }

        // 选中的布局方式 (用于生成布局)
        public static readonly DependencyProperty SelectedLayoutPatternProperty =
            DependencyProperty.Register("SelectedLayoutPattern", typeof(string),
                typeof(WindowConfigurationControl),
                new PropertyMetadata("网格"));

        public string SelectedLayoutPattern
        {
            get { return (string)GetValue(SelectedLayoutPatternProperty); }
            set { SetValue(SelectedLayoutPatternProperty, value); }
        }

        // 选中的布局预设
        public static readonly DependencyProperty SelectedLayoutPresetProperty =
            DependencyProperty.Register("SelectedLayoutPreset", typeof(string),
                typeof(WindowConfigurationControl),
                new PropertyMetadata("默认布局"));

        public string SelectedLayoutPreset
        {
            get { return (string)GetValue(SelectedLayoutPresetProperty); }
            set { SetValue(SelectedLayoutPresetProperty, value); }
        }

        // 水平间距
        public static readonly DependencyProperty HorizontalGapProperty =
            DependencyProperty.Register("HorizontalGap", typeof(double),
                typeof(WindowConfigurationControl),
                new PropertyMetadata(0.02d));

        public double HorizontalGap
        {
            get { return (double)GetValue(HorizontalGapProperty); }
            set { SetValue(HorizontalGapProperty, value); }
        }

        // 垂直间距
        public static readonly DependencyProperty VerticalGapProperty =
            DependencyProperty.Register("VerticalGap", typeof(double),
                typeof(WindowConfigurationControl),
                new PropertyMetadata(0.02d));

        public double VerticalGap
        {
            get { return (double)GetValue(VerticalGapProperty); }
            set { SetValue(VerticalGapProperty, value); }
        }

        // 边距
        public static readonly DependencyProperty MarginGapProperty =
            DependencyProperty.Register("MarginGap", typeof(double),
                typeof(WindowConfigurationControl),
                new PropertyMetadata(0.05d));

        public double MarginGap
        {
            get { return (double)GetValue(MarginGapProperty); }
            set { SetValue(MarginGapProperty, value); }
        }

        // 画布宽度 (内部使用)
        public static readonly DependencyProperty CanvasWidthProperty =
            DependencyProperty.Register("CanvasWidth", typeof(double),
                typeof(WindowConfigurationControl),
                new PropertyMetadata(0.0d));

        public double CanvasWidth
        {
            get { return (double)GetValue(CanvasWidthProperty); }
            set { SetValue(CanvasWidthProperty, value); }
        }

        // 画布高度 (内部使用)
        public static readonly DependencyProperty CanvasHeightProperty =
            DependencyProperty.Register("CanvasHeight", typeof(double),
                typeof(WindowConfigurationControl),
                new PropertyMetadata(0.0d));

        public double CanvasHeight
        {
            get { return (double)GetValue(CanvasHeightProperty); }
            set { SetValue(CanvasHeightProperty, value); }
        }

        // 是否处于编辑模式
        public static readonly DependencyProperty IsEditingProperty =
            DependencyProperty.Register("IsEditing", typeof(bool),
                typeof(WindowConfigurationControl),
                new PropertyMetadata(false));

        public bool IsEditing
        {
            get { return (bool)GetValue(IsEditingProperty); }
            set { SetValue(IsEditingProperty, value); }
        }

        #endregion

        #region 命令

        // 添加窗口命令
        public ICommand AddWindowCommand { get; }

        // 删除窗口命令
        public ICommand DeleteWindowCommand { get; }

        // 删除选中窗口命令
        public ICommand DeleteSelectedWindowCommand { get; }

        // 生成窗口命令
        public ICommand GenerateWindowsCommand { get; }

        // 将窗口置于最前命令
        public ICommand BringToFrontCommand { get; }

        // 将窗口置于最后命令
        public ICommand SendToBackCommand { get; }

        // 保存布局命令
        public ICommand SaveLayoutCommand { get; }

        // 加载布局预设命令
        public ICommand LoadLayoutPresetCommand { get; }

        // 应用配置命令
        public ICommand ApplyConfigurationCommand { get; }

        // 取消命令
        public ICommand CancelCommand { get; }

        #endregion

        #region 集合属性 (用于UI显示)

        // 窗口类型选项
        public ObservableCollection<string> WindowTypes { get; } = new ObservableCollection<string>()
        {
            "图像窗口", "数据窗口", "控制窗口", "图表窗口"
        };

        // 布局预设选项
        public ObservableCollection<string> LayoutPresets { get; } = new ObservableCollection<string>()
        {
            "默认布局", "2x2网格", "3x3网格", "左右分割", "上下分割"
        };

        // 窗口数量选项
        public ObservableCollection<int> WindowCountOptions { get; } = new ObservableCollection<int>()
        {
            1, 2, 4, 6, 9
        };

        // 布局模式选项
        public ObservableCollection<string> LayoutPatternOptions { get; } = new ObservableCollection<string>()
        {
            "网格", "水平排列", "垂直排列", "主次布局"
        };

        #endregion

        #region 事件

        // 自定义事件：当用户完成编辑并应用更改时触发
        public static readonly RoutedEvent ConfigurationAppliedEvent =
            EventManager.RegisterRoutedEvent("ConfigurationApplied",
                RoutingStrategy.Bubble,
                typeof(RoutedEventHandler),
                typeof(WindowConfigurationControl));

        public event RoutedEventHandler ConfigurationApplied
        {
            add { AddHandler(ConfigurationAppliedEvent, value); }
            remove { RemoveHandler(ConfigurationAppliedEvent, value); }
        }

        // 自定义事件：当用户取消编辑时触发
        public static readonly RoutedEvent ConfigurationCanceledEvent =
            EventManager.RegisterRoutedEvent("ConfigurationCanceled",
                RoutingStrategy.Bubble,
                typeof(RoutedEventHandler),
                typeof(WindowConfigurationControl));

        public event RoutedEventHandler ConfigurationCanceled
        {
            add { AddHandler(ConfigurationCanceledEvent, value); }
            remove { RemoveHandler(ConfigurationCanceledEvent, value); }
        }

        #endregion

        #region 拖拽和调整大小相关

        // Canvas实例
        private Canvas _canvas;

        // 拖拽相关字段
        private bool _isDragging;
        private System.Windows.Point _startPoint;
        private WindowItem _draggedWindow;

        // 调整大小相关字段
        private bool _isResizing;
        private ResizeDirection _currentResizeDirection;

        private enum ResizeDirection
        {
            None,
            TopLeft,
            Top,
            TopRight,
            Right,
            BottomRight,
            Bottom,
            BottomLeft,
            Left
        }

        // 窗口拖拽处理
        public void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is WindowItem windowItem)
            {
                // 设置选中的窗口
                SelectedWindowItem = windowItem;

                // 开始拖拽
                _isDragging = true;
                _startPoint = e.GetPosition(_canvas);
                _draggedWindow = windowItem;
                element.CaptureMouse();

                e.Handled = true;
            }
        }

        public void Window_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_isDragging && _draggedWindow != null && sender is FrameworkElement element)
            {
                System.Windows.Point currentPosition = e.GetPosition(_canvas);

                // 计算偏移量
                double offsetX = currentPosition.X - _startPoint.X;
                double offsetY = currentPosition.Y - _startPoint.Y;

                // 更新窗口位置
                _draggedWindow.X += offsetX;
                _draggedWindow.Y += offsetY;

                // 限制窗口不超出Canvas范围
                _draggedWindow.X = Math.Max(0, Math.Min(_draggedWindow.X, CanvasWidth - _draggedWindow.Width));
                _draggedWindow.Y = Math.Max(0, Math.Min(_draggedWindow.Y, CanvasHeight - _draggedWindow.Height));

                // 更新起始点
                _startPoint = currentPosition;

                e.Handled = true;
            }
            else if (_isResizing && _draggedWindow != null && sender is FrameworkElement resizeElement)
            {
                System.Windows.Point currentPosition = e.GetPosition(_canvas);
                double deltaX = currentPosition.X - _startPoint.X;
                double deltaY = currentPosition.Y - _startPoint.Y;

                // 根据调整方向执行相应的大小调整
                switch (_currentResizeDirection)
                {
                    case ResizeDirection.Right:
                        ResizeWidth(deltaX);
                        break;
                    case ResizeDirection.Bottom:
                        ResizeHeight(deltaY);
                        break;
                    case ResizeDirection.BottomRight:
                        ResizeWidth(deltaX);
                        ResizeHeight(deltaY);
                        break;
                    case ResizeDirection.Left:
                        ResizeFromLeft(deltaX);
                        break;
                    case ResizeDirection.Top:
                        ResizeFromTop(deltaY);
                        break;
                    case ResizeDirection.TopLeft:
                        ResizeFromLeft(deltaX);
                        ResizeFromTop(deltaY);
                        break;
                    case ResizeDirection.TopRight:
                        ResizeWidth(deltaX);
                        ResizeFromTop(deltaY);
                        break;
                    case ResizeDirection.BottomLeft:
                        ResizeFromLeft(deltaX);
                        ResizeHeight(deltaY);
                        break;
                }

                _startPoint = currentPosition;
                e.Handled = true;
            }
        }

        public void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if ((_isDragging || _isResizing) && sender is FrameworkElement element)
            {
                _isDragging = false;
                _isResizing = false;
                _currentResizeDirection = ResizeDirection.None;
                _draggedWindow = null;
                element.ReleaseMouseCapture();

                // 更新相对尺寸
                UpdateRelativeSizes();

                e.Handled = true;
            }
        }

        public void ResizeGrip_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.Tag is string directionStr &&
                element.DataContext is WindowItem windowItem)
            {
                if (Enum.TryParse<ResizeDirection>(directionStr, out var direction))
                {
                    // 设置选中的窗口
                    SelectedWindowItem = windowItem;

                    // 开始调整大小
                    _isResizing = true;
                    _currentResizeDirection = direction;
                    _startPoint = e.GetPosition(_canvas);
                    _draggedWindow = windowItem;
                    element.CaptureMouse();

                    e.Handled = true;
                }
            }
        }

        // 辅助方法：调整宽度
        private void ResizeWidth(double deltaX)
        {
            double newWidth = Math.Max(50, _draggedWindow.Width + deltaX);
            if (_draggedWindow.X + newWidth <= CanvasWidth)
            {
                _draggedWindow.Width = newWidth;
            }
        }

        // 辅助方法：调整高度
        private void ResizeHeight(double deltaY)
        {
            double newHeight = Math.Max(50, _draggedWindow.Height + deltaY);
            if (_draggedWindow.Y + newHeight <= CanvasHeight)
            {
                _draggedWindow.Height = newHeight;
            }
        }

        // 辅助方法：从左侧调整大小
        private void ResizeFromLeft(double deltaX)
        {
            double newX = _draggedWindow.X + deltaX;
            double newWidth = _draggedWindow.Width - deltaX;

            if (newWidth >= 50 && newX >= 0)
            {
                _draggedWindow.X = newX;
                _draggedWindow.Width = newWidth;
            }
        }

        // 辅助方法：从顶部调整大小
        private void ResizeFromTop(double deltaY)
        {
            double newY = _draggedWindow.Y + deltaY;
            double newHeight = _draggedWindow.Height - deltaY;

            if (newHeight >= 50 && newY >= 0)
            {
                _draggedWindow.Y = newY;
                _draggedWindow.Height = newHeight;
            }
        }

        // Canvas大小变化事件处理
        private void Canvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            CanvasWidth = e.NewSize.Width;
            CanvasHeight = e.NewSize.Height;

            // 可能需要调整窗口大小和位置以适应新的画布大小
            UpdateRelativeSizes();
        }

        #endregion

        #region 命令实现

        // 添加窗口
        private void AddWindow()
        {
            var newWindow = new WindowItem
            {
                Title = $"窗口 {WindowItems.Count + 1}",
                X = 50,
                Y = 50,
                Width = 200,
                Height = 150,
                WindowType = "图像窗口",
                ZOrder = 1,
                DisplayData = new DisplayData
                {
                    ShowOverlay = true
                }
            };

            WindowItems.Add(newWindow);
            SelectedWindowItem = newWindow;
        }

        // 删除窗口
        private void DeleteWindow(WindowItem window)
        {
            if (window != null)
            {
                WindowItems.Remove(window);
                if (SelectedWindowItem == window)
                {
                    SelectedWindowItem = WindowItems.FirstOrDefault();
                }
            }
        }

        // 删除选中窗口
        private void DeleteSelectedWindow()
        {
            if (SelectedWindowItem != null)
            {
                WindowItems.Remove(SelectedWindowItem);
                SelectedWindowItem = WindowItems.FirstOrDefault();
            }
        }

        // 生成窗口
        private void GenerateWindows()
        {
            // 保存当前选中窗口
            var currentSelectedItem = SelectedWindowItem;

            // 清空现有窗口
            WindowItems.Clear();

            // 根据选择的布局模式生成窗口
            switch (SelectedLayoutPattern)
            {
                case "网格":
                    GenerateGridLayout();
                    break;
                case "水平排列":
                    GenerateHorizontalLayout();
                    break;
                case "垂直排列":
                    GenerateVerticalLayout();
                    break;
                case "主次布局":
                    GenerateMainSubLayout();
                    break;
            }

            // 如果有窗口，选中第一个；否则尝试恢复原来的选中窗口
            if (WindowItems.Count > 0)
            {
                SelectedWindowItem = WindowItems[0];
            }
            else if (currentSelectedItem != null && WindowItems.Contains(currentSelectedItem))
            {
                SelectedWindowItem = currentSelectedItem;
            }
        }

        // 生成网格布局
        private void GenerateGridLayout()
        {
            int count = SelectedWindowCount;
            int rows = (int)Math.Ceiling(Math.Sqrt(count));
            int cols = (int)Math.Ceiling(count / (double)rows);

            double marginVal = MarginGap;
            double usableWidth = 1.0 - 2 * marginVal;
            double usableHeight = 1.0 - 2 * marginVal;

            double cellWidth = (usableWidth - HorizontalGap * (cols - 1)) / cols;
            double cellHeight = (usableHeight - VerticalGap * (rows - 1)) / rows;

            for (int i = 0; i < count; i++)
            {
                int row = i / cols;
                int col = i % cols;

                double x = marginVal + col * (cellWidth + HorizontalGap);
                double y = marginVal + row * (cellHeight + VerticalGap);

                WindowItems.Add(new WindowItem
                {
                    Title = $"窗口 {i + 1}",
                    X = x * CanvasWidth,
                    Y = y * CanvasHeight,
                    Width = cellWidth * CanvasWidth,
                    Height = cellHeight * CanvasHeight,
                    WindowType = "图像窗口",

                    ZOrder = 1,
                    DisplayData = new DisplayData
                    {
                        ShowOverlay = true,

                    }
                });
            }
        }

        // 生成水平布局
        private void GenerateHorizontalLayout()
        {
            int count = SelectedWindowCount;
            double marginVal = MarginGap;
            double usableWidth = 1.0 - 2 * marginVal;

            double cellWidth = (usableWidth - HorizontalGap * (count - 1)) / count;

            for (int i = 0; i < count; i++)
            {
                double x = marginVal + i * (cellWidth + HorizontalGap);

                WindowItems.Add(new WindowItem
                {
                    Title = $"窗口 {i + 1}",
                    X = x * CanvasWidth,
                    Y = marginVal * CanvasHeight,
                    Width = cellWidth * CanvasWidth,
                    Height = (1 - 2 * marginVal) * CanvasHeight,
                    WindowType = "图像窗口",

                    ZOrder = 1,
                    DisplayData = new DisplayData
                    {
                        ShowOverlay = true,
                    }
                });
            }
        }

        // 生成垂直布局
        private void GenerateVerticalLayout()
        {
            int count = SelectedWindowCount;
            double marginVal = MarginGap;
            double usableHeight = 1.0 - 2 * marginVal;

            double cellHeight = (usableHeight - VerticalGap * (count - 1)) / count;

            for (int i = 0; i < count; i++)
            {
                double y = marginVal + i * (cellHeight + VerticalGap);

                WindowItems.Add(new WindowItem
                {
                    Title = $"窗口 {i + 1}",
                    X = marginVal * CanvasWidth,
                    Y = y * CanvasHeight,
                    Width = (1 - 2 * marginVal) * CanvasWidth,
                    Height = cellHeight * CanvasHeight,
                    WindowType = "图像窗口",
                    ZOrder = 1,
                    DisplayData = new DisplayData
                    {
                        ShowOverlay = true
                    }
                });
            }
        }

        // 生成主次布局
        private void GenerateMainSubLayout()
        {
            int count = SelectedWindowCount;
            if (count < 2) count = 2; // 至少需要2个窗口

            double marginVal = MarginGap;
            double usableWidth = 1.0 - 2 * marginVal;
            double usableHeight = 1.0 - 2 * marginVal;

            // 主窗口占左侧2/3
            double mainWidth = usableWidth * 0.67;
            double mainHeight = usableHeight;

            // 次窗口占右侧1/3
            double subWidth = usableWidth * 0.33 - HorizontalGap;
            double subHeight = count > 2
                ? (usableHeight - (count - 2) * VerticalGap) / (count - 1)
                : usableHeight;

            // 添加主窗口
            WindowItems.Add(new WindowItem
            {
                Title = "主窗口",
                X = marginVal * CanvasWidth,
                Y = marginVal * CanvasHeight,
                Width = mainWidth * CanvasWidth,
                Height = mainHeight * CanvasHeight,
                WindowType = "图像窗口",
                ZOrder = 1,
                DisplayData = new DisplayData
                {
                    ShowOverlay = true,
                }
            });

            // 添加次窗口
            for (int i = 1; i < count; i++)
            {
                double y = marginVal + (i - 1) * (subHeight + VerticalGap);

                WindowItems.Add(new WindowItem
                {
                    Title = $"窗口 {i + 1}",
                    X = (marginVal + mainWidth + HorizontalGap) * CanvasWidth,
                    Y = y * CanvasHeight,
                    Width = subWidth * CanvasWidth,
                    Height = subHeight * CanvasHeight,
                    WindowType = "图像窗口",
                    ZOrder = 1,
                    DisplayData = new DisplayData
                    {
                        ShowOverlay = true
                    }
                });
            }
        }

        // 将窗口置于最前
        private void BringToFront()
        {
            if (SelectedWindowItem != null)
            {
                int maxZ = 1;
                if (WindowItems.Count > 0)
                {
                    maxZ = WindowItems.Max(w => w.ZOrder) + 1;
                }
                SelectedWindowItem.ZOrder = maxZ;
            }
        }

        // 将窗口置于最后
        private void SendToBack()
        {
            if (SelectedWindowItem != null)
            {
                int minZ = 1;
                if (WindowItems.Count > 0)
                {
                    minZ = WindowItems.Min(w => w.ZOrder) - 1;
                    if (minZ < 0) minZ = 0;
                }
                SelectedWindowItem.ZOrder = minZ;
            }
        }

        // 保存布局
        private void SaveLayout()
        {
            // 简单实现，实际应用中可能需要持久化到文件或数据库
            System.Windows.MessageBox.Show("布局已保存为预设: " + SelectedLayoutPreset);
        }

        // 加载布局预设
        private void LoadLayoutPreset()
        {
            // 简单实现，实际应用中应该从持久化存储中加载
            switch (SelectedLayoutPreset)
            {
                case "2x2网格":
                    SelectedWindowCount = 4;
                    SelectedLayoutPattern = "网格";
                    GenerateWindows();
                    break;
                case "3x3网格":
                    SelectedWindowCount = 9;
                    SelectedLayoutPattern = "网格";
                    GenerateWindows();
                    break;
                case "左右分割":
                    SelectedWindowCount = 2;
                    SelectedLayoutPattern = "水平排列";
                    GenerateWindows();
                    break;
                case "上下分割":
                    SelectedWindowCount = 2;
                    SelectedLayoutPattern = "垂直排列";
                    GenerateWindows();
                    break;
                case "默认布局":
                default:
                    SelectedWindowCount = 4;
                    SelectedLayoutPattern = "主次布局";
                    GenerateWindows();
                    break;
            }

            System.Windows.MessageBox.Show("已加载布局预设: " + SelectedLayoutPreset);
        }

        // 应用配置
        private void ApplyConfiguration()
        {
            RaiseEvent(new RoutedEventArgs(ConfigurationAppliedEvent, this));
        }

        // 取消配置
        private void CancelConfiguration()
        {
            RaiseEvent(new RoutedEventArgs(ConfigurationCanceledEvent, this));
        }

        #endregion

        #region 公共方法

        // 开始编辑窗口配置
        public void BeginEdit()
        {
            // 保存原始数据副本，以备取消时恢复
            BackupWindowItems();

            // 设置为编辑模式
            IsEditing = true;
        }

        // 更新相对尺寸
        public void UpdateRelativeSizes()
        {
            // 此方法可以在窗口位置或大小改变后调用，
            // 确保所有窗口都在画布范围内，并且处理相对大小等
        }

        #endregion

        #region 私有辅助方法

        private ObservableCollection<WindowItem> _backupItems;

        // 备份窗口项
        private void BackupWindowItems()
        {
            _backupItems = new ObservableCollection<WindowItem>();
            foreach (var item in WindowItems)
            {
                // 深度复制每个窗口项
                _backupItems.Add(DeepCopyWindowItem(item));
            }
        }

        // 恢复窗口项
        private void RestoreWindowItems()
        {
            if (_backupItems != null)
            {
                WindowItems.Clear();
                foreach (var item in _backupItems)
                {
                    WindowItems.Add(item);
                }
            }
        }

        // 深度复制WindowItem
        private WindowItem DeepCopyWindowItem(WindowItem source)
        {
            var copy = new WindowItem
            {
                Title = source.Title,
                X = source.X,
                Y = source.Y,
                Width = source.Width,
                Height = source.Height,
                WindowType = source.WindowType,
            };

            if (source.DisplayData != null)
            {
                copy.DisplayData = new DisplayData
                {
                    ShowOverlay = source.DisplayData.ShowOverlay,
                    StationIndex = source.DisplayData.StationIndex,
                    CamareIndex = source.DisplayData.CamareIndex,
                };
            }

            return copy;
        }

        #endregion
    }
}
