namespace WinFormsDemoApp
{
    partial class About
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(About));
            OKbutton = new System.Windows.Forms.Button();
            TitleLabel = new System.Windows.Forms.Label();
            CopyrightLabel = new System.Windows.Forms.Label();
            CompanyLabel = new System.Windows.Forms.Label();
            AuthorLabel = new System.Windows.Forms.Label();
            LinkLabel = new System.Windows.Forms.LinkLabel();
            DescriptionLabel = new System.Windows.Forms.Label();
            logoBox = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)logoBox).BeginInit();
            SuspendLayout();
            // 
            // OKbutton
            // 
            OKbutton.Location = new System.Drawing.Point(342, 134);
            OKbutton.Name = "OKbutton";
            OKbutton.Size = new System.Drawing.Size(49, 26);
            OKbutton.TabIndex = 0;
            OKbutton.Text = "&OK";
            OKbutton.UseVisualStyleBackColor = true;
            OKbutton.Click += OKbutton_Click;
            // 
            // TitleLabel
            // 
            TitleLabel.AutoSize = true;
            TitleLabel.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            TitleLabel.Location = new System.Drawing.Point(12, 9);
            TitleLabel.Name = "TitleLabel";
            TitleLabel.Size = new System.Drawing.Size(32, 15);
            TitleLabel.TabIndex = 1;
            TitleLabel.Text = "Title";
            // 
            // CopyrightLabel
            // 
            CopyrightLabel.AutoSize = true;
            CopyrightLabel.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            CopyrightLabel.Location = new System.Drawing.Point(121, 68);
            CopyrightLabel.Name = "CopyrightLabel";
            CopyrightLabel.Size = new System.Drawing.Size(61, 15);
            CopyrightLabel.TabIndex = 2;
            CopyrightLabel.Text = "Copyright";
            // 
            // CompanyLabel
            // 
            CompanyLabel.AutoSize = true;
            CompanyLabel.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            CompanyLabel.Location = new System.Drawing.Point(121, 87);
            CompanyLabel.Name = "CompanyLabel";
            CompanyLabel.Size = new System.Drawing.Size(58, 15);
            CompanyLabel.TabIndex = 3;
            CompanyLabel.Text = "Company";
            // 
            // AuthorLabel
            // 
            AuthorLabel.AutoSize = true;
            AuthorLabel.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            AuthorLabel.Location = new System.Drawing.Point(121, 106);
            AuthorLabel.Name = "AuthorLabel";
            AuthorLabel.Size = new System.Drawing.Size(46, 15);
            AuthorLabel.TabIndex = 4;
            AuthorLabel.Text = "Author";
            // 
            // LinkLabel
            // 
            LinkLabel.AutoSize = true;
            LinkLabel.Location = new System.Drawing.Point(121, 125);
            LinkLabel.Name = "LinkLabel";
            LinkLabel.Size = new System.Drawing.Size(29, 15);
            LinkLabel.TabIndex = 5;
            LinkLabel.TabStop = true;
            LinkLabel.Text = "Link";
            // 
            // DescriptionLabel
            // 
            DescriptionLabel.AutoSize = true;
            DescriptionLabel.Location = new System.Drawing.Point(12, 28);
            DescriptionLabel.MaximumSize = new System.Drawing.Size(370, 70);
            DescriptionLabel.Name = "DescriptionLabel";
            DescriptionLabel.Size = new System.Drawing.Size(67, 15);
            DescriptionLabel.TabIndex = 6;
            DescriptionLabel.Text = "Description";
            // 
            // logoBox
            // 
            logoBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            logoBox.Image = (System.Drawing.Image)resources.GetObject("logoBox.Image");
            logoBox.Location = new System.Drawing.Point(23, 65);
            logoBox.Name = "logoBox";
            logoBox.Size = new System.Drawing.Size(80, 80);
            logoBox.TabIndex = 7;
            logoBox.TabStop = false;
            // 
            // About
            // 
            AcceptButton = OKbutton;
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(407, 169);
            Controls.Add(logoBox);
            Controls.Add(DescriptionLabel);
            Controls.Add(LinkLabel);
            Controls.Add(AuthorLabel);
            Controls.Add(CompanyLabel);
            Controls.Add(CopyrightLabel);
            Controls.Add(TitleLabel);
            Controls.Add(OKbutton);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "About";
            ShowInTaskbar = false;
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            Text = "About TestGUI";
            ((System.ComponentModel.ISupportInitialize)logoBox).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Button OKbutton;
        private System.Windows.Forms.Label TitleLabel;
        private System.Windows.Forms.Label CopyrightLabel;
        private System.Windows.Forms.Label CompanyLabel;
        private System.Windows.Forms.Label AuthorLabel;
        private System.Windows.Forms.LinkLabel LinkLabel;
        private System.Windows.Forms.Label DescriptionLabel;
        private System.Windows.Forms.PictureBox logoBox;
    }
}