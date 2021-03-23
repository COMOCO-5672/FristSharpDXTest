using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WpfApp
{
    /// <summary>
    /// PlayControl.xaml 的交互逻辑
    /// </summary>
    public partial class PlayControl : UserControl
    {
        #region 字段

        private int _connectID { get; set; } = -1;

        private int _sessionID { get; set; } = -1;

        private D3DImageSource _d3D;

        private Thread _surfaceThread;

        private Thread _renderThread;

        private bool _stop = false;

        private int FPSCount = 0;

        #endregion
        public PlayControl()
        {
            InitializeComponent();

            this.Loaded += PlayControl_Loaded;
        }

        private void PlayControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (!LocalInfo._platformIDList.ContainsKey(LocalInfo.PlatformID))
            {
                var ret = SDKHelper.Login(LocalInfo.PlatformID, LocalInfo.LoginInfo);
                if (ret < 0)
                {
                    // 出错
                    return;
                }

                LocalInfo._platformIDList.TryAdd(LocalInfo.PlatformID, _connectID);
            }
            else
            {
                LocalInfo._platformIDList.TryGetValue(LocalInfo.PlatformID, out int _conID);

                _connectID = _conID;
            }

            var adapterInfo = AdapterHelper.GetAdapterInfo(Application.Current.MainWindow,
                Application.Current.MainWindow?.Width, Application.Current.MainWindow?.Height);

            if (string.IsNullOrEmpty(ChannelID)) return;

            _sessionID = SDKHelper.ProbeStream(_connectID,
                                               ChannelID,
                                               StreamType.Main,
                                               out StreamInfo streamInfo,
                                               CodecID.TCS_H265);

            _d3D = new D3DImageSource();

            _d3D?.InitDX9(streamInfo.width, streamInfo.height, adapterInfo.AdapterIndex);

            this.DcwtTmmwvcr.Source = _d3D.ImageSource;

            _sessionID = SDKHelper.StartRealPlayByHW(_connectID,
                                                     ChannelID,
                                                     StreamType.Main,
                                                     out StreamDetail streamDetail,
                                                     CodecID.TCS_H265);

            ResD3D9[] _d3D9s = new ResD3D9[streamDetail.count];

            for (int i = 0; i < _d3D9s.Length; i++)
            {
                _d3D9s[i].adapter = adapterInfo.OutputName;
                _d3D9s[i].stream_id = streamDetail.rects[i].stream_id;
                _d3D9s[i].res = _d3D.GetSurfaceDX9().NativePointer;
            }

            var v3 = SDKHelper.HWCreateD3d9(_sessionID, _d3D9s, _d3D9s.Length);
            if (v3 < 0)
            {
                Console.WriteLine($"[Error v3]{SDKHelper.GetLastError()}");
            }
            _surfaceThread = new Thread(SurfaceThread);

            _renderThread = new Thread(RenderThread);

            _surfaceThread.Name = "SurfaceThread";

            _renderThread.Name = "RenderThread";

            _surfaceThread.Start();

            _renderThread.Start();
        }

        #region 依赖属性



        public int Fps
        {
            get { return (int)GetValue(FpsProperty); }
            set { SetValue(FpsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Fps.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FpsProperty =
            DependencyProperty.Register("Fps", typeof(int), typeof(PlayControl), new PropertyMetadata(PropertyChangeCallBack));

        private static void PropertyChangeCallBack(DependencyObject obj,DependencyPropertyChangedEventArgs e)
        {
            if (obj is PlayControl control)
            {
                control.FpsTB.Text = e.NewValue.ToString();
            }
        }


        public string ChannelID
        {
            get { return (string)GetValue(ChannelIDProperty); }
            set { SetValue(ChannelIDProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ChannelID.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ChannelIDProperty =
            DependencyProperty.Register("ChannelID", typeof(string), typeof(PlayControl), new PropertyMetadata(""));


        #endregion

        /// <summary>
        /// 表现线程
        /// </summary>
        void SurfaceThread()
        {
            while (true && !_stop)
            {
                _d3D.OnRender();
                FPSCount++;
                Thread.Sleep(10);
            }
        }

        /// <summary>
        /// 渲染线程
        /// </summary>
        void RenderThread()
        {
            while (true && !_stop)
            {
                SDKHelper.HWRender(_sessionID, out long pts, out HWCurrentStatus status);

                Thread.Sleep(15);
            }
        }

    }
}
