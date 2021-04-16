using RenderCore.DataStruct;
using System;
using System.Collections.Generic;
using System.Windows.Threading;

namespace RenderCore
{
    public class RenderSourceInfo
    {
        public Guid ID;

        public RenderSourceInfo()
        {
            ID = Guid.NewGuid();
        }

        public int Height { get; set; }
        public bool Used { get; set; }
        public int Width { get; set; }
        public IRenderSource RenderSource { get; set; }
    }

    public sealed class RenderSourceManager
    {
        public static readonly RenderSourceManager Current = new RenderSourceManager();
        public static bool UseD3D9 = true;
        private object locker = new object();

        private List<RenderSourceInfo> renderSourceInfoList = new List<RenderSourceInfo>();

        private RenderSourceManager()
        {
        }

        public int Count { get { return renderSourceInfoList.Count; } }

        /// <summary>
        /// 回收RenderSourceInfo
        /// </summary>
        /// <param name="id"></param>
        public void Close(Guid id)
        {
            lock (locker)
            {
                var find = renderSourceInfoList.Find(e => e.ID == id);
                if (find != null)
                {
                    find.Used = false;
                    // TextLog.SaveDebug("RenderSource空闲成功，当前数量为 " + renderSourceInfoList.Count);
                }
            }
        }

        // TODO 谁调用
        public void Dispose()
        {
            renderSourceInfoList.ForEach(e => e.RenderSource.Dispose());
        }

        /// <summary>
        /// 获取RenderSourceInfo
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public RenderSourceInfo GetRenderSource(Dispatcher dispatcher, int width, int height)
        {
            lock (locker)
            {
                var find = renderSourceInfoList.Find(e => e.Width == width && e.Height == height && !e.Used);
                if (find != null)
                {
                    find.Used = true;
                    // TextLog.SaveDebug("RenderSource复用成功，当前数量为 " + renderSourceInfoList.Count);
                    return find;
                }

                IRenderSource renderSource = null;

                dispatcher.Invoke(() =>
                {
                    try
                    {
                        if (UseD3D9)
                            renderSource = new D3DImageSource();
                        else
                            renderSource = new WriteableBitmapSource();

                        var setupSuccess = renderSource.SetupSurface(width, height, FrameFormat.YV12);

                        if (!setupSuccess)
                        {
                        }

                        // TextLog.SaveError("Renderer.Core renderSource.SetupSurface error");
                    }
                    catch (Exception ex)
                    {
                        // TextLog.SaveError("renderSource 创建失败：" + ex.Message);
                        // throw new NotComponentException(ex.Message);
                    }
                });

                var renderSourceInfo = new RenderSourceInfo
                {
                    RenderSource = renderSource,
                    Used = true,
                    Width = width,
                    Height = height
                };

                renderSourceInfoList.Add(renderSourceInfo);

                // TextLog.SaveDebug("新RenderSource申请成功，当前数量为 " + renderSourceInfoList.Count);

                return renderSourceInfo;
            }
        }
    }
}