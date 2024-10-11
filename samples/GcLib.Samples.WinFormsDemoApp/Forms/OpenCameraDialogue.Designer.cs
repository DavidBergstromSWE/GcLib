namespace WinFormsDemoApp
{
    partial class OpenCameraDialogue
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
            this.CameraListBox = new System.Windows.Forms.ListBox();
            this.OKButton = new System.Windows.Forms.Button();
            this.CancelButton = new System.Windows.Forms.Button();
            this.VendorLabel = new System.Windows.Forms.Label();
            this.ModelLabel = new System.Windows.Forms.Label();
            this.SerialLabel = new System.Windows.Forms.Label();
            this.VendorNameLabel = new System.Windows.Forms.Label();
            this.ModelNameLabel = new System.Windows.Forms.Label();
            this.SerialNumberLabel = new System.Windows.Forms.Label();
            this.UniqueIDLabel = new System.Windows.Forms.Label();
            this.DeviceUniqueIDLabel = new System.Windows.Forms.Label();
            this.AccessibleLabel = new System.Windows.Forms.Label();
            this.AccessStatusLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // CameraListBox
            // 
            this.CameraListBox.FormattingEnabled = true;
            this.CameraListBox.ItemHeight = 15;
            this.CameraListBox.Location = new System.Drawing.Point(31, 27);
            this.CameraListBox.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.CameraListBox.Name = "CameraListBox";
            this.CameraListBox.Size = new System.Drawing.Size(236, 139);
            this.CameraListBox.TabIndex = 0;
            this.CameraListBox.SelectedIndexChanged += new System.EventHandler(this.CameraListBox_SelectedIndexChanged);
            this.CameraListBox.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.CameraListBox_MouseDoubleClick);
            // 
            // OKButton
            // 
            this.OKButton.Location = new System.Drawing.Point(170, 193);
            this.OKButton.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.OKButton.Name = "OKButton";
            this.OKButton.Size = new System.Drawing.Size(88, 27);
            this.OKButton.TabIndex = 1;
            this.OKButton.Text = "OK";
            this.OKButton.UseVisualStyleBackColor = true;
            this.OKButton.Click += new System.EventHandler(this.OKButton_Click);
            // 
            // CancelButton
            // 
            this.CancelButton.Location = new System.Drawing.Point(274, 193);
            this.CancelButton.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.CancelButton.Name = "CancelButton";
            this.CancelButton.Size = new System.Drawing.Size(88, 27);
            this.CancelButton.TabIndex = 2;
            this.CancelButton.Text = "Cancel";
            this.CancelButton.UseVisualStyleBackColor = true;
            this.CancelButton.Click += new System.EventHandler(this.CancelButton_Click);
            // 
            // VendorLabel
            // 
            this.VendorLabel.AutoSize = true;
            this.VendorLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.VendorLabel.Location = new System.Drawing.Point(292, 35);
            this.VendorLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.VendorLabel.Name = "VendorLabel";
            this.VendorLabel.Size = new System.Drawing.Size(47, 13);
            this.VendorLabel.TabIndex = 3;
            this.VendorLabel.Text = "Vendor";
            // 
            // ModelLabel
            // 
            this.ModelLabel.AutoSize = true;
            this.ModelLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.ModelLabel.Location = new System.Drawing.Point(292, 60);
            this.ModelLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.ModelLabel.Name = "ModelLabel";
            this.ModelLabel.Size = new System.Drawing.Size(41, 13);
            this.ModelLabel.TabIndex = 4;
            this.ModelLabel.Text = "Model";
            // 
            // SerialLabel
            // 
            this.SerialLabel.AutoSize = true;
            this.SerialLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.SerialLabel.Location = new System.Drawing.Point(292, 85);
            this.SerialLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.SerialLabel.Name = "SerialLabel";
            this.SerialLabel.Size = new System.Drawing.Size(39, 13);
            this.SerialLabel.TabIndex = 5;
            this.SerialLabel.Text = "Serial";
            // 
            // VendorNameLabel
            // 
            this.VendorNameLabel.AutoSize = true;
            this.VendorNameLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.VendorNameLabel.Location = new System.Drawing.Point(371, 35);
            this.VendorNameLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.VendorNameLabel.Name = "VendorNameLabel";
            this.VendorNameLabel.Size = new System.Drawing.Size(68, 13);
            this.VendorNameLabel.TabIndex = 6;
            this.VendorNameLabel.Text = "VendorName";
            // 
            // ModelNameLabel
            // 
            this.ModelNameLabel.AutoSize = true;
            this.ModelNameLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.ModelNameLabel.Location = new System.Drawing.Point(371, 60);
            this.ModelNameLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.ModelNameLabel.Name = "ModelNameLabel";
            this.ModelNameLabel.Size = new System.Drawing.Size(63, 13);
            this.ModelNameLabel.TabIndex = 7;
            this.ModelNameLabel.Text = "ModelName";
            // 
            // SerialNumberLabel
            // 
            this.SerialNumberLabel.AutoSize = true;
            this.SerialNumberLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.SerialNumberLabel.Location = new System.Drawing.Point(371, 85);
            this.SerialNumberLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.SerialNumberLabel.Name = "SerialNumberLabel";
            this.SerialNumberLabel.Size = new System.Drawing.Size(39, 13);
            this.SerialNumberLabel.TabIndex = 8;
            this.SerialNumberLabel.Text = "Serial#";
            // 
            // UniqueIDLabel
            // 
            this.UniqueIDLabel.AutoSize = true;
            this.UniqueIDLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.UniqueIDLabel.Location = new System.Drawing.Point(292, 111);
            this.UniqueIDLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.UniqueIDLabel.Name = "UniqueIDLabel";
            this.UniqueIDLabel.Size = new System.Drawing.Size(60, 13);
            this.UniqueIDLabel.TabIndex = 9;
            this.UniqueIDLabel.Text = "UniqueID";
            // 
            // DeviceUniqueIDLabel
            // 
            this.DeviceUniqueIDLabel.AutoSize = true;
            this.DeviceUniqueIDLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.DeviceUniqueIDLabel.Location = new System.Drawing.Point(371, 111);
            this.DeviceUniqueIDLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.DeviceUniqueIDLabel.Name = "DeviceUniqueIDLabel";
            this.DeviceUniqueIDLabel.Size = new System.Drawing.Size(84, 13);
            this.DeviceUniqueIDLabel.TabIndex = 10;
            this.DeviceUniqueIDLabel.Text = "DeviceUniqueID";
            // 
            // AccessibleLabel
            // 
            this.AccessibleLabel.AutoSize = true;
            this.AccessibleLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.AccessibleLabel.Location = new System.Drawing.Point(292, 136);
            this.AccessibleLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.AccessibleLabel.Name = "AccessibleLabel";
            this.AccessibleLabel.Size = new System.Drawing.Size(68, 13);
            this.AccessibleLabel.TabIndex = 11;
            this.AccessibleLabel.Text = "Accessible";
            // 
            // AccessStatusLabel
            // 
            this.AccessStatusLabel.AutoSize = true;
            this.AccessStatusLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.AccessStatusLabel.Location = new System.Drawing.Point(371, 136);
            this.AccessStatusLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.AccessStatusLabel.Name = "AccessStatusLabel";
            this.AccessStatusLabel.Size = new System.Drawing.Size(70, 13);
            this.AccessStatusLabel.TabIndex = 12;
            this.AccessStatusLabel.Text = "AccessStatus";
            // 
            // OpenCameraDialogue
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(530, 233);
            this.Controls.Add(this.AccessStatusLabel);
            this.Controls.Add(this.AccessibleLabel);
            this.Controls.Add(this.DeviceUniqueIDLabel);
            this.Controls.Add(this.UniqueIDLabel);
            this.Controls.Add(this.SerialNumberLabel);
            this.Controls.Add(this.ModelNameLabel);
            this.Controls.Add(this.VendorNameLabel);
            this.Controls.Add(this.SerialLabel);
            this.Controls.Add(this.ModelLabel);
            this.Controls.Add(this.VendorLabel);
            this.Controls.Add(this.CancelButton);
            this.Controls.Add(this.OKButton);
            this.Controls.Add(this.CameraListBox);
            this.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.Name = "OpenCameraDialogue";
            this.Text = "Choose camera";
            this.TopMost = true;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.OpenCameraDialogue_FormClosing);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListBox CameraListBox;
        private System.Windows.Forms.Button OKButton;
        private new System.Windows.Forms.Button CancelButton;
        private System.Windows.Forms.Label VendorLabel;
        private System.Windows.Forms.Label ModelLabel;
        private System.Windows.Forms.Label SerialLabel;
        private System.Windows.Forms.Label VendorNameLabel;
        private System.Windows.Forms.Label ModelNameLabel;
        private System.Windows.Forms.Label SerialNumberLabel;
        private System.Windows.Forms.Label UniqueIDLabel;
        private System.Windows.Forms.Label DeviceUniqueIDLabel;
        private System.Windows.Forms.Label AccessibleLabel;
        private System.Windows.Forms.Label AccessStatusLabel;
    }
}