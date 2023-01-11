using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Threading;

namespace Test_Hook
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private MouseHook _mouseHook = new MouseHook();
        private KeyboardHook _keyboardHook = new KeyboardHook();
        private NotifyIcon notifyIcon;
        private bool isMouseInWindow = true;

        public MainWindow()
        {
            InitializeComponent();
            StartHook();
            Notify();
        }

        private void StartHook()
        {
            _mouseHook.SetMouseHook();
            _mouseHook.SetMouseEvent(MouseDownHandle);

            _keyboardHook.SetKeyboardHook();
            _keyboardHook._keyDownHandle += (s, e) => { if (WindowState != WindowState.Minimized) e.Handled = true; }; // 指示已处理Hook事件，阻止消息传递
        }

        /// <summary>
        /// 鼠标响应事件
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private bool MouseDownHandle(MouseEventArgs args)
        {
            if (WindowState == WindowState.Minimized) return true;

            Point relativeLoc = PointFromScreen(new Point(args.Location.X, args.Location.Y)); // 转换参考系为当前窗口
            // 窗口内动作
            if ((relativeLoc.X > 0 && relativeLoc.X < Width) && (relativeLoc.Y > 0 && relativeLoc.Y < Height))
            {
                isMouseInWindow = true;
                // 双击：最小化，取消屏蔽
                if (args.Clicks == 2)
                {
                    WindowState = WindowState.Minimized;
                    Hide();
                }
            }
            // 窗口外动作
            else isMouseInWindow = false;

            return isMouseInWindow;
        }

        /// <summary>
        /// 最小化到托盘
        /// </summary>
        private void Notify()
        {
            notifyIcon = new NotifyIcon();
            notifyIcon.Text = "Hook屏蔽工具";
            notifyIcon.Icon = new System.Drawing.Icon(@"F:\Test\Test_Hook\Test_Hook\Downloads.ico");
            notifyIcon.Visible = true;
            notifyIcon.MouseDoubleClick += (s, e) => { Show(); WindowState = WindowState == WindowState.Normal ? WindowState.Minimized : WindowState.Normal; };
        }

        /// <summary>
        /// 窗口成为后台程序时发生
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Deactivated(object sender, EventArgs e)
        {
            Topmost = true;
        }

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            _mouseHook.UnhookMouse();
            _keyboardHook.UnhookKeyboard();
            notifyIcon.Dispose();
            Close();
        }
    }
}
