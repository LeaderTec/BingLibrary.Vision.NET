namespace BingLibrary.Vision
{
    public class BingImageWindowDatas
    {
        public static Dictionary<int, BingImageWindowData> bingImageWindowDatas = new Dictionary<int, BingImageWindowData>();

        public static BingImageWindowData GetWindowDatas(int key)
        {
            return bingImageWindowDatas[key + 1];
        }
    }
}