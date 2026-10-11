using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;

namespace VinXiangQi
{
    // 统一坐标转换与鼠标注入（无 Mainform 依赖，可被产品与测试台共用）。
    // 关键概念：
    //  物理屏幕坐标(physical)  — 真实屏幕像素，前台点击/截图用
    //  目标客户区逻辑坐标(logical) — 目标窗口自己理解的客户区坐标（DPI 不感知窗口会被系统缩放，logical×scale=physical）
    //  位图像素(bitmap pixel)  — 识别用的截图位图内的像素
    // 前台抓屏(universal front)：位图=物理像素，原点=窗口矩形(DWM)左上角
    // 后台 PrintWindow(universal rear)：位图=客户区逻辑像素，原点=客户区左上角（PW_CLIENTONLY）
    public static class WinCoord
    {
        [StructLayout(LayoutKind.Sequential)]
        struct RECT { public int L, T, R, B; }
        [StructLayout(LayoutKind.Sequential)]
        struct POINT { public int X, Y; }
        [StructLayout(LayoutKind.Sequential)]
        struct MOUSEINPUT { public int dx, dy; public uint mouseData, dwFlags, time; public IntPtr dwExtraInfo; }
        [StructLayout(LayoutKind.Sequential)]
        struct INPUT { public uint type; public MOUSEINPUT mi; }

        [DllImport("user32.dll")] static extern bool GetClientRect(IntPtr h, out RECT r);
        [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr h, out RECT r);
        [DllImport("user32.dll")] static extern bool ClientToScreen(IntPtr h, ref POINT p);
        [DllImport("user32.dll")] static extern IntPtr GetWindowDpiAwarenessContext(IntPtr h);
        [DllImport("user32.dll")] static extern IntPtr SetThreadDpiAwarenessContext(IntPtr ctx);
        [DllImport("dwmapi.dll")] static extern int DwmGetWindowAttribute(IntPtr h, int a, out RECT r, int s);
        [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr h, IntPtr pid);
        [DllImport("kernel32.dll")] static extern uint GetCurrentThreadId();
        [DllImport("user32.dll")] static extern bool AttachThreadInput(uint a, uint b, bool f);
        [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr h);
        [DllImport("user32.dll")] static extern bool BringWindowToTop(IntPtr h);
        [DllImport("user32.dll")] static extern bool ShowWindowAsync(IntPtr h, int cmd);
        [DllImport("user32.dll")] static extern bool SetCursorPos(int x, int y);
        [DllImport("user32.dll")] static extern bool GetCursorPos(out POINT p);
        [DllImport("user32.dll")] static extern uint SendInput(uint n, INPUT[] inputs, int size);
        [DllImport("user32.dll")] static extern bool PostMessage(IntPtr h, int msg, IntPtr wp, IntPtr lp);
        [DllImport("user32.dll")] static extern bool PrintWindow(IntPtr h, IntPtr hdc, uint flags);
        [DllImport("user32.dll")] static extern bool IsWindow(IntPtr h);

        const int SW_RESTORE = 9;
        const uint INPUT_MOUSE = 0;
        const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        const uint MOUSEEVENTF_LEFTUP = 0x0004;
        const int WM_ACTIVATE = 0x0006;
        const int WA_CLICKACTIVE = 2;
        const int WM_MOUSEMOVE = 0x0200;
        const int WM_LBUTTONDOWN = 0x0201;
        const int WM_LBUTTONUP = 0x0202;
        const int MK_LBUTTON = 1;

        // 目标显示缩放：物理客户区宽 / 目标逻辑客户区宽（DPI 不感知且被系统缩放时 >1）
        public static double ScaleOf(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero) return 1.0;
            try
            {
                RECT pr; GetClientRect(hwnd, out pr);   // 本进程为感知进程 → 物理
                IntPtr old = SetThreadDpiAwarenessContext(GetWindowDpiAwarenessContext(hwnd));
                RECT lr; GetClientRect(hwnd, out lr);   // 目标上下文 → 逻辑
                SetThreadDpiAwarenessContext(old);
                int pw = pr.R - pr.L, lw = lr.R - lr.L;
                int ph = pr.B - pr.T, lh = lr.B - lr.T;
                if (lw > 0 && lh > 0 && pw > 0 && ph > 0)
                {
                    double sx = (double)pw / lw, sy = (double)ph / lh;
                    double s = (sx + sy) / 2.0;
                    if (s >= 0.5 && s <= 8.0) return s;
                }
            }
            catch { }
            return 1.0;
        }

        // 客户区左上角的物理屏幕坐标
        public static Point ClientOriginPhysical(IntPtr hwnd)
        {
            POINT p = new POINT();
            ClientToScreen(hwnd, ref p);
            return new Point(p.X, p.Y);
        }

        // 窗口(DWM 扩展边框，与前台抓屏矩形一致)左上角的物理屏幕坐标
        public static Point WindowOriginPhysical(IntPtr hwnd)
        {
            RECT r;
            int hr = DwmGetWindowAttribute(hwnd, 9, out r, Marshal.SizeOf(typeof(RECT)));
            if (hr != 0) GetWindowRect(hwnd, out r);
            return new Point(r.L, r.T);
        }

        // 非客户区偏移（客户区相对窗口原点的物理偏移），用于前台位图 → 客户区坐标
        public static Point NonClientOffsetPhysical(IntPtr hwnd)
        {
            Point c = ClientOriginPhysical(hwnd);
            Point w = WindowOriginPhysical(hwnd);
            return new Point(c.X - w.X, c.Y - w.Y);
        }

        // ---- 位图坐标 → 物理屏幕坐标 ----
        // 前台抓屏位图：像素=物理像素，原点=窗口矩形左上角
        public static Point FrontBitmapToScreen(Point bmp, IntPtr hwnd)
        {
            Point w = WindowOriginPhysical(hwnd);
            return new Point(w.X + bmp.X, w.Y + bmp.Y);
        }
        // 后台 PrintWindow 位图（PW_CLIENTONLY，客户区）：像素=目标逻辑客户区像素
        public static Point RearBitmapToScreen(Point bmp, IntPtr hwnd)
        {
            Point c = ClientOriginPhysical(hwnd);
            double s = ScaleOf(hwnd);
            return new Point(c.X + (int)Math.Round(bmp.X * s), c.Y + (int)Math.Round(bmp.Y * s));
        }

        // ---- 位图坐标 → 目标逻辑客户区坐标（PostMessage 用） ----
        public static Point FrontBitmapToClientLogical(Point bmp, IntPtr hwnd)
        {
            Point nc = NonClientOffsetPhysical(hwnd);
            double s = ScaleOf(hwnd);
            return new Point(
                (int)Math.Round((bmp.X - nc.X) / s),
                (int)Math.Round((bmp.Y - nc.Y) / s));
        }
        public static Point RearBitmapToClientLogical(Point bmp)
        {
            return bmp;
        }

        // ---- 前台点击：置前(带线程附着) + 光标定位校验 + SendInput 按下/抬起 ----
        // 第二十四轮：句柄存活检测（窗口已关闭/重建时停止使用旧句柄）
        public static bool IsWindowAlive(IntPtr hwnd)
        {
            try { return hwnd != IntPtr.Zero && IsWindow(hwnd); }
            catch { return false; }
        }

        public static bool ForceForeground(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero) return false;
            if (GetForegroundWindow() == hwnd) return true;
            uint myThread = GetCurrentThreadId();
            IntPtr fg = GetForegroundWindow();
            uint fgThread = fg == IntPtr.Zero ? 0 : GetWindowThreadProcessId(fg, IntPtr.Zero);
            bool attached = false;
            try
            {
                if (fgThread != 0 && fgThread != myThread) attached = AttachThreadInput(myThread, fgThread, true);
                ShowWindowAsync(hwnd, SW_RESTORE);
                BringWindowToTop(hwnd);
                SetForegroundWindow(hwnd);
            }
            catch { }
            finally
            {
                if (attached) AttachThreadInput(myThread, fgThread, false);
            }
            Thread.Sleep(60);
            return GetForegroundWindow() == hwnd;
        }

        static void MouseInput(uint flags)
        {
            INPUT[] one = new INPUT[1];
            one[0].type = INPUT_MOUSE;
            one[0].mi.dwFlags = flags;
            SendInput(1, one, Marshal.SizeOf(typeof(INPUT)));
        }

        public static bool ForegroundClick(IntPtr hwnd, Point screen, int preWaitMs = 30, int holdMs = 40)
        {
            bool fgOk = ForceForeground(hwnd);
            if (!SetCursorPos(screen.X, screen.Y)) return false;
            POINT p; GetCursorPos(out p);
            if (Math.Abs(p.X - screen.X) > 2 || Math.Abs(p.Y - screen.Y) > 2)
            {
                SetCursorPos(screen.X, screen.Y);   // 一次重试
                Thread.Sleep(20);
            }
            if (preWaitMs < 0) preWaitMs = 0;
            if (preWaitMs > 1000) preWaitMs = 1000;
            if (holdMs < 10) holdMs = 10;
            if (holdMs > 500) holdMs = 500;
            Thread.Sleep(preWaitMs);   // 让目标收到移动消息后再按下
            MouseInput(MOUSEEVENTF_LEFTDOWN);
            Thread.Sleep(holdMs);
            MouseInput(MOUSEEVENTF_LEFTUP);
            return fgOk;
        }

        // ---- 统一坐标换算 ----
        // 抓屏位图有两种来源（与 ScreenshotOnce 的两种模式对应）：
        //   FrontWindowPhysical：前台抓屏（CopyFromScreen 窗口矩形），像素=物理像素，原点=窗口矩形左上角
        //   RearClientTarget   ：后台抓屏（PrintWindow 客户区），像素=目标自己的客户区像素，原点=客户区左上角
        public enum CaptureSpace { None, FrontWindowPhysical, RearClientTarget }

        // 位图坐标 → 物理屏幕坐标
        public static Point BitmapToScreen(CaptureSpace space, Point bmp, IntPtr gameHwnd)
        {
            if (space == CaptureSpace.FrontWindowPhysical)
            {
                Point w = WindowOriginPhysical(gameHwnd);   // 与前台抓屏同一来源(DWM 扩展边框)
                return new Point(w.X + bmp.X, w.Y + bmp.Y);
            }
            else
            {
                Point c = ClientOriginPhysical(gameHwnd);
                double s = ScaleOf(gameHwnd);
                return new Point(c.X + (int)Math.Round(bmp.X * s), c.Y + (int)Math.Round(bmp.Y * s));
            }
        }

        // 位图坐标 → 投递给 clickHwnd 的消息坐标（PostMessage 的 lParam 值）。
        // 推导：先换算成物理屏幕坐标，再减去目标客户区物理原点；
        // 系统向 DPI 不感知的目标投递时会自动除以缩放比 → 目标收到的恰好是它自己的客户区坐标。
        // 子窗口（ClickHandle 与 GameHandle 不同）自动成立，无需额外偏移。
        public static Point BitmapToMessageLParam(CaptureSpace space, Point bmp, IntPtr gameHwnd, IntPtr clickHwnd)
        {
            Point phys = BitmapToScreen(space, bmp, gameHwnd);
            IntPtr target = clickHwnd == IntPtr.Zero ? gameHwnd : clickHwnd;
            Point c0 = ClientOriginPhysical(target);
            return new Point(phys.X - c0.X, phys.Y - c0.Y);
        }

        // 窗口矩形的物理尺寸（DWM 扩展边框；与前台抓屏/位图原点一致，不做任何缩放换算）
        public static Rectangle WindowRectPhysical(IntPtr hwnd)
        {
            RECT r;
            int hr = DwmGetWindowAttribute(hwnd, 9, out r, Marshal.SizeOf(typeof(RECT)));
            if (hr != 0) GetWindowRect(hwnd, out r);
            return new Rectangle(r.L, r.T, r.R - r.L, r.B - r.T);
        }

        // 目标自己的客户区尺寸（目标 DPI 上下文：感知窗口=物理，不感知窗口=逻辑）
        public static Size ClientSizeTarget(IntPtr hwnd)
        {
            IntPtr old = SetThreadDpiAwarenessContext(GetWindowDpiAwarenessContext(hwnd));
            RECT r; GetClientRect(hwnd, out r);
            SetThreadDpiAwarenessContext(old);
            return new Size(r.R - r.L, r.B - r.T);
        }

        // R36：坐标范围纯函数——点是否落在 [0,宽) × [0,高) 内（独立可测）
        public static bool IsPointInSize(Point p, Size size)
        {
            return p.X >= 0 && p.Y >= 0 && p.X < size.Width && p.Y < size.Height;
        }

        // R36：后台点击客户区越界校验。lp 单位与 BitmapToMessageLParam 输出一致（物理像素偏移），
        // 界限取「目标物理客户区尺寸」= 目标客户区尺寸 × 缩放。越界消息会被目标窗口忽略——
        // 实测症状：投递 ok=True 但棋盘永远不变（「棋盘未改变/落子多次未成功」循环的成因之一）。
        public static bool IsClientPointInRange(IntPtr hwnd, Point p)
        {
            if (hwnd == IntPtr.Zero) return false;
            try
            {
                Size sz = ClientSizeTarget(hwnd);
                double s = ScaleOf(hwnd);
                int w = (int)Math.Round(sz.Width * s);
                int h = (int)Math.Round(sz.Height * s);
                if (w <= 0 || h <= 0) return false;
                return IsPointInSize(p, new Size(w, h));
            }
            catch { return false; }
        }

        // 后台抓屏：PrintWindow(PW_CLIENTONLY|PW_RENDERFULLCONTENT) 渲染客户区。
        // 渲染尺寸 = 目标自己的客户区像素（感知窗口=物理、不感知=逻辑），与 CaptureSpace.RearClientTarget 一致。
        public static Bitmap CaptureClient(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero) return new Bitmap(1, 1);
            Size sz = ClientSizeTarget(hwnd);
            if (sz.Width <= 0 || sz.Height <= 0) return new Bitmap(1, 1);
            Bitmap bmp = new Bitmap(sz.Width, sz.Height, PixelFormat.Format32bppArgb);
            try
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    IntPtr hdc = g.GetHdc();
                    try { PrintWindow(hwnd, hdc, 3); }   // 1=CLIENTONLY 2=RENDERFULLCONTENT
                    finally { g.ReleaseHdc(hdc); }
                }
            }
            catch { }
            return bmp;
        }

        // ---- 后台点击：lParam 用 BitmapToMessageLParam 的结果（勿再缩放） ----
        // 策略（0=默认；入口在“截图/鼠标高级设置”弹窗）：
        //   0 兼容序列：回放原版程序的鼠标消息序列——部分棋软（尤其对消息序列敏感的程序）用此形态
        //      才能识别点击：移动带 MK_LBUTTON、按下/抬起错开 1 像素；
        //   1 标准序列：移动/按下/抬起同坐标；
        //   2 激活式：先发 WM_ACTIVATE(WA_CLICKACTIVE)（部分程序要求先收到“被点击激活”消息）再走兼容序列。
        // 返回 false 表示消息投递失败（窗口句柄无效等），调用方应提示用户。
        public static bool BackgroundClick(IntPtr hwnd, Point lparamPoint, int holdMs, int strategy)
        {
            if (hwnd == IntPtr.Zero) return false;
            if (holdMs < 10) holdMs = 10;
            if (holdMs > 500) holdMs = 500;
            if (strategy < 0 || strategy > 2) strategy = 0;
            int x = lparamPoint.X, y = lparamPoint.Y;
            bool ok = true;
            if (strategy == 2)
            {
                ok &= PostMessage(hwnd, WM_ACTIVATE, (IntPtr)WA_CLICKACTIVE, IntPtr.Zero);
            }
            if (strategy == 1)
            {
                IntPtr lp1 = MakeLParam(x, y);
                ok &= PostMessage(hwnd, WM_MOUSEMOVE, IntPtr.Zero, lp1);
                ok &= PostMessage(hwnd, WM_LBUTTONDOWN, (IntPtr)MK_LBUTTON, lp1);
                Thread.Sleep(holdMs);
                ok &= PostMessage(hwnd, WM_LBUTTONUP, IntPtr.Zero, lp1);
            }
            else
            {
                // 兼容序列（原版行为）：移动 wParam=MK_LBUTTON；按下在 y-1、抬起在 y+1
                int yDown = y > 0 ? y - 1 : y;
                ok &= PostMessage(hwnd, WM_MOUSEMOVE, (IntPtr)MK_LBUTTON, MakeLParam(x, y));
                ok &= PostMessage(hwnd, WM_LBUTTONDOWN, (IntPtr)MK_LBUTTON, MakeLParam(x, yDown));
                Thread.Sleep(holdMs);
                ok &= PostMessage(hwnd, WM_LBUTTONUP, IntPtr.Zero, MakeLParam(x, y + 1));
            }
            return ok;
        }

        static IntPtr MakeLParam(int x, int y)
        {
            return (IntPtr)(((y & 0xFFFF) << 16) | (x & 0xFFFF));
        }
    }
}
