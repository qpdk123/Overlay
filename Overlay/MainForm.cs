using Overlay.Objects;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Modules;

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

        public enum OverlayMode
        {
            Beach,
            Abyss
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
        private ThreadTimer timer_calc = null; //연산용 쓰레드
        private ThreadTimer timer_display = null; //디스플레이용 쓰레드

        private OverlayMode mode = OverlayMode.Beach;

        private int __x, __y; //크롭 위치 조정용
        private Point loc_map_beach = new Point(96, 202);
        private Point loc_map_abyss = new Point(33, 202);

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

        private void MainForm_Load(object sender, EventArgs e)
        {
            //Trasure Box Data Init
            SettingManager<BeachBoxes>.Ins.Initialize(new BeachBoxes());
            SettingManager<BeachBoxes>.Ins.Load();

            SettingManager<AbyssBoxes>.Ins.Initialize(new AbyssBoxes());
            SettingManager<AbyssBoxes>.Ins.Load();

            //SettingManager<TrasureMaps>.Ins.Initialize(new TrasureMaps());
            //SettingManager<TrasureMaps>.Ins.Load();


            // 창 크기 보정
            GetWindowRect(this.hwnd, out RECT rect);
            SetWindowPos(this.hwnd, IntPtr.Zero, rect.Left, rect.Top, 1600, 900, 0x0000);
            this.TopMost = true;

            //랜더링 객체 초기화
            DirectX2DRender.Ins.Initialize(this.panel1);

            //Overlay Codes
            this.BackColor = Color.Black;
            this.TransparencyKey = Color.Black;
            this.TopMost = true;
            this.FormBorderStyle = FormBorderStyle.None;
            int initialStyle = GetWindowLong(this.Handle, -20);
            SetWindowLong(this.Handle, -20, initialStyle | 0x80000 | 0x20);

            //Thread Timer Init
            this.timer_calc = new ThreadTimer(100);
            this.timer_calc.Tick += Timer_calc_Tick;
            this.timer_display = new ThreadTimer(100);
            this.timer_display.Tick += Timer_display_Tick;

            //Hotkey Init
            this.hotkeyManager1.Initialize(this.Handle);
            this.hotkeyManager1.Regist(0, Keys.None, Keys.Up);
            this.hotkeyManager1.Regist(1, Keys.None, Keys.Down);
            this.hotkeyManager1.Regist(2, Keys.None, Keys.Left);
            this.hotkeyManager1.Regist(3, Keys.None, Keys.Right);
            this.hotkeyManager1.Regist(4, Keys.None, Keys.End);
            this.hotkeyManager1.Regist(5, Keys.None, Keys.NumPad4);
            this.hotkeyManager1.Regist(6, Keys.None, Keys.NumPad6);
            this.hotkeyManager1.Regist(7, Keys.None, Keys.NumPad8);
            this.hotkeyManager1.Regist(8, Keys.None, Keys.NumPad2);
            this.hotkeyManager1.Regist(9, Keys.None, Keys.F9);
            this.hotkeyManager1.Regist(10, Keys.None, Keys.F10);

            //Run Timer
            this.timer_calc.Run();
            this.timer_display.Run();
            this.timer1.Start();
        }

        private void HotkeyManager1_PressedHotkey(object sender, HotkeyEventArg e)
        {
            switch (e.HotkeyItem.ID)
            {
                case 0: this.HotKey_Up(); break;
                case 1: this.HotKey_Down(); break;
                case 2: this.HotKey_Left(); break;
                case 3: this.HotKey_Right(); break;
                case 4: this.HotKey_End(); break;
                case 5: this.HotKey_Num4(); break;
                case 6: this.HotKey_Num6(); break;
                case 7: this.HotKey_Num8(); break;
                case 8: this.HotKey_Num2(); break;
                case 9: this.HotKey_F9(); break;
                case 10: this.HotKey_F10(); break;
            }
        }

        private void HotKey_F10()
        {//비취해안 모드
            this.mode = OverlayMode.Abyss;
        }

        private void HotKey_F9()
        {//깊은심연 모드
            this.mode = OverlayMode.Beach;
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
            Bitmap rawImage = CaptureTool.Ins.GetSnapShot(this.hwnd);
            //Rectangle rect = new Rectangle(985, 435, 240, 38); //FULL HD 기준
            Rectangle rect = new Rectangle(825, 368, 75, 24); //HD 기준
            Bitmap cropImage = CaptureTool.Ins.CropImage(rawImage, rect);
            string ret = TessOCR.Ins.ReadFromItem(cropImage);

            if (string.IsNullOrEmpty(ret) == false)
            {
                string[] strPos = ret.Split(new char[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                int.TryParse(strPos[0], out int _x);
                int.TryParse(strPos[1], out int _y);

                if (_x <= 148 && _y <= 200)
                {
                    if (_x == 59 && _y == 134)  //잘못된 인식 케이스 수정
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

        private void Timer_display_Tick()
        {
            if (LocationData.Ins.charPos.isSelected == false) { return; }

            DXFrame frame = DirectX2DRender.Ins.GetFrame();
            frame.Actions.Enqueue(DirectX2DRender.Ins.Clear(Color.Black));

            //맵박스 그리기
            if (this.mode == OverlayMode.Beach)
            {
                frame.Actions.Enqueue(DirectX2DRender.Ins.DrawRectangle(this.loc_map_beach.X, this.loc_map_beach.Y, Properties.Resources.map_beach.Width, Properties.Resources.map_beach.Height, Color.Red, false));
                frame.Actions.Enqueue(DirectX2DRender.Ins.DrawText(this.loc_map_beach.X, this.loc_map_beach.Y, "[비취해변 모드]", "Arial", 20f, Color.White));
            }
            else if (this.mode == OverlayMode.Abyss)
            {
                frame.Actions.Enqueue(DirectX2DRender.Ins.DrawRectangle(this.loc_map_abyss.X, this.loc_map_abyss.Y, Properties.Resources.map_abyss.Width, Properties.Resources.map_abyss.Height, Color.Red, false));
                frame.Actions.Enqueue(DirectX2DRender.Ins.DrawText(this.loc_map_abyss.X, this.loc_map_abyss.Y, "[깊은심해 모드]", "Arial", 20f, Color.White));
            }

            //DirectX2DRender.Ins.DrawText(__x, __y, string.Format("{0} {1}", __x, __y), "Arial", 20f, Color.White);
            Point me = this.CalcScreenLocation(LocationData.Ins.charPos.pos.X, LocationData.Ins.charPos.pos.Y);
            frame.Actions.Enqueue(DirectX2DRender.Ins.DrawEllipse(me.X, me.Y, Color.LawnGreen, 10f, 10f, true));


            if (this.mode == OverlayMode.Beach)
            {
                //Draw Trasure Map Data
                for (int n = 0; n < LocationData.Ins.Length; ++n)
                {
                    Point loc = this.CalcScreenLocation(LocationData.Ins[n].pos.X, LocationData.Ins[n].pos.Y);
                    if (LocationData.Ins[n].isSelected == true)
                    {
                        frame.Actions.Enqueue(DirectX2DRender.Ins.DrawLine(me.X, me.Y, loc.X, loc.Y, Color.Lime));
                        frame.Actions.Enqueue(DirectX2DRender.Ins.DrawEllipse(me.X, me.Y, Color.LawnGreen, 10f, 10f, true));
                        frame.Actions.Enqueue(DirectX2DRender.Ins.DrawEllipse(loc.X, loc.Y, Color.LawnGreen, 7f, 7f, true));

                        int range = (int)Math.Sqrt(Math.Pow(LocationData.Ins.charPos.pos.X - LocationData.Ins[n].pos.X, 2) +
                            Math.Pow(LocationData.Ins.charPos.pos.Y - LocationData.Ins[n].pos.Y, 2));

                        //1칸 범위내에서 지도를 열수 있다.
                        if (LocationData.Ins[n].pos.X + 1 >= LocationData.Ins.charPos.pos.X &&
                            LocationData.Ins[n].pos.X - 1 <= LocationData.Ins.charPos.pos.X &&
                            LocationData.Ins[n].pos.Y + 1 >= LocationData.Ins.charPos.pos.Y &&
                            LocationData.Ins[n].pos.Y - 1 <= LocationData.Ins.charPos.pos.Y)
                        {
                            string text = string.Format("({0}, {1})", LocationData.Ins[n].pos.X, LocationData.Ins[n].pos.Y);
                            if (this.mode == OverlayMode.Beach)
                                frame.Actions.Enqueue(DirectX2DRender.Ins.DrawText(this.loc_map_beach.X, this.loc_map_beach.Y, text, "Arial", 20f, Color.LawnGreen));
                            else if (mode == OverlayMode.Abyss)
                                frame.Actions.Enqueue(DirectX2DRender.Ins.DrawText(this.loc_map_abyss.X, this.loc_map_abyss.Y, text, "Arial", 20f, Color.LawnGreen));

                        }
                        else
                        {
                            string text = string.Format("({0}, {1}) [{2} m]", LocationData.Ins[n].pos.X, LocationData.Ins[n].pos.Y, range);

                            if (this.mode == OverlayMode.Beach)
                                frame.Actions.Enqueue(DirectX2DRender.Ins.DrawText(this.loc_map_beach.X, this.loc_map_beach.Y, text, "Arial", 20f, Color.Red));
                            else if (this.mode == OverlayMode.Abyss)
                                frame.Actions.Enqueue(DirectX2DRender.Ins.DrawText(this.loc_map_abyss.X, this.loc_map_beach.Y, text, "Arial", 20f, Color.Red));
                        }
                    }
                    else
                    {
                        frame.Actions.Enqueue(DirectX2DRender.Ins.DrawEllipse(loc.X, loc.Y, Color.Blue, 5f, 5f, true));
                    }
                }

                //Draw Trasure Box (Abyss)
                for (int n = 0; n < SettingManager<BeachBoxes>.Ins.Data.BOXES.Count - 1; ++n)
                {
                    Point loc = this.CalcScreenLocation(
                        SettingManager<BeachBoxes>.Ins.Data.BOXES[n].X,
                        SettingManager<BeachBoxes>.Ins.Data.BOXES[n].Y);

                    frame.Actions.Enqueue(DirectX2DRender.Ins.DrawEllipse(loc.X, loc.Y, Color.Magenta, 3f, 3f, true));
                }
            }
            else if (this.mode == OverlayMode.Abyss)
            {
                //Draw Trasure Box (Abyss)
                for (int n = 0; n < SettingManager<AbyssBoxes>.Ins.Data.BOXES.Count; ++n)
                {
                    Point loc = this.CalcScreenLocation(
                        SettingManager<AbyssBoxes>.Ins.Data.BOXES[n].X,
                        SettingManager<AbyssBoxes>.Ins.Data.BOXES[n].Y);

                    frame.Actions.Enqueue(DirectX2DRender.Ins.DrawEllipse(loc.X, loc.Y, Color.Magenta, 3f, 3f, true));

                    //Point p = new Point(SettingManager<AbyssBoxes>.Ins.Data.BOXES[n].X, SettingManager<AbyssBoxes>.Ins.Data.BOXES[n].Y);
                    //string text = string.Format("{0}, {1}", p.X, p.Y);
                    //frame.Actions.Enqueue(DirectX2DRender.Ins.DrawText(loc.X, loc.Y, text, "Arial", 20f, Color.White));
                }
            }

            DirectX2DRender.Ins.Render(frame);
        }

        private void Timer_calc_Tick()
        {
            Bitmap rawImage = null;
            Bitmap cropImage = null;
            try
            {
                rawImage = CaptureTool.Ins.GetSnapShot(this.hwnd); //화면캡쳐
                Rectangle rectPos = new Rectangle(120, 152, 120, 40);   //크롭할 영역
                cropImage = CaptureTool.Ins.CropImage(rawImage, rectPos); //이미지 크롭
                string ret = TessOCR.Ins.ReadFromMap(cropImage);    //OCR

                ret = TessOCR.Ins.ExtractCoordinates(ret);  //정규식 파싱
                if (string.IsNullOrEmpty(ret) == false)
                {
                    string[] strPos = ret.Split(new char[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    int.TryParse(strPos[0], out int _x);
                    int.TryParse(strPos[1], out int _y);

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
            int _x = 0, _y = 0;
            if (this.mode == OverlayMode.Beach)
            {
                _x = (int)(Properties.Resources.map_beach.Width / (double)148 * x) + this.loc_map_beach.X;
                _y = Properties.Resources.map_beach.Height - (int)((Properties.Resources.map_beach.Height / (double)200) * (double)y) + this.loc_map_beach.Y;
            }
            else if (this.mode == OverlayMode.Abyss)
            {
                _x = (int)(Properties.Resources.map_abyss.Width / (double)200 * x) + this.loc_map_abyss.X;
                _y = Properties.Resources.map_abyss.Height - (int)((Properties.Resources.map_abyss.Height / (double)200) * (double)y) + this.loc_map_abyss.Y;
            }

            return new System.Drawing.Point(_x, _y);
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            this.hotkeyManager1.UnregistAllKeys();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            DirectX2DRender.Ins.Dispose();
            this.timer_calc.Stop();
            this.timer_display.Stop();
            SettingManager<TrasureMaps>.Ins.Save(Environment.CurrentDirectory);
        }

        private void MainForm_SizeChanged(object sender, EventArgs e)
        {
            DirectX2DRender.Ins.Initialize(this.panel1);
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            GetWindowRect(this.hwnd, out RECT rect);
            if (this.Location.X != rect.Left || this.Location.Y != rect.Top)
            {
                SetWindowPos(this.Handle, IntPtr.Zero, rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top, 0x0000);
            }

            if (LocationData.Ins.charPos.isSelected == true)
            {
                if (this.Visible == false)
                    this.Visible = true;
            }
            else
            {
                if (this.Visible == true)
                    this.Visible = false;
            }
        }
    }
}
