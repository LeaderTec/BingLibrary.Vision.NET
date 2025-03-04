using HalconDotNet;

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
    public class DispObjectBase
    {
        public HalconColors ShowColor { get; set; } = HalconColors.绿色;

        public HalconShowing ShowMode { get; set; } = HalconShowing.margin;

        public HObject ShowObject { get; set; }

        public bool IsShowDotLine { set; get; } = false;

        public DispObjectBase(HObject showObject, HalconColors color = HalconColors.绿色, HalconShowing showMode = HalconShowing.margin, bool isShowDotline = false)
        {
            ShowColor = color;
            ShowMode = showMode;
            IsShowDotLine = isShowDotline;
            ShowObject = showObject;
        }
    }
}