namespace Overlay
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            timer1 = new System.Windows.Forms.Timer(components);
            dF_Panel1 = new DF_Panel();
            SuspendLayout();
            // 
            // timer1
            // 
            timer1.Interval = 500;
            timer1.Tick += timer1_Tick;
            // 
            // dF_Panel1
            // 
            dF_Panel1.Dock = DockStyle.Fill;
            dF_Panel1.Location = new Point(0, 0);
            dF_Panel1.Margin = new Padding(0);
            dF_Panel1.Name = "dF_Panel1";
            dF_Panel1.Size = new Size(800, 450);
            dF_Panel1.TabIndex = 0;
            dF_Panel1.Paint += dF_Panel1_Paint;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            ControlBox = false;
            Controls.Add(dF_Panel1);
            Name = "MainForm";
            Text = "Form1";
            TopMost = true;
            FormClosed += MainForm_FormClosed;
            Load += MainForm_Load;
            VisibleChanged += MainForm_VisibleChanged;
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Timer timer1;
        private DF_Panel dF_Panel1;
    }
}
