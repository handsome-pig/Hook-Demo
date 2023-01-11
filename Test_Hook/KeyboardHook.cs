using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Diagnostics;
using System.ComponentModel;

namespace Test_Hook
{
    class KeyboardHook
    {
        private const int KEYBOARD = 2;
        private const int KEYBOARD_LL = 13;
        private const int KEYDOWN = 0X100;
        private const int KEYUP = 0X101;
        private const int SYSKEYDOWN = 0x104;
        private const int SYSKEYUP = 0X105;
        private const byte VKCODE_SHIFT = 0X10;
        private const byte VKCODE_CAPSLOCK = 0X14;

        private static Win32API.HookProc _keyboardHookProc;
        private int _keyboardHook;

        public delegate void KeyDownEvent(KeyEventArgs keyEventArgs);
        private KeyDownEvent _KeyDownEvent;

        public event KeyEventHandler _keyDownHandle;
        public event KeyEventHandler _keyUpHandle;
        private event KeyPressEventHandler _keyPressHandle;


        public void SetKeyboardHook()
        {
            _keyboardHookProc = new Win32API.HookProc(KeyboardHookProc);
            ProcessModule _module = Process.GetCurrentProcess().MainModule;
            IntPtr _moduleHandle = Win32API.GetModuleHandle(_module.ModuleName);
            _keyboardHook = Win32API.SetWindowsHookEx(KEYBOARD_LL, _keyboardHookProc, _moduleHandle, 0);

            if (_keyboardHook == 0)
            {
                int errorCode = Marshal.GetLastWin32Error();
                UnhookKeyboard();
                throw new Win32Exception(errorCode);
            }
        }

        /// <summary>
        /// 设置按下键的委托事件
        /// </summary>
        /// <param name="callback"></param>
        public void SetKeyDownEvent(KeyDownEvent callback)
        {
            _KeyDownEvent = callback;
        }

        public void UnhookKeyboard()
        {
            if (_keyboardHook != 0)
            {
                int retKeyboard = Win32API.UnhookWindowsHookEx(_keyboardHook);
                _keyboardHook = 0;
                if (retKeyboard == 0)
                {
                    int errorCode = Marshal.GetLastWin32Error();
                    throw new Win32Exception(errorCode);
                }
            }
        }

        /// <summary>
        /// Hook回调函数
        /// </summary>
        /// <param name="nCode">Hook ID</param>
        /// <param name="wParam"></param>
        /// <param name="lParam"></param>
        /// <returns>下一个钩挂的子程ID</returns>
        public int KeyboardHookProc(int nCode, int wParam, IntPtr lParam)
        {
            bool handled = false;
            if (nCode >= 0 && (_keyDownHandle != null || _keyUpHandle != null || _keyPressHandle != null))
            {
                Win32API.KeyboardHookStruct _keyboardHookStruct = (Win32API.KeyboardHookStruct)Marshal.PtrToStructure(lParam, typeof(Win32API.KeyboardHookStruct));

                if (_keyDownHandle != null && (wParam == KEYDOWN || wParam == SYSKEYDOWN))
                {
                    Keys _keyData = (Keys)_keyboardHookStruct.vkCode; // vkVode还是虚拟键，考虑用KeyInterop.KeyFromVirtualKey转为Key键
                    KeyEventArgs e = new KeyEventArgs(_keyData);
                    _keyDownHandle.Invoke(this, e);
                    handled = handled || e.Handled;
                }

                if (_keyUpHandle != null && (wParam == KEYUP || wParam == SYSKEYUP))
                {
                    Keys _keyData = (Keys)_keyboardHookStruct.vkCode;
                    KeyEventArgs e = new KeyEventArgs(_keyData);
                    _keyUpHandle.Invoke(this, e);
                    handled = handled || e.Handled;
                }

                if (_keyPressHandle != null && wParam == KEYDOWN)
                {
                    bool isDownShift = (Win32API.GetKeyState(VKCODE_SHIFT) & 0x80) == 0x80 ? true : false;
                    bool isDownCapslock = Win32API.GetKeyState(VKCODE_CAPSLOCK) != 0 ? true : false;

                    byte[] keyState = new byte[256];
                    Win32API.GetKeyboardState(keyState);
                    byte[] inBuffer = new byte[2];
                    if (Win32API.ToAscii(_keyboardHookStruct.vkCode, _keyboardHookStruct.scanCode, keyState, inBuffer, _keyboardHookStruct.flags) == 1)
                    {
                        char key = (char)inBuffer[0]; // 用户按下键的ASCII码
                        if ((isDownShift ^ isDownCapslock) && Char.IsLetter(key)) key = Char.ToUpper(key);
                        KeyPressEventArgs e = new KeyPressEventArgs(key);
                        _keyPressHandle.Invoke(this, e);
                        handled = handled || e.Handled;
                    }
                }
            }

            if (handled) return 1; // 阻止消息传递
            else return Win32API.CallNextHookEx(_keyboardHook, nCode, wParam, lParam);
        }
    }
}
