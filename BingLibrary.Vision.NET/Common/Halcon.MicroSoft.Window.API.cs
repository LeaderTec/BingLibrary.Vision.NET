using System.Runtime.InteropServices;

/*************************************************************************************
 *
 * 文 件 名:   MicroSoft
 * 描    述:
 *
 * 版    本：  V1.0.0.0
 * 创 建 者：  Bing
 * 创建时间：  2022/1/27 14:59:15
 * ======================================
 * 历史更新记录
 * 版本：V          修改时间：         修改人：
 * 修改内容：
 * ======================================
*************************************************************************************/

namespace BingLibrary.Vision
{
    public class HalconMicroSoft
    {
        public static void FinishDraw()
        {
            try
            {
                mouse_event(MOUSEEVENTF_RIGHTDOWN | MOUSEEVENTF_RIGHTUP, 0, 0, 0, 0);
            }
            catch { }
        }

        [DllImport("user32.dll")] public static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);

        [DllImport("user32.dll")] public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        //ShowWindow参数
        public const int SW_SHOWNORMAL = 1;

        public const int SW_RESTORE = 9;
        public const int SW_SHOWNOACTIVATE = 4;

        //SendMessage参数
        public const int WM_KEYDOWN = 0X100;

        public const int WM_KEYUP = 0X101;
        public const int WM_SYSCHAR = 0X106;
        public const int WM_SYSKEYUP = 0X105;
        public const int WM_SYSKEYDOWN = 0X104;
        public const int WM_CHAR = 0X102;
        public const int MOUSEEVENTF_MOVE = 0x0001; //移动鼠标
        public const int MOUSEEVENTF_LEFTDOWN = 0x0002; //模拟鼠标左键按下
        public const int MOUSEEVENTF_LEFTUP = 0x0004; //模拟鼠标左键抬起
        public const int MOUSEEVENTF_RIGHTDOWN = 0x0008; //模拟鼠标右键按下
        public const int MOUSEEVENTF_RIGHTUP = 0x0010; //模拟鼠标右键抬起
        public const int MOUSEEVENTF_MIDDLEDOWN = 0x0020; //模拟鼠标中键按下
        public const int MOUSEEVENTF_MIDDLEUP = 0x0040; //模拟鼠标中键抬起
        public const int MOUSEEVENTF_ABSOLUTE = 0x8000; //标示是否采用绝对坐标
    }
}