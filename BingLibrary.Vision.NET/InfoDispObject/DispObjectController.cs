using HalconDotNet;
using System.Collections.ObjectModel;

/*************************************************************************************
 *
 * 文 件 名:   HalconColors
 * 描    述:
 *
 * 版    本：  V1.0.0.0
 * 创 建 者：  Bing
 * 创建时间：  2022/1/27 10:35:16
 * ======================================
 * 历史更新记录
 * 版本：V          修改时间：         修改人：
 * 修改内容：
 * ======================================
*************************************************************************************/

namespace BingLibrary.Vision
{
    /// <summary>
    /// 外部显示对象控制器
    /// </summary>
    public class DispObjectController
    {
        private ObservableCollection<DispObjectBase> DispObjectList;

        public DispObjectController()
        {
            DispObjectList = new ObservableCollection<DispObjectBase>();
        }

        public ObservableCollection<DispObjectBase> GetDispObjectList()
        {
            return DispObjectList;
        }

        public void AddDispObjectVar(HObject showObject, HalconColors color = HalconColors.绿色, HalconShowing halconDrawing = HalconShowing.margin, bool isShowDotline = false)
        {
            if (showObject.CountObj() > 1)
            { _ = ""; }
            DispObjectList.Add(new DispObjectBase(showObject, color, halconDrawing, isShowDotline));
        }

        /// <summary>
        /// 清空信息
        /// </summary>
        public void Clear()
        {
            DispObjectList.Clear();
        }
    }
}