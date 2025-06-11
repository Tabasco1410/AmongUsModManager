namespace Among_Us_ModManeger_Updater
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            progressBar1 = new ProgressBar();
            label1 = new Label();
            logBox = new TextBox();
            SuspendLayout();
            // 
            // progressBar1
            // 
            progressBar1.Location = new Point(12, 92);
            progressBar1.Name = "progressBar1";
            progressBar1.Size = new Size(776, 39);
            progressBar1.TabIndex = 0;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(40, 42);
            label1.Name = "label1";
            label1.Size = new Size(119, 25);
            label1.TabIndex = 1;
            label1.Text = "アップデート中...";
            label1.Click += label1_Click;
            // 
            // logBox
            // 
            logBox.Location = new Point(12, 178);
            logBox.Multiline = true;
            logBox.Name = "logBox";
            logBox.ScrollBars = ScrollBars.Vertical;
            logBox.Size = new Size(776, 240);
            logBox.TabIndex = 2;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(logBox);
            Controls.Add(label1);
            Controls.Add(progressBar1);
            Name = "Form1";
            Text = "Updater";
            Load += Form1_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private ProgressBar progressBar1;
        private Label label1;
        private TextBox logBox;
    }
}
