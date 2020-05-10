using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Drawing;

namespace WooGrid
{
    public partial class Form1 : Form
    {
        MySettings settings;
        private GlobalKeyboardHook _globalKeyboardHook;
        public Form1()
        {
            InitializeComponent();
            _globalKeyboardHook = new GlobalKeyboardHook();
            _globalKeyboardHook.KeyboardPressed += OnKeyPressed;
            notifyIcon1.Icon = this.Icon;
            settings = MySettings.Load();
            
            
            //WindowPositions = new List<WindowPosition>();
            dataGridView1.DataSource = settings.WindowPositions;
        }

        private void Form1_Resize(object sender, System.EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                Hide();
                this.notifyIcon1.Visible = true;
            }
        }
        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            Show();
            this.WindowState = FormWindowState.Normal;
            notifyIcon1.Visible = false;
        }

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int Width, int Height, bool Repaint);
        
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;        // x position of upper-left corner  
            public int Top;         // y position of upper-left corner  
            public int Right;       // x position of lower-right corner  
            public int Bottom;      // y position of lower-right corner  
        }

        private void MoveActiveWindow(WindowPosition position)
        {
            IntPtr handle = GetForegroundWindow();
            MoveWindow(handle, position.Left, position.Top, position.Width, position.Height, true);
        }

        private void GetActiveWindowPosition(Keys key)
        {
            IntPtr handle = GetForegroundWindow();
            RECT pos;
            GetWindowRect(handle, out pos);

            settings.WindowPositions.Add(new WindowPosition() { Key = key, Screen = 1, Top = pos.Top, Left = pos.Left, Width = pos.Right - pos.Left, Height = pos.Bottom - pos.Top });
            // dataGridView1.DataSource = null;
            dataGridView1.DataSource = settings.WindowPositions;

        }

        const int WAIT_FOR_HOTKEY_1 = 0;
        const int WAIT_FOR_HOTKEY_2 = 1;
        const int WAIT_FOR_CMD = 2;
        const int WAIT_FOR_KEY = 3;
        
        int state = WAIT_FOR_HOTKEY_1;

        private void OnKeyPressed(object sender, GlobalKeyboardHookEventArgs e)
        {
            // EDT: No need to filter for VkSnapshot anymore. This now gets handled
            // through the constructor of GlobalKeyboardHook(...).
            if (e.KeyboardState == GlobalKeyboardHook.KeyboardState.KeyDown)
            {
                Debug.WriteLine(state);
                Keys loggedKey = e.KeyboardData.Key;

                if (state == WAIT_FOR_HOTKEY_1)
                {
                    if (loggedKey == Keys.LWin) state = WAIT_FOR_HOTKEY_2;
                    return;
                }

                if (state == WAIT_FOR_HOTKEY_2)
                {
                    if (loggedKey == Keys.LShiftKey) state = WAIT_FOR_CMD;
                    return;
                }

                if (state == WAIT_FOR_KEY)
                {
                    GetActiveWindowPosition(loggedKey);
                    state = WAIT_FOR_HOTKEY_1;
                    return;
                }

                if (state == WAIT_FOR_CMD)
                {
                    if (loggedKey == Keys.L)
                    {
                        state = WAIT_FOR_KEY;
                        return;
                    }
                    
                    foreach (var position in settings.WindowPositions)
                    {
                        if (loggedKey == position.Key)
                        {
                            MoveActiveWindow(position);
                            state = WAIT_FOR_HOTKEY_1;
                            return;
                        }
                    }
                    return;
                }
                Debug.WriteLine(state);
            }
        }

        class MySettings : AppSettings<MySettings>
        {
            public int version = 0;
            public List<WindowPosition> WindowPositions = new  List<WindowPosition>();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            settings.Save();
        }
    }
}
