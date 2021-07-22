using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp
{
    /*********************************************************
	*作者 ：jiaopeilun@iseetech.com.cn
	*创建日期：2021/3/16 11:40:29
	*描述说明：
	*
	*更改历史：
	*
	*******************************************************************
	* Copyright @ Admin 2021. All rights reserved.
	*******************************************************************
	*
	*********************************************************/
    public class SDKHelper
    {
        //const string dllName = @"MediaAccessSDK\TerraVisionSDK.dll";
        //#if DEBUG
        //        private const string dllName = @"TerraVisionSDK.dll";
        //#else
        //       private const string dllName = @"TerraVisionSDK.dll";
        //#endif

#if DEBUG
        private const string dllDirectory = @"MediaAccessSDK\Debug";
#else
        private const string dllDirectory = @"MediaAccessSDK\Release";
#endif

        private const string dllName = @"TerraVisionSDK.dll";
        static SDKHelper()
        {
            //检查SDK环境变量
            CheckSDKEnvironment();

            if (!IsRunAsAdmin())
                throw new Exception("请使用管理员身份运行。");

            var r = Init();
            if (r < 0)
                throw new Exception("视频接入SDK初始化失败！"); // 暂时support.xml要放在程序的根目录下。
            else
                Debug.WriteLine("视频接入SDK初始化成功！");
        }

        #region 检测SDK是否被添加到环境变量中
        private const string CONST_TRSDK_BIN = "TRSDK_BIN";

        /// <summary>
        /// 确保 SDK 在 PATH 环境变量中
        /// </summary>
        private static void CheckSDKEnvironment()
        {
            /**
             * 分三种情况：
             * 1. SDK 在 TRSDK_BIN 中
             * 2. SDK 在 PATH 中
             * 3. SDK 在 客户端路径 下
             */
            /**
             * 目的：
             * 确保 SDK 在 PATH 中
             * 
             * 流程：
             * 1. 检查 SDK 是否在 PATH 中
             * 2. 如果不在，检查 TRSDK_BIN 环境变量是否存在，如果存在则添加进 PATH
             * 3. 如果没有 TRSDK_BIN，将客户端路径下的 SDK 目录添加进 PATH
             */
            if (!ExistsOnPath(dllName))
            {
                // TextLog.SaveDebug($"TerraVisionSDK.dll不位于PATH环境变量中，自动配置临时环境变量");

                if (!SetPublicSDKEnvironment())
                {
                    //将客户端路径下的 SDK 目录添加进 PATH
                    SetLocalSDKEnvironment();
                }
            }
        }

        /// <summary>
        /// 新版 SDK 安装后，会自动设置 TRSDK_BIN 环境变量
        /// 将 TRSDK_BIN 添加进 PATH 中
        /// </summary>
        /// <returns>true: 成功, false: 失败</returns>
        private static bool SetPublicSDKEnvironment()
        {
            if (ExistsOnPath(dllName, CONST_TRSDK_BIN))
            {
                // TextLog.SaveDebug($"在{CONST_TRSDK_BIN}中找到了TerraVisionSDK.dll");

                AddToPath($"{Environment.GetEnvironmentVariable(CONST_TRSDK_BIN)}");
                return true;
            }
            return false;
        }

        /// <summary>
        /// 将客户端路径下的 SDK 目录添加进 PATH
        /// </summary>
        private static void SetLocalSDKEnvironment()
        {
            string sdkPath = Process.GetCurrentProcess().MainModule.FileName;
            sdkPath = Path.GetDirectoryName(sdkPath);
            sdkPath = Path.Combine(sdkPath, dllDirectory);

            AddToPath(sdkPath);
        }

        /// <summary>
        /// 将指定目录添加进 PATH
        /// </summary>
        /// <param name="dir"></param>
        private static void AddToPath(string dir)
        {
            string PATH = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            if (PATH.Trim().EndsWith(Path.PathSeparator.ToString()))
            {
                PATH += dir;
            }
            else
            {
                PATH += $"{Path.PathSeparator}{dir}";
            }
            Environment.SetEnvironmentVariable("PATH", PATH, EnvironmentVariableTarget.Process);
        }

        /// <summary>
        /// 判断一个文件是否存在于PATH中
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private static bool ExistsOnPath(string fileName, string envVar = "PATH")
        {
            return GetFullPath(fileName, envVar) != null;
        }

        /// <summary>
        /// 从指定的环境变量中，获取指定文件名的完整路径
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="envVar"></param>
        /// <returns></returns>
        private static string GetFullPath(string fileName, string envVar = "PATH")
        {
            if (File.Exists(fileName))
                return Path.GetFullPath(fileName);

            var values = Environment.GetEnvironmentVariable(envVar);
            if (values != null)
            {
                foreach (var path in values.Split(Path.PathSeparator))
                {
                    try
                    {
                        var fullPath = Path.Combine(path, fileName);
                        if (File.Exists(fullPath))
                            return fullPath;
                    }
                    catch (Exception ex)
                    {
                        // TextLog.SaveError($"解析环境变量{envVar}时，遇到非法路径：{path}，解析失败原因：{ex.Message}");
                    }
                }
            }
            return null;
        }

        #endregion

        private static bool IsRunAsAdmin()
        {
            WindowsIdentity id = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(id);

            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        #region Init & Login

        [DllImport(dllName, EntryPoint = "TCS_Init")]
        public static extern int Init();

        [DllImport(dllName, EntryPoint = "TCS_UnInit")]
        public static extern int UnInit();

        #region data struct

        /// <summary>
        /// 回调函数数据类型定义
        /// </summary>
        public enum ConnectionNotifyType
        {
            STATE_DISCONNECT = 0, // 接流进程退出
            STATE_VIDEODISCONNECT = 1, //平台断流
            STATE_VIDEO_CONNECT = 2 //平台流恢复（一般情况不可能）
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct ConnectionNotify
        {
            public ConnectionNotifyType type;
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void CONNECTION_CALLBACK(int connectionId, ConnectionNotify data, IntPtr pUserData);

        public enum SocketType
        {
            TCS_UDP = 0,
            TCS_TCP
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct ConnectionCallback
        {
            public CONNECTION_CALLBACK Callback;
            public IntPtr UserData;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct LoginInfo
        {
            public string ip;
            public UInt16 port;
            public string username;
            public string password;
            public SocketType st;
            public ConnectionCallback callback;
        }

        #endregion data struct

        [DllImport(dllName, EntryPoint = "TCS_Login")]
        static extern int InnerLogin(int platform, LoginInfo loginInfo);
        public static int Login(string platformID, LoginInfo loginInfo)
        {
            // TextLog.SaveNormal($"kmy-log--login-platformID{platformID}loginInfo-{loginInfo.username}");
            int r = InnerLogin(int.Parse(platformID), loginInfo);
            // TextLog.SaveNormal($"kmy-log--login-end:{r}");
            //CameraInfo[] camerainfos = GetCameraList(r);
            //for (int i = 0; i < camerainfos.Length; i++)
            //{
            //    // TextLog.SaveNormal($"相机{i}相机ID{camerainfos[i].cameraId}");
            //}
            return r;

        }

        [DllImport(dllName, EntryPoint = "TCS_Logout")]
        public static extern int Logout(int connectId);

        #endregion Init & Login

        #region Decode by Hardware

        /// <summary>
        /// 探测实时流的相关码流信息
        /// </summary>
        /// <param name="connectId">连接ID</param>
        /// <param name="cameraId">相机ID</param>
        /// <param name="streamType">流类型</param>
        /// <param name="info">流信息</param>
        /// <param name="codecId">解码方式</param>
        /// <param name="timeout">超时时间</param>
        /// <returns>成功返回sessionID，失败返回TCSResult::Failed(详见TCS_GetLastError)</returns>
        [DllImport(dllName, EntryPoint = "TCS_ProbeStream")]
        public static extern int ProbeStream(int connectId, string cameraId, StreamType streamType, out StreamInfo info, CodecID codecId);

        /// <summary>
        /// 是否支持硬件解码
        /// </summary>
        /// <param name="info">视频流信息</param>
        /// <returns>成功返回TCSResult::Success,失败返回TCSResult::Failed(详见TCS_GetLastError)</returns>
        [DllImport(dllName, EntryPoint = "TCS_IsSupportDecodeByHW")]
        public static extern int IsSupportDecodeByHW(out StreamInfo info);

        /// <summary>
        /// 开启一路实时流的硬件解码
        /// </summary>
        /// <param name="connectId">连接ID</param>
        /// <param name="cameraId">相机ID</param>
        /// <param name="streamType">视频流类型</param>
        /// <param name="streamDetail">视频流详细信息</param>
        /// <param name="codecId">编码方式</param>
        /// <returns>成功返回SessionId，失败返回TCSResult::Failed</returns>
        [DllImport(dllName, EntryPoint = "TCS_StartRealPlayByHW")]
        public static extern int StartRealPlayByHW(int connectId, string cameraId, StreamType streamType, out StreamDetail streamDetail, CodecID codecId);

        /// <summary>
        /// 开启一路历史流的硬件解码
        /// </summary>
        /// <param name="connectId">连接ID</param>
        /// <param name="cameraId">相机ID</param>
        /// <param name="streamType">视频流类型</param>
        /// <param name="begionTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <param name="streamDetail">视频流详细信息</param>
        /// <param name="codecId">编码方式</param>
        /// <returns>成功返回SessionId，失败返回TCSResult::Failed</returns>
        [DllImport(dllName, EntryPoint = "TCS_StartRecordPlayByHW")]
        public static extern int StartRecordPlayByHW(int connectId, string cameraId, StreamType streamType, Int64 begionTime, Int64 endTime, out StreamDetail streamDetail, CodecID codecId);

        /// <summary>
        /// 创建d3d9与cuda的交互
        /// </summary>
        /// <param name="sessionId">SessionID</param>
        /// <param name="d3D9">d3d9信息</param>
        /// <param name="resSize"></param>
        /// <returns>成功返回TCSResult::Success，失败返回TCSResult::Failed</returns>
        [DllImport(dllName, EntryPoint = "TCS_HWCreateD3d9", CallingConvention = CallingConvention.Cdecl)]
        public static extern int HWCreateD3d9(int sessionId, ResD3D9[] d3D9, int resSize);

        /// <summary>
        /// 硬件渲染
        /// </summary>
        /// <param name="sessionId">SessionID</param>
        /// <param name="pts"></param>
        /// <param name="currentStatus">硬件当前状态</param>
        /// <returns>成功返回TCSResult::Success，失败返回TCSResult::Failed</returns>
        [DllImport(dllName, EntryPoint = "TCS_HWRender")]
        public static extern int HWRender(int sessionId, out long pts, out HWCurrentStatus currentStatus);

        #endregion

        #region RTSP Connect

        /// <summary>
        /// 打开RTSP直连地址
        /// </summary>
        /// <param name="clusterConfig">配置信息</param>
        /// <param name="playPolicy">播放策略</param>
        /// <param name="func">回调函数</param>
        /// <param name="stream">视频流信息</param>
        /// <returns>成功返回TCSResult::Success，失败返回TCSResult::Failed</returns>
        [DllImport(dllName, EntryPoint = "TCS_StartRealPlayByUrl")]
        public static extern int StartRealPlayByRTSP(out int connectId,
                                                     PlayOption playOption,
                                                     PlayPolicy playPolicy,
                                                     DIRECTURL_ABNORMAL_CALLBACK func,
                                                     out StreamDetail streamDetail,
                                                     out StreamInfo_Desc streamInfoDesc,
                                                     ADD_INFO_MEDIADATA_CALLBACK mediaData,
                                                     IntPtr ptr);

        [DllImport(dllName, EntryPoint = "TCS_StartGetFrame")]
        public static extern int StartGetFrame(int sessionId,DECODE_CALLBACK cb);

        [DllImport(dllName,EntryPoint = "TCS_StopGetFrame")]
        public static extern int StopGetFrame();

        [DllImport(dllName, EntryPoint = "TCS_RestartReceiveStream")]
        public static extern int RestartReceiveStream(int sessionId, string url);

        #endregion

        #region Platform

        [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential, CharSet = System.Runtime.InteropServices.CharSet.Ansi)]
        public struct PlatformInfo
        {
            /// PlatformId->int
            public int id;

            /// char[128]
            [System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.ByValTStr, SizeConst = 128)]
            public string description;
        }

        [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct PlatformInfoSet
        {
            /// unsigned int
            public uint cnt;

            /// PlatformInfo[64]
            //[System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.ByValArray, SizeConst = 64, ArraySubType = System.Runtime.InteropServices.UnmanagedType.Struct)]
            [System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.ByValArray, SizeConst = 64)]
            public PlatformInfo[] infos;
        }

        [System.Runtime.InteropServices.DllImportAttribute(dllName, EntryPoint = "TCS_GetSupportedPlatform")]
        private static extern IntPtr InnerGetSupportedPlatform();

        public static PlatformInfoSet GetSupportedPlatform()
        {
            IntPtr platformInfos = InnerGetSupportedPlatform();

            var result = (PlatformInfoSet)Marshal.PtrToStructure(platformInfos, typeof(PlatformInfoSet));

            Marshal.FreeHGlobal(platformInfos);

            return result;
        }

        #endregion Platform

        #region Devices

        [DllImport(dllName, EntryPoint = "TCS_GetCameraAmount")]
        private static extern int GetCameraAmount(int connectId);

        private const int MAX_STRINGLEN = 64;

        /// <summary>
        /// 相机信息
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct CameraInfo
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
            public char[] cameraId;                                                         //相机标识

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAX_STRINGLEN)]
            public char[] name;                                                             //相机名称

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAX_STRINGLEN)]
            public char[] ip;                                                               //IP地址

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAX_STRINGLEN)]
            public char[] firm;                                                             //厂商

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAX_STRINGLEN)]
            public char[] model;                                                            //型号

            public byte enabled;                                                            //是否可用 0否 1是 其它未知
            public byte bPTZ;                                                               //能否进行PTZ控制 0否 1是 其它未知
        }

        [DllImport(dllName, EntryPoint = "TCS_GetCameraList")]
        private static extern int InnerGetCameraList(int connectId, IntPtr cameras, int cameraAmount);

        public static CameraInfo[] GetCameraList(int connectId)
        {
            int cameraAmount = GetCameraAmount(connectId);

            var cameras = new CameraInfo[cameraAmount];

            int size = Marshal.SizeOf(typeof(CameraInfo));

            IntPtr infosIntptr = Marshal.AllocHGlobal(size * cameraAmount);
            for (int i = 0; i < cameraAmount; i++)
            {
                Marshal.StructureToPtr<CameraInfo>(cameras[0], infosIntptr, false);
            }

            int result = InnerGetCameraList(connectId, infosIntptr, size * cameraAmount);
            if (result >= 0)
                return cameras;
            else
                // TextLog.SaveError($"TCS_GetCameraList error, result={result}");

                return null;
        }

        #endregion Devices

        #region Video

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct MediaData
        {
            public int errorcode;       //0 正常，1 异常断流
            public int imageWidth;      //图像宽
            public int imageHeight;     //图像高
            public int yStride;         //跨度(同linesize)
            public IntPtr yData;        //Y分量数据
            public IntPtr uData;        //U分量数据
            public IntPtr vData;        //V分量数据
            public Int64 timestamp;     //时间戳(毫秒)
            //public Int64 gopId;     // gopID
            //public Int64 pictrueId; //pictureId
            //public Int64 gop_timestamp;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct RawData
        {
            public int errorcode;       //0 正常，1 异常断流
            public Int64 timestamp;     //时间戳(毫秒)
            public IntPtr pData;        //原始数据流
            public int dataSize;         //原始数据流长度
        }

        public enum SDKStreamType
        {
            Main = 0,
            Sub = 1
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void MEDIADATA_CALLBACK(int sessionId, MediaData data, IntPtr userData);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void ADD_INFO_MEDIADATA_CALLBACK(int sessionId, MediaData data, HWCurrentStatus currentStatus, IntPtr userData);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void DECODE_CALLBACK(int sessionId,MediaData data,int[] pitch, IntPtr userData);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void RAWDATA_CALLBACK(int sessionId, RawData data, IntPtr userData);

        [DllImport(dllName, EntryPoint = "TCS_StartRealPlayRawData")]
        public static extern int InnerStartReceiveRawDataRealStream(int connectId, string cameraId, SDKStreamType streamType, RAWDATA_CALLBACK callback, IntPtr userData);

        [DllImport(dllName, EntryPoint = "TCS_StartRealPlay")]
        public static extern int InnerStartReceiveRealStream(int connectId, string cameraId, SDKStreamType streamType, MEDIADATA_CALLBACK callback, IntPtr userData);

        public static int StartReceiveRealStream(int connectId, string cameraId, StreamType mediaStreamType, MEDIADATA_CALLBACK callback, IntPtr userData)
        {
            return InnerStartReceiveRealStream(connectId, cameraId, GetInnerStreamType(mediaStreamType), callback, userData);
        }

        public static int StartReceiveRealStreamRawData(int connectId, string cameraId, StreamType mediaStreamType, RAWDATA_CALLBACK callback, IntPtr userData)
        {
            return InnerStartReceiveRawDataRealStream(connectId, cameraId, GetInnerStreamType(mediaStreamType), callback, userData);
        }

        [DllImport(dllName, EntryPoint = "TCS_StopRealPlay")]
        public static extern int InnerStopRealPlay(int sessionId);

        public static bool StopReceiveRealStream(int sessionId)
        {
            if (sessionId < 0)
                return true;
            int result = InnerStopRealPlay(sessionId);
            CheckResult(result, "停止实时视频失败");

            return result >= 0;
        }

        #endregion Video

        #region Record

        [DllImport(dllName, EntryPoint = "TCS_RecordSearch")]
        private static extern int InnerRecordSearch(int connectId, string cameraId, Int64 beginTime, int minutes, IntPtr result);

        public static IEnumerable<bool> RecordingSearch(int connectId, string cameraId, DateTime beginTime, int minutes)
        {
            if (connectId < 0)
                return new List<bool>();

            IntPtr handle = Marshal.AllocHGlobal(minutes);

            int result = InnerRecordSearch(connectId, cameraId, GetUnixTime(beginTime), minutes, handle);
            if (result < 0)
                CheckResult(result, $"查询录像记录失败。result:{result}");

            byte[] bytes = new byte[minutes];
            Marshal.Copy(handle, bytes, 0, minutes);

            Marshal.FreeHGlobal(handle);

            return bytes.Select(i => i == 1);
        }

        [DllImport(dllName, EntryPoint = "TCS_StartPlayback")]
        private static extern int InnerStartPlayback(int connectId, string cameraId, SDKStreamType streamType, Int64 beginTime, Int64 endTime, MEDIADATA_CALLBACK callback, IntPtr userData);

        public static int StartReceiveRecordingStream(int connectId, string cameraId, StreamType mediaStreamType, DateTime beginTime, DateTime endTime, MEDIADATA_CALLBACK callback)
        {
            SDKStreamType innerType = GetInnerStreamType(mediaStreamType);
            int result = InnerStartPlayback(connectId, cameraId, innerType, GetUnixTime(beginTime), GetUnixTime(endTime), callback, IntPtr.Zero);
            if (result < 0)
                CheckResult(result, "开启录像失败。");

            return result;
        }

        [DllImport(dllName, EntryPoint = "TCS_StartPlaybackRawData")]
        private static extern int InnerStartPlaybackRawData(int connectId, string cameraId, SDKStreamType streamType, Int64 beginTime, Int64 endTime, RAWDATA_CALLBACK callback, IntPtr userData);

        public static int StartReceiveRecordingRawStream(int connectId, string cameraId, StreamType mediaStreamType, DateTime beginTime, DateTime endTime, RAWDATA_CALLBACK callback, IntPtr userData)
        {
            SDKStreamType innerType = GetInnerStreamType(mediaStreamType);
            int result = InnerStartPlaybackRawData(connectId, cameraId, innerType, GetUnixTime(beginTime), GetUnixTime(endTime), callback, IntPtr.Zero);
            if (result < 0)
                CheckResult(result, "开启录像失败。");

            return result;
        }

        private static SDKStreamType GetInnerStreamType(StreamType mediaStreamType)
        {
            SDKStreamType innerType = SDKStreamType.Sub;
            switch (mediaStreamType)
            {
                case StreamType.Error:
                    throw new Exception("translate error StreamType to SDKStreamType !");
                case StreamType.Main:
                    innerType = SDKStreamType.Main;
                    break;

                case StreamType.Sub:
                    innerType = SDKStreamType.Sub;
                    break;
            }
            return innerType;
        }

        [DllImport(dllName, EntryPoint = "TCS_StopPlayback")]
        private static extern int InnerStopReceiveRecord(int sessionId);

        public static bool StopReceiveRecordingStream(int sessionId)
        {
            int result = InnerStopReceiveRecord(sessionId);
            CheckResult(result, "停止录像播放失败。");

            return result >= 0;
        }

        [DllImport(dllName, EntryPoint = "TCS_PausePlay")]
        private static extern int InnerPause(int sessionId);

        public static bool Pause(int sessionId)
        {
            int result = InnerPause(sessionId);
            CheckResult(result, "暂停录像播放失败");

            return result >= 0;
        }

        private static void CheckResult(int result, string message)
        {
            if (result < 0)
            {
                // TextLog.SaveError(message + ", Error=" + GetLastError());
                //throw new Exception(message);
            }
        }

        [DllImport(dllName, EntryPoint = "TCS_ContiuePlay")]
        private static extern int InnerContinue(int sessionId);

        public static bool Continue(int sessionId)
        {
            return InnerContinue(sessionId) >= 0;
        }

        [DllImport(dllName, EntryPoint = "TCS_SetPlaySpeed")]
        private static extern int InnerSetSpeed(int sessionId, PlaySpeed speed);

        public static bool SetSpeed(int sessionId, PlaySpeed speed)
        {
            return InnerSetSpeed(sessionId, speed) >= 0;
        }

        private enum PlaySequence
        {
            SequencePlay = 0,   //顺序播放
            RewindPlay,         //倒序播放
        }

        [DllImport(dllName, EntryPoint = "TCS_SetPlaySequence")]
        private static extern int InnerSetPlaySequence(int sessionId, PlaySequence sequece);

        public static bool SetPlaySequence(int sessionId, bool sequential)
        {
            return InnerSetPlaySequence(sessionId, sequential ? PlaySequence.SequencePlay : PlaySequence.RewindPlay) >= 0;
        }

        [DllImport(dllName, EntryPoint = "TCS_SkipPlay")]
        private static extern int InnerSkipPlay(int sessionId, Int64 timestamp);

        public static bool SkipPlay(int sessionId, DateTime datetime)
        {
            return InnerSkipPlay(sessionId, GetUnixTime(datetime)) >= 0;
        }

        #endregion Record

        #region Download Record

        [DllImport(dllName, EntryPoint = "TCS_StartDownload")]
        private static extern int InnerStartDownload(int connectionId, string cameraId, string saveTo, Int64 beginTime, Int64 endTime, SDKStreamType streamType);

        public static int StartDownload(int connectionId, string cameraId, string saveTo, DateTime beginTime, DateTime endTime, StreamType mediaStreamType)
        {
            SDKStreamType innerStreamType = GetInnerStreamType(mediaStreamType);

            int result = InnerStartDownload(connectionId, cameraId, saveTo, GetUnixTime(beginTime), GetUnixTime(endTime), innerStreamType);
            return result;
        }

        [DllImport(dllName, EntryPoint = "TCS_StopDownload")]
        private static extern int InnerStopDownload(int sessionId);

        public static bool StopDownload(int sessionId)
        {
            return InnerStopDownload(sessionId) >= 0;
        }

        [DllImport(dllName, EntryPoint = "TCS_GetDownloadProgress")]
        public static extern int GetDownloadProgress(int sessionId);

        #endregion Download Record

        #region Local Record

        [DllImport(dllName, EntryPoint = "TCS_StartLocalRecord", CharSet = CharSet.Ansi)]
        private static extern int InnerStartLocalRecord(int sessionId, string pathName);

        /// <summary>
        /// 开始本地录像
        /// </summary>
        /// <param name="sessionId"></param>
        /// <param name="saveTo">存储文件的完整路径</param>
        /// <returns></returns>
        public static bool StartLocalRecord(int sessionId, string saveTo)
        {

            //Encoding ascii = Encoding.ASCII;
            //Encoding unicode = Encoding.Unicode;
            //// Convert the string into a byte[].
            //byte[] unicodeBytes = unicode.GetBytes(saveTo);

            //// Perform the conversion from one encoding to the other.
            //byte[] asciiBytes = Encoding.Convert(unicode, ascii, unicodeBytes);
            //char[] asciiChars = new char[ascii.GetCharCount(asciiBytes, 0, asciiBytes.Length)];
            //ascii.GetChars(asciiBytes, 0, asciiBytes.Length, asciiChars, 0);

            var result = InnerStartLocalRecord(sessionId, saveTo);
            //var result = InnerStartLocalRecord(sessionId, @"D:\TerraVision\Terra\VideoRecordSave\video");

            // TextLog.SaveNormal($"开始录像{saveTo}result{result}");

            return result >= 0;
        }

        [DllImport(dllName, EntryPoint = "TCS_StopLocalRecord")]
        private static extern int InnerStopLocalRecord(int sessionId);

        public static bool StopLocalRecord(int sessionId)
        {
            int result = InnerStopLocalRecord(sessionId);
            return result >= 0;
        }

        #endregion Local Record

        #region PTZ

        //[DllImport(dllName, EntryPoint = "TCS_PTZControl")]
        //private static extern int InnerCallPTZ(int connectId, string cameraId, PTZControls control, int speed);

        //public static bool CallPTZ(int connectId, string cameraId, PTZControls control, int speed)
        //{
        //    return InnerCallPTZ(connectId, cameraId, control, speed) >= 0;
        //}

        [DllImport(dllName, EntryPoint = "TCS_PresetSet")]
        private static extern int InnerSetPreset(int connectId, string cameraId, int presetId, string presetName);

        public static bool SetPreset(int connectId, string cameraId, int presetId, string presetName)
        {
            return InnerSetPreset(connectId, cameraId, presetId, presetName) >= 0;
        }

        [DllImport(dllName, EntryPoint = "TCS_PresetCall")]
        private static extern int InnerGotoPreset(int connectId, string cameraId, int presetId);

        public static bool GotoPreset(int connectId, string cameraId, int presetId)
        {
            return InnerGotoPreset(connectId, cameraId, presetId) >= 0;
        }

        [DllImport(dllName, EntryPoint = "TCS_PresetDel")]
        private static extern int InnerDeletePreset(int connectId, string cameraId, int presetId);

        public static bool DeletePreset(int connectId, string cameraId, int presetId)
        {
            return InnerDeletePreset(connectId, cameraId, presetId) >= 0;
        }

        #endregion PTZ

        #region Get Error Info

        private enum ErrorCode
        {
            OK = 0,                     //成功
            SystemError,                //系统返回的错误
            InvalidConnection,          //无效连接
            ConfigFileError,            //配置文件错误
            InvalidSession,             //无效session
            StatusError,                //状态错误
            ParamError,                 //参数错误
            PlatformNotSupport = 101,   //平台不支持
            InvalidCamera,              //无效Camera
            ExecFaild,                  //执行失败
            FilePathError,				//路径错误
            PlatformError               //平台反馈的错误
        }

        [DllImport(dllName, EntryPoint = "TCS_GetLastError")]
        private static extern ErrorCode InnerGetLastError();

        public static string GetLastError()
        {
            return InnerGetLastError().ToString();
        }

        [DllImport(dllName, EntryPoint = "TCS_GetVersion")]
        static extern IntPtr InnerGetSdkVersion();

        public static string GetSdkVersion()
        {
            IntPtr ptr = InnerGetSdkVersion();
            string v = Marshal.PtrToStringAnsi(ptr);
            return v;
        }
        #endregion Get Error Info

        #region Utility

        protected static DateTime unixChinaTime = new DateTime(1970, 1, 1, 8, 0, 0);

        // unix时间转换
        private static Int64 GetUnixTime(DateTime time)
        {
            return (Int64)time.Subtract(unixChinaTime).TotalMilliseconds;
        }

        #endregion Utility
    }
}
