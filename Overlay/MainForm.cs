using Overlay.Objects;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Modules;

namespace Overlay
{
    public partial class MainForm : Form
    {
        #region ��������� ����ü
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
        #endregion

        private IntPtr hwnd = IntPtr.Zero;
        private ThreadTimer timer_calc = null;
        private ThreadTimer timer_display = null;
        private object _locker = new object();

        private int __x, __y; //ũ�� ��ġ ������
        private Point loc_map_hd = new Point(88, 173);

        public MainForm()
        {
            InitializeComponent();
        }

        public MainForm(IntPtr _hwnd)
        {
            try
            {
                this.hwnd = _hwnd;
                InitializeComponent();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }

        //protected override CreateParams CreateParams
        //{
        //    get
        //    {
        //        CreateParams cp = base.CreateParams;
        //        cp.ExStyle |= 0x02000000; //WS_EX_COMPOSITED
        //        return cp;
        //    }
        //}

        private void MainForm_Load(object sender, EventArgs e)
        {
            // â ũ�� ����
            GetWindowRect(this.hwnd, out RECT rect);
            SetWindowPos(this.hwnd, IntPtr.Zero, rect.Left, rect.Top, 1600, 900, 0x0000);

            //������ ��ü �ʱ�ȭ
            DirectX2DRender.Ins.Initialize(this.panel1);

            //Overlay Codes
            this.BackColor = Color.Black;
            this.TransparencyKey = Color.Black;
            this.TopMost = true;
            //this.FormBorderStyle = FormBorderStyle.None;
            int initialStyle = GetWindowLong(this.Handle, -20);
            SetWindowLong(this.Handle, -20, initialStyle | 0x80000 | 0x20);

            //Thread Timer Init
            this.timer_calc = new ThreadTimer(100);
            this.timer_calc.Tick += Timer_calc_Tick;
            this.timer_display = new ThreadTimer(100);
            this.timer_display.Tick += Timer_display_Tick;

            HotKeyManager.Ins.Initialize(this.Handle);
            HotKeyManager.Ins.Regist(0, Keys.None, Keys.Up);
            HotKeyManager.Ins.Regist(1, Keys.None, Keys.Down);
            HotKeyManager.Ins.Regist(2, Keys.None, Keys.Left);
            HotKeyManager.Ins.Regist(3, Keys.None, Keys.Right);
            HotKeyManager.Ins.Regist(4, Keys.None, Keys.End);

            HotKeyManager.Ins.Regist(5, Keys.None, Keys.NumPad4);
            HotKeyManager.Ins.Regist(6, Keys.None, Keys.NumPad6);
            HotKeyManager.Ins.Regist(7, Keys.None, Keys.NumPad8);
            HotKeyManager.Ins.Regist(8, Keys.None, Keys.NumPad2);

            this.TopMost = true;

            this.timer_calc.Run();
            this.timer_display.Run();
            this.timer1.Start();
        }

        private void Timer_display_Tick()
        {
            if (LocationData.Ins.charPos.isSelected == false) { return; }

            lock (this._locker)
            {

                DirectX2DRender.Ins.BeginDraw(); //�׸��� ����
                DirectX2DRender.Ins.Clear(Color.Black); //���� ����

                //�ʹڽ� �׸���
                DirectX2DRender.Ins.DrawRectangle(this.loc_map_hd.X, this.loc_map_hd.Y, Properties.Resources.mapHD.Width, Properties.Resources.mapHD.Height, Color.Red, false);
                Point me = this.CalcScreenLocation(LocationData.Ins.charPos.pos.X, LocationData.Ins.charPos.pos.Y);
                DirectX2DRender.Ins.DrawEllipse(me.X, me.Y, Color.LawnGreen, 10f, 10f, true); //�� ��ġ �׸���

                for (int n = 0; n < LocationData.Ins.Length; ++n)
                {
                    Point loc = this.CalcScreenLocation(LocationData.Ins[n].pos.X, LocationData.Ins[n].pos.Y);
                    if (LocationData.Ins[n].isSelected == true)
                    {
                        DirectX2DRender.Ins.DrawLine(me.X, me.Y, loc.X, loc.Y, Color.Lime);
                        DirectX2DRender.Ins.DrawEllipse(me.X, me.Y, Color.LawnGreen, 10f, 10f, true); //�� ��ġ �׸���
                        DirectX2DRender.Ins.DrawEllipse(loc.X, loc.Y, Color.LawnGreen, 5f, 5f, true);

                        int range = (int)Math.Sqrt(Math.Pow(LocationData.Ins.charPos.pos.X - LocationData.Ins[n].pos.X, 2) +
                            Math.Pow(LocationData.Ins.charPos.pos.Y - LocationData.Ins[n].pos.Y, 2));

                        //1ĭ ���������� ������ ���� �ִ�.
                        if (LocationData.Ins[n].pos.X + 1 >= LocationData.Ins.charPos.pos.X &&
                            LocationData.Ins[n].pos.X - 1 <= LocationData.Ins.charPos.pos.X &&
                            LocationData.Ins[n].pos.Y + 1 >= LocationData.Ins.charPos.pos.Y &&
                            LocationData.Ins[n].pos.Y - 1 <= LocationData.Ins.charPos.pos.Y)
                        {
                            string text = string.Format("({0}, {1})", LocationData.Ins[n].pos.X, LocationData.Ins[n].pos.Y);
                            DirectX2DRender.Ins.DrawText(this.loc_map_hd.X, this.loc_map_hd.Y, text, "Arial", 20f, Color.LawnGreen);
                        }
                        else
                        {
                            string text = string.Format("({0}, {1}) [{2} m]", LocationData.Ins[n].pos.X, LocationData.Ins[n].pos.Y, range);
                            DirectX2DRender.Ins.DrawText(this.loc_map_hd.X, this.loc_map_hd.Y, text, "Arial", 20f, Color.Red);
                        }
                    }
                    else
                    {
                        DirectX2DRender.Ins.DrawEllipse(loc.X, loc.Y, Color.Blue, 5f, 5f, true);
                    }
                }

                DirectX2DRender.Ins.EndDraw();
            }
        }

        private void Timer_calc_Tick()
        {
            Bitmap rawImage = null;
            Bitmap cropImage = null;
            try
            {
                rawImage = this.GetScreenImage();    //��ü �̹���
                Rectangle rectPos = new Rectangle(127, 152, 115, 40);   //ũ���� ����
                cropImage = TessOCR.Ins.ImageCrop(rawImage, rectPos);    //��ü �̹������� ũ��    
                string ret = TessOCR.Ins.ReadFromMap(cropImage);    //OCR
                
                ret = TessOCR.Ins.ExtractCoordinates(ret);  //���Խ� �Ľ�
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
                //Debug.WriteLine(e.ToString());
            }
            finally
            {
                if (rawImage != null)
                    rawImage.Dispose();
                if (cropImage != null)
                    cropImage.Dispose();
            }
        }

        private Point CalcScreenLocation(int x, int y)
        {
            int _x = (int)(Properties.Resources.mapHD.Width / (double)148 * x) + this.loc_map_hd.X;
            int _y = Properties.Resources.mapHD.Height - (int)((Properties.Resources.mapHD.Height / (double)200) * (double)y) + this.loc_map_hd.Y;

            return new System.Drawing.Point(_x, _y);
        }

        private void DrawCenteredText(Graphics g, string text, Font font, Brush brush, PointF desiredLocation)
        {
            // �ؽ�Ʈ�� ũ�� ���
            SizeF textSize = g.MeasureString(text, font);

            // �ؽ�Ʈ�� ��ġ ��� (���ϴ� ��ǥ�� y���� �������� ��� ����)
            PointF textLocation = new PointF(desiredLocation.X - textSize.Width / 2, desiredLocation.Y - textSize.Height / 2);

            // �ؽ�Ʈ �׸���
            g.DrawString(text, font, brush, textLocation);
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            HotKeyManager.Ins.UnregistAllKeys();
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

                    case (IntPtr)0x5: this.HotKey_Num4(); break;
                    case (IntPtr)0x6: this.HotKey_Num6(); break;
                    case (IntPtr)0x7: this.HotKey_Num8(); break;
                    case (IntPtr)0x8: this.HotKey_Num2(); break;
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
                if (this.timer_calc.IsRun == true)
                {
                    this.timer_calc.Stop();
                    LocationData.Ins.charPos.isSelected = false;
                }
            }
            else
            {
                if (this.timer_calc.IsRun == false)
                {
                    this.timer_calc.Run();
                }
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
            //Rectangle rect = new Rectangle(985, 435, 240, 38); //FULL HD ����
            Rectangle rect = new Rectangle(825, 368, 75, 24); //HD ����
            Bitmap cropImage = TessOCR.Ins.ImageCrop(rawImage, rect);
            string ret = TessOCR.Ins.ReadFromItem(cropImage);

            if (string.IsNullOrEmpty(ret) == false)
            {
                string[] strPos = ret.Split(new char[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                int.TryParse(strPos[0], out int _x);
                int.TryParse(strPos[1], out int _y);

                if (_x <= 148 && _y <= 200)
                {
                    if (_x == 59 && _y == 134)  //�߸��� �ν� ���̽� ����
                    {
                        _x = 39;
                    }

                    LocationData.Ins.Add(_x, _y);
                }
            }
        }

        private void HotKey_Num4()
        {
            __x--;
        }
        private void HotKey_Num6()
        {
            __x++;
        }
        private void HotKey_Num8()
        {
            __y--;
        }
        private void HotKey_Num2()
        {
            __y++;
        }

        private Bitmap GetScreenImage()
        {
            //â �ڵ��� �׷��ȵ����͸� ������
            using (Graphics graphicsData = Graphics.FromHwnd(this.hwnd))
            {
                //�׷��� �����Ϳ��� â ũ�⸦ ������
                Rectangle rect = Rectangle.Round(graphicsData.VisibleClipBounds);

                //â ũ��� ���� �������� ��Ʈ���� ����
                Bitmap bmp = new Bitmap(rect.Width, rect.Height);

                //���� ���� ��Ʈ�ʿ� ��ũ���� ������
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    IntPtr hdc = g.GetHdc();
                    PrintWindow(hwnd, hdc, 0x2);
                    g.ReleaseHdc(hdc);
                }

                return bmp;
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            DirectX2DRender.Ins.Dispose();
            this.timer_calc.Stop();
            this.timer_display.Stop();
        }

        private void MainForm_SizeChanged(object sender, EventArgs e)
        {
            lock (this._locker)
            {
                DirectX2DRender.Ins.Initialize(this.panel1);
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            GetWindowRect(this.hwnd, out RECT rect);
            if (this.Location.X!= rect.Left || this.Location.Y != rect.Top)
            {
                SetWindowPos(this.Handle, IntPtr.Zero, rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top, 0x0000);
            }

            if (LocationData.Ins.charPos.isSelected == true)
            {
                if(this.Visible == false)
                    this.Visible = true;
            }
            else
            {
                if(this.Visible == true)
                    this.Visible = false;
            }
        }
    }
}
