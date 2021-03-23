using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp
{
    /*********************************************************
	*作者 ：Admin
	*创建日期：2021/3/16 11:42:58
	*描述说明：
	*
	*更改历史：
	*
	*******************************************************************
	* Copyright @ Admin 2021. All rights reserved.
	*******************************************************************
	*
	*********************************************************/
    public class Model
    {

    }

    /// <summary>
    /// 解码方式枚举
    /// </summary>
    public enum CodecID
    {
        TCS_CODEC_UNKOWN = 0,
        TCS_H264 = 27,
        TCS_H265 = 173
    }

    /// <summary>
    /// 视频流信息
    /// </summary>
    public struct StreamInfo
    {
        public CodecID codecId;
        public int width;
        public int height;
        public int fps;
        public int fold;
    }

    /// <summary>
    /// 视频流详细信息
    /// </summary>
    public struct StreamDetail
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public _Rect[] rects;
        public int count;
        public int fps;
        public CodecID codecId;
    }

    public struct _Rect
    {
        public int stream_id;
        public int width;
        public int height;
    }

    /// <summary>
    /// D3D 9
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ResD3D9
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string adapter;
        public int stream_id;
        public IntPtr res;
    }

    /// <summary>
    /// 硬件状态信息
    /// </summary>
    public struct HWCurrentStatus
    {
        public int frameCount; // 当前缓存的解码后的帧数
        public int naluCount; // 当前缓存的解码前的帧数
        public int usePrevFrameTotal; // 总共使用多少帧前一帧
        public int dropFrameTotal; // 总共丢掉了多少解码后的帧
        public int dropNaluTotal; // 总共丢掉了多少解码前的帧
        public double naluRecentFps; // 当前收到的nalu的速率（帧/s）
        public double naluTotalFps; // 总共收到的nalu的速率（帧/s）
        public double renderRecentFps; // 最近渲染的速率（帧/s）
    }

    /// <summary>
    /// 解码方式
    /// </summary>
    public enum DecodeType
    {
        /// <summary>
        /// 软件解码
        /// </summary>
        CPU = 0,

        /// <summary>
        /// 硬件解码
        /// </summary>
        GPU = 1
    }
    public enum StreamType
    {
        Error = -1,
        Main = 0,
        Sub
    }

    //播放速度定义
    public enum PlaySpeed
    {
        SPEED_1X = 0,
        FAST_2X,
        FAST_4X,
        FAST_8X,
        FAST_16X,
        SLOW_2X,
        SLOW_4X,
        SLOW_8X,
        SLOW_16X,
    }
}
