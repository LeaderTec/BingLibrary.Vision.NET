/*************************************************************************************
 *
 * 文 件 名:   HalconColors
 * 描    述:
 *
 * 版    本：  V1.0.0.0
 * 创 建 者：  Bing
 * 创建时间：  2022/1/27 10:32:55
 * ======================================
 * 历史更新记录
 * 版本：V          修改时间：         修改人：
 * 修改内容：
 * ======================================
*************************************************************************************/

namespace BingLibrary.Vision
{
    public class MessageBase
    {
        public double PositionX { get; set; } = 0;

        public double PositionY { get; set; } = 0;

        public HalconColors ShowColor { get; set; } = HalconColors.绿色;

        public string ShowContent { get; set; } = string.Empty;

        public int ShowFontSize { set; get; }

        public bool ShowBox { set; get; } = true;

        public HalconCoordinateSystem ShowMode { set; get; }

        public MessageBase(double posX, double posY, string text, int fontsize = 12, HalconColors color = HalconColors.绿色, bool showBox = true, HalconCoordinateSystem mode = HalconCoordinateSystem.image)
        {
            PositionX = posX;
            PositionY = posY;
            ShowColor = color;
            ShowContent = text;
            ShowFontSize = fontsize;
            ShowMode = mode;
            ShowBox = showBox;
        }
    }
}