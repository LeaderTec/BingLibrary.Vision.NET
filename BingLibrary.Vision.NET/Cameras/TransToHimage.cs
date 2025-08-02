using HalconDotNet;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace BingLibrary.Vision.Cameras
{
    /*************************************************************************************
     *
     * 文 件 名:   TransToHimage
     * 描    述:
     *
     * 版    本：  V1.0.0.0
     * 创 建 者：  Bing
     * 创建时间：  2025/3/1 10:40:21
     * ======================================
     * 历史更新记录
     * 版本：V          修改时间：         修改人：
     * 修改内容：
     * ======================================
    *************************************************************************************/

    public static class TransToHimage
    {

       
        /// <summary>
        /// 将Bitmap转换为HImage（支持24位和8位色深）
        /// </summary>
        /// <param name="sourceImage">源图像对象</param>
        /// <param name="pixelFormat">目标像素格式（仅支持Format24bppRgb/Format8bppIndexed）</param>
        /// <returns>转换成功的HImage对象</returns>
        /// <exception cref="ArgumentNullException">当源图像为空时抛出</exception>
        /// <exception cref="ArgumentException">当参数不满足要求时抛出</exception>
        public static HImage ConvertBitmapToHImage(Bitmap sourceImage)
        {
            try
            {
                // 参数验证
                if (sourceImage == null)
                    throw new ArgumentNullException(nameof(sourceImage));

                if (sourceImage.Width == 0 || sourceImage.Height == 0)
                    throw new ArgumentException("Invalid image dimensions", nameof(sourceImage));

                return CreateHImageFromBitmap(sourceImage, sourceImage.PixelFormat);

                // 创建兼容格式的临时位图
                using (var convertedBitmap = new Bitmap(
                    sourceImage.Width,
                    sourceImage.Height,
                    sourceImage.PixelFormat))
                {
                    // 设置8位灰度调色板
                    if (sourceImage.PixelFormat == PixelFormat.Format8bppIndexed)
                        SetGrayscalePalette(convertedBitmap);

                    // 高质量图像转换
                    using (var g = Graphics.FromImage(convertedBitmap))
                    {
                        g.CompositingQuality = CompositingQuality.HighQuality;
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        g.SmoothingMode = SmoothingMode.HighQuality;
                        g.DrawImage(sourceImage, new Rectangle(0, 0, convertedBitmap.Width, convertedBitmap.Height));
                    }

                    return CreateHImageFromBitmap(convertedBitmap, convertedBitmap.PixelFormat);

                }
            }
            catch { return new HImage(); }
        }

        /// <summary>
        /// 创建HImage的核心方法
        /// </summary>
        private static HImage CreateHImageFromBitmap(Bitmap bitmap, PixelFormat format)
        {
            var rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            var data = bitmap.LockBits(rect, ImageLockMode.ReadOnly, format);

            try
            {
                var hImage = new HImage();
                var pointer = data.Scan0;

                if (format == PixelFormat.Format24bppRgb)
                {
                    hImage.GenImageInterleaved(
                        pointer,          // 像素数据指针
                        "bgr",            // 颜色顺序
                        bitmap.Width,     // 图像宽度
                        bitmap.Height,    // 图像高度
                        -1,               // 自动计算步长
                        "byte",           // 像素类型
                        0, 0, 0, 0, -1, 0 // 其他参数（默认值）
                    );
                }
                else
                {
                    hImage.GenImage1(
                        "byte",           // 像素类型
                        bitmap.Width,     // 图像宽度
                        bitmap.Height,    // 图像高度
                        pointer           // 像素数据指针
                    );
                }

                return hImage;
            }
            finally
            {
                bitmap.UnlockBits(data);
            }
        }

        /// <summary>
        /// 设置8位灰度调色板
        /// </summary>
        private static void SetGrayscalePalette(Bitmap bitmap)
        {
            if (bitmap.PixelFormat != PixelFormat.Format8bppIndexed)
                return;

            ColorPalette palette = bitmap.Palette;
            for (int i = 0; i < 256; i++)
                palette.Entries[i] = Color.FromArgb(i, i, i);

            bitmap.Palette = palette;
        }
    }
}