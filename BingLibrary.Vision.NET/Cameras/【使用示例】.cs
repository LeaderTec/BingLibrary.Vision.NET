using HalconDotNet;

namespace BingLibrary.Vision.Cameras
{
    internal class 使用示例
    {
        private ICamera camera1, camera2;

        private void test()
        {
            //创建相机
            //海康相机
            camera1 = CamFactory.CreatCamera(CameraBrand.HaiKang);
            //大华相机
            camera2 = CamFactory.CreatCamera(CameraBrand.DaHua);

            //获取相机SN枚举，非必要
            var cams1 = camera1.GetListEnum();
            var cams2 = camera1.GetListEnum();

            //指定SN初始化相机，或从上一步中选择一个SN
            camera1.InitDevice("SN0000");
            camera2.InitDevice("SN0000");

            #region 同步拍照

            //拍照，等待图像
            camera1.GrabImageWithSoftTrigger(out var bitmap1);
            camera2.GrabImageWithSoftTrigger(out var bitmap2);

            #endregion 同步拍照

            #region 回调模式拍照，可送入队列

            camera1.StartWith_SoftTriggerModel((bitmap) =>
            {
                //回调处理，可送入队列
                HImage image1 = TransToHimage.ConvertBitmapToHImage(bitmap);
            });
            camera2.StartWith_SoftTriggerModel((bitmap) =>
            {
                //回调处理，可送入队列
                HImage image2 = TransToHimage.ConvertBitmapToHImage(bitmap);
            });
            //触发拍照
            camera1.SoftTrigger();
            camera2.SoftTrigger();

            #endregion 回调模式拍照，可送入队列

            #region 设置参数

            camera1.SetExpouseTime(1000);
            camera1.SetGain(10);

            #endregion 设置参数

            //关闭相机
            camera1.CloseDevice();
            camera2.CloseDevice();
        }
    }
}