using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using D3D9 = SharpDX.Direct3D9;

namespace WpfApp
{
    /*********************************************************
	*作者 ：Admin
	*创建日期：2021/3/16 16:34:59
	*描述说明：
	*
	*更改历史：
	*
	*******************************************************************
	* Copyright @ Admin 2021. All rights reserved.
	*******************************************************************
	*
	*********************************************************/
    public class D3DImageSource : IDisposable
    {
        private D3DImage imageSource;

        private D3D9.DeviceEx _d3d9Device;
        private D3D9.Surface _backBuffer;
        private D3D9.Surface _d3dSurface;

        private D3D9.Texture _renderTarget;

        private Int32Rect imageSourceRect;

        public D3DImage ImageSource { get => imageSource; private set => imageSource = value; }

        #region DX9
        public void InitDX9(int width, int height, int adapterId)
        {
            //DXVersion = DXVersionEnum.DX9;
            ImageSource = new D3DImage();
            CreateD3D9RenderTarget(width, height, adapterId);
        }

        public D3D9.Surface GetSurfaceDX9()
        {
            return _backBuffer;
        }

        public void Capture(string fileName)
        {
            if (_backBuffer != null)
                D3D9.Surface.ToFile(_backBuffer, fileName, D3D9.ImageFileFormat.Bmp);
        }

        /// <summary>
        /// 创建D3D渲染目标
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="adapterId"></param>
        private void CreateD3D9RenderTarget(int width, int height, int adapterId)
        {
            var d3DContext = new D3D9.Direct3DEx();

            var presentParams = GetPresentParameters_DX9(d3DContext, adapterId, width, height);

            D3D9.CreateFlags createFlags;

            D3D9.Capabilities caps = d3DContext.GetDeviceCaps(adapterId, D3D9.DeviceType.Hardware);

            if ((caps.DeviceCaps & D3D9.DeviceCaps.HWTransformAndLight) > 0)
                createFlags = D3D9.CreateFlags.HardwareVertexProcessing | D3D9.CreateFlags.Multithreaded;
            else
                createFlags = D3D9.CreateFlags.SoftwareVertexProcessing;

            _d3d9Device = new D3D9.DeviceEx(d3DContext, adapterId, D3D9.DeviceType.Hardware, IntPtr.Zero, createFlags, presentParams);

            _backBuffer = _d3d9Device.GetBackBuffer(0, 0);

            _d3dSurface = D3D9.Surface.CreateOffscreenPlain(_d3d9Device, width, height, D3D9.Format.X8R8G8B8, D3D9.Pool.SystemMemory);

            //_renderTarget = new D3D9.Texture(_d3d9Device, width, height, 1, D3D9.Usage.RenderTarget, D3D9.Format.X8R8G8B8, D3D9.Pool.Default);
            //using (var surface = _renderSurface.GetSurfaceLevel(0))
            //{
            //    imageSource.Lock();
            //    imageSource.SetBackBuffer(D3DResourceType.IDirect3DSurface9, surface.NativePointer);
            //    imageSource.Unlock();
            //}
            SetImageSourceBackBuffer(_backBuffer.NativePointer);
        }

        /// <summary>
        /// 配置D3D9参数信息信息
        /// </summary>
        /// <param name="d3DContext">D3d</param>
        /// <param name="adapterId">显卡序号</param>
        /// <param name="width">绘制宽度</param>
        /// <param name="height">绘制高度</param>
        /// <returns></returns>
        private static D3D9.PresentParameters GetPresentParameters_DX9(D3D9.Direct3DEx d3DContext, int adapterId, int width, int height)
        {
            var presentParams = new D3D9.PresentParameters();

            presentParams.Windowed = true;
            presentParams.BackBufferCount = 1;
            presentParams.SwapEffect = D3D9.SwapEffect.Discard;
            presentParams.DeviceWindowHandle = NativeMethods.GetDesktopWindow();//应该设置为IntPtr.Zero
            presentParams.BackBufferWidth = width;
            presentParams.BackBufferHeight = height;
            presentParams.PresentationInterval = D3D9.PresentInterval.Default;

            return presentParams;
        }

        /// <summary>
        /// 将绘制的内容与WPF的D3DImage进行绑定
        /// </summary>
        /// <param name="backbufferPtr"></param>
        private void SetImageSourceBackBuffer(IntPtr backbufferPtr)
        {
            if (!this.ImageSource.Dispatcher.CheckAccess())
            {
                this.ImageSource.Dispatcher.Invoke((Action)(() => this.SetImageSourceBackBuffer(backbufferPtr)));
                return;
            }
            this.ImageSource.Lock();
            this.ImageSource.SetBackBuffer(D3DResourceType.IDirect3DSurface9, backbufferPtr);
            this.ImageSource.Unlock();

            imageSourceRect = new Int32Rect(0, 0, this.ImageSource.PixelWidth, this.ImageSource.PixelHeight);
        }

        #endregion

        /// <summary>
        /// 将DX的Texture更新到DX9的Texture上并在WPF上显示出来
        /// </summary>
        public void OnRender()
        {
            try
            {
                if (this.imageSource != null && !this.imageSource.Dispatcher.CheckAccess())
                {
                    if (!this.imageSource.Dispatcher.HasShutdownStarted)
                        this.imageSource.Dispatcher.Invoke(() => this.OnRender());
                    return;
                }
                if (!ImageSource.IsFrontBufferAvailable) return;

                ImageSource.Lock();
                ImageSource.SetBackBuffer(D3DResourceType.IDirect3DSurface9, _d3d9Device.GetBackBuffer(0, 0).NativePointer);
                ImageSource.AddDirtyRect(imageSourceRect);
                ImageSource.Unlock();
            }
            catch (Exception ex)
            {
                // TextLog.SaveError(ex.Message);
            }
            //stopwatch.Restart();

           

            //stopwatch.Stop();
            //long spendtime = stopwatch.ElapsedMilliseconds;
            //if (spendtime > 10)
            //{
            //    LogManager.Log("time: " + spendtime, LogManager.MessageType.Info, false);
            //}
        }

        public void Dispose()
        {
            try
            {
                _d3dSurface?.Dispose();
                _backBuffer?.Dispose();
                _d3d9Device?.Dispose();
            }
            catch (Exception ex)
            {
                //// TextLog.SaveError("error while disposing. ErrorMsg: " + ex.Message);
            }
        }

        public static class NativeMethods
        {
            [DllImport("user32.dll", SetLastError = false)]
            public static extern IntPtr GetDesktopWindow();
        }
    }
}
