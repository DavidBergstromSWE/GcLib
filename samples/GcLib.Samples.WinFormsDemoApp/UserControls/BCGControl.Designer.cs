
namespace WinFormsDemoApp
{
    partial class BCGControl
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
            this.components = new System.ComponentModel.Container();
            this.HistogramBox = new Emgu.CV.UI.HistogramBox();
            ((System.ComponentModel.ISupportInitialize)(this.HistogramBox)).BeginInit();
            this.SuspendLayout();
            // 
            // HistogramBox
            // 
            this.HistogramBox.Location = new System.Drawing.Point(18, 11);
            this.HistogramBox.Name = "HistogramBox";
            this.HistogramBox.Size = new System.Drawing.Size(242, 131);
            this.HistogramBox.TabIndex = 2;
            this.HistogramBox.TabStop = false;
            // 
            // BCGControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.HistogramBox);
            this.Name = "BCGControl";
            this.Size = new System.Drawing.Size(279, 162);
            ((System.ComponentModel.ISupportInitialize)(this.HistogramBox)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private Emgu.CV.UI.HistogramBox HistogramBox;
    }
}
