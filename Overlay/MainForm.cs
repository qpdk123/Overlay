using Overlay.Objects;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Overlay
{
    public partial class MainForm : Form
    {
        #region 사용자정의 구조체
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }
        #endregion

        #region WIN32API DLL
        [DllImport("user32.dll", SetLastError = true)]
        static extern int GetWindowLong(IntPtr hwnd, int index);
        [DllImport("user32.dll")]
        static extern int SetWindowLong(IntPtr hwnd, int index, int dwlong);
        [DllImport("user32.dll")]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
        [DllImport("user32.dll")]
        internal static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, int nFlags);
        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hwnd);
        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);
        // 비트블럿 함수 가져오기
        [DllImport("gdi32.dll")]
        private static extern bool BitBlt(IntPtr hdcDest, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, int dwRop);
        //핫키등록
        [DllImport("user32.dll")]
        private static extern int RegisterHotKey(int hwnd, int id, int fsModifiers, int vk);
        //핫키제거
        [DllImport("user32.dll")]
        private static extern int UnregisterHotKey(int hwnd, int id);
        #endregion

        private IntPtr hwnd = IntPtr.Zero;
        private ThreadTimer timer = default(ThreadTimer);

        private int __x, __y; //크롭 위치 조정용

        private Pen pen_line = new Pen(new SolidBrush(Color.Red), 2);
        private Pen pen_dest = new Pen(new SolidBrush(Color.LawnGreen), 4);
        private SolidBrush brush_dot_blue = new SolidBrush(Color.Blue);
        private SolidBrush brush_dot_red = new SolidBrush(Color.Red);
        private SolidBrush brush_dot_orange = new SolidBrush(Color.DarkOrange);
        private SolidBrush brush_dot_green = new SolidBrush(Color.Green);
        private SolidBrush brush_dot_Magenta = new SolidBrush(Color.DarkMagenta);
        private Font font = new Font("Arial", 14, FontStyle.Bold);

        private Point loc_map_hd = new Point(98, 201);

        public MainForm()
        {
            InitializeComponent();
        }

        public MainForm(IntPtr _hwnd)
        {
            this.hwnd = _hwnd;
            InitializeComponent();
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x02000000; //WS_EX_COMPOSITED
                return cp;
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            // 창 크기 보정
            GetWindowRect(this.hwnd, out RECT rect);
            SetWindowPos(this.hwnd, IntPtr.Zero, rect.Left, rect.Top, 1600, 900, 0x0000);

            //Overlay Codes
            this.BackColor = Color.Wheat;
            this.TransparencyKey = Color.Wheat;
            this.TopMost = true;
            this.FormBorderStyle = FormBorderStyle.None;
            int initialStyle = GetWindowLong(this.Handle, -20);
            SetWindowLong(this.Handle, -20, initialStyle | 0x80000 | 0x20);

            //Thread Timer Init
            this.timer = new ThreadTimer(500);
            this.timer.Tick += Timer_Tick;

            //Regist Global Hotkey
            RegisterHotKey((int)this.Handle, 0, 0x0, (int)Keys.Up);
            RegisterHotKey((int)this.Handle, 1, 0x0, (int)Keys.Down);
            RegisterHotKey((int)this.Handle, 2, 0x0, (int)Keys.Left);
            RegisterHotKey((int)this.Handle, 3, 0x0, (int)Keys.Right);
            RegisterHotKey((int)this.Handle, 4, 0x0, (int)Keys.End);

            this.TopMost = true;

            this.timer1.Start();
        }

        private void Timer_Tick()
        {
            try
            {
                Bitmap rawImage = this.GetScreenImage();    //전체 이미지
                Rectangle rectPos = new Rectangle(127, 152, 115, 40);   //크롭할 영역
                Bitmap cropImage = TessOCR.Ins.ImageCrop(rawImage, rectPos);    //전체 이미지에서 크롭
                string ret = TessOCR.Ins.ReadFromMap(cropImage);    //OCR
                ret = TessOCR.Ins.ExtractCoordinates(ret);  //정규식 파싱
                if (string.IsNullOrEmpty(ret) == false)
                {
                    string[] strPos = ret.Split(new char[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    int.TryParse(strPos[0], out int _x);
                    int.TryParse(strPos[1], out int _y);

                    if (_x <= 148 && _y <= 200)
                    {
                        LocationData.Ins.charPos.pos = new Point(_x, _y);
                        LocationData.Ins.charPos.isSelected = true;
                        LocationData.LOC loc = LocationData.Ins.FindNearDot(_x, _y);

                        if (loc != default(LocationData.LOC))
                        {
                            LocationData.Ins.DeSelectAll();
                            loc.isSelected = true;
                            this.timer.Interval = 500;
                        }
                    }
                    else
                    {
                        LocationData.Ins.charPos.isSelected = false;
                    }
                }
                else
                {
                    LocationData.Ins.charPos.isSelected = false;
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
            }
            finally
            {
                GC.Collect();
                this.Invalidate();
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            GetWindowRect(this.hwnd, out RECT rect);
            SetWindowPos(this.Handle, IntPtr.Zero, rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top, 0x0000);
        }

        private void dF_Panel1_Paint(object sender, PaintEventArgs e)
        {
            if (LocationData.Ins.charPos.isSelected == false) { this.timer.Interval = 1000; return; }

            using (Bitmap ov = new Bitmap(Properties.Resources.mapHD.Width, Properties.Resources.mapHD.Height))
            {
                using (Graphics g = Graphics.FromImage(ov))
                {
                    g.DrawRectangle(this.pen_line, 0, 0, Properties.Resources.mapHD.Width, Properties.Resources.mapHD.Height);
                    int dotSize = 10;
                    Point me = this.CalcScreenLocation(LocationData.Ins.charPos.pos.X, LocationData.Ins.charPos.pos.Y);
                    g.FillEllipse(this.brush_dot_green, me.X - dotSize / 2, me.Y - dotSize / 2, dotSize, dotSize);
                    for (int n = 0; n < LocationData.Ins.Length; ++n)
                    {
                        Point dot = this.CalcScreenLocation(LocationData.Ins[n].pos.X, LocationData.Ins[n].pos.Y);

                        if (LocationData.Ins[n].isSelected == true)
                        {
                            g.FillRectangle(this.brush_dot_green, dot.X - dotSize / 2, dot.Y - dotSize / 2, dotSize, dotSize);
                            g.DrawLine(this.pen_dest, me, dot);

                            int range = (int)Math.Sqrt(Math.Pow(LocationData.Ins.charPos.pos.X - LocationData.Ins[n].pos.X, 2) +
                                Math.Pow(LocationData.Ins.charPos.pos.Y - LocationData.Ins[n].pos.Y, 2));

                            //1칸 범위내에서 지도를 열수 있다.
                            if(LocationData.Ins[n].pos.X + 1 >= LocationData.Ins.charPos.pos.X &&
                                LocationData.Ins[n].pos.X - 1 <= LocationData.Ins.charPos.pos.X &&
                                LocationData.Ins[n].pos.Y + 1 >= LocationData.Ins.charPos.pos.Y &&
                                LocationData.Ins[n].pos.Y - 1 <= LocationData.Ins.charPos.pos.Y)
                            {
                                g.DrawString(
                                string.Format("({0}, {1})", LocationData.Ins[n].pos.X, LocationData.Ins[n].pos.Y),
                                this.font,
                                this.brush_dot_green,
                                0, 0);
                            }
                            else
                            {
                                g.DrawString(
                                string.Format("({0}, {1}) [{2} m]", LocationData.Ins[n].pos.X, LocationData.Ins[n].pos.Y, range),
                                this.font,
                                this.brush_dot_Magenta,
                                0, 0);
                            }
                            
                        }
                        else
                        {
                            g.FillEllipse(this.brush_dot_blue, dot.X - dotSize / 2, dot.Y - dotSize / 2, dotSize, dotSize);
                        }
                    }

                    e.Graphics.DrawImage(ov, this.loc_map_hd);
                }
            }
        }

        private Point CalcScreenLocation(int x, int y)
        {
            int _x = (int)(Properties.Resources.mapHD.Width / (double)148 * x);
            int _y = Properties.Resources.mapHD.Height - (int)((Properties.Resources.mapHD.Height / (double)200) * (double)y);

            return new System.Drawing.Point(_x, _y);
        }

        private void DrawCenteredText(Graphics g, string text, Font font, Brush brush, PointF desiredLocation)
        {
            // 텍스트의 크기 계산
            SizeF textSize = g.MeasureString(text, font);

            // 텍스트의 위치 계산 (원하는 좌표의 y값을 기준으로 가운데 정렬)
            PointF textLocation = new PointF(desiredLocation.X - textSize.Width / 2, desiredLocation.Y - textSize.Height / 2);

            // 텍스트 그리기
            g.DrawString(text, font, brush, textLocation);
        }

        private void MainForm_VisibleChanged(object sender, EventArgs e)
        {
            if (this.Visible == true)
            {
                if (this.timer.IsRun == false)
                {
                    this.timer.Run();
                }
            }
            else
            {
                if (this.timer.IsRun == true)
                {
                    this.timer.Stop();
                }
            }
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            this.timer1.Stop();
            this.timer.Stop();

            //UnRegist Global Hotkey
            UnregisterHotKey((int)this.hwnd, 0);
            UnregisterHotKey((int)this.hwnd, 1);
            UnregisterHotKey((int)this.hwnd, 2);
            UnregisterHotKey((int)this.hwnd, 3);
            UnregisterHotKey((int)this.hwnd, 4);
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            //0x312 WM_HOTKEY
            if (m.Msg == 0x312)
            {
                switch (m.WParam)
                {
                    case (IntPtr)0x0: this.HotKey_Up(); break;
                    case (IntPtr)0x1: this.HotKey_Down(); break;
                    case (IntPtr)0x2: this.HotKey_Left(); break;
                    case (IntPtr)0x3: this.HotKey_Right(); break;
                    case (IntPtr)0x4: this.HotKey_End(); break;
                }
            }
        }

        private void HotKey_End()
        {
            Application.Exit();
        }

        private void HotKey_Right()
        {
            if (this.Visible == true)
            {
                this.Visible = false;
            }
            else
            {
                this.Visible = true;
            }
        }

        private void HotKey_Left()
        {
            LocationData.Ins.ClearAll();
        }

        private void HotKey_Down()
        {
            var ret = LocationData.Ins.GetSelectedData();
            if (ret != null)
            {
                LocationData.Ins.Remove(ret.pos.X, ret.pos.Y);
            }
        }

        private void HotKey_Up()
        {
            Bitmap rawImage = this.GetScreenImage();
            //Rectangle rect = new Rectangle(985, 435, 240, 38); //FULL HD 기준
            Rectangle rect = new Rectangle(825, 368, 75, 24); //HD 기준
            Bitmap cropImage = TessOCR.Ins.ImageCrop(rawImage, rect);
            string ret = TessOCR.Ins.ReadFromItem(cropImage);

            if (string.IsNullOrEmpty(ret) == false)
            {
                string[] strPos = ret.Split(new char[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                int.TryParse(strPos[0], out int _x);
                int.TryParse(strPos[1], out int _y);

                if (_x <= 148 && _y <= 200)
                {
                    if(_x == 59 && _y ==  134)  //잘못된 인식 케이스 수정
                    {
                        _x = 30;
                    }

                    LocationData.Ins.Add(_x, _y);
                }
            }
        }

        private Bitmap GetScreenImage()
        {
            //창 핸들의 그래픽데이터를 가져옴
            using (Graphics graphicsData = Graphics.FromHwnd(this.hwnd))
            {
                //그래픽 데이터에서 창 크기를 가져옴
                Rectangle rect = Rectangle.Round(graphicsData.VisibleClipBounds);

                //창 크기와 동일 사이즈의 비트맵을 만듬
                Bitmap bmp = new Bitmap(rect.Width, rect.Height);

                //새로 만든 비트맵에 스크린을 복사함
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    IntPtr hdc = g.GetHdc();
                    PrintWindow(hwnd, hdc, 0x2);
                    g.ReleaseHdc(hdc);
                }

                return bmp;
            }
        }
    }
}
