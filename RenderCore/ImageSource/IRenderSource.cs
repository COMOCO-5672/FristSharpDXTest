using RenderCore.DataStruct;
using System;
using System.Windows.Media;

namespace RenderCore
{
    public interface IRenderSource : IDisposable
    {
        event Action DisposeCompleteEvent;

        event EventHandler ImageSourceChanged;

        ImageSource ImageSource { get; }

        bool CheckFormat(FrameFormat format);

        void Render(IntPtr buffer);

        void Render(IntPtr yBuffer, IntPtr uBuffer, IntPtr vBuffer);

        bool SetupSurface(int videoWidth, int videoHeight, FrameFormat format);
    }
}