using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Prism.Commands;
using Prism.Mvvm;

namespace WpfApp
{
    /*********************************************************
	*作者 ：Admin
	*创建日期：2021/3/16 11:48:37
	*描述说明：
	*
	*更改历史：
	*
	*******************************************************************
	* Copyright @ Admin 2021. All rights reserved.
	*******************************************************************
	*
	*********************************************************/
    public class MainWindowVM : BindableBase
    {
        #region 变量

        private int _connectID = -1;

        private int[] _channelIDlist;

        private int FPSCount = 0;

        private int _sessionID;

        private static char[] name = new char[64];

        private static D3DImageSource _d3D;

        private List<int> _sessionList = new List<int>();

        /// <summary>
        /// 登录失败
        /// </summary>
        private volatile bool _loginFailed = false;

        private Dispatcher _dispatcher { get; set; }

        private object _locker = new object();

        private Thread _renderThread;

        private volatile bool _isStop = false;

        private Thread _hwRenderThread;

        private volatile bool _isStopHw = false;

        private Timer _timer = null;

        private Thread _restartThread;

        #endregion

        #region 构造函数

        public MainWindowVM()
        {
            _dispatcher = Application.Current.Dispatcher;
            Init();
        }

        #endregion

        #region 命令

        public DelegateCommand Loadcommand { get; set; }

        public DelegateCommand PlayCommand { get; set; }

        #endregion

        #region 方法

        void Init()
        {
            BindCommand();
        }

        /// <summary>
        /// 绑定Command
        /// </summary>
        void BindCommand()
        {
            Loadcommand = new DelegateCommand(Load);
            PlayCommand = new DelegateCommand(Play);
        }

        /// <summary>
        /// 播放
        /// </summary>
        void Play()
        {
            // 准备工作
            //var list = ChannelId.Split(',');

            //_channelIDlist = new int[list.Length];

            //for (int i = 0; i < _channelIDlist.Length; i++)
            //{
            //    _channelIDlist[i] = Convert.ToInt32(list[i]);
            //}


            _dispatcher?.Invoke(() =>
            {
                //PlayInfos.Clear();
                //foreach (int i in _channelIDlist)
                //{
                //    PlayInfos.Add(new PlayInfo()
                //    {
                //        ChannelID = i.ToString()
                //    });
                //}
                //return;

                PlatformLoginInfo plateFormLoginInfo = new PlatformLoginInfo()
                {
                    IP = "192.168.5.88",
                    Port = 81,
                    IsAuto = false,
                    UserName = "admin",
                    Password = "trkj@88888",
                    PlatformID = "8",
                    SocketType = SocketType.TCS_TCP,
                    PlatformName = "Leopard",
                    PlatformType = "leopard",
                };

                var login = new SDKHelper.LoginInfo()
                {
                    ip = plateFormLoginInfo.IP,
                    port = plateFormLoginInfo.Port,
                    username = plateFormLoginInfo.UserName,
                    password = plateFormLoginInfo.Password,
                    st = SDKHelper.SocketType.TCS_TCP,
                    callback = new SDKHelper.ConnectionCallback() { Callback = connectionCallback }
                };
                LocalInfo.PlatformID = plateFormLoginInfo.PlatformID;

                LocalInfo.LoginInfo = login;

                _connectID = SDKHelper.Login(plateFormLoginInfo.PlatformID, login);
                var adapterInfo = AdapterHelper.GetAdapterInfo(Application.Current.MainWindow, Application.Current.MainWindow?.Width, Application.Current.MainWindow?.Height);

                //_sessionList?.ForEach((session) =>
                //{
                //    SDKHelper.InnerStopRealPlay(session);
                //});
                //_sessionList?.Clear();

                var _sessionId = SDKHelper.ProbeStream(_connectID, ChannelId, StreamType.Main, out StreamInfo streamInfo, CodecID.TCS_CODEC_UNKOWN);

                if (_sessionId < 0)
                {
                    goto error;
                }

                var v1 = SDKHelper.IsSupportDecodeByHW(out streamInfo);

                if (v1 < 0)
                {
                    Console.WriteLine($"[Error V1]{SDKHelper.GetLastError()}");
                    goto error;
                }

                _d3D = new D3DImageSource();

                _d3D?.InitDX9(streamInfo.width, streamInfo.height, adapterInfo.AdapterIndex);

                lock (_locker)
                {
                    ImageRenderCollection.Clear();

                    ImageRenderCollection.Add(_d3D);

                    ImageSource = ImageRenderCollection[0].ImageSource;
                }

                _sessionID = SDKHelper.StartRealPlayByHW(_connectID, _channelId.ToString(), StreamType.Main, out StreamDetail streamDetail, CodecID.TCS_CODEC_UNKOWN);

                if (_sessionID < 0)
                {
                    goto error;
                }

                int v3 = 0;

                ResD3D9[] _d3D9s = new ResD3D9[streamDetail.count];

                char[] ch = new char[8];

                for (int i = 0; i < _d3D9s.Length; i++)
                {
                    _d3D9s[i].adapter = adapterInfo.OutputName;
                    _d3D9s[i].stream_id = streamDetail.rects[i].stream_id;
                    _d3D9s[i].res = ImageRenderCollection[0].GetSurfaceDX9().NativePointer;
                }

                //DLLHelper.add(1, _d3D9s, 1);

                v3 = SDKHelper.HWCreateD3d9(_sessionID, _d3D9s, _d3D9s.Length);

                if (v3 < 0)
                {
                    Console.WriteLine($"[Error V3]{SDKHelper.GetLastError()}");
                    goto error;
                }
                _renderThread = new Thread(RenderThread);

                _renderThread.Name = "RenderThread";

                _renderThread.Start();

                _hwRenderThread = new Thread(HwRenderThread);

                _hwRenderThread.Name = "HwRenderThread";

                _hwRenderThread.Start();

                _timer = new Timer(CalculateFPS, null, 0, 1000);

                // 断线重连
                
                if (_restartThread == null)
                {
                    _restartThread = new Thread(RestartThread);

                    _restartThread.Name = "restartThread";

                    _restartThread.Start();
                }
                return;

                error:
                {
                    var iStopRet = SDKHelper.StopReceiveRealStream(_sessionID);

                    _loginFailed = true;
                   
                }
            });
        }

        private void connectionCallback(int connectionId, SDKHelper.ConnectionNotify data, IntPtr pUserData)
        {
            switch (data.type)
            {
                case SDKHelper.ConnectionNotifyType.STATE_DISCONNECT:
                case SDKHelper.ConnectionNotifyType.STATE_VIDEODISCONNECT:
                    //TextLog.SaveNormal($"收到平台断流回调，回调值：{(int)data.type}，平台名称：{platformInfo.PlatformName}，平台类型：{platformInfo.PlatformType}");
                    //重新连接
                    //Restart();
                    //标记平台登录失败，需要重新连接
                    _loginFailed = true;
                    //Logined = false;
                    break;
                default:
                    //TextLog.SaveNormal($"收到平台断流的回调消息未处理，回调值：{(int)data.type}");
                    break;
            }
        }

        /// <summary>
        /// 渲染线程
        /// </summary>
        void RenderThread()
        {
            while (!_isStop)
            {
                lock (_locker)
                {
                    foreach (var source in ImageRenderCollection)
                    {
                        source.OnRender();
                        FPSCount++;
                    }
                }
                Thread.Sleep(10);
            }
        }

        /// <summary>
        /// 调用TRSDK渲染
        /// </summary>
        void HwRenderThread()
        {
            while (!_isStopHw)
            {
                SDKHelper.HWRender(_sessionID, out long pts, out HWCurrentStatus status);
                Thread.Sleep(15);
            }
        }
        /// <summary>
        /// 计算FPS
        /// </summary>
        void CalculateFPS(object state)
        {
            Fps = FPSCount;
            FPSCount = 0;
        }

        /// <summary>
        /// 加载
        /// </summary>
        void Load()
        {

        }

        public static void GetUTF8Buffer(string inputString, int bufferLen, out byte[] utf8Buffer)
        {
            utf8Buffer = new byte[bufferLen];
            byte[] tempBuffer = System.Text.Encoding.UTF8.GetBytes(inputString);
            for (int i = 0; i < tempBuffer.Length; ++i)
            {
                utf8Buffer[i] = tempBuffer[i];
            }
        }

        /// <summary>
        /// 重连线程
        /// </summary>
        void RestartThread()
        {
            while (true)
            {
                if (_loginFailed)
                {
                    // 重连
                    var stopSuc = SDKHelper.StopReceiveRealStream(_sessionID);
                    if (!stopSuc)
                    {
                        Console.WriteLine("StopReceive error");
                    }
                    Console.WriteLine("LoginOut");
                    var loginOut = SDKHelper.Logout(_connectID);
                    Console.WriteLine(loginOut > 0 ? "退出成功" : "退出失败");

                    _isStop = _isStopHw = true;

                    _renderThread?.Join();
                    _hwRenderThread?.Join();
                    _loginFailed = false;

                    _isStop = _isStopHw = false;
                    Play();
                }
                Thread.Sleep(TimeSpan.FromSeconds(5));
            }
        }

        #endregion

        #region 属性

        /// <summary>
        /// 图像Image
        /// </summary>
        private ImageSource _imageSource;
        public ImageSource ImageSource
        {
            get => _imageSource;
            set
            {
                _imageSource = value;
                RaisePropertyChanged(nameof(ImageSource));
            }
        }

        public List<D3DImageSource> ImageRenderCollection { get; } = new List<D3DImageSource>();

        /// <summary>
        /// 字典集合
        /// </summary>
        private ConcurrentDictionary<string, D3DImageSource> _sources = new ConcurrentDictionary<string, D3DImageSource>();

        /// <summary>
        /// FPS
        /// </summary>
        private int _fps = 0;
        public int Fps
        {
            get => _fps;
            set
            {
                _fps = value;
                RaisePropertyChanged(nameof(Fps));
            }
        }

        /// <summary>
        /// 通道ID
        /// </summary>
        public string ChannelId
        {
            get => _channelId;
            set
            {
                _channelId = value;
                RaisePropertyChanged(nameof(ChannelId));
            }
        }

        public ObservableCollection<PlayInfo> PlayInfos
        {
            get { return _playInfos ?? (_playInfos = new ObservableCollection<PlayInfo>()); }
            set
            {
                _playInfos = value;
                RaisePropertyChanged(nameof(PlayInfos));
            }
        }

        private string _channelId = "149";

        private ObservableCollection<PlayInfo> _playInfos;

        #endregion
    }

    public class PlayInfo
    {
        public string ChannelID { get; set; }
    }
}
