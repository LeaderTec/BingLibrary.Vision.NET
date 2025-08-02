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
using System;
using System.Diagnostics;
using Prism.Regions;
using BingLibraryLite.Services;

using System.Collections.ObjectModel;

using System.Collections.Generic;
using BingLibrary.Communication;
using BingLibrary.Vision.Engine;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace BingLibrary.Vision.NET.Test.ViewModels
{
    public class MyTriggerData
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public partial class MainWindowViewModel : ObservableObject
    {
        private string _title = "BingLibrary.Vision.NET.Test";

        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }

        private BingLibraryLite.Services.ILoggerService _loggerService;
        private IRegionManager _regionManager;

        public MainWindowViewModel(IRegionManager regionManager, BingLibraryLite.Services.ILoggerService loggerService)
        {
            IsEnabled1 = true;//打开相机
            IsEnabled2 = true;//关闭相机
            IsEnabled3 = false;//模式设置
            IsEnabled4 = false;//开始拍照
            IsEnabled5 = false;//停止拍照
            IsEnabled6 = false;//软触发
            IsEnabled7 = false;//参数设置

            _loggerService = loggerService;
            _regionManager = regionManager;
            init();
        }

        private async void init()
        {
            await Task.Delay(100);
            // _regionManager.RequestNavigate("LogWin", "LogWin");
        }

        [ObservableProperty] private BingImageWindowData _imageWindowData;
        private ICamera<MyTriggerData> camera;

        [ObservableProperty]
        private ObservableCollection<string> _cameraBrands = new ObservableCollection<string>()
        {
            "海康相机",
             "海康相机3D",
            "大华相机",
            "巴斯勒相机",
            "大恒相机"
        };

        [ObservableProperty] private int _cameraBrandIndex = 0;

        [ObservableProperty] private ObservableCollection<string> _cameraNames = new ObservableCollection<string>();
        [ObservableProperty] private int _cameraNameIndex = 0;

        private List<CameraInfo> currentCameraInfos = new List<CameraInfo>();

        [RelayCommand]
        private void GetCameraNames()
        {
            if (CameraBrandIndex == 0) camera = CamFactory<MyTriggerData>.CreateCamera(CameraBrand.HaiKang);
            else if (CameraBrandIndex == 1) camera = CamFactory<MyTriggerData>.CreateCamera(CameraBrand.HaiKang3D); 
            else if(CameraBrandIndex == 2) camera = CamFactory<MyTriggerData>.CreateCamera(CameraBrand.DaHua);
            else if (CameraBrandIndex == 3) camera = CamFactory<MyTriggerData>.CreateCamera(CameraBrand.Basler);
            else if (CameraBrandIndex == 4) camera = CamFactory<MyTriggerData>.CreateCamera(CameraBrand.DaHeng);

            CameraNames.Clear();
            currentCameraInfos = camera.GetListEnum();
            foreach (var item in currentCameraInfos)
            {
                CameraNames.Add($"{item.CameraName};{item.CameraSN}");
            }
        }

        private async void testLog()
        {
            for (int i = 0; i < 200; i++)
            {
                _loggerService.Info(i.ToString());
                await Task.Delay(1);
            }
        }

        [ObservableProperty] private string _status;

        [ObservableProperty] private string _status1;
        [ObservableProperty] private string _status2;

        private WorkerEngine we1 = new WorkerEngine();
        private WorkerEngine we2 = new WorkerEngine();

        [RelayCommand]
        private async void RunTest()
        {
            Status1 = "";
            Status2 = "";
            await Task.Delay(10);
            we2 = new WorkerEngine();
            we1.AddProcedure("MultiRunTest", "D:\\Test\\HalScripts");
            we2.AddProcedure("MultiRunTest", "");
            runWE1();
            runWE2();

            we1.RemoveProcedure("MultiRunTest");
        }

        private int result1;
        private int result2;

        private void runWE1()
        {
            we1.SetParam("MultiRunTest", "input", 1);
            we1.ExecuteProcedure("MultiRunTest");
            result1 = we1.GetParam<HTuple>("MultiRunTest", "output");
            Status1 = result1.ToString();
        }

        private void runWE2()
        {
            we2.SetParam("MultiRunTest", "input", 2);
            we2.ExecuteProcedure("MultiRunTest");
            result2 = we2.GetParam<HTuple>("MultiRunTest", "output");
            Status2 = result2.ToString();
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
                IsEnabled2 = camera.InitDevice(currentCameraInfos[CameraNameIndex]);
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

        private System.Diagnostics.Stopwatch sw = new Stopwatch();

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
                        double fc = 0;
                        if (sw.ElapsedMilliseconds != 0)
                            fc = 1000.0 / sw.ElapsedMilliseconds;

                        Status = $"耗时：{sw.ElapsedMilliseconds}ms,预估帧率：{fc}";
                        sw.Restart();
                        await bimaps.EnqueueAsync(x);
                        _ = displayImage();
                    });
                }
                else
                {
                    if (IsSoftTrigger)
                    {
                        camera.StartWith_SoftTriggerModel(async x =>
                        {
                            await bimaps.EnqueueAsync(x);
                            _ = displayImage();
                        });
                    }
                    else
                    {
                        camera.StartWith_HardTriggerModel(TriggerSource.Line0, async x =>
                        {
                            await bimaps.EnqueueAsync(x);
                            _ = displayImage();
                        });
                    }
                }
            }
            catch { }
        }

        [RelayCommand]
        private async void Stop()
        {
            try
            {
                camera.StopGrabbing();
                imageCount = 0;
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
        private void Load()
        {
            WorkerEngine we = new WorkerEngine();
            we.EnableJIT();
            we.ReloadProcedure("AddTest", "D:\\Test\\HalScripts");
            we.SetParam("AddTest", "value1", 3);
            we.SetParam("AddTest", "value2", 5);
            we.ExecuteProcedure("AddTest");
            var rst1 = we.GetParam<HTuple>("AddTest", "result");
            we.Dispose();
            return;
            string filePath = SelectFileWpf();
            if (!String.IsNullOrEmpty(filePath))
            {
                bool rst = camera.LoadCamConfig(filePath);
                if (!rst)
                    HandyControl.Controls.MessageBox.Show("配置文件加载失败！");
            }
        }

        private string SelectFileWpf()
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog()
            {
                Filter = "相机配置文件 (.mfs)|*.mfs|All files (*.*)|*.*"
            };
            var result = openFileDialog.ShowDialog();
            if (result == true)
            {
                return openFileDialog.FileName;
            }
            else
            {
                return null;
            }
        }

        [ObservableProperty] private int _pinTuCount = 1;
        private int imageCount = 0;
        private HImage tileImages = new HImage();

        private async Task displayImage()
        {
            await semaphoreSlim.WaitAsync();
            if (bimaps.Count > 0)
            {
                bool rst = camera.TryGetNextTriggerData(out MyTriggerData myTriggerData);
                if (rst)
                    Status = $"耗时:{sw.ElapsedMilliseconds}ms \r\nID：{myTriggerData.Id}\r\nName：{myTriggerData.Name}\r\n拼图计数：{imageCount + 1}";
                else
                    Status = $"耗时:{sw.ElapsedMilliseconds}ms";
                Bitmap bitmap = await bimaps.DequeueAsync();
                await Task.Run(() =>
                {
                    HImage image = TransToHimage.ConvertBitmapToHImage(bitmap);
                    bitmap?.Dispose();
                    ImageWindowData.DisplayImage(new HImage(image));
                    ImageWindowData.RefreshWindow();
                    if (imageCount == 0)
                        ImageWindowData.FitImage();

                    if (PinTuCount > 1)
                    {
                        imageCount = imageCount + 1;
                        if (!tileImages.IsInitialized())
                        {
                            tileImages = new HImage(image);
                            System.Diagnostics.Debug.WriteLine("贴图清空");
                        }
                        else
                            tileImages = tileImages.ConcatObj(image);
                        System.Diagnostics.Debug.WriteLine("线扫计数" + imageCount);
                        if (imageCount == PinTuCount)
                        {
                            System.Diagnostics.Debug.WriteLine("开始拼图" + imageCount);
                            HImage finalImage = tileImages.TileImages(1, "vertical");
                            ImageWindowData.DisplayImage(new HImage(finalImage));
                            ImageWindowData.FitImage();
                            //  ImageWindowData.RefreshWindow();
                            imageCount = 0;
                            tileImages?.Dispose();
                            tileImages = new HImage();
                        }
                    }
                });
            }

            semaphoreSlim.Release();
        }

        private int myTriggerDataIndex = 0;

        [ObservableProperty] private string _triggerVar = "Hello World";

        [RelayCommand]
        private void SoftTrigger()
        {
            try
            {
                MyTriggerData myTriggerData = new() { Id = 0, Name = TriggerVar };

                camera.SoftTrigger(myTriggerData);
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

        [RelayCommand]
        private void NetAdapter()
        {
            try
            {
                var process = new Process
                {
                    StartInfo =
                    {
                        WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory,
                        UseShellExecute = true,
                        FileName = $"{AppDomain.CurrentDomain.BaseDirectory}NetAdapterTool\\NetAdapterTool.exe",
                        CreateNoWindow = true,
                        Verb = "runas"
                    }
                };
                process.Start();
            }
            catch { }
        }
    }
}