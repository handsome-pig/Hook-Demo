using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.ComponentModel;
using System.Windows.Threading;
using System.Threading;

namespace Test_Hook
{
    public class MouseHook
    {
        // 鼠标动作
        private const int MOUSE = 7;
        private const int MOUSE_LL = 14;
        private const int MOUSE_MOVE = 0x200;
        private const int LBUTTON_DOWN = 0x201;
        private const int RBUTTON_DOWN = 0X204;
        private const int MBUTTON_DOWN = 0X207;
        private const int LBUTTON_UP = 0X202;
        private const int RBUTTON_UP = 0X205;
        private const int MBUTTON_UP = 0X208;
        private const int LBUTTONDBCLICK = 0X203;
        private const int RBUTTONDBCLICK = 0X206;
        private const int MBUTTONDBCLICK = 0X209;
        private const int MOUSEWHEEL = 0X20A;

        private int _mouseHook;
        private static Win32API.HookProc _mouseHookProc;
        public delegate bool MouseEvent(MouseEventArgs mouseEventArgs);
        private MouseEvent _mouseEvent;

        private TimeSpan _lastClickTime = new TimeSpan(DateTime.Now.Ticks);
        private TimeSpan _curClickTime = new TimeSpan();

        public void SetMouseHook()
        {
            _mouseHookProc = new Win32API.HookProc(MouseHookProc);
            ProcessModule _module = Process.GetCurrentProcess().MainModule; // 当前进程主模块
            IntPtr _moduleHandle = Win32API.GetModuleHandle(_module.ModuleName);
            _mouseHook = Win32API.SetWindowsHookEx(MOUSE_LL, _mouseHookProc, _moduleHandle, 0);

            if (_mouseHook == 0)
            {
                int errorCode = Marshal.GetLastWin32Error();
                UnhookMouse();
                throw new Win32Exception(errorCode);
            }
        }

        /// <summary>
        /// 设置鼠标的委托事件
        /// </summary>
        /// <param name="callback"></param>
        public void SetMouseEvent(MouseEvent callback)
        {
            _mouseEvent = callback;
        }

        /// <summary>
        /// 卸载鼠标Hook
        /// </summary>
        public void UnhookMouse()
        {
            if (_mouseHook != 0)
            {
                int retMouse = Win32API.UnhookWindowsHookEx(_mouseHook);
                _mouseHook = 0;
                if (retMouse == 0)
                {
                    int errorCode = Marshal.GetLastWin32Error();
                    throw new Win32Exception(errorCode);
                }
            }
        }

        /// <summary>
        /// 监控鼠标事件
        /// </summary>
        /// <param name="nCode"></param>
        /// <param name="wParam"></param>
        /// <param name="lParam"></param>
        /// <returns></returns>
        private int MouseHookProc(int nCode, int wParam, IntPtr lParam)
        {
            bool isMouseInWindow = true;
            MouseEventArgs _MouseArgs;
            if (nCode >= 0)
            {
                Win32API.MouseLLHookStruct _mouseLLHookStruct = (Win32API.MouseLLHookStruct)Marshal.PtrToStructure(lParam, typeof(Win32API.MouseLLHookStruct));
                MouseButtons _button = MouseButtons.None;
                int clickCount = 0;
                short mouseDelta = 0;
                if (wParam == LBUTTON_DOWN)
                {
                    _button = MouseButtons.Left;
                    _curClickTime = new TimeSpan(DateTime.Now.Ticks);
                    bool isDoubleClick = _curClickTime.Subtract(_lastClickTime) < new TimeSpan(0, 0, 0, 0, 300) ? true : false; // 敲击间隔小于0.3s，视为双击
                    clickCount = isDoubleClick ? 2 : 1;
                    _lastClickTime = _curClickTime;

                    _MouseArgs = new MouseEventArgs(_button, clickCount, _mouseLLHookStruct.point.X, _mouseLLHookStruct.point.Y, mouseDelta);
                    isMouseInWindow = _mouseEvent.Invoke(_MouseArgs);

                    if (!isMouseInWindow) return 1; // 阻止此消息继续向下传递
                }
                else if (wParam == RBUTTON_DOWN || wParam == RBUTTON_UP)
                {
                    _button = MouseButtons.Right;
                    clickCount = 1;

                    _MouseArgs = new MouseEventArgs(_button, clickCount, _mouseLLHookStruct.point.X, _mouseLLHookStruct.point.Y, mouseDelta);
                    isMouseInWindow = _mouseEvent.Invoke(_MouseArgs);

                    if (!isMouseInWindow) return 1;
                }
            }

            return Win32API.CallNextHookEx(_mouseHook, nCode, wParam, lParam);
        }
    }
}

