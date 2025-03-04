using Prism.Mvvm;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using BingLibrary.Vision.Cameras;
using ZstdSharp.Unsafe;
using System.Threading;
using HalconDotNet;
using System.Drawing;
using System.Threading.Tasks;

namespace BingLibrary.Vision.NET.Test.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        private string _title = "BingLibrary.Vision.NET.Test";

        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }

        public MainWindowViewModel()
        {
            IsEnabled1 = true;//打开相机
            IsEnabled2 = false;//关闭相机
            IsEnabled3 = false;//模式设置
            IsEnabled4 = false;//开始拍照
            IsEnabled5 = false;//停止拍照
            IsEnabled6 = false;//软触发
            IsEnabled7 = false;//参数设置
        }

        [ObservableProperty] private BingImageWindowData _imageWindowData;
        private ICamera camera;

        [ObservableProperty]
        private ObservableCollection<string> _cameraBrands = new ObservableCollection<string>()
        {
            "海康相机",
            "大华相机",
            "巴斯勒相机",
            "大恒相机"
        };

        [ObservableProperty] private int _cameraBrandIndex = 0;

        [ObservableProperty] private ObservableCollection<string> _cameraNames = new ObservableCollection<string>();
        [ObservableProperty] private int _cameraNameIndex = 0;

        [RelayCommand]
        private void GetCameraNames()
        {
            if (CameraBrandIndex == 1) camera = CamFactory.CreatCamera(CameraBrand.DaHua);
            else if (CameraBrandIndex == 2) camera = CamFactory.CreatCamera(CameraBrand.Basler);
            else if (CameraBrandIndex == 3) camera = CamFactory.CreatCamera(CameraBrand.DaHeng);
            else camera = CamFactory.CreatCamera(CameraBrand.HaiKang);

            CameraNames.Clear();
            var list = camera.GetListEnum();
            foreach (var item in list)
            {
                CameraNames.Add(item);
            }
        }

        [ObservableProperty] private bool _isEnabled1 = true;
        [ObservableProperty] private bool _isEnabled2 = true;
        [ObservableProperty] private bool _isEnabled3 = true;
        [ObservableProperty] private bool _isEnabled4 = true;
        [ObservableProperty] private bool _isEnabled5 = true;
        [ObservableProperty] private bool _isEnabled6 = true;
        [ObservableProperty] private bool _isEnabled7 = true;

        [ObservableProperty] private bool _isContinueGrab = true;

        [ObservableProperty] private bool _isSoftTrigger = true;

        [ObservableProperty] private string _expouseTime = "0";
        [ObservableProperty] private string _gain = "0";
        [ObservableProperty] private string _frameRate = "0";

        [RelayCommand]
        private void Open()
        {
            try
            {
                IsEnabled2 = camera.InitDevice(CameraNames[CameraNameIndex]);
                Read();
                if (IsEnabled2)
                {
                    IsEnabled1 = false;//打开相机
                    IsEnabled2 = true;//关闭相机
                    IsEnabled3 = true;//模式设置
                    IsEnabled4 = true;//开始拍照
                    IsEnabled5 = false;//停止拍照
                    IsEnabled6 = true;//软触发
                    IsEnabled7 = true;//参数设置
                }
            }
            catch { }
        }

        [RelayCommand]
        private void Close()
        {
            try
            {
                camera.CloseDevice();
            }
            catch { }
            IsEnabled1 = true;//打开相机
            IsEnabled2 = false;//关闭相机
            IsEnabled3 = false;//模式设置
            IsEnabled4 = false;//开始拍照
            IsEnabled5 = false;//停止拍照
            IsEnabled6 = false;//软触发
            IsEnabled7 = false;//参数设置
        }

        private SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1);

        private BingLibrary.Tools.AsyncQueue<Bitmap> bimaps = new Tools.AsyncQueue<Bitmap>();

        [RelayCommand]
        private async void Start()
        {
            try
            {
                IsEnabled1 = false;//打开相机
                IsEnabled2 = true;//关闭相机
                IsEnabled3 = false;//模式设置
                IsEnabled4 = false;//开始拍照
                IsEnabled5 = true;//停止拍照
                IsEnabled6 = true;//软触发
                IsEnabled7 = true;//参数设置

                if (IsContinueGrab)
                {
                    IsEnabled6 = false;//模式设置

                    camera.StartWith_Continue(async x =>
                    {
                        await bimaps.EnqueueAsync(x);
                        _ = displayImage();
                    });
                }
                else
                {
                    camera.StartWith_SoftTriggerModel(async x =>
                    {
                        await bimaps.EnqueueAsync(x);
                        _ = displayImage();
                    });
                }
            }
            catch { }
        }

        private async Task displayImage()
        {
            await semaphoreSlim.WaitAsync(0);
            if (bimaps.Count > 0)
            {
                Bitmap bitmap = await bimaps.DequeueAsync();
                HImage image = TransToHimage.ConvertBitmapToHImage(bitmap);
                bitmap?.Dispose();
                ImageWindowData.DisplayImage(image);
                ImageWindowData.RefreshWindow();
            }

            semaphoreSlim.Release();
        }

        [RelayCommand]
        private async void Stop()
        {
            try
            {
                camera.StopGrabbing();

                IsEnabled1 = false;//打开相机
                IsEnabled2 = true;//关闭相机
                IsEnabled3 = true;//模式设置
                IsEnabled4 = true;//开始拍照
                IsEnabled5 = false;//停止拍照
                IsEnabled6 = true;//软触发
                IsEnabled7 = true;//参数设置
            }
            catch { }
        }

        [RelayCommand]
        private void SoftTrigger()
        {
            try
            {
                camera.SoftTrigger();
            }
            catch { }
        }

        [RelayCommand]
        private void Read()
        {
            try
            {
                camera.GetExpouseTime(out ulong value1);
                ExpouseTime = value1.ToString();
                camera.GetGain(out float value2);
                Gain = value2.ToString();
                camera.GetFrameRate(out float value3);
                FrameRate = value3.ToString();
            }
            catch { }
        }

        [RelayCommand]
        private void Write()
        {
            try
            {
                camera.SetExpouseTime(ulong.Parse(ExpouseTime));
                camera.SetGain(float.Parse(Gain));
            }
            catch { }
        }
    }
}