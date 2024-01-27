using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;
using WindowsInput.Native;
using System.Runtime.InteropServices.WindowsRuntime;
using WindowsInput;
using System.Diagnostics;

namespace AutoRDP
{
    public class AutoRDP
    {
        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern IntPtr GetWindowThreadProcessId(IntPtr hWnd, IntPtr ProcessId);

        [DllImport("user32.dll")]
        static extern IntPtr AttachThreadInput(IntPtr idAttach, IntPtr idAttachTo, bool fAttach);

        [DllImport("user32.dll")]
        static extern IntPtr GetFocus();

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll", EntryPoint = "SendMessage", CharSet = CharSet.Auto)]
        public static extern bool SendMessage(IntPtr hWnd, uint Msg, int wParam, StringBuilder lParam);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SendMessage(int hWnd, int Msg, int wparam, int lparam);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern uint RealGetWindowClass(IntPtr hwnd, [Out] StringBuilder pszType, uint cchType);

        const int WM_GETTEXT = 0x000D;
        const int WM_GETTEXTLENGTH = 0x000E;

        private readonly InputSimulator inputSimulator = new InputSimulator();

        public AutoRDP()
        {
            
        }

        /// <summary>
        /// Auto inserts password in RDP authentication dialog with timeout in seconds.
        /// </summary>
        /// <param name="password">Password to auto instert</param>
        /// <param name="timeout">Timeout value in seconds</param>
        /// <exception cref="TimeoutException"></exception>
        public async void Login(string username, string password, int timeout, int delayInputs = 250)
        {
            await CancelAfterAsync(ct => LoginAsync(username, password, ct, delayInputs), TimeSpan.FromSeconds(timeout)).ConfigureAwait(false);
        }

        /// <inheritdoc cref="Login"/>
        public async Task LoginAsync(string username, string password, CancellationToken cancellationToken, int delayInputs = 250)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            while (!cancellationToken.IsCancellationRequested)
            {
                var hwnd = this.FocusedControlInActiveWindow();
                var title = GetWindowTitle(hwnd);
                if (title == "Windows Security")
                {
                    // Password -> More choices
                    await Task.Delay(delayInputs, cancellationToken).ConfigureAwait(false);
                    this.inputSimulator.Keyboard.KeyPress(VirtualKeyCode.TAB);
                    // Open 'More choices'
                    this.inputSimulator.Keyboard.KeyPress(VirtualKeyCode.SPACE);
                    await Task.Delay(delayInputs, cancellationToken).ConfigureAwait(false);
                    // More choices -> First user
                    this.inputSimulator.Keyboard.KeyPress(VirtualKeyCode.TAB);
                    // First user -> Use a different account
                    this.inputSimulator.Keyboard.KeyPress(VirtualKeyCode.TAB);
                    // Open 'Use a different account'
                    this.inputSimulator.Keyboard.KeyPress(VirtualKeyCode.SPACE);
                    await Task.Delay(delayInputs, cancellationToken).ConfigureAwait(false);
                    // Insert username
                    this.inputSimulator.Keyboard.TextEntry(username);
                    // Username -> Password
                    this.inputSimulator.Keyboard.KeyPress(VirtualKeyCode.TAB);
                    // Inser Password
                    this.inputSimulator.Keyboard.TextEntry(password);
                    // Connect
                    this.inputSimulator.Keyboard.KeyPress(VirtualKeyCode.RETURN);
                    return;
                }
                await Task.Delay(200, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Retrieves the handle to the window that has the keyboard focus, 
        /// if the window is attached to the calling thread's message queue.
        /// </summary>
        /// <remarks>https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getfocus</remarks>
        /// <returns>The return value is the handle to the window with the keyboard focus. 
        /// If the calling thread's message queue does not have an associated window with 
        /// the keyboard focus, the return value is NULL.</returns>
        public IntPtr FocusedControlInActiveWindow()
        {
            IntPtr activeWindowHandle = GetForegroundWindow();

            IntPtr activeWindowThread = GetWindowThreadProcessId(activeWindowHandle, IntPtr.Zero);
            IntPtr thisWindowThread = GetWindowThreadProcessId(new Form().Handle, IntPtr.Zero);

            AttachThreadInput(activeWindowThread, thisWindowThread, true);
            IntPtr focusedControlHandle = GetFocus();
            AttachThreadInput(activeWindowThread, thisWindowThread, false);

            return focusedControlHandle;
        }

        /// <summary>
        /// Copies the text of the specified window's title bar (if it has one) into a buffer. 
        /// If the specified window is a control, the text of the control is copied. However, 
        /// GetWindowText cannot retrieve the text of a control in another application.
        /// </summary>
        /// <remarks>https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getwindowtexta</remarks>
        /// <param name="hWnd">A handle to the window or control containing the text.</param>
        /// <returns>
        /// GetWindowText retrieves the text of a window. For regular windows, 
        /// this is the text which appears in the title bar. For controls, 
        /// this is the text in the control. Note that GetWindowText cannot 
        /// retrieve the text in a control owned by another program. 
        /// To get that text, use the WM_GETTEXT message instead.</returns>
        public string GetWindowTitle(IntPtr hWnd)
        {
            var length = GetWindowTextLength(hWnd) + 1;
            var title = new StringBuilder(length);
            GetWindowText(hWnd, title, length);
            return title.ToString();
        }

        /// <summary>
        /// Copies the text that corresponds to a window into a buffer provided by the caller.
        /// </summary>
        /// <remarks>https://learn.microsoft.com/en-us/windows/win32/winmsg/wm-gettext</remarks>
        /// <param name="hWnd">A handle to the window whose window procedure will receive the message. 
        /// If this parameter is HWND_BROADCAST ((HWND)0xffff), 
        /// the message is sent to all top-level windows in the system, 
        /// including disabled or invisible unowned windows, overlapped windows, 
        /// and pop-up windows; but the message is not sent to child windows.</param>
        /// <returns>The return value is the number of characters copied, 
        /// not including the terminating null character.</returns>
        public string GetControlText(IntPtr hWnd)
        {

            // Get the size of the string required to hold the window title (including trailing null.) 
            Int32 titleSize = SendMessage((int)hWnd, WM_GETTEXTLENGTH, 0, 0).ToInt32();

            // If titleSize is 0, there is no title so return an empty string (or null)
            if (titleSize == 0)
                return String.Empty;

            StringBuilder title = new StringBuilder(titleSize + 1);

            SendMessage(hWnd, (int)WM_GETTEXT, title.Capacity, title);

            return title.ToString();
        }

        /// <summary>
        /// Start a task with timeout.
        /// </summary>
        /// <param name="startTask">Task to wait for timeout</param>
        /// <exception cref="TimeoutException"></exception>
        internal static async Task CancelAfterAsync(Func<CancellationToken, Task> startTask, TimeSpan timeout)
        {
            using (var timeoutCancellation = new CancellationTokenSource())
            {
                var originalTask = startTask(timeoutCancellation.Token);
                var delayTask = Task.Delay(timeout, timeoutCancellation.Token);
                var completedTask = await Task.WhenAny(originalTask, delayTask);
                // Cancel timeout to stop either task:
                // - Either the original task completed, so we need to cancel the delay task.
                // - Or the timeout expired, so we need to cancel the original task.
                // Canceling will not affect a task, that is already completed.
                timeoutCancellation.Cancel();
                if (completedTask != originalTask)
                {
                    // timeout
                    throw new TimeoutException();
                }
            }
        }
    }
}
