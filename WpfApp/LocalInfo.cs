using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;

namespace WpfApp
{
    /*********************************************************
	*作者 ：Admin
	*创建日期：2021/3/22 18:08:07
	*描述说明：
	*
	*更改历史：
	*
	*******************************************************************
	* Copyright @ Admin 2021. All rights reserved.
	*******************************************************************
	*
	*********************************************************/
    public static class LocalInfo
    {
        public static SDKHelper.LoginInfo LoginInfo { get; set; } = new SDKHelper.LoginInfo()
        {
			ip = "192.168.5.123",
			port = 81,
			username = "admin",
			password = "trkj@88888",
			st=SDKHelper.SocketType.TCS_TCP,
			callback = new SDKHelper.ConnectionCallback()
            {
				Callback = null
            }
        };

        public static string PlatformID { get; set; } = "8";

		public static ConcurrentDictionary<string,int> _platformIDList = new ConcurrentDictionary<string, int>();
    }
}
