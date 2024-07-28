using PicaPico;
using System.Runtime.InteropServices;
using System;
using System.Windows;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using System.Windows.Media;
using System.ComponentModel;
using Gma.System.MouseKeyHook;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace MiddleCV
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : FluentWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            InitWindowBackdropType();
            InitRunMode();
            Closing += MainWindow_Closing;
            SkipMiddle.IsChecked = true;
        }

        private const int MODE_ONEKEY = 1;
        private const int MODE_DOWBLE_KEY = 2;

        private void InitRunMode()
        {
            List<KeyValuePair<int, string>> modes = new List<KeyValuePair<int, string>>
            {
                new KeyValuePair<int, string>(MODE_ONEKEY, "单击鼠标中键复制粘贴"),
                new KeyValuePair<int, string>(MODE_DOWBLE_KEY, "单击复制，双击粘贴"),
            };
            RunMode.ItemsSource = modes;
            RunMode.SelectedValuePath = "Key";
            RunMode.DisplayMemberPath = "Value";
            RunMode.SelectedValue = MODE_ONEKEY;
        }

        private void InitWindowBackdropType()
        {
            if (IsMicaTabbedSupported)
            {
                WindowBackdropType = WindowBackdropType.Tabbed;
            }
            else if (IsMicaSupported)
            {
                WindowBackdropType = WindowBackdropType.Mica;
            }
            else if (IsAcrylicSupported)
            {
                WindowStyle = WindowStyle.None;
                AllowsTransparency = true;
                DragHelper.Visibility = Visibility.Visible;
                WindowBackdropType = WindowBackdropType.Acrylic;
                DragHelper.PreviewMouseLeftButtonDown += DragWindow;
                Activated += WindowActivated;
                Deactivated += WindowDeactivated;
            }
            else
            {
                WindowBackdropType = WindowBackdropType.Auto;
            }
            ThemeListener.ThemeChanged += ApplyTheme;
            ApplyTheme(ThemeListener.IsDarkMode);
            System.Drawing.Icon appIcon = System.Drawing.Icon.ExtractAssociatedIcon(GetExePath());
            Icon = Imaging.CreateBitmapSourceFromHIcon(
                        appIcon.Handle,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions());
            TitleBarIcon.Source = Icon;
        }

        string GetExePath()
        {
            Process currentProcess = Process.GetCurrentProcess();
            return currentProcess.MainModule?.FileName ?? string.Empty;
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();
#if NETFRAMEWORK
            if(!exited)
            {
                new Microsoft.Toolkit.Uwp.Notifications.ToastContentBuilder().AddText("MiddleCV 已最小化").Show();
            }
#endif
        }

        private void ApplyTheme(bool isDark)
        {
            ApplicationThemeManager.Apply(
              isDark ? ApplicationTheme.Dark : ApplicationTheme.Light,
              WindowBackdropType,
              true
            );
            if (WindowBackdropType == WindowBackdropType.Acrylic)
            {
                if (IsActive) WindowActivated(null, null);
                else WindowDeactivated(null, null);
            }
            else
            {
                WinBackground.Background = Brushes.Transparent;
            }
        }

        private void DragWindow(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (WindowStyle == WindowStyle.None)
            {
                DragMove();
            }
        }

        private readonly Brush _blackBackgroundA = new SolidColorBrush(Color.FromArgb(0xA0, 0x1F, 0x1F, 0x1F));
        private readonly Brush _whiteBackgroundA = new SolidColorBrush(Color.FromArgb(0xA0, 0xFF, 0xFF, 0xFF));
        private readonly Brush _blackBackground = new SolidColorBrush(Color.FromRgb(0x1F, 0x1F, 0x1F));
        private readonly Brush _whiteBackground = new SolidColorBrush(Color.FromRgb(0xFF, 0xFF, 0xFF));

        private void WindowActivated(object sender, EventArgs e)
        {
            WinBackground.Background = ThemeListener.IsDarkMode ? _blackBackgroundA : _whiteBackgroundA;
        }

        private void WindowDeactivated(object sender, EventArgs e)
        {
            WinBackground.Background = ThemeListener.IsDarkMode ? _blackBackground : _whiteBackground;
        }

        public static bool IsAcrylicSupported => IsWindowsNT && OSVersion >= new Version(10, 0) && OSVersion < new Version(10, 0, 22523);

        public static bool IsMicaSupported => IsWindowsNT && OSVersion >= new Version(10, 0, 21996);

        public static bool IsMicaTabbedSupported => IsWindowsNT && OSVersion >= new Version(10, 0, 22523);

        public static bool IsWindowsNT => Environment.OSVersion.Platform == PlatformID.Win32NT;

        private static readonly Version _osVersion = GetOSVersion();

        public static Version OSVersion => _osVersion;

        private static Version GetOSVersion()
        {
            var osv = new RTL_OSVERSIONINFOEX();
            osv.dwOSVersionInfoSize = (uint)Marshal.SizeOf(osv);
            _ = RtlGetVersion(out osv);
            return new Version((int)osv.dwMajorVersion, (int)osv.dwMinorVersion, (int)osv.dwBuildNumber);
        }

        [DllImport("ntdll.dll")]
        private static extern int RtlGetVersion(out RTL_OSVERSIONINFOEX lpVersionInformation);

        [StructLayout(LayoutKind.Sequential)]
        private struct RTL_OSVERSIONINFOEX
        {
            internal uint dwOSVersionInfoSize;
            internal uint dwMajorVersion;
            internal uint dwMinorVersion;
            internal uint dwBuildNumber;
            internal uint dwPlatformId;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            internal string szCSDVersion;
        }

        private bool exited = false;
        private void Exit(object sender, RoutedEventArgs e)
        {
            exited = true;
            Application.Current.Shutdown();
        }

        private void RunCV_Checked(object sender, RoutedEventArgs e)
        {
            SetMouseHook(RunCV.IsChecked ?? false);
        }

        private IKeyboardMouseEvents mouseHook;
        private const int BUTTON_MIDDLE = 0x00400000;

        private void SetMouseHook(bool running)
        {
            if(running)
            {
                mouseHook = Hook.GlobalEvents();
                mouseHook.MouseDownExt += OnMouseDownExt;
            }
            else if(mouseHook != null)
            {
                mouseHook.MouseDownExt -= OnMouseDownExt;
                mouseHook.Dispose();
                mouseHook = null;
            }
        }

        private CancellationTokenSource clickDelayToken;
        private void OnMouseDownExt(object sender, MouseEventExtArgs e)
        {
            if ((int)e.Button != BUTTON_MIDDLE)
            {
                return;
            }
            e.Handled = SkipMiddle.IsChecked ?? true;
            if (!(RunMode.SelectedValue is int))
            {
                return;
            }
            if ((int)RunMode.SelectedValue == MODE_DOWBLE_KEY)
            {
                if (clickDelayToken != null)
                {
                    clickDelayToken.Cancel();
                }
                clickDelayToken = new CancellationTokenSource();
            }
            RunCopyPaste(e.Clicks, clickDelayToken?.Token);
        }

        private bool lastCopy = false;
        private async void RunCopyPaste(int clicks, CancellationToken? cancellationToken)
        {
            if(cancellationToken != null)
            {
                try
                {
                    await Task.Delay(500, (CancellationToken)cancellationToken);
                }
                catch (Exception) { }
                if (((CancellationToken)cancellationToken).IsCancellationRequested)
                {
                    return;
                }
            }
            if (!(RunMode.SelectedValue is int))
            {
                return;
            }
            if ((int)RunMode.SelectedValue == MODE_DOWBLE_KEY)
            {
                if (clicks == 1)
                {
                    CopyedText.Text = GetWindowSelectedText();
                }
                else
                {
                    PasteText();
                }
            }
            else
            {
                if (lastCopy)
                {
                    PasteText();
                    lastCopy = false;
                }
                else
                {
                    string text = GetWindowSelectedText();
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        CopyedText.Text = text;
                        lastCopy = true;
                    }
                    else if(!string.IsNullOrWhiteSpace(CopyedText.Text))
                    {
                        PasteText();
                        lastCopy = false;
                    }
                }
            }
        }

        private string GetWindowSelectedText()
        {
            string text = string.Empty;
            RunAsSTA(() =>
            {
                try
                {
                    var originalData = Clipboard.GetDataObject();
                    SendCopy();
                    Thread.Sleep(100);
                    try
                    {
                        text = Clipboard.GetText();
                    }
                    catch { }
                    if (originalData != null) Clipboard.SetDataObject(originalData);
                } catch { }
            });
            return text;
        }

        private void PasteText()
        {
            string text = CopyedText.Text;
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }
            RunAsSTA(() =>
            {
                try
                {
                    var originalData = Clipboard.GetDataObject();
                    Clipboard.SetText(text);
                    SendCopy(true);
                    Thread.Sleep(100);
                    if (originalData != null) Clipboard.SetDataObject(originalData);
                }
                catch { }
            });
        }

        private static void RunAsSTA(Action threadStart)
        {
            try
            {
                Thread t = new Thread(new ThreadStart(threadStart));
                t.SetApartmentState(ApartmentState.STA);
                t.Start();
                t.Join();
            }
            catch (Exception)
            {
            }
        }

        static void SendCopy(bool isPaste=false)
        {
            INPUT[] inputs = new INPUT[1];
            inputs[0].type = INPUT_KEYBOARD;

            inputs[0].inputUnion.keyboardInput.wVk = VK_CONTROL;
            inputs[0].inputUnion.keyboardInput.dwFlags = KEYEVENTF_KEYDOWN;
            _ = SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT)));

            inputs[0].inputUnion.keyboardInput.wVk = isPaste ? VK_V : VK_C;
            inputs[0].inputUnion.keyboardInput.dwFlags = KEYEVENTF_KEYDOWN;
            _ = SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT)));

            inputs[0].inputUnion.keyboardInput.dwFlags = KEYEVENTF_KEYUP;
            _ = SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT)));

            inputs[0].inputUnion.keyboardInput.wVk = VK_CONTROL;
            inputs[0].inputUnion.keyboardInput.dwFlags = KEYEVENTF_KEYUP;
            _ = SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT)));
        }
        public const int INPUT_KEYBOARD = 1;
        public const ushort VK_CONTROL = 0x11;
        public const uint KEYEVENTF_KEYDOWN = 0x0000;
        public const uint KEYEVENTF_KEYUP = 0x0002;
        public const ushort VK_C = 0x43;
        public const ushort VK_V = 0x56;

        [DllImport("user32.dll")]
        internal static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [StructLayout(LayoutKind.Sequential)]
        public struct INPUT
        {
            public uint type;
            public InputUnion inputUnion;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct InputUnion
        {
            [FieldOffset(0)]
            public MOUSEINPUT mouseInput;
            [FieldOffset(0)]
            public KEYBDINPUT keyboardInput;
            [FieldOffset(0)]
            public HARDWAREINPUT hardwareInput;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct HARDWAREINPUT
        {
            public uint uMsg;
            public ushort wParamL;
            public ushort wParamH;
        }

        private void StartUp_Checked(object sender, RoutedEventArgs e)
        {
            SetStartup(StartUp.IsChecked ?? false);
        }

        private static void SetStartup(bool startup)
        {
            string startupFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            string appPath = Process.GetCurrentProcess()?.MainModule.FileName ?? string.Empty;
            string lnkPath = Path.Combine(startupFolderPath, Path.GetFileNameWithoutExtension(appPath) + ".lnk");
            var exists = File.Exists(lnkPath);
            if (exists && startup) return;
            if (!exists && !startup) return;
            if (startup)
            {
                ShellLink.Shortcut.CreateShortcut(appPath, "startup").WriteToFile(lnkPath);
            }
            else
            {
                File.Delete(lnkPath);
            }
        }

        private void CopyedText_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            CopyedText.IsReadOnly = !CopyedText.IsReadOnly;
        }

        private void RunTopmost_Checked(object sender, RoutedEventArgs e)
        {
            Topmost = RunTopmost.IsChecked ?? false;
        }
    }
}
