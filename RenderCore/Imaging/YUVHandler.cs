using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace RenderCore
{
    public class YUVHandler
    {
        private int frameDataLength;
        private int height;
        private int width;

        public CaptureHelper(int width, int height)
        {
            this.width = width;
            this.height = height;
            frameDataLength = width * height * 3 / 2;
        }

        public static bool Snapshot(IntPtr yuvDataHandle, string toFile, int width, int height)
        {
            if (!Directory.Exists(Path.GetDirectoryName(Path.GetFullPath(toFile))))
                Directory.CreateDirectory(Path.GetDirectoryName(toFile));
            int size = width * height * 3 / 2;
            byte[] buffer = new byte[size];
            System.Runtime.InteropServices.Marshal.Copy(yuvDataHandle, buffer, 0, size);
            //File.WriteAllBytes(toFile, buffer);
            YUV420SaveAsBMPFile(buffer, width, height, toFile);
            return File.Exists(toFile);
        }

        /// <summary>
        /// YUV420图片字节流转换成System.Drawing.Bitmap对象
        /// </summary>
        /// <param name="yuv420Frame">YUV420图片数组</param>
        /// <param name="width">图片宽度</param>
        /// <param name="height">图片高度</param>
        /// <returns></returns>
        public static System.Drawing.Bitmap YUV420FrameToImage(byte[] yuv420Frame, int width, int height)
        {
            byte[] rgbFrame = YUV420ToRGB(yuv420Frame, width, height);
            System.Drawing.Bitmap rev = null;
            // 写 BMP 图像文件。
            int yu = width * 3 % 4;
            int bytePerLine = 0;
            yu = yu != 0 ? 4 - yu : yu;
            bytePerLine = width * 3 + yu;
            using (System.IO.Stream ms = new System.IO.MemoryStream())
            {
                using (System.IO.BinaryWriter bw = new System.IO.BinaryWriter(ms))
                {
                    #region 文件头14字节

                    bw.Write('B');
                    bw.Write('M');
                    bw.Write(bytePerLine * height + 54); //文件总长度
                    bw.Write(0);
                    bw.Write(54); //图像数据地址

                    #endregion 文件头14字节

                    #region 位图信息头40字节

                    bw.Write(40); //信息头长度
                    bw.Write(width); //位图宽度（像素）
                    bw.Write(height); //位图高度（像素);
                    bw.Write((ushort)1); // 总是1
                    bw.Write((ushort)24); //色深 2的24次方，即24位彩色
                    bw.Write(0); //压缩方式 0 不压缩
                    bw.Write(bytePerLine * height); //图像数据大小（字节）
                    bw.Write(0); //水平分辨率
                    bw.Write(0); //垂直分辨率
                    bw.Write(0); //图像使用的颜色数，0全部使用
                    bw.Write(0); //重要的颜色数，0全部都重要

                    #endregion 位图信息头40字节

                    byte[] data = new byte[bytePerLine * height];

                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            data[y * bytePerLine + x * 3] = rgbFrame[bytePerLine * (height - y - 1) + x * 3 + 2];     //Blue
                            data[y * bytePerLine + x * 3 + 1] = rgbFrame[bytePerLine * (height - y - 1) + x * 3 + 1]; //Green
                            data[y * bytePerLine + x * 3 + 2] = rgbFrame[bytePerLine * (height - y - 1) + x * 3 + 0]; //Red
                        }
                    }
                    bw.Write(data, 0, data.Length);
                    bw.Flush();
                    ms.Seek(0, System.IO.SeekOrigin.Begin);
                    rev = new System.Drawing.Bitmap(ms);
                }
            }
            return rev;
        }

        /// <summary>
        /// YUV420图片字节数据保存为.bmp图片
        /// </summary>
        /// <param name="rgbFrame">YUV420图片数组</param>
        /// <param name="width">图片宽度</param>
        /// <param name="height">图片高度</param>
        /// <param name="bmpFile">文件存储路径(*.bmp)</param>
        public static void YUV420SaveAsBMPFile(byte[] yuv420Frame, int width, int height, string bmpFile)
        {
            byte[] rgbFrame = YUV420ToRGB(yuv420Frame, width, height);

            // 写 BMP 图像文件。
            int yu = width * 3 % 4;
            int bytePerLine = 0;
            yu = yu != 0 ? 4 - yu : yu;
            bytePerLine = width * 3 + yu;

            try
            {
                using (FileStream fs = File.Open(bmpFile, FileMode.OpenOrCreate))
                {
                    using (BinaryWriter bw = new BinaryWriter(fs))
                    {
                        #region 文件头14字节

                        bw.Write('B');
                        bw.Write('M');
                        bw.Write(bytePerLine * height + 54); //文件总长度
                        bw.Write(0);
                        bw.Write(54); //图像数据地址

                        #endregion 文件头14字节

                        #region 位图信息头40字节

                        bw.Write(40); //信息头长度
                        bw.Write(width); //位图宽度（像素）
                        bw.Write(height); //位图高度（像素);
                        bw.Write((ushort)1); // 总是1
                        bw.Write((ushort)24); //色深 2的24次方，即24位彩色
                        bw.Write(0); //压缩方式 0 不压缩
                        bw.Write(bytePerLine * height); //图像数据大小（字节）
                        bw.Write(0); //水平分辨率
                        bw.Write(0); //垂直分辨率
                        bw.Write(0); //图像使用的颜色数，0全部使用
                        bw.Write(0); //重要的颜色数，0全部都重要

                        #endregion 位图信息头40字节

                        byte[] data = new byte[bytePerLine * height];

                        for (int y = 0; y < height; y++)
                        {
                            for (int x = 0; x < width; x++)
                            {
                                data[y * bytePerLine + x * 3] = rgbFrame[bytePerLine * (height - y - 1) + x * 3 + 2];     //Blue
                                data[y * bytePerLine + x * 3 + 1] = rgbFrame[bytePerLine * (height - y - 1) + x * 3 + 1]; //Green
                                data[y * bytePerLine + x * 3 + 2] = rgbFrame[bytePerLine * (height - y - 1) + x * 3 + 0]; //Red
                            }
                        }
                        bw.Write(data, 0, data.Length);
                        bw.Flush();
                    }
                }
            }
            catch (Exception ex)
            {
                TextLog.SaveError($"YUV420SaveAsBMPFile error:{ex.Message}");
            }
        }

        /// <summary>
        /// 保存jpg图片
        /// </summary>
        /// <param name="yuv420Frame"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="jpgFile"></param>
        public static void YUV420SaveAsJPGFile(byte[] yuv420Frame, int width, int height, string jpgFile)
        {
            if (width <= 0 || height <= 0) { System.Diagnostics.Debug.WriteLine("保存图片宽高不能小于0吧"); return; }

            //创建一个bitmap类型的bmp变量来读取文件。
            System.Drawing.Bitmap bmp = YUV420FrameToImage(yuv420Frame, width, height);

            //新建第二个bitmap类型的bmp2变量，我这里是根据我的程序需要设置的。
            Bitmap bmp2 = new Bitmap(width, height);

            //将第一个bmp拷贝到bmp2中
            Graphics draw = Graphics.FromImage(bmp2);
            draw.DrawImage(bmp, 0, 0);

            // Get a bitmap.
            //Bitmap bmp1 = new Bitmap(@"c:\TestPhoto.jpg");
            ImageCodecInfo jgpEncoder = GetEncoder(ImageFormat.Jpeg);

            // Create an Encoder object based on the GUID
            // for the Quality parameter category.
            System.Drawing.Imaging.Encoder myEncoder = System.Drawing.Imaging.Encoder.Quality;

            // Create an EncoderParameters object.
            // An EncoderParameters object has an array of EncoderParameter
            // objects. In this case, there is only one
            // EncoderParameter object in the array.
            EncoderParameters myEncoderParameters = new EncoderParameters(1);

            EncoderParameter myEncoderParameter = new EncoderParameter(myEncoder, 100L);
            myEncoderParameters.Param[0] = myEncoderParameter;
            bmp2.Save(jpgFile, jgpEncoder, myEncoderParameters);
            bmp2.Dispose();

            draw.Dispose();
            bmp.Dispose();//释放bmp文件资源
        }

        /// <summary>
        /// YUV420图片字节流转换成 RGB图片字节流（不含文件头及位图信息）
        /// </summary>
        /// <param name="yuv420Frame">YUV420图片数组</param>
        /// <param name="width">图片宽度</param>
        /// <param name="height">图片高度</param>
        /// <returns>RGB图片字节数组</returns>
        public static byte[] YUV420ToRGB(byte[] yuv420Frame, int width, int height)
        {
            byte[] rgb = new byte[width * height * 3];
            int dIndex = 0;
            for (int py = 0; py < height; py++)
            {
                byte[] pdata;
                for (int px = 0; px < width; px++)
                {
                    pdata = YUV420ToRGB888(yuv420Frame, width, height, px, py);
                    rgb[dIndex++] = pdata[0];
                    rgb[dIndex++] = pdata[1];
                    rgb[dIndex++] = pdata[2];
                }
            }
            return rgb;
        }

        public bool Snapshot(IntPtr yuvDataHandle, string toFile)
        {
            if (!Directory.Exists(Path.GetDirectoryName(Path.GetFullPath(toFile))))
                Directory.CreateDirectory(Path.GetDirectoryName(toFile));
            byte[] buffer = new byte[frameDataLength];
            System.Runtime.InteropServices.Marshal.Copy(yuvDataHandle, buffer, 0, frameDataLength);
            //File.WriteAllBytes(toFile, buffer);
            YUV420SaveAsBMPFile(buffer, width, height, toFile);
            return File.Exists(toFile);
        }

        public bool Snapshot(IntPtr y, IntPtr u, IntPtr v, string toFile)
        {
            if (!Directory.Exists(Path.GetDirectoryName(Path.GetFullPath(toFile))))
                Directory.CreateDirectory(Path.GetDirectoryName(toFile));

            byte[] buffer = Combine(y, u, v);

            //YUV420SaveAsJPGFile(buffer, width, height, toFile);
            YUV420SaveAsBMPFile(buffer, width, height, toFile);

            return File.Exists(toFile);
        }

        public bool Snapshot(byte[] yuvByte, string toFile)
        {
            YUV420SaveAsBMPFile(yuvByte, width, height, toFile);

            return File.Exists(toFile);
        }

        public byte[] UVConvert(IntPtr yuvDataHandle)
        {
            if (null == yuvDataHandle || width <= 0 || height <= 0)
                return null;

            byte[] m_pBuffer = new byte[width * height * 3 / 2];

            try
            {
                Marshal.Copy(yuvDataHandle, m_pBuffer, 0, width * height);
                Marshal.Copy(yuvDataHandle + width * height * 5 / 4, m_pBuffer, width * height, width * height / 4);
                Marshal.Copy(yuvDataHandle + width * height, m_pBuffer, width * height * 5 / 4, width * height / 4);
            }
            catch (Exception ex)
            {
                TextLog.SaveError($"UVConvert error: {ex.Message}");
                return null;
            }

            return m_pBuffer;
        }

        private static ImageCodecInfo GetEncoder(ImageFormat format)
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

        private byte[] Combine(IntPtr yBuffer, IntPtr uBuffer, IntPtr vBuffer)
        {
            int size = width * height;

            byte[] buffer = new byte[size * 3 / 2];

            Marshal.Copy(yBuffer, buffer, 0, size);
            Marshal.Copy(vBuffer, buffer, size, size / 4);
            Marshal.Copy(uBuffer, buffer, size * 5 / 4, size / 4);

            return buffer;
        }

        #region 辅助方法

        private static byte clip(int p)
        {
            if (p < 0)
            {
                return 0;
            }
            if (p > 255)
            {
                return 255;
            }
            else
            {
                return (byte)p;
            }
        }

        /// <summary>
        /// 把YUV420帧中指定位置的YUV像素转换为RGB像素
        /// </summary>
        /// <param name="yuv420">YUV420帧数据</param>
        /// <param name="width">YUV420帧数据宽度</param>
        /// <param name="height">YUV420帧数据高度</param>
        /// <param name="px">像素点X坐标</param>
        /// <param name="py">像素点Y坐标</param
        /// >
        /// <returns></returns>
        private static byte[] YUV420ToRGB888(byte[] yuv420, int width, int height, int px, int py)
        {
            int total = width * height;
            byte y, u, v;
            byte[] rgb;
            y = yuv420[py * width + px];
            u = yuv420[(py / 2) * (width / 2) + (px / 2) + total];
            v = yuv420[(py / 2) * (width / 2) + (px / 2) + total + (total / 4)];
            rgb = YUV444ToRGB888(y, v, u);
            return rgb;
        }

        /// <summary>
        /// 把YUV444像素转换为RGB888像素
        /// </summary>
        /// <param name="Y">YUV444像素Y</param>
        /// <param name="U">YUV444像素U</param>
        /// <param name="V">YUV444像素V</param>
        /// <returns></returns>
        private static byte[] YUV444ToRGB888(byte Y, byte U, byte V)
        {
            byte[] rgb = new byte[3];
            int C, D, E;
            byte R, G, B;

            //微软提供转换
            C = Y - 16;
            D = U - 128;
            E = V - 128;

            R = clip((298 * C + 409 * E + 128) >> 8);
            G = clip((298 * C - 100 * D - 208 * E + 128) >> 8);
            B = clip((298 * C + 516 * D + 128) >> 8);

            rgb[0] = R;
            rgb[1] = G;
            rgb[2] = B;

            return rgb;
        }

        #endregion 辅助方法
    }
}
