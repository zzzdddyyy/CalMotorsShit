﻿namespace CalMotorsShit
{
    partial class Form1
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.rdbRight = new System.Windows.Forms.RadioButton();
            this.rdbFront = new System.Windows.Forms.RadioButton();
            this.btnCapture = new System.Windows.Forms.Button();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.btnDetect = new System.Windows.Forms.Button();
            this.textBox1 = new System.Windows.Forms.TextBox();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // rdbRight
            // 
            this.rdbRight.AutoSize = true;
            this.rdbRight.Location = new System.Drawing.Point(1036, 504);
            this.rdbRight.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.rdbRight.Name = "rdbRight";
            this.rdbRight.Size = new System.Drawing.Size(71, 16);
            this.rdbRight.TabIndex = 0;
            this.rdbRight.TabStop = true;
            this.rdbRight.Text = "右侧相机";
            this.rdbRight.UseVisualStyleBackColor = true;
            // 
            // rdbFront
            // 
            this.rdbFront.AutoSize = true;
            this.rdbFront.Location = new System.Drawing.Point(1036, 537);
            this.rdbFront.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.rdbFront.Name = "rdbFront";
            this.rdbFront.Size = new System.Drawing.Size(71, 16);
            this.rdbFront.TabIndex = 0;
            this.rdbFront.TabStop = true;
            this.rdbFront.Text = "前方相机";
            this.rdbFront.UseVisualStyleBackColor = true;
            // 
            // btnCapture
            // 
            this.btnCapture.Location = new System.Drawing.Point(1006, 576);
            this.btnCapture.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.btnCapture.Name = "btnCapture";
            this.btnCapture.Size = new System.Drawing.Size(100, 33);
            this.btnCapture.TabIndex = 1;
            this.btnCapture.Text = "获取图像";
            this.btnCapture.UseVisualStyleBackColor = true;
            this.btnCapture.Click += new System.EventHandler(this.btnCapture_Click);
            // 
            // pictureBox1
            // 
            this.pictureBox1.Location = new System.Drawing.Point(6, 6);
            this.pictureBox1.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(978, 598);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox1.TabIndex = 2;
            this.pictureBox1.TabStop = false;
            // 
            // btnDetect
            // 
            this.btnDetect.Location = new System.Drawing.Point(1006, 419);
            this.btnDetect.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.btnDetect.Name = "btnDetect";
            this.btnDetect.Size = new System.Drawing.Size(100, 33);
            this.btnDetect.TabIndex = 3;
            this.btnDetect.Text = "查看";
            this.btnDetect.UseVisualStyleBackColor = true;
            this.btnDetect.Click += new System.EventHandler(this.btnDetect_Click);
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(987, 6);
            this.textBox1.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.textBox1.Multiline = true;
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(142, 398);
            this.textBox1.TabIndex = 4;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.ClientSize = new System.Drawing.Size(1146, 612);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.btnDetect);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.btnCapture);
            this.Controls.Add(this.rdbFront);
            this.Controls.Add(this.rdbRight);
            this.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.Name = "Form1";
            this.Text = "计算电机位移量";
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.RadioButton rdbRight;
        private System.Windows.Forms.RadioButton rdbFront;
        private System.Windows.Forms.Button btnCapture;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Button btnDetect;
        private System.Windows.Forms.TextBox textBox1;
    }
}

