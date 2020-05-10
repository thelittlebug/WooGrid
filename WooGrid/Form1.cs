using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.IO;

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
            settings.Save();
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

        [DllImport("dwmapi.dll")]
        static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute, out RECT pvAttribute, int cbAttribute);


        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        private void printRect(RECT rect)
        {
            Debug.WriteLine($"{rect.Left} {rect.Top} {rect.Right} {rect.Bottom}");
        }

        private RECT getWindowGrid(IntPtr handle)
        {
            RECT pos;
            GetWindowRect(handle, out pos);

            int grid_w = Screen.PrimaryScreen.WorkingArea.Width / 4;
            int grid_h = Screen.PrimaryScreen.WorkingArea.Height / 4;

            return new RECT() {
                Left = Convert.ToInt32((double)pos.Left / grid_w),
                Top = Convert.ToInt32((double)pos.Top / grid_h),
                Right = Convert.ToInt32((double)(pos.Right - pos.Left) / grid_w),
                Bottom = Convert.ToInt32((double)(pos.Bottom - pos.Top) / grid_h)
            };
        }

        private RECT getWindowMargin(IntPtr handle)
        {
            RECT pos;
            RECT margin;

            GetWindowRect(handle, out pos);

            const int DWMWA_EXTENDED_FRAME_BOUNDS = 9;
            DwmGetWindowAttribute(GetForegroundWindow(), DWMWA_EXTENDED_FRAME_BOUNDS, out margin, Marshal.SizeOf<RECT>());

            return new RECT()
            {
                Left = Math.Abs(pos.Left - margin.Left),
                Top = Math.Abs(pos.Top - margin.Top),
                Right = Math.Abs(pos.Right - margin.Right),
                Bottom = Math.Abs(pos.Bottom - margin.Bottom)
            };
        }

        private void moveWindowAbsolute(RECT pos)
        {
            IntPtr handle = GetForegroundWindow();
            RECT margin = getWindowMargin(handle);

            int grid_w = Screen.PrimaryScreen.WorkingArea.Width / 4;
            int grid_h = Screen.PrimaryScreen.WorkingArea.Height / 4;

            MoveWindow(handle,
                pos.Left * grid_w - margin.Left,
                pos.Top * grid_h - margin.Top,
                pos.Right * grid_w + margin.Left + margin.Right,
                pos.Bottom * grid_h + margin.Top + margin.Bottom,
                true
            );
        }

        private void moveWindowRelative(RECT delta)
        {
            IntPtr handle = GetForegroundWindow();

            RECT w = getWindowGrid(handle);
            moveWindowAbsolute(new RECT() {
                Left = w.Left + delta.Left,
                Top = w.Top + delta.Top,
                Right = w.Right + delta.Right,
                Bottom = w.Bottom + delta.Bottom
            });
        }

        const int WAIT_FOR_SHORTCUT = 0;
        const int WAIT_FOR_CMD = 1;
        int state = WAIT_FOR_SHORTCUT;
        public List<Keys> PressedKeys = new List<Keys>();

        private void OnKeyPressed(object sender, GlobalKeyboardHookEventArgs e)
        {
            Keys loggedKey = e.KeyboardData.Key;

            if (e.KeyboardState == GlobalKeyboardHook.KeyboardState.KeyUp)
            {
                if (PressedKeys.Contains(loggedKey))
                {
                    PressedKeys.Remove(loggedKey);
                }
            }

            if (e.KeyboardState == GlobalKeyboardHook.KeyboardState.KeyDown)
            {
                if (state == WAIT_FOR_SHORTCUT)
                {
                    if (!PressedKeys.Contains(loggedKey))
                    {
                        PressedKeys.Add(loggedKey);
                    }

                    if (settings.SC.OrderBy(m => m).SequenceEqual(PressedKeys.OrderBy(m => m)))
                    {
                        state = WAIT_FOR_CMD;
                        e.Handled = true;
                        return;
                    }

                    if (settings.SC_MOVE_RIGHT.OrderBy(m => m).SequenceEqual(PressedKeys.OrderBy(m => m)))
                    {
                        moveWindowRelative(new RECT() { Left = 1, Top = 0, Right = 0, Bottom = 0 });
                        e.Handled = true;
                        return;
                    }

                    if (settings.SC_MOVE_LEFT.OrderBy(m => m).SequenceEqual(PressedKeys.OrderBy(m => m)))
                    {
                        moveWindowRelative(new RECT() { Left = -1, Top = 0, Right = 0, Bottom = 0 });
                        e.Handled = true;
                        return;
                    }

                    if (settings.SC_MOVE_UP.OrderBy(m => m).SequenceEqual(PressedKeys.OrderBy(m => m)))
                    {
                        moveWindowRelative(new RECT() { Left = 0, Top = -1, Right = 0, Bottom = 0 });
                        e.Handled = true;
                        return;
                    }

                    if (settings.SC_MOVE_DOWN.OrderBy(m => m).SequenceEqual(PressedKeys.OrderBy(m => m)))
                    {
                        moveWindowRelative(new RECT() { Left = 0, Top = 1, Right = 0, Bottom = 0 });
                        e.Handled = true;
                        return;
                    }

                    if (settings.SC_SIZE_X_PLUS.OrderBy(m => m).SequenceEqual(PressedKeys.OrderBy(m => m)))
                    {
                        moveWindowRelative(new RECT() { Left = 0, Top = 0, Right = 1, Bottom = 0 });
                        e.Handled = true;
                        return;
                    }
                    if (settings.SC_SIZE_X_MINUS.OrderBy(m => m).SequenceEqual(PressedKeys.OrderBy(m => m)))
                    {
                        moveWindowRelative(new RECT() { Left = 0, Top = 0, Right = -1, Bottom = 0 });
                        e.Handled = true;
                        return;
                    }
                    if (settings.SC_SIZE_Y_PLUS.OrderBy(m => m).SequenceEqual(PressedKeys.OrderBy(m => m)))
                    {
                        moveWindowRelative(new RECT() { Left = 0, Top = 0, Right = 0, Bottom = 1 });
                        e.Handled = true;
                        return;
                    }
                    if (settings.SC_SIZE_Y_MINUS.OrderBy(m => m).SequenceEqual(PressedKeys.OrderBy(m => m)))
                    {
                        moveWindowRelative(new RECT() { Left = 0, Top = 0, Right = 0, Bottom = -1 });
                        e.Handled = true;
                        return;
                    }
                    return;
                }

                if (state == WAIT_FOR_CMD)
                {
                    foreach (var position in settings.WindowPositions)
                    {
                        if (loggedKey == position.Key)
                        {
                            moveWindowAbsolute(new RECT() {
                                Left = position.Left,
                                Top = position.Top,
                                Right = position.Right,
                                Bottom = position.Bottom
                            });
                            state = WAIT_FOR_SHORTCUT;
                            e.Handled = true;
                            return;
                        }
                    }
                    return;
                }
            }
        }

        class MySettings : AppSettings<MySettings>
        {
            public int version = 0;
            public List<Keys> SC = new List<Keys>() { Keys.LControlKey, Keys.LShiftKey, Keys.Y };
            public List<Keys> SC_MOVE_RIGHT = new List<Keys>() { Keys.LControlKey, Keys.LShiftKey, Keys.A, Keys.Right };
            public List<Keys> SC_MOVE_LEFT = new List<Keys>() { Keys.LControlKey, Keys.LShiftKey, Keys.A, Keys.Left };
            public List<Keys> SC_MOVE_UP = new List<Keys>() { Keys.LControlKey, Keys.LShiftKey, Keys.A, Keys.Up };
            public List<Keys> SC_MOVE_DOWN = new List<Keys>() { Keys.LControlKey, Keys.LShiftKey, Keys.A, Keys.Down };
            public List<Keys> SC_SIZE_X_PLUS = new List<Keys>() { Keys.LControlKey, Keys.LShiftKey, Keys.S, Keys.Right };
            public List<Keys> SC_SIZE_X_MINUS = new List<Keys>() { Keys.LControlKey, Keys.LShiftKey, Keys.S, Keys.Left };
            public List<Keys> SC_SIZE_Y_PLUS = new List<Keys>() { Keys.LControlKey, Keys.LShiftKey, Keys.S, Keys.Down };
            public List<Keys> SC_SIZE_Y_MINUS = new List<Keys>() { Keys.LControlKey, Keys.LShiftKey, Keys.S, Keys.Up };
            public List<WindowPosition> WindowPositions = new List<WindowPosition>() {
                new WindowPosition() {
                    Key = Keys.F1,
                    Left = 0,
                    Top = 0,
                    Right = 2,
                    Bottom = 2
                },
                new WindowPosition() {
                    Key = Keys.F2,
                    Left = 2,
                    Top = 0,
                    Right = 4,
                    Bottom = 2
                },
                new WindowPosition() {
                    Key = Keys.F3,
                    Left = 0,
                    Top = 2,
                    Right = 2,
                    Bottom = 4
                },
                new WindowPosition() {
                    Key = Keys.F4,
                    Left = 2,
                    Top = 2,
                    Right = 4,
                    Bottom = 4
                }
            };
        }

        private void button3_Click(object sender, EventArgs e)
        {
            var curDir = Directory.GetCurrentDirectory();
            var file = $@"{curDir}\settings.json";
            Process.Start("notepad.exe", file);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            settings = MySettings.Load();
        }
    }
}
