using SharpDX.Mathematics.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using D3D9 = SharpDX.Direct3D9;

namespace TR.BL.VideoManager.MultiGPUPlayer
{
    public class ChunkedRender : IDisposable
    {
        private IntPtr _targetHandle;

        private int imgWidth;
        private int imgHeight;

        private D3D9.DeviceEx _d3d9Device;
        private D3D9.Surface _backBuffer;
        private D3D9.Surface _d3dSurface;

        public void InitDX9(int width, int height, int adapterId, IntPtr targetHandle)
        {
            imgWidth = width;
            imgHeight = height;

            _targetHandle = targetHandle;
            CreateD3D9RenderTarget(width, height, adapterId, targetHandle);
        }

        public D3D9.Surface GetSurfaceDX9()
        {
            return _backBuffer;
        }

        private void CreateD3D9RenderTarget(int width, int height, int adapterId, IntPtr targetHandle)
        {
            var d3DContext = new D3D9.Direct3DEx();

            var presentParams = GetPresentParameters_DX9(d3DContext, adapterId, width, height, targetHandle);

            D3D9.CreateFlags createFlags;
            D3D9.Capabilities caps = d3DContext.GetDeviceCaps(adapterId, D3D9.DeviceType.Hardware);
            if ((caps.DeviceCaps & D3D9.DeviceCaps.HWTransformAndLight) > 0)
                createFlags = D3D9.CreateFlags.HardwareVertexProcessing;
            //createFlags = D3D9.CreateFlags.HardwareVertexProcessing | D3D9.CreateFlags.Multithreaded;
            else
                createFlags = D3D9.CreateFlags.SoftwareVertexProcessing;

            _d3d9Device = new D3D9.DeviceEx(d3DContext, adapterId, D3D9.DeviceType.Hardware, targetHandle, createFlags, presentParams);
            _backBuffer = _d3d9Device.GetBackBuffer(0, 0);

            _d3dSurface = D3D9.Surface.CreateOffscreenPlain(_d3d9Device, width, height, D3D9.Format.X8R8G8B8, D3D9.Pool.SystemMemory);

            //测试覆盖
            //InitTestOverlay(_d3d9Device);
        }

        private static D3D9.PresentParameters GetPresentParameters_DX9(D3D9.Direct3DEx d3DContext, int adapterId, int width, int height, IntPtr targetHandle)
        {
            var d3ddm = d3DContext.GetAdapterDisplayMode(adapterId);

            var presentParams = new D3D9.PresentParameters();

            presentParams.Windowed = true;
            presentParams.BackBufferCount = 1;
            presentParams.SwapEffect = D3D9.SwapEffect.Discard;
            presentParams.DeviceWindowHandle = IntPtr.Zero;
            presentParams.BackBufferWidth = width;
            presentParams.BackBufferHeight = height;
            presentParams.BackBufferFormat = d3ddm.Format;
            presentParams.FullScreenRefreshRateInHz = 0;
            presentParams.PresentationInterval = D3D9.PresentInterval.Immediate;

            return presentParams;
        }

        private DateTime _lastRenderErrorLogTime = DateTime.Now.AddHours(-1);
        public void OnRender()
        {
            try
            {
                //测试覆盖
                //TestRenderOverlayText();
                _d3d9Device.Present();
            }
            catch (Exception ex)
            {
                if (_lastRenderErrorLogTime.AddMinutes(5) < DateTime.Now)
                {
                    _lastRenderErrorLogTime = DateTime.Now;
                }
            }

        }

        public void Dispose()
        {
            try
            {
                _d3dSurface?.Dispose();
                _backBuffer?.Dispose();
                _d3d9Device?.Dispose();

                //DisposeTestOverlay();
            }
            catch (Exception ex)
            {

            }
        }

        #region TestOverlay
        private D3D9.FontDescription fontDescription;
        private D3D9.Font font;
        private string displayText = "Direct3D9 Text!";
        private int xDir = 1;
        private int yDir = 1;
        private RawRectangle fontDimension;

        private RawColorBGRA colorWhite = new RawColorBGRA(255, 255, 255, 255);

        private RawVector2[] lineVector = new RawVector2[5];
        private D3D9.Line line;
        private D3D9.Texture carrierLogo;
        private D3D9.Sprite sprite;
        private RawVector3 logoPos = new RawVector3();

        private void InitTestOverlay(D3D9.Device device)
        {

            fontDescription = new D3D9.FontDescription()
            {
                Height = 72,
                Italic = false,
                CharacterSet = D3D9.FontCharacterSet.Ansi,
                FaceName = "Arial",
                MipLevels = 0,
                OutputPrecision = D3D9.FontPrecision.TrueType,
                PitchAndFamily = D3D9.FontPitchAndFamily.Default,
                Quality = D3D9.FontQuality.ClearType,
                Weight = D3D9.FontWeight.Bold
            };

            font = new D3D9.Font(device, fontDescription);
            fontDimension = font.MeasureText(null, displayText, new RawRectangle(0, 0, imgWidth, imgHeight), D3D9.FontDrawFlags.Center | D3D9.FontDrawFlags.VerticalCenter);

            carrierLogo = D3D9.Texture.FromFile(device, @"D:\projects\PanoramaClient\bin\Debug\Images\airline\cn.png");
            sprite = new D3D9.Sprite(device);

            line = new D3D9.Line(device);
        }

        private void TestRenderOverlayText()
        {
            // Make the text boucing on the screen limits
            if ((fontDimension.Right + xDir) > imgWidth)
                xDir = -1;
            else if ((fontDimension.Left + xDir) <= 0)
                xDir = 1;

            if ((fontDimension.Bottom + yDir) > imgHeight)
                yDir = -1;
            else if ((fontDimension.Top + yDir) <= 0)
                yDir = 1;

            fontDimension.Left += (int)xDir;
            fontDimension.Top += (int)yDir;
            fontDimension.Bottom += (int)yDir;
            fontDimension.Right += (int)xDir;

            //计算直线位置
            lineVector[0].X = fontDimension.Left;
            lineVector[0].Y = fontDimension.Top;
            lineVector[1].X = fontDimension.Right;
            lineVector[1].Y = fontDimension.Top;
            lineVector[2].X = fontDimension.Right;
            lineVector[2].Y = fontDimension.Bottom;
            lineVector[3].X = fontDimension.Left;
            lineVector[3].Y = fontDimension.Bottom;
            lineVector[4].X = fontDimension.Left;
            lineVector[4].Y = fontDimension.Top;

            //计算logo位置
            logoPos.X = fontDimension.Left;
            logoPos.Y = fontDimension.Top - 30;

            _d3d9Device.BeginScene();

            //画方框
            line.Draw(lineVector, colorWhite);
            // Draw the text
            font.DrawText(null, displayText, fontDimension, D3D9.FontDrawFlags.Center | D3D9.FontDrawFlags.VerticalCenter, colorWhite);
            //画logo
            sprite.Begin(D3D9.SpriteFlags.AlphaBlend);
            sprite.Draw(carrierLogo, colorWhite, null, null, logoPos);
            sprite.End();

            _d3d9Device.EndScene();
        }

        private void DisposeTestOverlay()
        {
            line?.Dispose();
            carrierLogo?.Dispose();
            sprite?.Dispose();
        }
        #endregion
    }
}
