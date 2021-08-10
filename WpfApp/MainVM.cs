using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Prism.Commands;
using Prism.Mvvm;
using RenderCore;
using System.Diagnostics;
using System.Windows.Controls;
using TR.BL.VideoManager.MultiGPUPlayer;

namespace WpfApp
{
    /*-----------------------------------
	 * Author: Admin
	 * CreateTime：2021/4/22 18:19:19
	 * Description:
	 *
	 
	 * Solution
	 
	 * *[ChageLog]：
	 
	 *
	 *******************************************************************
	 * Copyright @ Admin 2021. All rights reserved.
	 *******************************************************************
	 --------------------------------------*/
    public class MainVM : BindableBase
    {
        #region 变量

        private int _connectId = -1;

        private int _sessionID = -1;

        private static D3DImageSource _d3D;

        private Thread _renderThread = null;

        private Thread _hwRenderThread = null;

        private Timer _timer = null;

        private Thread _restartThread;

        private int FPSCount = 0;

        private object _locker = new object();

        private volatile bool _loginFailed = false;

        private volatile bool _isStopHw = false;

        private volatile bool _isStop = false;

        private MainWindow _mainWindow;

        public static DateTime UnixTimeStampStart = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        //private static SDKHelper.MEDIADATA_CALLBACK mediaCallback = new SDKHelper.MEDIADATA_CALLBACK(MediaDataCallbackDispatch);

        private SDKHelper.MEDIADATA_CALLBACK mediadataCallback { get; set; }

        private SDKHelper.ADD_INFO_MEDIADATA_CALLBACK addInfoMediadataCallback { get; set; }

        private DIRECTURL_ABNORMAL_CALLBACK directurlAbnormalCallback { get; set; }

        private SDKHelper.DECODE_CALLBACK decodeCallback { get; set; }

        public static MainVM Instance = null;

        private RenderSourceInfo renderSourceInfo;

        #endregion

        #region 构造函数

        public MainVM()
        {
            _dispatcher = Application.Current.Dispatcher;
            Init();
        }

        ~MainVM()
        {

            try
            {
                _isStop = true;
                _renderThread?.Join();

                _isStopHw = true;
                _hwRenderThread?.Join();

                Stop();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        #endregion

        #region 命令

        public DelegateCommand Loadcommand { get; set; }

        public DelegateCommand PlayCommand { get; set; }

        public DelegateCommand StopCommand { get; set; }

        public DelegateCommand StartGetFrameCommand { get; set; }

        public DelegateCommand StopGetFrameCommand { get; set; }

        public DelegateCommand OpenEnhanceCommand { get; set; }

        public DelegateCommand CloseEnhanceCommand { get; set; }

        public DelegateCommand<object> SelectComoBoxChangeCommand { get; set; }

        #endregion

        #region 方法

        void Init()
        {
            BindCommand();
            Instance = this;
        }

        void BindCommand()
        {
            Loadcommand = new DelegateCommand(Load);
            PlayCommand = new DelegateCommand(Play);
            StopCommand = new DelegateCommand(Stop);
            StartGetFrameCommand = new DelegateCommand(StartGetFrame);
            StopGetFrameCommand = new DelegateCommand(StopGetFrame);

            OpenEnhanceCommand = new DelegateCommand(OpenEnhance);
            CloseEnhanceCommand = new DelegateCommand(CloseEnhance);
            SelectComoBoxChangeCommand = new DelegateCommand<object>(SelectChanged);
        }

        void Load()
        {

        }

        void Play()
        {
            Debug.WriteLine("开始播放");
            var callback = new SDKHelper.ConnectionCallback() { Callback = connectionCallback };

            mediadataCallback = new SDKHelper.MEDIADATA_CALLBACK(MediaDataCallbackDispatch);

            addInfoMediadataCallback = new SDKHelper.ADD_INFO_MEDIADATA_CALLBACK(AddInfoMediaDataCallBack);

            directurlAbnormalCallback = new DIRECTURL_ABNORMAL_CALLBACK(DirectUrlCallBack);

            var clusterConfig = new ClusterConfig()
            {
                ENABLE = 0,
                RTSP_ADDR = "rtsp://192.168.5.168:3554/ch5002_Stream_sub",
            };
            var playPolicy = new PlayPolicy()
            {
                is_reconnect = 1,
                play_the_way = PlayTheWay.ALLOW_CHANGE_PRIORITY_GPU,
                stream_type = StreamType.Main,
                reconnect_interval_ms = 1000,
                reconnect_number = 3,
                remain_reconnect_number = 0,
            };

            var adapterInfo = AdapterHelper.GetAdapterInfo(Application.Current.MainWindow, Application.Current.MainWindow?.Width, Application.Current.MainWindow?.Height);

            //ChunkedRender chunkedRender = new ChunkedRender();

            //chunkedRender?.InitDX9(7680,1080,adapterInfo.AdapterIndex,ImageSource);

            _d3D = new D3DImageSource();

            StreamOption streamOption = new StreamOption()
            {
                max_frame_cache = 10,
                max_nalu_cache = 20,
                probe_size = 8000000,
                timeout = 5000000,
                use_tcp = 1,
                hardware = 1,
                blockMode = 0
            };

            PlayOption playOption = new PlayOption();

            playOption = new PlayOption()
            {
                streamOption = streamOption,
                url = clusterConfig.RTSP_ADDR,
            };

            _sessionID = SDKHelper.StartRealPlayByRTSP(out int connectId,
                                                       playOption, playPolicy,
                                                       directurlAbnormalCallback,
                                                       out StreamDetail stream,
                                                       out StreamInfo_Desc streamInfoDesc,
                                                       addInfoMediadataCallback, IntPtr.Zero);

            _d3D?.InitDX9(stream.rects[0].width, stream.rects[0].height, adapterInfo.AdapterIndex);

            lock (_locker)
            {
                ImageRenderCollection.Clear();

                ImageRenderCollection.Add(_d3D);

                ImageSource = ImageRenderCollection[0].ImageSource;
            }

            _connectId = connectId;

            if (streamInfoDesc.current_decode_model == 0)
            {
                return;
            }

            ResD3D9[] _d3D9s = new ResD3D9[stream.count];

            for (int i = 0; i < _d3D9s.Length; i++)
            {
                _d3D9s[i].adapter = adapterInfo.OutputName;
                _d3D9s[i].stream_id = stream.rects[i].stream_id;
                _d3D9s[i].res = ImageRenderCollection[i].GetSurfaceDX9().NativePointer;
            }

            var createD3d = SDKHelper.HWCreateD3d9(_sessionID, _d3D9s, _d3D9s.Length);

            if (createD3d < 0)
            {
                goto error;
            }

            _renderThread = new Thread(RenderThread);

            _renderThread.IsBackground = true;

            _renderThread.Name = "RenderThread";

            _renderThread.Start();

            _hwRenderThread = new Thread(HwRenderThread);

            _hwRenderThread.IsBackground = true;

            _hwRenderThread.Name = "HwRenderThread";

            _hwRenderThread.Start();

            _timer = new Timer(CalculateFPS, null, 0, 1000);

            // 断线重连

            //if (_restartThread == null)
            //{
            //    _restartThread = new Thread(RestartThread);

            //    _restartThread.Name = "restartThread";

            //    _restartThread.Start();
            //}
            return;

            error:
            {
                //var iStopRet = SDKHelper.StopReceiveRealStream(_sessionID);

                _loginFailed = true;
            }
        }

        void Stop()
        {

            _isStop = true;
            _renderThread?.Join();

            _isStopHw = true;
            _hwRenderThread?.Join();

            if (_sessionID < 0)
                MessageBox.Show("停止失败，_sessionId不合格");
            SDKHelper.StopReceiveRealStream(_sessionID);
        }

        void StartGetFrame()
        {
            decodeCallback = new SDKHelper.DECODE_CALLBACK(GetFrameDataCallBack);
            SDKHelper.StartGetFrame(_sessionID, decodeCallback);
        }

        void StopGetFrame()
        {
            SDKHelper.StopGetFrame();
        }

        void OpenEnhance()
        {
            if (_sessionID < 0) return;
            int ret = SDKHelper.OpenImageEnhance(_sessionID, "garma=1.4", 8);
            if (ret != 0)
            {
                MessageBox.Show("打开图像增强失败");
            }
        }

        void SelectChanged(object obj)
        { 
            int ID = Convert.ToInt32(((obj as ComboBox).SelectedValue as ImageEnhance).ID);
            switch (ID)
            {
                case 1:
                    if (SDKHelper.OpenImageEnhance(_sessionID, "", 1) != 0)
                    {
                        MessageBox.Show("Error");
                    }
                    break;
                case 2:
                    if (SDKHelper.OpenImageEnhance(_sessionID, "garma=1.4", 8) != 0)
                    {
                        MessageBox.Show("Error");
                    }
                    break;

                case 3:
                    if (SDKHelper.OpenImageEnhance(_sessionID, "garma=0.6", 8) != 0)
                    {
                        MessageBox.Show("Error");
                    }
                    break;
                default:
                    break;
            }
        }

        void CloseEnhance()
        {
            if (_sessionID < 0) return;

            if (SDKHelper.CloseImageEnhance(_sessionID) != 0)
            {
                MessageBox.Show("关闭图像增强失败失败");
            }

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
                    //_loginFailed = true;
                    //Logined = false;
                    break;
                default:
                    //TextLog.SaveNormal($"收到平台断流的回调消息未处理，回调值：{(int)data.type}");
                    break;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sessionId"></param>
        /// <param name="data"></param>
        /// <param name="userData"></param>
        private void MediaDataCallbackDispatch(int sessionId, SDKHelper.MediaData data, IntPtr userData)
        {
            MainVM.Instance.ProcessMediaData(new YUVData()
            {
                Width = data.imageWidth,
                Height = data.imageHeight,

                yStride = data.yStride,
                Y = data.yData,
                U = data.uData,
                V = data.vData,

                //DateTime = UnixTimeStampStart.AddMilliseconds(data.timestamp)
            });
        }

        private void AddInfoMediaDataCallBack(int sessionId, SDKHelper.MediaData data, HWCurrentStatus currentStatus, IntPtr userData)
        {
            MainVM.Instance.ProcessMediaData(new YUVData()
            {
                Width = data.imageWidth,
                Height = data.imageHeight,

                yStride = data.yStride,
                Y = data.yData,
                U = data.uData,
                V = data.vData,

                //DateTime = UnixTimeStampStart.AddMilliseconds(data.timestamp)
            });
        }

        /// <summary>
        /// 直连Url异动回调函数
        /// </summary>
        /// <param name="sessionId">SessionId</param>
        /// <param name="directUrlCallBack">直连回调CallBack</param>
        /// <param name="userData">用户指针</param>
        private void DirectUrlCallBack(int sessionId, DirectUrlCallBackInfo directUrlCallBack, IntPtr userData)
        {

        }

        private void GetFrameDataCallBack(int sessionId, SDKHelper.MediaData data, int[] pitch, IntPtr userData)
        {
            MainVM.Instance.NV12YUVCallBack(new YUVData()
            {
                Width = data.imageWidth,
                Height = data.imageHeight,
                yStride = data.yStride,
                Y = data.yData,
                U = data.uData,
                V = data.vData
            });
        }

        private void ProcessMediaData(YUVData data)
        {
            if (renderSourceInfo == null)
            {
                renderSourceInfo = RenderSourceManager.Current.GetRenderSource(_dispatcher, data.Width, data.Height);
                renderSourceInfo.RenderSource.Render(data.Y, data.U, data.V);
                ImageSource = renderSourceInfo.RenderSource.ImageSource;
            }
            else
            {
                renderSourceInfo.RenderSource.Render(data.Y, data.U, data.V);
            }
        }

        private RenderSourceInfo _yuvRenderSourceInfo;

        private void NV12YUVCallBack(YUVData data)
        {
            if (_yuvRenderSourceInfo == null)
            {
                _yuvRenderSourceInfo = RenderSourceManager.Current.GetRenderSource(_dispatcher, data.Width, data.Height);
                _yuvRenderSourceInfo.RenderSource.Render(data.Y, data.U, data.V);
                YuvCallBackImageSource = _yuvRenderSourceInfo.RenderSource.ImageSource;
            }
            else
            {
                _yuvRenderSourceInfo.RenderSource.Render(data.Y, data.U, data.V);
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
            Debug.WriteLine("RenderThread Exit");
        }

        /// <summary>
        /// 调用TRSDK渲染
        /// </summary>
        void HwRenderThread()
        {
            while (!_isStopHw)
            {
                if (SDKHelper.HWRender(_sessionID, out long pts, out HWCurrentStatus status) != 0)
                {
                    //SDKHelper.RestartReceiveStream(_sessionID, "rtsp://192.168.5.184:1554/ch5001_Stream_main");
                    Thread.Sleep(TimeSpan.FromSeconds(1));
                }
                Thread.Sleep(15);
            }
            Debug.WriteLine("HWRenderThread Exit");
            Thread.Sleep(10);
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
        /// 重连线程
        /// </summary>
        //void RestartThread()
        //{
        //    while (true)
        //    {
        //        if (_loginFailed)
        //        {
        //            // 重连
        //            var stopSuc = SDKHelper.StopReceiveRealStream(_sessionID);
        //            if (!stopSuc)
        //            {
        //                Console.WriteLine("StopReceive error");
        //            }
        //            Console.WriteLine("LoginOut");
        //            var loginOut = SDKHelper.Logout(_connectID);
        //            Console.WriteLine(loginOut > 0 ? "退出成功" : "退出失败");

        //            _isStop = _isStopHw = true;

        //            _renderThread?.Join();
        //            _hwRenderThread?.Join();
        //            _loginFailed = false;

        //            _isStop = _isStopHw = false;
        //            Play();
        //        }
        //        Thread.Sleep(TimeSpan.FromSeconds(5));
        //    }
        //}

        #endregion

        #region 属性

        private Dispatcher _dispatcher { get; set; }

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

        private ImageSource _yuvCallBackImageSource;
        public ImageSource YuvCallBackImageSource
        {
            get => _yuvCallBackImageSource;
            set
            {
                _yuvCallBackImageSource = value;
                RaisePropertyChanged(nameof(YuvCallBackImageSource));
            }
        }

        public List<D3DImageSource> ImageRenderCollection { get; } = new List<D3DImageSource>();

        public List<ChunkedRender> ChunkedRenderCollection { get; } = new List<ChunkedRender>();

        private ObservableCollection<ImageEnhance> _enhanceCollection = null;

        public ObservableCollection<ImageEnhance> EnhanceCollection
        {
            get
            {
                if (_enhanceCollection == null)
                    _enhanceCollection = new ObservableCollection<ImageEnhance>();

                if (_enhanceCollection.Count >= 1) return _enhanceCollection;
                _enhanceCollection.Add(new ImageEnhance() { Name = "1", ID = "1" });
                _enhanceCollection.Add(new ImageEnhance() { Name = "2", ID = "2" });
                _enhanceCollection.Add(new ImageEnhance() { Name = "3", ID = "3" });
                return _enhanceCollection;
            }
            set
            {
                _enhanceCollection = value;
                RaisePropertyChanged(nameof(EnhanceCollection));
            }
        }

        #endregion

    }

    public class ImageEnhance
    {
        public string Name { get; set; }

        public string ID { get; set; }
    }
}
