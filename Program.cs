using System;
using System.Runtime.InteropServices;
using System.Security.Principal;

class Program
{
    // 定义 ConsoleCtrlDelegate 委托
    delegate bool ConsoleCtrlDelegate(int CtrlType);

    // 定义常量
    const int WM_HOTKEY = 0x0312;
    const uint MOD_CTRL = 0x0002;
    const uint VK_F1 = 0x70;
    const uint VK_HOME = 0x24;
    const int HOTKEY_ID = 18;

    const int MOUSEEVENTF_LEFTDOWN = 0x02;
    const int MOUSEEVENTF_LEFTUP = 0x04;


    static void Main(string[] args)
    {
        System.Console.WriteLine(args.Length);
        int delay = 300;
        if(args.Length != 0){
            delay = int.Parse(args[0]);
        }

        var currentPrincipal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
        bool isAdmin = currentPrincipal.IsInRole(WindowsBuiltInRole.Administrator);
        if (!isAdmin)
        {
            Console.WriteLine($"当前用户{(isAdmin ? "" : "不")}具有管理员权限");
            System.Console.WriteLine("请以管理员权限运行该程序！");
            Console.ReadKey();
            return;
        }

        // 注册全局热键
        RegisterHotKey(IntPtr.Zero, HOTKEY_ID, MOD_CTRL, VK_HOME);
        Console.Write("按");
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write("Ctrl+Home");
        Console.ResetColor();
        Console.Write("键: ");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write("开启");
        Console.ResetColor();
        Console.Write("/");
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Write("关闭");
        Console.ResetColor();
        System.Console.WriteLine("自动点击功能");


        // 设置信号处理函数
        SetConsoleCtrlHandler(new ConsoleCtrlDelegate(ConsoleCtrlHandler), true);

        bool isMouseClicking = false;
        Task task = null;
        CancellationTokenSource cts = null;

        // 进入消息循环
        while (true)
        {
            var msg = new MSG();
            if (GetMessage(out msg, IntPtr.Zero, 0, 0) == 0)
            {
                break;
            }

            // 处理热键消息
            if (msg.message == WM_HOTKEY && msg.wParam.ToInt32() == HOTKEY_ID)
            {
                if (isMouseClicking)
                {
                    // 停止模拟鼠标点击
                    Console.Beep(800, 100);
                    cts.Cancel();
                    isMouseClicking = false;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("关闭.");
                    Console.ResetColor();
                }
                else
                {
                    // 开始模拟鼠标点击
                    Console.Beep(1200, 200);
                    cts = new CancellationTokenSource();
                    task = ClickMouseAsync(delay, cts.Token);
                    isMouseClicking = true;
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("启动...");
                    Console.ResetColor();
                }
            }

            // 分发消息
            TranslateMessage(ref msg);
            DispatchMessage(ref msg);
        }
    }

    static async Task ClickMouseAsync(int delay, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, IntPtr.Zero); // 模拟鼠标左键点击
            await Task.Delay(50);
            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, IntPtr.Zero);   // 模拟鼠标左键松开
            await Task.Delay(delay);
        }
    }

    static bool ConsoleCtrlHandler(int ctrlType)
    {
        System.Console.WriteLine(ctrlType);
        if (ctrlType == 2 || ctrlType == 0) // CTRL_CLOSE_EVENT
        {
            UnregisterHotKey(IntPtr.Zero, HOTKEY_ID);   // 注销全局热键
        }
        return false; // 继续运行默认的信号处理函数
    }

    // 定义 MSG 结构体
    [StructLayout(LayoutKind.Sequential)]
    struct MSG
    {
        public IntPtr hwnd;
        public uint message;
        public IntPtr wParam;
        public IntPtr lParam;
        public uint time;
        public POINT pt;
    }

    // 定义 POINT 结构体
    [StructLayout(LayoutKind.Sequential)]
    struct POINT
    {
        public int x;
        public int y;
    }

    [DllImport("user32.dll")]
    static extern int GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

    [DllImport("user32.dll")]
    static extern bool TranslateMessage([In] ref MSG lpMsg);

    [DllImport("user32.dll")]
    static extern IntPtr DispatchMessage([In] ref MSG lpmsg);

    [DllImport("user32.dll")]
    static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool SetConsoleCtrlHandler(ConsoleCtrlDelegate handlerRoutine, bool add);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, IntPtr dwExtraInfo);

    [DllImport("user32.dll")]
    static extern bool GetCursorPos(out POINT lpPoint);
}
