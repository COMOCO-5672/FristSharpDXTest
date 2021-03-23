using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp
{
    /*********************************************************
	*作者 ：Admin
	*创建日期：2021/3/16 11:55:44
	*描述说明：模拟平台
	*
	*更改历史：
	*
	*******************************************************************
	* Copyright @ Admin 2021. All rights reserved.
	*******************************************************************
	*
	*********************************************************/
    public class Platform
    {

    }

    public enum SocketType
    {
        TCS_UDP = 0,
        TCS_TCP
    }
    public class PlatformLoginInfo
    {
        public string IP;
        public string Password;
        public string PlatformID;
        public string PlatformType;
        public System.UInt16 Port;
        public SocketType SocketType;
        public string UserName;
        public bool IsAuto;
        public string PlatformName;
    }
}
