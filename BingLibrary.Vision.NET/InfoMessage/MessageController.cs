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
    /// 消息控制器
    /// </summary>
    public class MessageController
    {
        private ObservableCollection<MessageBase> MessageList;

        public MessageController()
        {
            MessageList = new ObservableCollection<MessageBase>();
        }

        public ObservableCollection<MessageBase> GetMessageList()
        {
            return MessageList;
        }

        /// <summary>
        /// 添加信息
        /// </summary>
        /// <param name="message"></param>
        /// <param name="row"></param>
        /// <param name="column"></param>
        /// <param name="fontsize"></param>
        /// <param name="color"></param>
        /// <param name="mode"></param>
        public void AddMessageVar(string message, int row, int column, int fontsize = 12, HalconColors color = HalconColors.绿色, bool showBox = true, HalconCoordinateSystem mode = HalconCoordinateSystem.image)
        {
            MessageList.Add(new MessageBase(row, column, message, fontsize, color, showBox, mode));
        }

        /// <summary>
        /// 清空信息
        /// </summary>
        public void Clear()
        {
            MessageList.Clear();
        }
    }
}