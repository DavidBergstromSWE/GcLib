
namespace WinFormsDemoApp
{
    partial class StatusControl
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.StatusPanel = new CustomGUIControls.BorderedGroupBox();
            this.RecordingQueueSize = new System.Windows.Forms.Label();
            this.RecordingQueueTitle = new System.Windows.Forms.Label();
            this.DisplayQueueSize = new System.Windows.Forms.Label();
            this.DisplayQueueTitle = new System.Windows.Forms.Label();
            this.FrameRateAverage = new System.Windows.Forms.Label();
            this.FrameRateCurrent = new System.Windows.Forms.Label();
            this.FrameRateAvgTitle = new System.Windows.Forms.Label();
            this.FrameRateCurrentTitle = new System.Windows.Forms.Label();
            this.ImagesDropped = new System.Windows.Forms.Label();
            this.OutputBufferQueueSize = new System.Windows.Forms.Label();
            this.ImagesDroppedTitle = new System.Windows.Forms.Label();
            this.OutputBufferQueueTitle = new System.Windows.Forms.Label();
            this.ImagesGrabbed = new System.Windows.Forms.Label();
            this.ImagesGrabbedTitle = new System.Windows.Forms.Label();
            this.StatusPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // StatusPanel
            // 
            this.StatusPanel.Controls.Add(this.RecordingQueueSize);
            this.StatusPanel.Controls.Add(this.RecordingQueueTitle);
            this.StatusPanel.Controls.Add(this.DisplayQueueSize);
            this.StatusPanel.Controls.Add(this.DisplayQueueTitle);
            this.StatusPanel.Controls.Add(this.FrameRateAverage);
            this.StatusPanel.Controls.Add(this.FrameRateCurrent);
            this.StatusPanel.Controls.Add(this.FrameRateAvgTitle);
            this.StatusPanel.Controls.Add(this.FrameRateCurrentTitle);
            this.StatusPanel.Controls.Add(this.ImagesDropped);
            this.StatusPanel.Controls.Add(this.OutputBufferQueueSize);
            this.StatusPanel.Controls.Add(this.ImagesDroppedTitle);
            this.StatusPanel.Controls.Add(this.OutputBufferQueueTitle);
            this.StatusPanel.Controls.Add(this.ImagesGrabbed);
            this.StatusPanel.Controls.Add(this.ImagesGrabbedTitle);
            this.StatusPanel.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.StatusPanel.Location = new System.Drawing.Point(4, 0);
            this.StatusPanel.Margin = new System.Windows.Forms.Padding(0);
            this.StatusPanel.Name = "StatusPanel";
            this.StatusPanel.Padding = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.StatusPanel.Size = new System.Drawing.Size(622, 64);
            this.StatusPanel.TabIndex = 6;
            this.StatusPanel.TabStop = false;
            this.StatusPanel.Text = "Status";
            // 
            // RecordingQueueSize
            // 
            this.RecordingQueueSize.AutoSize = true;
            this.RecordingQueueSize.Location = new System.Drawing.Point(456, 38);
            this.RecordingQueueSize.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.RecordingQueueSize.Name = "RecordingQueueSize";
            this.RecordingQueueSize.Size = new System.Drawing.Size(13, 14);
            this.RecordingQueueSize.TabIndex = 24;
            this.RecordingQueueSize.Text = "0";
            // 
            // RecordingQueueTitle
            // 
            this.RecordingQueueTitle.AutoSize = true;
            this.RecordingQueueTitle.Location = new System.Drawing.Point(346, 38);
            this.RecordingQueueTitle.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.RecordingQueueTitle.Name = "RecordingQueueTitle";
            this.RecordingQueueTitle.Size = new System.Drawing.Size(88, 14);
            this.RecordingQueueTitle.TabIndex = 23;
            this.RecordingQueueTitle.Text = "RecordingQueue";
            // 
            // DisplayQueueSize
            // 
            this.DisplayQueueSize.AutoSize = true;
            this.DisplayQueueSize.Location = new System.Drawing.Point(586, 20);
            this.DisplayQueueSize.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.DisplayQueueSize.Name = "DisplayQueueSize";
            this.DisplayQueueSize.Size = new System.Drawing.Size(13, 14);
            this.DisplayQueueSize.TabIndex = 22;
            this.DisplayQueueSize.Text = "0";
            // 
            // DisplayQueueTitle
            // 
            this.DisplayQueueTitle.AutoSize = true;
            this.DisplayQueueTitle.Location = new System.Drawing.Point(504, 20);
            this.DisplayQueueTitle.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.DisplayQueueTitle.Name = "DisplayQueueTitle";
            this.DisplayQueueTitle.Size = new System.Drawing.Size(74, 14);
            this.DisplayQueueTitle.TabIndex = 21;
            this.DisplayQueueTitle.Text = "DisplayQueue";
            // 
            // FrameRateAverage
            // 
            this.FrameRateAverage.AutoSize = true;
            this.FrameRateAverage.Location = new System.Drawing.Point(297, 38);
            this.FrameRateAverage.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.FrameRateAverage.Name = "FrameRateAverage";
            this.FrameRateAverage.Size = new System.Drawing.Size(13, 14);
            this.FrameRateAverage.TabIndex = 20;
            this.FrameRateAverage.Text = "0";
            // 
            // FrameRateCurrent
            // 
            this.FrameRateCurrent.AutoSize = true;
            this.FrameRateCurrent.Location = new System.Drawing.Point(297, 19);
            this.FrameRateCurrent.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.FrameRateCurrent.Name = "FrameRateCurrent";
            this.FrameRateCurrent.Size = new System.Drawing.Size(13, 14);
            this.FrameRateCurrent.TabIndex = 19;
            this.FrameRateCurrent.Text = "0";
            // 
            // FrameRateAvgTitle
            // 
            this.FrameRateAvgTitle.AutoSize = true;
            this.FrameRateAvgTitle.Location = new System.Drawing.Point(167, 38);
            this.FrameRateAvgTitle.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.FrameRateAvgTitle.Name = "FrameRateAvgTitle";
            this.FrameRateAvgTitle.Size = new System.Drawing.Size(88, 14);
            this.FrameRateAvgTitle.TabIndex = 18;
            this.FrameRateAvgTitle.Text = "Frame rate (avg)";
            // 
            // FrameRateCurrentTitle
            // 
            this.FrameRateCurrentTitle.AutoSize = true;
            this.FrameRateCurrentTitle.Location = new System.Drawing.Point(167, 19);
            this.FrameRateCurrentTitle.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.FrameRateCurrentTitle.Name = "FrameRateCurrentTitle";
            this.FrameRateCurrentTitle.Size = new System.Drawing.Size(105, 14);
            this.FrameRateCurrentTitle.TabIndex = 17;
            this.FrameRateCurrentTitle.Text = "Frame rate (current)";
            // 
            // ImagesDropped
            // 
            this.ImagesDropped.AutoSize = true;
            this.ImagesDropped.Location = new System.Drawing.Point(125, 38);
            this.ImagesDropped.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.ImagesDropped.Name = "ImagesDropped";
            this.ImagesDropped.Size = new System.Drawing.Size(13, 14);
            this.ImagesDropped.TabIndex = 5;
            this.ImagesDropped.Text = "0";
            // 
            // OutputBufferQueueSize
            // 
            this.OutputBufferQueueSize.AutoSize = true;
            this.OutputBufferQueueSize.Location = new System.Drawing.Point(456, 20);
            this.OutputBufferQueueSize.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.OutputBufferQueueSize.Name = "OutputBufferQueueSize";
            this.OutputBufferQueueSize.Size = new System.Drawing.Size(13, 14);
            this.OutputBufferQueueSize.TabIndex = 16;
            this.OutputBufferQueueSize.Text = "0";
            // 
            // ImagesDroppedTitle
            // 
            this.ImagesDroppedTitle.AutoSize = true;
            this.ImagesDroppedTitle.Location = new System.Drawing.Point(21, 38);
            this.ImagesDroppedTitle.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.ImagesDroppedTitle.Name = "ImagesDroppedTitle";
            this.ImagesDroppedTitle.Size = new System.Drawing.Size(84, 14);
            this.ImagesDroppedTitle.TabIndex = 4;
            this.ImagesDroppedTitle.Text = "Images dropped";
            // 
            // OutputBufferQueueTitle
            // 
            this.OutputBufferQueueTitle.AutoSize = true;
            this.OutputBufferQueueTitle.Location = new System.Drawing.Point(346, 19);
            this.OutputBufferQueueTitle.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.OutputBufferQueueTitle.Name = "OutputBufferQueueTitle";
            this.OutputBufferQueueTitle.Size = new System.Drawing.Size(102, 14);
            this.OutputBufferQueueTitle.TabIndex = 15;
            this.OutputBufferQueueTitle.Text = "OutputBufferQueue";
            // 
            // ImagesGrabbed
            // 
            this.ImagesGrabbed.AutoSize = true;
            this.ImagesGrabbed.Location = new System.Drawing.Point(125, 19);
            this.ImagesGrabbed.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.ImagesGrabbed.Name = "ImagesGrabbed";
            this.ImagesGrabbed.Size = new System.Drawing.Size(13, 14);
            this.ImagesGrabbed.TabIndex = 3;
            this.ImagesGrabbed.Text = "0";
            // 
            // ImagesGrabbedTitle
            // 
            this.ImagesGrabbedTitle.AutoSize = true;
            this.ImagesGrabbedTitle.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.ImagesGrabbedTitle.Location = new System.Drawing.Point(21, 19);
            this.ImagesGrabbedTitle.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.ImagesGrabbedTitle.Name = "ImagesGrabbedTitle";
            this.ImagesGrabbedTitle.Size = new System.Drawing.Size(84, 14);
            this.ImagesGrabbedTitle.TabIndex = 2;
            this.ImagesGrabbedTitle.Text = "Images grabbed";
            // 
            // StatusControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.StatusPanel);
            this.Name = "StatusControl";
            this.Size = new System.Drawing.Size(630, 68);
            this.StatusPanel.ResumeLayout(false);
            this.StatusPanel.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private CustomGUIControls.BorderedGroupBox StatusPanel;
        private System.Windows.Forms.Label RecordingQueueSize;
        private System.Windows.Forms.Label RecordingQueueTitle;
        private System.Windows.Forms.Label DisplayQueueSize;
        private System.Windows.Forms.Label DisplayQueueTitle;
        private System.Windows.Forms.Label FrameRateAverage;
        private System.Windows.Forms.Label FrameRateCurrent;
        private System.Windows.Forms.Label FrameRateAvgTitle;
        private System.Windows.Forms.Label FrameRateCurrentTitle;
        private System.Windows.Forms.Label ImagesDropped;
        private System.Windows.Forms.Label OutputBufferQueueSize;
        private System.Windows.Forms.Label ImagesDroppedTitle;
        private System.Windows.Forms.Label OutputBufferQueueTitle;
        private System.Windows.Forms.Label ImagesGrabbed;
        private System.Windows.Forms.Label ImagesGrabbedTitle;
    }
}
