using WinFormsDemoApp.Controls;
namespace WinFormsDemoApp.UserControls;

partial class GcPlayBackControl
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
        System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(GcPlayBackControl));
        ColorSlider = new ColorSlider();
        PlaybackTextBox = new System.Windows.Forms.TextBox();
        PlayPauseButton = new System.Windows.Forms.Button();
        StartButton = new System.Windows.Forms.Button();
        StepBackButton = new System.Windows.Forms.Button();
        StepForwardButton = new System.Windows.Forms.Button();
        EndButton = new System.Windows.Forms.Button();
        SuspendLayout();
        // 
        // ColorSlider
        // 
        ColorSlider.BackColor = System.Drawing.Color.Transparent;
        ColorSlider.BarPenColorBottom = System.Drawing.Color.FromArgb(87, 94, 110);
        ColorSlider.BarPenColorTop = System.Drawing.Color.FromArgb(55, 60, 74);
        ColorSlider.BorderRoundRectSize = new System.Drawing.Size(8, 8);
        ColorSlider.ElapsedInnerColor = System.Drawing.Color.FromArgb(21, 56, 152);
        ColorSlider.ElapsedPenColorBottom = System.Drawing.Color.FromArgb(99, 130, 208);
        ColorSlider.ElapsedPenColorTop = System.Drawing.Color.FromArgb(95, 140, 180);
        ColorSlider.Font = new System.Drawing.Font("Microsoft Sans Serif", 6F);
        ColorSlider.ForeColor = System.Drawing.SystemColors.Control;
        ColorSlider.LargeChange = new decimal(new int[] { 5, 0, 0, 0 });
        ColorSlider.Location = new System.Drawing.Point(2, 5);
        ColorSlider.Maximum = new decimal(new int[] { 100, 0, 0, 0 });
        ColorSlider.Minimum = new decimal(new int[] { 0, 0, 0, 0 });
        ColorSlider.Name = "ColorSlider";
        ColorSlider.Padding = 2;
        ColorSlider.ScaleDivisions = new decimal(new int[] { 10, 0, 0, 0 });
        ColorSlider.ScaleSubDivisions = new decimal(new int[] { 5, 0, 0, 0 });
        ColorSlider.ShowDivisionsText = false;
        ColorSlider.ShowSmallScale = false;
        ColorSlider.Size = new System.Drawing.Size(634, 25);
        ColorSlider.SmallChange = new decimal(new int[] { 1, 0, 0, 0 });
        ColorSlider.TabIndex = 1;
        ColorSlider.ThumbInnerColor = System.Drawing.Color.FromArgb(21, 56, 152);
        ColorSlider.ThumbPenColor = System.Drawing.Color.FromArgb(21, 56, 152);
        ColorSlider.ThumbRoundRectSize = new System.Drawing.Size(14, 14);
        ColorSlider.ThumbSize = new System.Drawing.Size(14, 14);
        ColorSlider.TickAdd = 0F;
        ColorSlider.TickColor = System.Drawing.Color.White;
        ColorSlider.TickDivide = 0F;
        ColorSlider.TickStyle = System.Windows.Forms.TickStyle.None;
        ColorSlider.Value = new decimal(new int[] { 100, 0, 0, 0 });
        // 
        // PlaybackTextBox
        // 
        PlaybackTextBox.Enabled = false;
        PlaybackTextBox.Font = new System.Drawing.Font("Segoe UI", 8F);
        PlaybackTextBox.Location = new System.Drawing.Point(184, 36);
        PlaybackTextBox.Name = "PlaybackTextBox";
        PlaybackTextBox.Size = new System.Drawing.Size(173, 22);
        PlaybackTextBox.TabIndex = 8;
        // 
        // PlayPauseButton
        // 
        PlayPauseButton.BackgroundImage = (System.Drawing.Image)resources.GetObject("PlayPauseButton.BackgroundImage");
        PlayPauseButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
        PlayPauseButton.Location = new System.Drawing.Point(14, 36);
        PlayPauseButton.Name = "PlayPauseButton";
        PlayPauseButton.Size = new System.Drawing.Size(22, 22);
        PlayPauseButton.TabIndex = 9;
        PlayPauseButton.UseVisualStyleBackColor = true;
        PlayPauseButton.Click += PlaybackPlayPauseButton_Click;
        // 
        // StartButton
        // 
        StartButton.BackgroundImage = (System.Drawing.Image)resources.GetObject("StartButton.BackgroundImage");
        StartButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
        StartButton.Location = new System.Drawing.Point(58, 36);
        StartButton.Name = "StartButton";
        StartButton.Size = new System.Drawing.Size(22, 22);
        StartButton.TabIndex = 10;
        StartButton.UseVisualStyleBackColor = true;
        StartButton.Click += PlaybackStartButton_Click;
        // 
        // StepBackButton
        // 
        StepBackButton.BackgroundImage = (System.Drawing.Image)resources.GetObject("StepBackButton.BackgroundImage");
        StepBackButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
        StepBackButton.Location = new System.Drawing.Point(84, 36);
        StepBackButton.Name = "StepBackButton";
        StepBackButton.Size = new System.Drawing.Size(22, 22);
        StepBackButton.TabIndex = 11;
        StepBackButton.UseVisualStyleBackColor = true;
        StepBackButton.Click += PlaybackStepBackButton_Click;
        // 
        // StepForwardButton
        // 
        StepForwardButton.BackgroundImage = (System.Drawing.Image)resources.GetObject("StepForwardButton.BackgroundImage");
        StepForwardButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
        StepForwardButton.Location = new System.Drawing.Point(110, 36);
        StepForwardButton.Name = "StepForwardButton";
        StepForwardButton.Size = new System.Drawing.Size(22, 22);
        StepForwardButton.TabIndex = 12;
        StepForwardButton.UseVisualStyleBackColor = true;
        StepForwardButton.Click += PlaybackStepForwardButton_Click;
        // 
        // EndButton
        // 
        EndButton.BackgroundImage = (System.Drawing.Image)resources.GetObject("EndButton.BackgroundImage");
        EndButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
        EndButton.Location = new System.Drawing.Point(136, 36);
        EndButton.Name = "EndButton";
        EndButton.Size = new System.Drawing.Size(22, 22);
        EndButton.TabIndex = 13;
        EndButton.UseVisualStyleBackColor = true;
        EndButton.Click += PlaybackEndButton_Click;
        // 
        // GcPlayBackControl
        // 
        AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        BackColor = System.Drawing.Color.Transparent;
        Controls.Add(EndButton);
        Controls.Add(StepForwardButton);
        Controls.Add(StepBackButton);
        Controls.Add(StartButton);
        Controls.Add(PlayPauseButton);
        Controls.Add(PlaybackTextBox);
        Controls.Add(ColorSlider);
        Name = "GcPlayBackControl";
        Size = new System.Drawing.Size(640, 65);
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private ColorSlider ColorSlider;
    private System.Windows.Forms.TextBox PlaybackTextBox;
    private System.Windows.Forms.Button PlayPauseButton;
    private System.Windows.Forms.Button StartButton;
    private System.Windows.Forms.Button StepBackButton;
    private System.Windows.Forms.Button StepForwardButton;
    private System.Windows.Forms.Button EndButton;
}
