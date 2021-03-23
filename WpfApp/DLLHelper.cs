using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp
{
    /*********************************************************
	*作者 ：Admin
	*创建日期：2021/3/18 14:07:04
	*描述说明：
	*
	*更改历史：
	*
	*******************************************************************
	* Copyright @ Admin 2021. All rights reserved.
	*******************************************************************
	*
	*********************************************************/
    public static class DLLHelper
    {
        private const string dllname = "DllTest.dll";

		[DllImport(dllname,EntryPoint = "add")]
        public static extern char add(int sessionId, ResD3D9[] p, int size);
    }
}
