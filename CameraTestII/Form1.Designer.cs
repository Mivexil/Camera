namespace CameraTestII
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.OriginalPictureBox = new System.Windows.Forms.PictureBox();
            this.FilteredPictureBox = new System.Windows.Forms.PictureBox();
            this.MinCbBar = new System.Windows.Forms.TrackBar();
            this.MinCbTextBox = new System.Windows.Forms.TextBox();
            this.MaxCrBar = new System.Windows.Forms.TrackBar();
            this.MaxCrTextBox = new System.Windows.Forms.TextBox();
            this.RectangleFilterBar = new System.Windows.Forms.TrackBar();
            this.RectangleFilterSetButton = new System.Windows.Forms.Button();
            this.RectangleFilterTextBox = new System.Windows.Forms.TextBox();
            this.OriginalImageLabel = new System.Windows.Forms.Label();
            this.FilteredImageLabel = new System.Windows.Forms.Label();
            this.RectangleFilterLabel = new System.Windows.Forms.Label();
            this.MaxCrLabel = new System.Windows.Forms.Label();
            this.MinCbLabel = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.OriginalPictureBox)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.FilteredPictureBox)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.MinCbBar)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.MaxCrBar)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.RectangleFilterBar)).BeginInit();
            this.SuspendLayout();
            // 
            // OriginalPictureBox
            // 
            this.OriginalPictureBox.Location = new System.Drawing.Point(11, 49);
            this.OriginalPictureBox.Name = "OriginalPictureBox";
            this.OriginalPictureBox.Size = new System.Drawing.Size(719, 550);
            this.OriginalPictureBox.TabIndex = 0;
            this.OriginalPictureBox.TabStop = false;
            // 
            // FilteredPictureBox
            // 
            this.FilteredPictureBox.Location = new System.Drawing.Point(736, 50);
            this.FilteredPictureBox.Name = "FilteredPictureBox";
            this.FilteredPictureBox.Size = new System.Drawing.Size(719, 550);
            this.FilteredPictureBox.TabIndex = 0;
            this.FilteredPictureBox.TabStop = false;
            // 
            // MinCbBar
            // 
            this.MinCbBar.Location = new System.Drawing.Point(130, 604);
            this.MinCbBar.Maximum = 300;
            this.MinCbBar.Name = "MinCbBar";
            this.MinCbBar.Size = new System.Drawing.Size(1242, 45);
            this.MinCbBar.TabIndex = 1;
            this.MinCbBar.Value = 300;
            this.MinCbBar.Visible = false;
            this.MinCbBar.Scroll += new System.EventHandler(this.MinCbBar_Scroll);
            // 
            // MinCbTextBox
            // 
            this.MinCbTextBox.Location = new System.Drawing.Point(1376, 616);
            this.MinCbTextBox.Name = "MinCbTextBox";
            this.MinCbTextBox.ReadOnly = true;
            this.MinCbTextBox.Size = new System.Drawing.Size(80, 20);
            this.MinCbTextBox.TabIndex = 2;
            this.MinCbTextBox.Text = "0.5";
            this.MinCbTextBox.Visible = false;
            // 
            // MaxCrBar
            // 
            this.MaxCrBar.Location = new System.Drawing.Point(128, 650);
            this.MaxCrBar.Maximum = 300;
            this.MaxCrBar.Name = "MaxCrBar";
            this.MaxCrBar.Size = new System.Drawing.Size(1242, 45);
            this.MaxCrBar.TabIndex = 6;
            this.MaxCrBar.Visible = false;
            this.MaxCrBar.Scroll += new System.EventHandler(this.MaxCrBar_Scroll);
            // 
            // MaxCrTextBox
            // 
            this.MaxCrTextBox.Location = new System.Drawing.Point(1376, 662);
            this.MaxCrTextBox.Name = "MaxCrTextBox";
            this.MaxCrTextBox.ReadOnly = true;
            this.MaxCrTextBox.Size = new System.Drawing.Size(80, 20);
            this.MaxCrTextBox.TabIndex = 2;
            this.MaxCrTextBox.Text = "-0.5";
            this.MaxCrTextBox.Visible = false;
            // 
            // RectangleFilterBar
            // 
            this.RectangleFilterBar.Location = new System.Drawing.Point(159, 621);
            this.RectangleFilterBar.Maximum = 255;
            this.RectangleFilterBar.Name = "RectangleFilterBar";
            this.RectangleFilterBar.Size = new System.Drawing.Size(1092, 45);
            this.RectangleFilterBar.TabIndex = 7;
            this.RectangleFilterBar.Scroll += new System.EventHandler(this.RectangleFilterBar_Scroll);
            // 
            // RectangleFilterSetButton
            // 
            this.RectangleFilterSetButton.Location = new System.Drawing.Point(1363, 632);
            this.RectangleFilterSetButton.Name = "RectangleFilterSetButton";
            this.RectangleFilterSetButton.Size = new System.Drawing.Size(93, 23);
            this.RectangleFilterSetButton.TabIndex = 8;
            this.RectangleFilterSetButton.Text = "Set threshold";
            this.RectangleFilterSetButton.UseVisualStyleBackColor = true;
            this.RectangleFilterSetButton.Click += new System.EventHandler(this.RectangleFilterSetButton_Click);
            // 
            // RectangleFilterTextBox
            // 
            this.RectangleFilterTextBox.Location = new System.Drawing.Point(1257, 633);
            this.RectangleFilterTextBox.Name = "RectangleFilterTextBox";
            this.RectangleFilterTextBox.ReadOnly = true;
            this.RectangleFilterTextBox.Size = new System.Drawing.Size(100, 20);
            this.RectangleFilterTextBox.TabIndex = 9;
            this.RectangleFilterTextBox.Text = "0";
            // 
            // OriginalImageLabel
            // 
            this.OriginalImageLabel.AutoSize = true;
            this.OriginalImageLabel.Location = new System.Drawing.Point(302, 9);
            this.OriginalImageLabel.Name = "OriginalImageLabel";
            this.OriginalImageLabel.Size = new System.Drawing.Size(76, 13);
            this.OriginalImageLabel.TabIndex = 10;
            this.OriginalImageLabel.Text = "Original image:";
            // 
            // FilteredImageLabel
            // 
            this.FilteredImageLabel.AutoSize = true;
            this.FilteredImageLabel.Location = new System.Drawing.Point(1076, 9);
            this.FilteredImageLabel.Name = "FilteredImageLabel";
            this.FilteredImageLabel.Size = new System.Drawing.Size(91, 13);
            this.FilteredImageLabel.TabIndex = 10;
            this.FilteredImageLabel.Text = "Processed image:";
            // 
            // RectangleFilterLabel
            // 
            this.RectangleFilterLabel.AutoSize = true;
            this.RectangleFilterLabel.Location = new System.Drawing.Point(12, 636);
            this.RectangleFilterLabel.Name = "RectangleFilterLabel";
            this.RectangleFilterLabel.Size = new System.Drawing.Size(141, 13);
            this.RectangleFilterLabel.TabIndex = 11;
            this.RectangleFilterLabel.Text = "Rectangle filtering threshold:";
            // 
            // MaxCrLabel
            // 
            this.MaxCrLabel.AutoSize = true;
            this.MaxCrLabel.Location = new System.Drawing.Point(12, 666);
            this.MaxCrLabel.Name = "MaxCrLabel";
            this.MaxCrLabel.Size = new System.Drawing.Size(110, 13);
            this.MaxCrLabel.TabIndex = 11;
            this.MaxCrLabel.Text = "Maximum red chroma:";
            this.MaxCrLabel.Visible = false;
            // 
            // MinCbLabel
            // 
            this.MinCbLabel.AutoSize = true;
            this.MinCbLabel.Location = new System.Drawing.Point(12, 620);
            this.MinCbLabel.Name = "MinCbLabel";
            this.MinCbLabel.Size = new System.Drawing.Size(112, 13);
            this.MinCbLabel.TabIndex = 11;
            this.MinCbLabel.Text = "Minimum blue chroma:";
            this.MinCbLabel.Visible = false;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1467, 701);
            this.Controls.Add(this.RectangleFilterSetButton);
            this.Controls.Add(this.RectangleFilterBar);
            this.Controls.Add(this.MinCbLabel);
            this.Controls.Add(this.MaxCrLabel);
            this.Controls.Add(this.RectangleFilterLabel);
            this.Controls.Add(this.MaxCrTextBox);
            this.Controls.Add(this.MinCbTextBox);
            this.Controls.Add(this.FilteredImageLabel);
            this.Controls.Add(this.OriginalImageLabel);
            this.Controls.Add(this.RectangleFilterTextBox);
            this.Controls.Add(this.FilteredPictureBox);
            this.Controls.Add(this.OriginalPictureBox);
            this.Controls.Add(this.MinCbBar);
            this.Controls.Add(this.MaxCrBar);
            this.Name = "Form1";
            this.Text = "PianoCam";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.Form1_FormClosed);
            this.Shown += new System.EventHandler(this.Form1_Load);
            ((System.ComponentModel.ISupportInitialize)(this.OriginalPictureBox)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.FilteredPictureBox)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.MinCbBar)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.MaxCrBar)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.RectangleFilterBar)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox OriginalPictureBox;
        private System.Windows.Forms.PictureBox FilteredPictureBox;
        private System.Windows.Forms.TrackBar MinCbBar;
        private System.Windows.Forms.TextBox MinCbTextBox;
        private System.Windows.Forms.TrackBar MaxCrBar;
        private System.Windows.Forms.TextBox MaxCrTextBox;
        private System.Windows.Forms.TrackBar RectangleFilterBar;
        private System.Windows.Forms.Button RectangleFilterSetButton;
        private System.Windows.Forms.TextBox RectangleFilterTextBox;
        private System.Windows.Forms.Label OriginalImageLabel;
        private System.Windows.Forms.Label FilteredImageLabel;
        private System.Windows.Forms.Label RectangleFilterLabel;
        private System.Windows.Forms.Label MaxCrLabel;
        private System.Windows.Forms.Label MinCbLabel;
    }
}

