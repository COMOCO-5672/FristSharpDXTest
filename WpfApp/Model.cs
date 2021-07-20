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

    public struct StreamInfo_Desc
    {
        public int allow_decode_gpu;    // 视频流和设备是否支持硬件解码
        public int allow_decode_cpu;    // 视频流肯定是支持CPU解码的，当前值一直为1
        public int current_decode_model;    // 当前选择使用的解码方式，0：cpu，1：gpu
    }

    public struct StreamInfoOut
    {
        public StreamInfo_Desc streamInfoDesc;
        public StreamDetail streamDetail;
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
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
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

    public enum PlayTheWay
    {
        ONLY_CPU = 0,   // 只允许CPU播放
        ONLY_GPU,   // 只允许GPU播放
        ALLOW_CHANGE_PRIORITY_CPU,  // 允许两种播放模式进行自由切换,优先CPU
        ALLOW_CHANGE_PRIORITY_GPU   // 允许两种播放模式进行自由切换,优先GPU
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ClusterConfig
    {
        public int ENABLE;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string CONSUL_HTTP;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string SERVICE_NAME;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string RTSP_ADDR;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PlayPolicy
    {
        public int is_reconnect;       // 是否进行重连
        public uint reconnect_number;        // 重连次数
        public uint remain_reconnect_number;    // 剩余重连次数
        public PlayTheWay play_the_way;     // 播放方式
        public StreamType stream_type;      // 视频流类型
        public ulong reconnect_interval_ms;     // 重连间隔
    }

    /// RtspOption
    /// max_frame_cache 解码后的视频帧的最大缓存数,默认20
    /// max_nalu_cache 解码前的视频帧的最大缓存数，默认100
    /// probe_size 最大探测包的大小，这个值和相机的码率有关，目前8000000可以覆盖到8*4K相机大小的Hevc编码的码率。
    /// timeout 连接最大超时时间（单位微妙），默认使用5000000。
    /// use_tcp 1:使用TCP 0:使用UDP。
    /// hardware 1:使用GPU解码 0:使用CPU解码。

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct StreamOption
    {
        public int max_frame_cache;
        public int max_nalu_cache;
        public int probe_size;
        public int timeout;
        public int use_tcp;
        public int hardware;
        public int blockMode;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PlayOption
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 1024)]
        public string url;
        public StreamOption streamOption;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PlayParameter
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string key;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string value;
    }

    public struct NALData
    {
        public DateTime TimeStamp;     //时间戳(毫秒)
        public IntPtr Data;        //原始数据流
        public int DataSize;         //原始数据流长度
    }

    public struct YUVData
    {
        public int Width;       //图像宽
        public int Height;      //图像高

        public int yStride;     //跨度(同linesize)
        public IntPtr Y;        //Y分量数据
        public IntPtr U;        //U分量数据
        public IntPtr V;        //V分量数据

        public DateTime DateTime;       //时间戳
    }


    /// <summary>
    /// 视频直连回调函数，异常回调
    /// </summary>
    /// <param name="sessionId"></param>
    /// <param name="pUseData"></param>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void DIRECTURL_ABNORMAL_CALLBACK(int sessionId, DirectUrlCallBackInfo directUrlCallBackInfo, IntPtr pUseData);

    /// <summary>
    /// 异常信息回调报文头
    /// </summary>
    public enum Abnormal_Msg_Header
    {
        unknown = 0,    // 未知错误
        cutoff_videostream,     // 视频断流
        decode_failed,          // 解码失败
        decode_changed,         // 解码方式改变
        url_changed,            // 视频播放地址发生改变
        play_failed             // 播放失败，播放成功后中途失败
    }

    /// <summary>
    /// 直连视频异动回调
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct DirectUrlCallBackInfo
    {
        public Abnormal_Msg_Header abnormalMsgHeader;       // 回调报文头
        public PlayPolicy playPolicy;                       // 播放策略信息
        public StreamInfo_Desc streamInfoDesc;              // 视频解码方式
        public StreamDetail streamDetail;                   // 视频信息

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 1024)]
        public string currentPlayURL;                       // 当前播放视频URL
    }

}
