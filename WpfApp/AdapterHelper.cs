using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace WpfApp
{
    /*********************************************************
	*作者 ：Admin
	*创建日期：2021/3/17 9:39:02
	*描述说明：
	*
	*更改历史：
	*
	*******************************************************************
	* Copyright @ Admin 2021. All rights reserved.
	*******************************************************************
	*
	*********************************************************/
    public class AdapterHelper
    {
        private static readonly Point ZeroPoint = new Point(0, 0);

        /// <summary>
        /// 获取显卡输出口与显卡的对应关系
        /// </summary>
        /// <returns></returns>
        public static Dictionary<IntPtr, int> GetMonitorAndAdapterMap()
        {
            Dictionary<IntPtr, int> result = new Dictionary<IntPtr, int>();

            var factory = new SharpDX.DXGI.Factory1();
            var adapters = factory.Adapters;
            for (int i = 0; i < adapters.Length; i++)
            {
                var adapter = adapters[i];
                foreach (var op in adapter.Outputs)
                {
                    result[op.Description.MonitorHandle] = i;
                }
            }

            return result;
        }

        /// <summary>
        /// 根据控件坐标，获取其对应的显卡输出口及显卡信息
        /// </summary>
        /// <param name="uiElements"></param>
        /// <returns></returns>
        public static List<AdapterInfo> GetAdapterInfos(IEnumerable<UIElement> uiElements)
        {
            List<AdapterInfo> result = null;
            if (uiElements != null)
            {
                result = new List<AdapterInfo>();
                var monitorAdapterMap = GetMonitorAndAdapterMap();

                foreach (var ui in uiElements)
                {
                    //获取每个控件的屏幕坐标
                    Point point = ui.PointToScreen(ZeroPoint);
                    //根据控件坐标获取其所在的显卡及显卡输出口名称
                    SlimDX.Windows.DisplayMonitor monitor = SlimDX.Windows.DisplayMonitor.FromPoint(new System.Drawing.Point((int)point.X, (int)point.Y));

                    if (monitorAdapterMap.TryGetValue(monitor.Handle, out int adapterIndex))
                    {
                        result.Add(new AdapterInfo()
                        {
                            AdapterIndex = adapterIndex,
                            OutputName = monitor.DeviceName
                        });
                    }
                    else
                    {
                        throw new InvalidOperationException($"错误，找不到点（{point.X}, {point.Y}）对应的显示器");
                    }
                }
            }
            return result;
        }

        public static AdapterInfo GetAdapterInfo(UIElement uiElement, double? width, double? height)
        {
            if (uiElement == null)
                return GetDefaultAdapterInfo();

            //获取控件的屏幕坐标
            Point point = uiElement.PointToScreen(ZeroPoint);

            //程序全屏时PointToScreen函数返回值相对于屏幕点（0,0）左上角坐标出现负数
            if (point.X < 0 || point.Y < 0)
            {
                point = new Point(0, 0);
            }

            var monitorAdapterMap = GetMonitorAndAdapterMap();

            AdapterInfo adapterInfo = TryGetAdapterInfo(new System.Drawing.Point((int)point.X, (int)point.Y), monitorAdapterMap);

            if (adapterInfo == null && width != null && height != null)
                adapterInfo = TryGetAdapterInfo(new System.Drawing.Point((int)(point.X + width) / 2, (int)(point.Y + height) / 2), monitorAdapterMap);

            if (adapterInfo == null)
                adapterInfo = GetDefaultAdapterInfo();
            return adapterInfo;
        }

        public static AdapterInfo GetDefaultAdapterInfo(Dictionary<IntPtr, int> monitorAdapterMap = null)
        {
            monitorAdapterMap = monitorAdapterMap ?? GetMonitorAndAdapterMap();
            SlimDX.Windows.DisplayMonitor monitor = SlimDX.Windows.DisplayMonitor.EnumerateMonitors()[0];
            AdapterInfo adapterInfo = null;
            if (monitorAdapterMap.TryGetValue(monitor.Handle, out int adapterIndex))
            {
                adapterInfo = new AdapterInfo()
                {
                    AdapterIndex = adapterIndex,
                    OutputName = monitor.DeviceName
                };
            }
            return adapterInfo;
        }

        public static AdapterInfo TryGetAdapterInfo(System.Drawing.Point point, Dictionary<IntPtr, int> monitorAdapterMap = null)
        {
            SlimDX.Windows.DisplayMonitor monitor = SlimDX.Windows.DisplayMonitor.FromPoint(point);
            monitorAdapterMap = monitorAdapterMap ?? GetMonitorAndAdapterMap();
            if (monitorAdapterMap.TryGetValue(monitor.Handle, out int adapterIndex))
            {
                return new AdapterInfo()
                {
                    AdapterIndex = adapterIndex,
                    OutputName = monitor.DeviceName
                };
            }
            return null;
        }
    }

    public class AdapterInfo
    {
        /// <summary>
        /// 显卡的索引编号
        /// </summary>
        public int AdapterIndex;

        /// <summary>
        /// 显卡输出名称
        /// </summary>
        public string OutputName;
    }
}
