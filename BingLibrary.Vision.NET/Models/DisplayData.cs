using BingLibrary.Vision;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;

namespace BingLibrary.Vision.Models
{
    // 窗口项类
    // 窗口项类
    public partial class WindowItem : ObservableObject
    {

        // 是否使用相对定位
        [ObservableProperty]
        private bool _useRelativePositioning = true;

        //// 父容器引用
        [ObservableProperty]
        private CanvasWindowsContainer _parent;

        // 标题
        [ObservableProperty]
        private string _title;

        // 位置和尺寸（实际像素值）
        [ObservableProperty]
        private double _x;

        [ObservableProperty]
        private double _y;

        [ObservableProperty]
        private int _zOrder;

        [ObservableProperty]
        private double _width;

        [ObservableProperty]
        private double _height;

        // 相对位置和尺寸（0-1范围内的比例值）
        [ObservableProperty]
        private double _relativeX;

        [ObservableProperty]
        private double _relativeY;

        [ObservableProperty]
        private double _relativeWidth;

        [ObservableProperty]
        private double _relativeHeight;

        // 窗口类型
        [ObservableProperty]
        private string _windowType;

        // 显示数据（用于与其他模块交互）
        [ObservableProperty]
        private DisplayData _displayData = new DisplayData();

        // 附加属性，例如背景色、边框颜色等
        [ObservableProperty]
        private string _backgroundColor = "#FFFFFF";

        [ObservableProperty]
        private string _borderColor = "#000000";

        [ObservableProperty]
        private double _borderThickness = 1;

        [ObservableProperty]
        private double _opacity = 1.0;

        // 窗口可见性
        [ObservableProperty]
        private bool _isVisible = true;

        // 窗口锁定状态（锁定后不可移动或调整大小）
        [ObservableProperty]
        private bool _isLocked = false;

        // 保持纵横比
        [ObservableProperty]
        private bool _maintainAspectRatio = false;
    }

    // 显示数据类
    public partial class DisplayData : ObservableObject
    {
        // 显示名称
        [ObservableProperty]
        private string _displayName;

        // 工位索引
        [ObservableProperty]
        private int _stationIndex;

        // 相机索引
        [ObservableProperty]
        private int _camareIndex;

        // 其他可能的属性，如显示规则、数据源等
        [ObservableProperty]
        private string _dataSource = "";

        [ObservableProperty]
        private bool _showStatistics = false;

        [ObservableProperty]
        private bool _showOverlay = true;

        [ObservableProperty]
        private string _overlayType = "标准";

        [ObservableProperty] public BingImageWindowData _imageWindowData = new BingImageWindowData();

        [ObservableProperty] private string _barcode = "";
    }


}
