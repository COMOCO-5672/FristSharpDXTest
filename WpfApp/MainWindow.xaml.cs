using SharpDX.Direct3D;
using SharpDX.Mathematics.Interop;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using D2D = SharpDX.Direct2D1;
using D3D11 = SharpDX.Direct3D11;
using D3D9 = SharpDX.Direct3D9;
using DXGI = SharpDX.DXGI;

namespace WpfApp
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        #region 变量

        private float _x;
        private float _y;
        private float _dx = 1;
        private float _dy = 1;

        private D3DImage _d3D;

        private D3D11.Device device;

        private D3D9.Texture _renderTarget;

        private D2D.RenderTarget _d2DRenderTarget;

        private Thread _renderThread = null;

        private bool _isFrist = true;

        private volatile bool _isDispose = false;

        private System.Windows.Threading.Dispatcher _dispather;

        private DispatcherTimer _dispatcherTimer;

        #endregion
        public MainWindow()
        {
            InitializeComponent();

            this.DataContext = new MainVM();

            //_dispatcherTimer = new DispatcherTimer();

            //_dispatcherTimer.Tick += _dispatcherTimer_Tick;

            //_dispatcherTimer.Interval = TimeSpan.FromSeconds(1);

            //_dispatcherTimer?.Start();

            //this.DataContext = new MainWindowVM();

            //_d3D = KsyosqStmckfy;

            //_dispather = this.Dispatcher;

            //this.Loaded += MainWindow_Loaded;

            //this.SizeChanged += MainWindow_SizeChanged;
        }

        private void _dispatcherTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                Console.WriteLine("窗口宽度" + this.Width + "窗口高度" + this.Height);
                Console.WriteLine("画布宽度" + this.DcwtTmmwvcr.Source.Width + "画布高度" + this.DcwtTmmwvcr.Source.Height);
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                //throw;
            }
           
        }

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!_isFrist)
            {
                _isDispose = true;
                _renderThread?.Abort();
                CreateRenderTarget();
                _isDispose = false;
                CreateThread();
                //device.ImmediateContext.Rasterizer.SetViewport(0, 0, (int)ActualWidth, (int)ActualHeight);
            }
        }

        private void OnRender()
        {
            CompositionTarget_Rendering(null, null);
        }

        private void CreateThread()
        {
            _renderThread = new Thread(() =>
            {
                while (true && !_isDispose)
                {
                    _dispather?.Invoke(() =>
                    {
                        OnRender();
                    });
                    Thread.Sleep(10);
                }
            });
            _renderThread.Name = "OnRender";
            _isDispose = false;
            _renderThread.Start();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var dispatcher = this.Dispatcher;

            CreateRenderTarget();

            CreateThread();
            _isFrist = false;
            //ThreadPool.QueueUserWorkItem((obj) =>
            //{
            //    while (true)
            //    {
            //        dispatcher?.Invoke(() =>
            //        {
            //            CompositionTarget_Rendering(null, null);
            //        });
            //        Thread.Sleep(10);
            //    }
            //});  // 线程渲染滑块

            //CompositionTarget.Rendering += CompositionTarget_Rendering; // 拖动时重新渲染,导致滑块速度播放变快
        }


        private void CreateRenderTarget()
        {
            // 创建
            device = new D3D11.Device(DriverType.Hardware, D3D11.DeviceCreationFlags.BgraSupport);

            var width = Math.Max((int)ActualWidth, 100);

            var height = Math.Max((int)ActualHeight, 100);

            // 渲染信息
            var renderDesc = new D3D11.Texture2DDescription
            {
                BindFlags = D3D11.BindFlags.RenderTarget | D3D11.BindFlags.ShaderResource,
                Format = DXGI.Format.B8G8R8A8_UNorm,
                Width = width,
                Height = height,
                MipLevels = 1,
                SampleDescription = new DXGI.SampleDescription(1, 0),
                Usage = D3D11.ResourceUsage.Default,
                OptionFlags = D3D11.ResourceOptionFlags.Shared,
                CpuAccessFlags = D3D11.CpuAccessFlags.None,
                ArraySize = 1
            };

            // 渲染目标
            var renderTarget = new D3D11.Texture2D(device, renderDesc);

            var surface = renderTarget.QueryInterface<DXGI.Surface>();

            // 创建D2D工厂
            var d2DFactory = new D2D.Factory();

            // 渲染属性
            var renderTargetProperties = new D2D.RenderTargetProperties(new D2D.PixelFormat(DXGI.Format.Unknown, D2D.AlphaMode.Premultiplied));

            _d2DRenderTarget = new D2D.RenderTarget(d2DFactory, surface, renderTargetProperties);

            SetRenderTarget(renderTarget);

            device.ImmediateContext.Rasterizer.SetViewport(0, 0, (int)ActualWidth, (int)ActualHeight);
        }

        private void CompositionTarget_Rendering(object sender, EventArgs e)
        {
            _d2DRenderTarget.BeginDraw();

            OnRender(_d2DRenderTarget);

            _d2DRenderTarget.EndDraw();

            device.ImmediateContext.Flush();

            _d3D.Lock();

            _d3D.AddDirtyRect(new Int32Rect(0,0,_d3D.PixelWidth,_d3D.PixelHeight));

            _d3D.Unlock();

            base.InvalidateVisual();
        }

        private void OnRender(D2D.RenderTarget renderTarget)
        {
            var brush = new D2D.SolidColorBrush(_d2DRenderTarget, new RawColor4(1, 0, 0, 1));

            renderTarget.Clear(null);

            renderTarget.DrawRectangle(new RawRectangleF(_x, _y, _x + 10, _y + 10), brush);

            _x = _x + _dx;
            _y = _y + _dy;
            if (_x >= ActualWidth - 10 || _x <= 0)
            {
                _dx = -_dx;
            }

            if (_y >= ActualHeight - 10 || _y <= 0)
            {
                _dy = -_dy;
            }
        }

        private void SetRenderTarget(D3D11.Texture2D target)
        {
            var format = TranslateFormat(target);

            var handle = GetSharedHandle(target);

            var presentParams = GetPresentParameters();

            var createFlags = D3D9.CreateFlags.HardwareVertexProcessing
                | D3D9.CreateFlags.Multithreaded | D3D9.CreateFlags.FpuPreserve;

            var d3DContext = new D3D9.Direct3DEx();
            var d3DDevice = new D3D9.DeviceEx(d3DContext, 0, D3D9.DeviceType.Hardware,
                IntPtr.Zero, createFlags, presentParams);

            _renderTarget = new D3D9.Texture(d3DDevice, target.Description.Width, target.Description.Height, 1,
                D3D9.Usage.RenderTarget, format, D3D9.Pool.Default, ref handle);

            using (var surface = _renderTarget.GetSurfaceLevel(0))
            {
                _d3D.Lock();
                _d3D.SetBackBuffer(D3DResourceType.IDirect3DSurface9, surface.NativePointer);
                _d3D.Unlock();
            }

        }

        private static D3D9.Format TranslateFormat(D3D11.Texture2D texture)
        {
            switch (texture.Description.Format)
            {
                case DXGI.Format.R10G10B10A2_UNorm:
                    return D3D9.Format.A2B10G10R10;
                case DXGI.Format.R16G16B16A16_Float:
                    return D3D9.Format.A16B16G16R16F;
                case DXGI.Format.B8G8R8A8_UNorm:
                    return D3D9.Format.A8R8G8B8;
                default:
                    return D3D9.Format.Unknown;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="texture"></param>
        /// <returns></returns>
        private IntPtr GetSharedHandle(D3D11.Texture2D texture)
        {
            using (var resource = texture.QueryInterface<DXGI.Resource>())
            {
                return resource.SharedHandle;
            }
        }

        /// <summary>
        /// 获取窗口指针
        /// </summary>
        /// <returns></returns>
        private static D3D9.PresentParameters GetPresentParameters()
        {
            var presendParams = new D3D9.PresentParameters();

            presendParams.Windowed = true;
            presendParams.SwapEffect = D3D9.SwapEffect.Discard;
            presendParams.PresentationInterval = D3D9.PresentInterval.Default;
            presendParams.DeviceWindowHandle = NativeMethods.GetDesktopWindow();
            presendParams.PresentationInterval = D3D9.PresentInterval.Default;

            return presendParams;
        }

    }
    public static class NativeMethods
    {
        /// <summary>
        /// 该函数返回桌面窗口的句柄。桌面窗口覆盖整个屏幕。桌面窗口是一个要在其上绘制所有的图标和其他窗口的区域。
        /// 【说明】获得代表整个屏幕的一个窗口（桌面窗口）句柄.
        /// </summary>
        /// <returns>返回值：函数返回桌面窗口的句柄。</returns>
        [DllImport("user32.dll", EntryPoint = "GetDesktopWindow", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr GetDesktopWindow();
    }
}
