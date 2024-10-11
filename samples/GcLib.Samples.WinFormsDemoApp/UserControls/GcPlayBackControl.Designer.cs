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
        this.ColorSlider = new ColorSlider();
        this.PlaybackTextBox = new System.Windows.Forms.TextBox();
        this.PlayPauseButton = new System.Windows.Forms.Button();
        this.StartButton = new System.Windows.Forms.Button();
        this.StepBackButton = new System.Windows.Forms.Button();
        this.StepForwardButton = new System.Windows.Forms.Button();
        this.EndButton = new System.Windows.Forms.Button();
        this.SuspendLayout();
        // 
        // ColorSlider
        // 
        this.ColorSlider.BackColor = System.Drawing.Color.Transparent;
        this.ColorSlider.BarPenColorBottom = System.Drawing.Color.FromArgb(((int)(((byte)(87)))), ((int)(((byte)(94)))), ((int)(((byte)(110)))));
        this.ColorSlider.BarPenColorTop = System.Drawing.Color.FromArgb(((int)(((byte)(55)))), ((int)(((byte)(60)))), ((int)(((byte)(74)))));
        this.ColorSlider.BorderRoundRectSize = new System.Drawing.Size(8, 8);
        this.ColorSlider.ElapsedInnerColor = System.Drawing.Color.FromArgb(((int)(((byte)(21)))), ((int)(((byte)(56)))), ((int)(((byte)(152)))));
        this.ColorSlider.ElapsedPenColorBottom = System.Drawing.Color.FromArgb(((int)(((byte)(99)))), ((int)(((byte)(130)))), ((int)(((byte)(208)))));
        this.ColorSlider.ElapsedPenColorTop = System.Drawing.Color.FromArgb(((int)(((byte)(95)))), ((int)(((byte)(140)))), ((int)(((byte)(180)))));
        this.ColorSlider.Font = new System.Drawing.Font("Microsoft Sans Serif", 6F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
        this.ColorSlider.ForeColor = System.Drawing.SystemColors.Control;
        this.ColorSlider.LargeChange = new decimal(new int[] {
        5,
        0,
        0,
        0});
        this.ColorSlider.Location = new System.Drawing.Point(5, 5);
        this.ColorSlider.Maximum = new decimal(new int[] {
        100,
        0,
        0,
        0});
        this.ColorSlider.Minimum = new decimal(new int[] {
        0,
        0,
        0,
        0});
        this.ColorSlider.Name = "ColorSlider";
        this.ColorSlider.Padding = 2;
        this.ColorSlider.ScaleDivisions = new decimal(new int[] {
        10,
        0,
        0,
        0});
        this.ColorSlider.ScaleSubDivisions = new decimal(new int[] {
        5,
        0,
        0,
        0});
        this.ColorSlider.ShowDivisionsText = false;
        this.ColorSlider.ShowSmallScale = false;
        this.ColorSlider.Size = new System.Drawing.Size(736, 25);
        this.ColorSlider.SmallChange = new decimal(new int[] {
        1,
        0,
        0,
        0});
        this.ColorSlider.TabIndex = 1;
        this.ColorSlider.ThumbInnerColor = System.Drawing.Color.FromArgb(((int)(((byte)(21)))), ((int)(((byte)(56)))), ((int)(((byte)(152)))));
        this.ColorSlider.ThumbPenColor = System.Drawing.Color.FromArgb(((int)(((byte)(21)))), ((int)(((byte)(56)))), ((int)(((byte)(152)))));
        this.ColorSlider.ThumbRoundRectSize = new System.Drawing.Size(14, 14);
        this.ColorSlider.ThumbSize = new System.Drawing.Size(14, 14);
        this.ColorSlider.TickAdd = 0F;
        this.ColorSlider.TickColor = System.Drawing.Color.White;
        this.ColorSlider.TickDivide = 0F;
        this.ColorSlider.TickStyle = System.Windows.Forms.TickStyle.None;
        this.ColorSlider.Value = new decimal(new int[] {
        100,
        0,
        0,
        0});
        // 
        // PlaybackTextBox
        // 
        this.PlaybackTextBox.Enabled = false;
        this.PlaybackTextBox.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
        this.PlaybackTextBox.Location = new System.Drawing.Point(184, 36);
        this.PlaybackTextBox.Name = "PlaybackTextBox";
        this.PlaybackTextBox.Size = new System.Drawing.Size(173, 22);
        this.PlaybackTextBox.TabIndex = 8;
        // 
        // PlayPauseButton
        // 
        this.PlayPauseButton.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("PlayPauseButton.BackgroundImage")));
        this.PlayPauseButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
        this.PlayPauseButton.Location = new System.Drawing.Point(14, 36);
        this.PlayPauseButton.Name = "PlayPauseButton";
        this.PlayPauseButton.Size = new System.Drawing.Size(22, 22);
        this.PlayPauseButton.TabIndex = 9;
        this.PlayPauseButton.UseVisualStyleBackColor = true;
        this.PlayPauseButton.Click += new System.EventHandler(this.PlaybackPlayPauseButton_Click);
        // 
        // StartButton
        // 
        this.StartButton.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("StartButton.BackgroundImage")));
        this.StartButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
        this.StartButton.Location = new System.Drawing.Point(58, 36);
        this.StartButton.Name = "StartButton";
        this.StartButton.Size = new System.Drawing.Size(22, 22);
        this.StartButton.TabIndex = 10;
        this.StartButton.UseVisualStyleBackColor = true;
        this.StartButton.Click += new System.EventHandler(this.PlaybackStartButton_Click);
        // 
        // StepBackButton
        // 
        this.StepBackButton.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("StepBackButton.BackgroundImage")));
        this.StepBackButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
        this.StepBackButton.Location = new System.Drawing.Point(84, 36);
        this.StepBackButton.Name = "StepBackButton";
        this.StepBackButton.Size = new System.Drawing.Size(22, 22);
        this.StepBackButton.TabIndex = 11;
        this.StepBackButton.UseVisualStyleBackColor = true;
        this.StepBackButton.Click += new System.EventHandler(this.PlaybackStepBackButton_Click);
        // 
        // StepForwardButton
        // 
        this.StepForwardButton.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("StepForwardButton.BackgroundImage")));
        this.StepForwardButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
        this.StepForwardButton.Location = new System.Drawing.Point(110, 36);
        this.StepForwardButton.Name = "StepForwardButton";
        this.StepForwardButton.Size = new System.Drawing.Size(22, 22);
        this.StepForwardButton.TabIndex = 12;
        this.StepForwardButton.UseVisualStyleBackColor = true;
        this.StepForwardButton.Click += new System.EventHandler(this.PlaybackStepForwardButton_Click);
        // 
        // EndButton
        // 
        this.EndButton.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("EndButton.BackgroundImage")));
        this.EndButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
        this.EndButton.Location = new System.Drawing.Point(136, 36);
        this.EndButton.Name = "EndButton";
        this.EndButton.Size = new System.Drawing.Size(22, 22);
        this.EndButton.TabIndex = 13;
        this.EndButton.UseVisualStyleBackColor = true;
        this.EndButton.Click += new System.EventHandler(this.PlaybackEndButton_Click);
        // 
        // PlayBackControl
        // 
        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.BackColor = System.Drawing.Color.Transparent;
        this.Controls.Add(this.EndButton);
        this.Controls.Add(this.StepForwardButton);
        this.Controls.Add(this.StepBackButton);
        this.Controls.Add(this.StartButton);
        this.Controls.Add(this.PlayPauseButton);
        this.Controls.Add(this.PlaybackTextBox);
        this.Controls.Add(this.ColorSlider);
        this.Name = "PlayBackControl";
        this.Size = new System.Drawing.Size(746, 64);
        this.ResumeLayout(false);
        this.PerformLayout();

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
