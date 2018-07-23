//Author:Star Chen(techenstar@qq.com)
//License:MIT License
//Url:https://github.com/techenstar/PreviewImageHandler
//Version:1.0.0
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Web;

namespace PreviewImageHandler
{
    /// <summary>
    /// PreImg 的摘要说明
    /// </summary>
    public class PreImg : MicroAPI.MicroAPIAsync
    {
        public static string ThumbnailLocation = "~/PreImgTemp/";
        public static byte Quality = 75;
        public static Color LeaveWhiteColor = Color.White;
        static string[] supportedExt = new[] { ".bmp", ".gif", ".jpg", ".jpeg", ".png", ".tiff", ".exif" };
        public PreImg()
        {
            RegActionAsync("d", context => Preview(context, PreviewImageMode.Default));
            RegActionAsync("c", context => Preview(context, PreviewImageMode.Cut));
            RegActionAsync("lw", context => Preview(context, PreviewImageMode.LeaveWhite));
        }

        static void Ready(HttpContext context, out int width, out int height, out string filepath)
        {
            int.TryParse(context.Request.QueryString["w"], out width);
            int.TryParse(context.Request.QueryString["h"], out height);
            if (width <= 0 || height <= 0)
            {
                throw new ArgumentOutOfRangeException("参数w和h必须为大于0的int类型");
            }

            var url = HttpUtility.UrlDecode(context.Request.QueryString["url"]);
            Uri uri;
            if (!string.IsNullOrEmpty(url) && Uri.TryCreate(url, UriKind.Relative, out uri))
            {
                filepath = context.Server.MapPath(url);
            }
            else
            {
                throw new ArgumentNullException("参数url不能为空,且必须为相对路径");
            }

            if (File.Exists(filepath) && supportedExt.Contains(Path.GetExtension(filepath)))
            {

            }
            else
            {
                throw new ArgumentException("文件 " + filepath + " 不存在,或是不支持的文件格式,仅支持" + string.Join(",", supportedExt));
            }
        }
        static Type modeType = typeof(PreviewImageMode);
        static void Preview(HttpContext context, PreviewImageMode mode)
        {
            int width, height;
            string url, filepath;
            Ready(context, out width, out height, out filepath);
            var rootPath = context.Server.MapPath("~/");
            string thumbnailPath = context.Server.MapPath(ThumbnailLocation) + filepath.Substring(rootPath.Length, filepath.Length - rootPath.Length); 
            thumbnailPath = thumbnailPath + "_" + width + "_" + height + "_" + Enum.GetName(modeType, mode) + ".jpg";
            if (File.Exists(thumbnailPath))
            {
                OutputImage(context, thumbnailPath);
            }
            else
            {
                using (var bitmap = LoadFile(filepath))
                {
                    using (var newImage = GenerateThumbnail(bitmap, mode, width, height))
                    {
                        OutputImage(context, newImage);

                        SaveFile(thumbnailPath, newImage);
                    }
                }
            }
        }

        static Bitmap LoadFile(string filepath)
        {
            try
            {
                var bitmap = new Bitmap(filepath);
                return bitmap;
            }
            catch (Exception)
            {
                throw;
            }
        }
        static void OutputImage(HttpContext context, Bitmap bitmap)
        {
            context.Response.ClearContent();

            context.Response.Cache.SetCacheability(HttpCacheability.Public);
            context.Response.Cache.SetExpires(DateTime.Now.AddDays(7));

            context.Response.Cache.SetLastModified(DateTime.Now);
            context.Response.ContentType = "image/jpeg";
            bitmap.Save(context.Response.OutputStream, ImageFormat.Jpeg);
            context.Response.Flush();
        }

        static void OutputImage(HttpContext context, string fileName)
        {
            context.Response.ClearContent();
            context.Response.Cache.SetCacheability(HttpCacheability.Public);
            context.Response.Cache.SetExpires(DateTime.Now.AddDays(7));
            context.Response.Cache.SetLastModified(DateTime.Now);
            context.Response.ContentType = "image/jpeg";
            context.Response.WriteFile(fileName, true);
            context.Response.Flush();
        }
        static void SaveFile(string filepath, Bitmap bitmap)
        {
            if (!File.Exists(filepath))
            {
                string folder = Path.GetDirectoryName(filepath);
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                //先在磁盘上写入临时文件，最后进行重命名，以减少多线程环境下多个线程写入同一文件造成的冲突

                string tempFilename = filepath + "_" + DateTime.Now.ToString("yyyyMMddHHmmssffffff") + "_" + Guid.NewGuid().ToString("N") + ".temp";
                if (File.Exists(tempFilename))
                {
                    //临时文件已经存在，这种情况极其罕见，因此不做任何处理
                }
                else
                {
                    ImageCodecInfo jgpEncoder = GetEncoder(ImageFormat.Jpeg);

                    Encoder encoder = System.Drawing.Imaging.Encoder.Quality;
                    EncoderParameters parms = new EncoderParameters(1);
                    EncoderParameter parm = new EncoderParameter(encoder, (long)Quality);
                    parms.Param[0] = parm;

                    try
                    {
                        bitmap.Save(tempFilename, jgpEncoder, parms);

                        //这里为了防止其他线程已经生成了文件导致下一步Move失败，先删除可能存在的该文件
                        File.Delete(filepath);
                        File.Move(tempFilename, filepath);
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                    finally
                    {
                        File.Delete(tempFilename);
                    }
                }
            }
        }

        public static Bitmap GenerateThumbnail(Bitmap oldImage, PreviewImageMode mode, int newWidth, int newHeight)
        {
            if (newWidth <= 0 || newHeight <= 0)
            {
                throw new ArgumentOutOfRangeException("newWidth 或 newHeight不能小于0");
            }
            int oldWidth = oldImage.Width, oldHeight = oldImage.Height;
            Bitmap newImage = new Bitmap(newWidth, newHeight);
            var interpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            switch (mode)
            {
                case PreviewImageMode.Cut://裁剪
                    if (oldWidth * newHeight > oldHeight * newWidth)
                    {
                        oldWidth = oldHeight * newWidth / newHeight;
                    }
                    else if (oldWidth * newHeight < oldHeight * newWidth)
                    {
                        oldHeight = oldWidth * newHeight / newWidth;
                    }
                    using (Graphics g = Graphics.FromImage(newImage))
                    {
                        g.InterpolationMode = interpolationMode;
                        g.DrawImage(oldImage, new Rectangle(0, 0, newImage.Width, newImage.Height), new Rectangle((oldImage.Width - oldWidth) / 2, (oldImage.Height - oldHeight) / 2, oldWidth, oldHeight), GraphicsUnit.Pixel);
                    }
                    break;
                case PreviewImageMode.LeaveWhite://留白
                    int w = newWidth, h = newHeight;
                    if (oldWidth * newHeight > oldHeight * newWidth)
                    {
                        h = oldHeight * newWidth / oldWidth;
                    }
                    else if (oldWidth * newHeight < oldHeight * newWidth)
                    {
                        w = oldWidth * newHeight / oldHeight;
                    }
                    using (Graphics g = Graphics.FromImage(newImage))
                    {
                        g.InterpolationMode = interpolationMode;
                        g.FillRectangle(new SolidBrush(LeaveWhiteColor), new Rectangle(0, 0, newImage.Width, newImage.Height));
                        g.DrawImage(oldImage, new Rectangle((newImage.Width - w) / 2, (newImage.Height - h) / 2, w, h),
                            new Rectangle(0, 0, oldImage.Width, oldImage.Height),
                            GraphicsUnit.Pixel);
                    }
                    break;
                default:
                    using (Graphics g = Graphics.FromImage(newImage))
                    {
                        g.InterpolationMode = interpolationMode;
                        g.DrawImage(oldImage, new Rectangle(0, 0, newWidth, newHeight));
                    }
                    break;
            }
            return newImage;
        }
        static ImageCodecInfo GetEncoder(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();

            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }
    }

    public enum PreviewImageMode
    {
        /// <summary>
        /// 按照所需尺寸缩放，不进行裁剪和留白，缩略图和原始图片比例不一致时将发生拉伸
        /// </summary>
        Default,
        /// <summary>
        /// 裁剪模式，缩略图和原始比例不一致时进行裁剪以保证比例（不变形），但会损失展示内容
        /// </summary>
        Cut,
        /// <summary>
        /// 保持比例且不裁剪，而进行留白处理，用纯色填充空隙
        /// </summary>
        LeaveWhite
    }
}