using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace VinXiangQi
{
    // 布局调整模式：让指定分组框可自由拖动（移动 / 边缘缩放），调整结果通过 Changed 回调保存。
    // 说明：只在分组框自身的空白/边框区域响应（子控件照常可用）。
    public class LayoutEditor
    {
        const int Edge = 8;        // 边缘 / 角判定宽度
        const int MinWidth = 80;   // 最小宽度
        const int MinHeight = 50;  // 最小高度

        class Entry
        {
            public GroupBox Box;
            public Control Parent;
        }

        readonly List<Entry> entries = new List<Entry>();
        readonly Dictionary<GroupBox, Entry> lookup = new Dictionary<GroupBox, Entry>();

        public bool Enabled = false;
        public Action Changed;   // 一次调整完成后回调（用于保存布局）

        Entry active;
        int zone;                 // 0=移动，1=上，2=下，3=左，4=右，5=左上，6=右上，7=左下，8=右下
        Point startMouse;         // 屏幕坐标
        Rectangle startBounds;    // 相对父容器

        public void Add(GroupBox box)
        {
            if (box == null || box.Parent == null) return;
            Entry e = new Entry();
            e.Box = box;
            e.Parent = box.Parent;
            entries.Add(e);
            lookup[box] = e;
            box.MouseDown += Box_MouseDown;
            box.MouseMove += Box_MouseMove;
            box.MouseUp += Box_MouseUp;
        }

        public void SetEnabled(bool on)
        {
            Enabled = on;
            active = null;
            foreach (Entry e in entries)
            {
                e.Box.Cursor = on ? Cursors.SizeAll : Cursors.Default;
            }
        }

        int HitZone(Entry e, Point p)
        {
            int w = e.Box.Width;
            int h = e.Box.Height;
            bool left = p.X <= Edge;
            bool right = p.X >= w - Edge;
            bool top = p.Y <= Edge;
            bool bottom = p.Y >= h - Edge;
            if (left && top) return 5;
            if (right && top) return 6;
            if (left && bottom) return 7;
            if (right && bottom) return 8;
            if (left) return 3;
            if (right) return 4;
            if (top) return 1;
            if (bottom) return 2;
            return 0;
        }

        static Cursor ZoneCursor(int z)
        {
            if (z == 3 || z == 4) return Cursors.SizeWE;
            if (z == 1 || z == 2) return Cursors.SizeNS;
            if (z == 5 || z == 8) return Cursors.SizeNWSE;
            if (z == 6 || z == 7) return Cursors.SizeNESW;
            return Cursors.SizeAll;
        }

        void Box_MouseDown(object sender, MouseEventArgs e)
        {
            if (!Enabled || e.Button != MouseButtons.Left) return;
            GroupBox box = (GroupBox)sender;
            Entry en;
            if (!lookup.TryGetValue(box, out en)) return;
            active = en;
            zone = HitZone(en, e.Location);
            startMouse = Cursor.Position;
            startBounds = box.Bounds;
            box.Capture = true;
            box.BringToFront();
        }

        void Box_MouseMove(object sender, MouseEventArgs e)
        {
            if (!Enabled) return;
            GroupBox box = (GroupBox)sender;
            if (active == null || active.Box != box)
            {
                Entry en;
                if (lookup.TryGetValue(box, out en))
                {
                    box.Cursor = ZoneCursor(HitZone(en, e.Location));
                }
                return;
            }

            int dx = Cursor.Position.X - startMouse.X;
            int dy = Cursor.Position.Y - startMouse.Y;
            bool useLeft = zone == 3 || zone == 5 || zone == 7;
            bool useRight = zone == 4 || zone == 6 || zone == 8;
            bool useTop = zone == 1 || zone == 5 || zone == 6;
            bool useBottom = zone == 2 || zone == 7 || zone == 8;

            int maxW = active.Parent.ClientSize.Width;
            int maxH = active.Parent.ClientSize.Height;

            Rectangle nb;
            if (zone == 0)
            {
                // 移动：整体平移，钳制在父容器内
                int nx = startBounds.X + dx;
                int ny = startBounds.Y + dy;
                if (nx < 0) nx = 0;
                if (ny < 0) ny = 0;
                if (nx + startBounds.Width > maxW) nx = maxW - startBounds.Width;
                if (ny + startBounds.Height > maxH) ny = maxH - startBounds.Height;
                nb = new Rectangle(nx, ny, startBounds.Width, startBounds.Height);
            }
            else
            {
                // 缩放：所拖边移动、对边固定；再钳制到父容器与最小尺寸
                int left = startBounds.Left, top = startBounds.Top, right = startBounds.Right, bottom = startBounds.Bottom;
                if (useLeft) left = Math.Min(left + dx, right - MinWidth);
                if (useRight) right = Math.Max(right + dx, left + MinWidth);
                if (useTop) top = Math.Min(top + dy, bottom - MinHeight);
                if (useBottom) bottom = Math.Max(bottom + dy, top + MinHeight);
                if (left < 0) left = 0;
                if (top < 0) top = 0;
                if (right > maxW) right = maxW;
                if (bottom > maxH) bottom = maxH;
                if (right - left < MinWidth)
                {
                    if (useLeft) left = right - MinWidth; else right = left + MinWidth;
                }
                if (bottom - top < MinHeight)
                {
                    if (useTop) top = bottom - MinHeight; else bottom = top + MinHeight;
                }
                nb = Rectangle.FromLTRB(left, top, right, bottom);
            }

            box.Bounds = nb;
        }

        void Box_MouseUp(object sender, MouseEventArgs e)
        {
            if (active == null || active.Box != (GroupBox)sender) return;
            ((GroupBox)sender).Capture = false;
            active = null;
            if (Changed != null) Changed();
        }
    }
}
