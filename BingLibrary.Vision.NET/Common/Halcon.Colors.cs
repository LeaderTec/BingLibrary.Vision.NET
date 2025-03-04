using System.ComponentModel;

/*************************************************************************************
 *
 * 文 件 名:   HalconColors
 * 描    述:
 *
 * 版    本：  V1.0.0.0
 * 创 建 者：  Bing
 * 创建时间：  2022/1/27 11:11:27
 * ======================================
 * 历史更新记录
 * 版本：V          修改时间：         修改人：
 * 修改内容：
 * ======================================
*************************************************************************************/

namespace BingLibrary.Vision
{
    /// <summary>
    /// 颜色合集，这里包含常用的一些。
    /// </summary>
    public enum HalconColors
    {
        [Description("black")] 黑色,
        [Description("#000000c0")] 黑色七五成,
        [Description("#00000080")] 黑色五成,
        [Description("#00000040")] 黑色二五成,

        [Description("white")] 白色,
        [Description("#ffffffc0")] 白色七五成,
        [Description("#ffffff80")] 白色五成,
        [Description("#ffffff40")] 白色二五成,

        [Description("red")] 红色,
        [Description("#ff0000c0")] 红色七五成,
        [Description("#ff000080")] 红色五成,
        [Description("#ff000040")] 红色二五成,

        [Description("green")] 绿色,
        [Description("#00ff00c0")] 绿色七五成,
        [Description("#00ff0080")] 绿色五成,
        [Description("#00ff0040")] 绿色二五成,

        [Description("blue")] 蓝色,
        [Description("#0000ffc0")] 蓝色七五成,
        [Description("#0000ff80")] 蓝色五成,
        [Description("#0000ff40")] 蓝色二五成,

        [Description("violet")] 紫色,
        [Description("#ee82eec0")] 紫色七五成,
        [Description("#ee82ee80")] 紫色五成,
        [Description("#ee82ee40")] 紫色二五成,

        [Description("gray")] 灰色,
        [Description("#bebebec0")] 灰色七五成,
        [Description("#bebebe80")] 灰色五成,
        [Description("#bebebe40")] 灰色二五成,

        [Description("cyan")] 青色,
        [Description("#00ffffc0")] 青色七五成,
        [Description("#00ffff80")] 青色五成,
        [Description("#00ffff40")] 青色二五成,

        [Description("magenta")] 洋红色,
        [Description("#ff00ffc0")] 洋红色七五成,
        [Description("#ff00ff80")] 洋红色五成,
        [Description("#ff00ff40")] 洋红色二五成,

        [Description("yelow")] 黄色,
        [Description("#ffff00c0")] 黄色七五成,
        [Description("#ffff0080")] 黄色五成,
        [Description("#ffff0040")] 黄色二五成,

        [Description("coral")] 珊瑚色,
        [Description("#ff7f50c0")] 珊瑚色七五成,
        [Description("#ff7f5080")] 珊瑚色五成,
        [Description("#ff7f5040")] 珊瑚色二五成,

        [Description("pink")] 粉色,
        [Description("#ffc0cbc0")] 粉色七五成,
        [Description("#ffc0cb80")] 粉色五成,
        [Description("#ffc0cb40")] 粉色二五成,

        [Description("orange")] 橙色,
        [Description("#ffa500c0")] 橙色七五成,
        [Description("#ffa50080")] 橙色五成,
        [Description("#ffa50040")] 橙色二五成,

        [Description("gold")] 金色,
        [Description("#ffd700c0")] 金色七五成,
        [Description("#ffd70080")] 金色五成,
        [Description("#ffd70040")] 金色二五成,

        [Description("navy")] 海军蓝色,
        [Description("#000080c0")] 海军蓝色七五成,
        [Description("#00008080")] 海军蓝色五成,
        [Description("#00008040")] 海军蓝色二五成,
    }
}