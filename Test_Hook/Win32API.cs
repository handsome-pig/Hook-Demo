using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows;

namespace Test_Hook
{
    public class Win32API
    {
        #region 结构体成员内存布局

        [StructLayout(LayoutKind.Sequential)]
        public class Point
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public class MouseHookStruct
        {
            public Point point;
            public int hwnd;
            public int wHitTestCode;
            public int dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public class MouseLLHookStruct
        {
            public Point point;
            public int mouseData;
            public int flags;
            public int time;
            public int dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public class KeyboardHookStruct
        {
            public int vkCode;
            public int scanCode;
            public int flags;
            public int time;
            public int dwExtraInfo;
        }

        #endregion

        #region DLL非托管函数调用

        /// <summary>
        /// Hook子程，即回调函数委托，监控某一特定类型的事件
        /// </summary>
        /// <param name="nCode">Hook ID</param>
        /// <param name="wParam"></param>
        /// <param name="lParam"></param>
        /// <returns></returns>
        public delegate int HookProc(int nCode, int wParam, IntPtr lParam);

        /// <summary>
        /// 安装Hook
        /// </summary>
        /// <param name="idHook">钩子的类型，即它处理的消息类型（键盘钩子，鼠标钩子等）</param>
        /// <param name="lpfn">钩子回调函数的地址指针，即接收的消息由谁处理</param>
        /// <param name="hInstance">需要钩子拦截的程序句柄，0/null为当前进程/模块，</param>
        /// <param name="dwThreaId">与Hook回调相关联线程的标识符，若为0，则回调与所有线程相关联，即全局Hook</param>
        /// <returns>Hook回调的句柄</returns>
        [DllImport("user32.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hInstance, int dwThreaId);

        /// <summary>
        /// 卸载Hook
        /// </summary>
        /// <param name="idHook"></param>
        /// <returns></returns>
        [DllImport("user32.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int UnhookWindowsHookEx(int idHook);

        /// <summary>
        /// 下一个钩挂的函数
        /// </summary>
        /// <param name="idHook">当前Hook的句柄</param>
        /// <param name="nCode">传给Hook过程的事件ID</param>
        /// <param name="wParam"></param>
        /// <param name="lParam"></param>
        /// <returns></returns>
        [DllImport("user32.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Auto)]
        public static extern int CallNextHookEx(int idHook, int nCode, int wParam, IntPtr lParam);


        [DllImport("user32.dll")]
        public static extern int ToAscii(int virtualKeyCode, int scanCode, byte[] keyState, byte[] transKey, int fullState);

        [DllImport("user32.dll")]
        public static extern int GetKeyboardState(byte[] keyState);

        [DllImport("user32.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Auto)]
        public static extern short GetKeyState(int key);

        [DllImport("kernel32.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Auto)]
        public static extern IntPtr GetModuleHandle(string moduleName);

        #endregion

        public struct Dpi
        {
            public double X { get; set; }
            public double Y { get; set; }

            public Dpi(double x, double y)
            {
                X = x;
                Y = y;
            }
        }

        public static Dpi GetDpiFromVisual(Visual visual)
        {
            var source = PresentationSource.FromVisual(visual);

            var dpiX = 96.0;
            var dpiY = 96.0;

            if (source?.CompositionTarget != null)
            {
                dpiX = 96.0 * source.CompositionTarget.TransformToDevice.M11;
                dpiY = 96.0 * source.CompositionTarget.TransformToDevice.M22;
            }

            return new Dpi(dpiX, dpiY);
        }
    }
}
