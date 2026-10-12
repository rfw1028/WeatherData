namespace WeatherDataService
{
    partial class Form1
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
            tb1 = new RichTextBox();
            tb2 = new TextBox();
            cal1 = new MonthCalendar();
            button1 = new Button();
            SuspendLayout();
            // 
            // tb1
            // 
            tb1.AcceptsTab = true;
            tb1.Location = new Point(12, 887);
            tb1.Name = "tb1";
            tb1.ScrollBars = RichTextBoxScrollBars.Vertical;
            tb1.Size = new Size(2589, 814);
            tb1.TabIndex = 1;
            tb1.Text = "";
            // 
            // tb2
            // 
            tb2.Location = new Point(12, 365);
            tb2.Multiline = true;
            tb2.Name = "tb2";
            tb2.ScrollBars = ScrollBars.Vertical;
            tb2.Size = new Size(2589, 503);
            tb2.TabIndex = 2;
            // 
            // cal1
            // 
            cal1.Location = new Point(42, 32);
            cal1.Name = "cal1";
            cal1.TabIndex = 3;
            // 
            // button1
            // 
            button1.Location = new Point(489, 52);
            button1.Name = "button1";
            button1.Size = new Size(380, 54);
            button1.TabIndex = 4;
            button1.Text = "Retrieve ASOS Data";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(18F, 40F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(2614, 1713);
            Controls.Add(button1);
            Controls.Add(cal1);
            Controls.Add(tb2);
            Controls.Add(tb1);
            Font = new Font("Candara", 14.1428576F, FontStyle.Bold, GraphicsUnit.Point, 0);
            Margin = new Padding(4);
            Name = "Form1";
            Text = "Form1";
            Load += Form1_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        public RichTextBox tb1;        
        public TextBox tb2;
        private MonthCalendar cal1;
        private Button button1;
    }
}
