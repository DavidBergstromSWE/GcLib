﻿using WinFormsDemoApp.Controls;
using WinFormsDemoApp.UserControls;

namespace WinFormsDemoApp;

partial class WinFormsDemoApp
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
        components = new System.ComponentModel.Container();
        System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(WinFormsDemoApp));
        DeviceInfoPanel = new BorderedGroupBox();
        VendorNameLabel = new System.Windows.Forms.Label();
        ClassLabel = new System.Windows.Forms.Label();
        ClassTitle = new System.Windows.Forms.Label();
        UniqueIDLabel = new System.Windows.Forms.Label();
        SerialLabel = new System.Windows.Forms.Label();
        ModelLabel = new System.Windows.Forms.Label();
        UniqueIDTitle = new System.Windows.Forms.Label();
        SerialTitle = new System.Windows.Forms.Label();
        ModelTitle = new System.Windows.Forms.Label();
        VendorTitle = new System.Windows.Forms.Label();
        AcquisitionPanel = new BorderedGroupBox();
        SaveImagesTextBox = new System.Windows.Forms.TextBox();
        MultiFrameTitle = new System.Windows.Forms.Label();
        AcquisitionFrameCountTextBox = new System.Windows.Forms.TextBox();
        SaveImagesButton = new System.Windows.Forms.Button();
        AcquisitionModeComboBox = new System.Windows.Forms.ComboBox();
        AcquisitionModeTitle = new System.Windows.Forms.Label();
        RecordButton = new System.Windows.Forms.Button();
        StopButton = new System.Windows.Forms.Button();
        PlayButton = new System.Windows.Forms.Button();
        ConnectButton = new System.Windows.Forms.Button();
        SaveFileDialog = new System.Windows.Forms.SaveFileDialog();
        DisplayPanel = new System.Windows.Forms.Panel();
        ExitPlaybackButton = new System.Windows.Forms.Button();
        PlayBackControl = new GcPlayBackControl();
        DisplayControl = new GcDisplayControl();
        StatusControl = new StatusControl();
        MainMenu = new System.Windows.Forms.MenuStrip();
        FileMenuItem = new System.Windows.Forms.ToolStripMenuItem();
        LoadConfigurationMenuItem = new System.Windows.Forms.ToolStripMenuItem();
        SaveConfigurationMenuItem = new System.Windows.Forms.ToolStripMenuItem();
        FileMenuSeparator1 = new System.Windows.Forms.ToolStripSeparator();
        OpenImagesMenuItem = new System.Windows.Forms.ToolStripMenuItem();
        CloseImagesMenuItem = new System.Windows.Forms.ToolStripMenuItem();
        FileMenuSeparator2 = new System.Windows.Forms.ToolStripSeparator();
        ExitMenuItem = new System.Windows.Forms.ToolStripMenuItem();
        ViewMenuItem = new System.Windows.Forms.ToolStripMenuItem();
        ShowStatusControlMenuItem = new System.Windows.Forms.ToolStripMenuItem();
        DisplayMenuItem = new System.Windows.Forms.ToolStripMenuItem();
        DisplayFPSCheckBox = new System.Windows.Forms.ToolStripMenuItem();
        DisplayFrameCounterCheckBox = new System.Windows.Forms.ToolStripMenuItem();
        DisplayTimeStampCheckBox = new System.Windows.Forms.ToolStripMenuItem();
        DisplayAllCheckBox = new System.Windows.Forms.ToolStripMenuItem();
        DisplayMenuSeparator1 = new System.Windows.Forms.ToolStripSeparator();
        LimitFPSCheckBox = new System.Windows.Forms.ToolStripMenuItem();
        HelpMenuItem = new System.Windows.Forms.ToolStripMenuItem();
        AboutMenuItem = new System.Windows.Forms.ToolStripMenuItem();
        OpenFileDialog = new System.Windows.Forms.OpenFileDialog();
        DisconnectButton = new System.Windows.Forms.Button();
        ParameterGridView = new GcParameterGridView();
        DeviceInfoPanel.SuspendLayout();
        AcquisitionPanel.SuspendLayout();
        DisplayPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)DisplayControl).BeginInit();
        MainMenu.SuspendLayout();
        SuspendLayout();
        // 
        // DeviceInfoPanel
        // 
        DeviceInfoPanel.Controls.Add(VendorNameLabel);
        DeviceInfoPanel.Controls.Add(ClassLabel);
        DeviceInfoPanel.Controls.Add(ClassTitle);
        DeviceInfoPanel.Controls.Add(UniqueIDLabel);
        DeviceInfoPanel.Controls.Add(SerialLabel);
        DeviceInfoPanel.Controls.Add(ModelLabel);
        DeviceInfoPanel.Controls.Add(UniqueIDTitle);
        DeviceInfoPanel.Controls.Add(SerialTitle);
        DeviceInfoPanel.Controls.Add(ModelTitle);
        DeviceInfoPanel.Controls.Add(VendorTitle);
        DeviceInfoPanel.Font = new System.Drawing.Font("Arial", 8.5F, System.Drawing.FontStyle.Bold);
        DeviceInfoPanel.Location = new System.Drawing.Point(12, 120);
        DeviceInfoPanel.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
        DeviceInfoPanel.Name = "DeviceInfoPanel";
        DeviceInfoPanel.Padding = new System.Windows.Forms.Padding(4, 3, 4, 3);
        DeviceInfoPanel.Size = new System.Drawing.Size(328, 168);
        DeviceInfoPanel.TabIndex = 19;
        DeviceInfoPanel.TabStop = false;
        DeviceInfoPanel.Text = "Device info";
        // 
        // VendorNameLabel
        // 
        VendorNameLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F);
        VendorNameLabel.Location = new System.Drawing.Point(100, 29);
        VendorNameLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
        VendorNameLabel.MaximumSize = new System.Drawing.Size(200, 13);
        VendorNameLabel.Name = "VendorNameLabel";
        VendorNameLabel.Size = new System.Drawing.Size(200, 13);
        VendorNameLabel.TabIndex = 30;
        VendorNameLabel.Text = "VendorName";
        VendorNameLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        // 
        // ClassLabel
        // 
        ClassLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F);
        ClassLabel.Location = new System.Drawing.Point(100, 134);
        ClassLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
        ClassLabel.MaximumSize = new System.Drawing.Size(200, 13);
        ClassLabel.Name = "ClassLabel";
        ClassLabel.Size = new System.Drawing.Size(200, 13);
        ClassLabel.TabIndex = 35;
        ClassLabel.Text = "ClassName";
        ClassLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        // 
        // ClassTitle
        // 
        ClassTitle.AutoSize = true;
        ClassTitle.Font = new System.Drawing.Font("Arial", 8F, System.Drawing.FontStyle.Bold);
        ClassTitle.Location = new System.Drawing.Point(20, 134);
        ClassTitle.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
        ClassTitle.Name = "ClassTitle";
        ClassTitle.Size = new System.Drawing.Size(38, 14);
        ClassTitle.TabIndex = 34;
        ClassTitle.Text = "Class";
        // 
        // UniqueIDLabel
        // 
        UniqueIDLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F);
        UniqueIDLabel.Location = new System.Drawing.Point(100, 108);
        UniqueIDLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
        UniqueIDLabel.MaximumSize = new System.Drawing.Size(200, 13);
        UniqueIDLabel.Name = "UniqueIDLabel";
        UniqueIDLabel.Size = new System.Drawing.Size(200, 13);
        UniqueIDLabel.TabIndex = 33;
        UniqueIDLabel.Text = "UniqueID";
        UniqueIDLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        // 
        // SerialLabel
        // 
        SerialLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F);
        SerialLabel.Location = new System.Drawing.Point(100, 82);
        SerialLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
        SerialLabel.MaximumSize = new System.Drawing.Size(200, 13);
        SerialLabel.Name = "SerialLabel";
        SerialLabel.Size = new System.Drawing.Size(200, 13);
        SerialLabel.TabIndex = 32;
        SerialLabel.Text = "SerialNumber";
        SerialLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        // 
        // ModelLabel
        // 
        ModelLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F);
        ModelLabel.Location = new System.Drawing.Point(100, 55);
        ModelLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
        ModelLabel.MaximumSize = new System.Drawing.Size(200, 13);
        ModelLabel.Name = "ModelLabel";
        ModelLabel.Size = new System.Drawing.Size(200, 13);
        ModelLabel.TabIndex = 31;
        ModelLabel.Text = "ModelName";
        ModelLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        // 
        // UniqueIDTitle
        // 
        UniqueIDTitle.AutoSize = true;
        UniqueIDTitle.Font = new System.Drawing.Font("Arial", 8F, System.Drawing.FontStyle.Bold);
        UniqueIDTitle.Location = new System.Drawing.Point(20, 108);
        UniqueIDTitle.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
        UniqueIDTitle.Name = "UniqueIDTitle";
        UniqueIDTitle.Size = new System.Drawing.Size(55, 14);
        UniqueIDTitle.TabIndex = 29;
        UniqueIDTitle.Text = "UniqueID";
        // 
        // SerialTitle
        // 
        SerialTitle.AutoSize = true;
        SerialTitle.Font = new System.Drawing.Font("Arial", 8F, System.Drawing.FontStyle.Bold);
        SerialTitle.Location = new System.Drawing.Point(20, 82);
        SerialTitle.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
        SerialTitle.Name = "SerialTitle";
        SerialTitle.Size = new System.Drawing.Size(38, 14);
        SerialTitle.TabIndex = 28;
        SerialTitle.Text = "Serial";
        // 
        // ModelTitle
        // 
        ModelTitle.AutoSize = true;
        ModelTitle.Font = new System.Drawing.Font("Arial", 8F, System.Drawing.FontStyle.Bold);
        ModelTitle.Location = new System.Drawing.Point(20, 55);
        ModelTitle.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
        ModelTitle.Name = "ModelTitle";
        ModelTitle.Size = new System.Drawing.Size(41, 14);
        ModelTitle.TabIndex = 27;
        ModelTitle.Text = "Model";
        // 
        // VendorTitle
        // 
        VendorTitle.AutoSize = true;
        VendorTitle.Font = new System.Drawing.Font("Arial", 8F, System.Drawing.FontStyle.Bold);
        VendorTitle.Location = new System.Drawing.Point(20, 29);
        VendorTitle.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
        VendorTitle.Name = "VendorTitle";
        VendorTitle.Size = new System.Drawing.Size(47, 14);
        VendorTitle.TabIndex = 26;
        VendorTitle.Text = "Vendor";
        // 
        // AcquisitionPanel
        // 
        AcquisitionPanel.Controls.Add(SaveImagesTextBox);
        AcquisitionPanel.Controls.Add(MultiFrameTitle);
        AcquisitionPanel.Controls.Add(AcquisitionFrameCountTextBox);
        AcquisitionPanel.Controls.Add(SaveImagesButton);
        AcquisitionPanel.Controls.Add(AcquisitionModeComboBox);
        AcquisitionPanel.Controls.Add(AcquisitionModeTitle);
        AcquisitionPanel.Font = new System.Drawing.Font("Arial", 8.5F, System.Drawing.FontStyle.Bold);
        AcquisitionPanel.Location = new System.Drawing.Point(12, 330);
        AcquisitionPanel.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
        AcquisitionPanel.Name = "AcquisitionPanel";
        AcquisitionPanel.Padding = new System.Windows.Forms.Padding(4, 3, 4, 3);
        AcquisitionPanel.Size = new System.Drawing.Size(328, 113);
        AcquisitionPanel.TabIndex = 4;
        AcquisitionPanel.TabStop = false;
        AcquisitionPanel.Text = "Acquisition settings";
        // 
        // SaveImagesTextBox
        // 
        SaveImagesTextBox.Font = new System.Drawing.Font("Arial", 7.5F);
        SaveImagesTextBox.Location = new System.Drawing.Point(93, 75);
        SaveImagesTextBox.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
        SaveImagesTextBox.Name = "SaveImagesTextBox";
        SaveImagesTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Horizontal;
        SaveImagesTextBox.Size = new System.Drawing.Size(225, 19);
        SaveImagesTextBox.TabIndex = 15;
        // 
        // MultiFrameTitle
        // 
        MultiFrameTitle.AutoSize = true;
        MultiFrameTitle.Location = new System.Drawing.Point(256, 19);
        MultiFrameTitle.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
        MultiFrameTitle.Name = "MultiFrameTitle";
        MultiFrameTitle.Size = new System.Drawing.Size(14, 15);
        MultiFrameTitle.TabIndex = 13;
        MultiFrameTitle.Text = "#";
        MultiFrameTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
        // 
        // AcquisitionFrameCountTextBox
        // 
        AcquisitionFrameCountTextBox.Enabled = false;
        AcquisitionFrameCountTextBox.Location = new System.Drawing.Point(238, 37);
        AcquisitionFrameCountTextBox.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
        AcquisitionFrameCountTextBox.Name = "AcquisitionFrameCountTextBox";
        AcquisitionFrameCountTextBox.Size = new System.Drawing.Size(51, 21);
        AcquisitionFrameCountTextBox.TabIndex = 12;
        AcquisitionFrameCountTextBox.Text = "10";
        AcquisitionFrameCountTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
        // 
        // SaveImagesButton
        // 
        SaveImagesButton.Font = new System.Drawing.Font("Arial", 8F);
        SaveImagesButton.Location = new System.Drawing.Point(9, 73);
        SaveImagesButton.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
        SaveImagesButton.Name = "SaveImagesButton";
        SaveImagesButton.Size = new System.Drawing.Size(77, 25);
        SaveImagesButton.TabIndex = 14;
        SaveImagesButton.Text = "Save As...";
        SaveImagesButton.UseVisualStyleBackColor = true;
        SaveImagesButton.Click += SaveImagesButton_Click;
        // 
        // AcquisitionModeComboBox
        // 
        AcquisitionModeComboBox.Enabled = false;
        AcquisitionModeComboBox.FormattingEnabled = true;
        AcquisitionModeComboBox.Location = new System.Drawing.Point(126, 37);
        AcquisitionModeComboBox.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
        AcquisitionModeComboBox.Name = "AcquisitionModeComboBox";
        AcquisitionModeComboBox.Size = new System.Drawing.Size(101, 22);
        AcquisitionModeComboBox.TabIndex = 11;
        // 
        // AcquisitionModeTitle
        // 
        AcquisitionModeTitle.AutoSize = true;
        AcquisitionModeTitle.Font = new System.Drawing.Font("Arial", 8F, System.Drawing.FontStyle.Bold);
        AcquisitionModeTitle.Location = new System.Drawing.Point(18, 40);
        AcquisitionModeTitle.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
        AcquisitionModeTitle.Name = "AcquisitionModeTitle";
        AcquisitionModeTitle.Size = new System.Drawing.Size(104, 14);
        AcquisitionModeTitle.TabIndex = 10;
        AcquisitionModeTitle.Text = "Acquisition mode";
        // 
        // RecordButton
        // 
        RecordButton.Enabled = false;
        RecordButton.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Bold);
        RecordButton.Location = new System.Drawing.Point(63, 455);
        RecordButton.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
        RecordButton.Name = "RecordButton";
        RecordButton.Size = new System.Drawing.Size(71, 30);
        RecordButton.TabIndex = 2;
        RecordButton.Text = "REC";
        RecordButton.UseVisualStyleBackColor = true;
        RecordButton.Click += RecordButton_Click;
        // 
        // StopButton
        // 
        StopButton.Enabled = false;
        StopButton.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Bold);
        StopButton.Location = new System.Drawing.Point(221, 455);
        StopButton.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
        StopButton.Name = "StopButton";
        StopButton.Size = new System.Drawing.Size(71, 30);
        StopButton.TabIndex = 1;
        StopButton.Text = "STOP";
        StopButton.UseVisualStyleBackColor = true;
        StopButton.Click += StopButton_Click;
        // 
        // PlayButton
        // 
        PlayButton.Enabled = false;
        PlayButton.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Bold);
        PlayButton.Location = new System.Drawing.Point(142, 455);
        PlayButton.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
        PlayButton.Name = "PlayButton";
        PlayButton.Size = new System.Drawing.Size(71, 30);
        PlayButton.TabIndex = 0;
        PlayButton.Text = "PLAY";
        PlayButton.UseVisualStyleBackColor = true;
        PlayButton.Click += PlayButton_Click;
        // 
        // ConnectButton
        // 
        ConnectButton.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Bold);
        ConnectButton.Location = new System.Drawing.Point(74, 73);
        ConnectButton.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
        ConnectButton.Name = "ConnectButton";
        ConnectButton.Size = new System.Drawing.Size(100, 28);
        ConnectButton.TabIndex = 18;
        ConnectButton.Text = "Connect";
        ConnectButton.UseVisualStyleBackColor = true;
        ConnectButton.Click += ConnectButton_Click;
        // 
        // DisplayPanel
        // 
        DisplayPanel.Controls.Add(ExitPlaybackButton);
        DisplayPanel.Controls.Add(PlayBackControl);
        DisplayPanel.Controls.Add(DisplayControl);
        DisplayPanel.Controls.Add(StatusControl);
        DisplayPanel.Location = new System.Drawing.Point(356, 38);
        DisplayPanel.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
        DisplayPanel.Name = "DisplayPanel";
        DisplayPanel.Size = new System.Drawing.Size(646, 604);
        DisplayPanel.TabIndex = 23;
        // 
        // ExitPlaybackButton
        // 
        ExitPlaybackButton.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
        ExitPlaybackButton.Location = new System.Drawing.Point(274, 562);
        ExitPlaybackButton.Name = "ExitPlaybackButton";
        ExitPlaybackButton.Size = new System.Drawing.Size(99, 23);
        ExitPlaybackButton.TabIndex = 8;
        ExitPlaybackButton.Text = "EXIT Playback";
        ExitPlaybackButton.UseVisualStyleBackColor = true;
        ExitPlaybackButton.Visible = false;
        ExitPlaybackButton.Click += ExitPlaybackButton_Click;
        // 
        // PlayBackControl
        // 
        PlayBackControl.BackColor = System.Drawing.Color.Transparent;
        PlayBackControl.Location = new System.Drawing.Point(3, 488);
        PlayBackControl.Name = "PlayBackControl";
        PlayBackControl.Size = new System.Drawing.Size(640, 65);
        PlayBackControl.TabIndex = 6;
        // 
        // DisplayControl
        // 
        DisplayControl.Location = new System.Drawing.Point(3, 2);
        DisplayControl.Name = "DisplayControl";
        DisplayControl.ShowFPS = false;
        DisplayControl.ShowFrameID = false;
        DisplayControl.ShowTimeStamp = false;
        DisplayControl.Size = new System.Drawing.Size(640, 480);
        DisplayControl.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage;
        DisplayControl.TabIndex = 2;
        DisplayControl.TabStop = false;
        DisplayControl.TextOverlayColor = System.Drawing.Color.Black;
        DisplayControl.TextOverlayFont = Emgu.CV.CvEnum.FontFace.HersheyPlain;
        // 
        // StatusControl
        // 
        StatusControl.DataStream = null;
        StatusControl.DisplayThread = null;
        StatusControl.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
        StatusControl.ImageWriter = null;
        StatusControl.Location = new System.Drawing.Point(8, 495);
        StatusControl.Name = "StatusControl";
        StatusControl.Size = new System.Drawing.Size(630, 72);
        StatusControl.TabIndex = 7;
        // 
        // MainMenu
        // 
        MainMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { FileMenuItem, ViewMenuItem, DisplayMenuItem, HelpMenuItem });
        MainMenu.Location = new System.Drawing.Point(0, 0);
        MainMenu.Name = "MainMenu";
        MainMenu.Size = new System.Drawing.Size(1384, 24);
        MainMenu.TabIndex = 29;
        MainMenu.Text = "Main Menu";
        // 
        // FileMenuItem
        // 
        FileMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { LoadConfigurationMenuItem, SaveConfigurationMenuItem, FileMenuSeparator1, OpenImagesMenuItem, CloseImagesMenuItem, FileMenuSeparator2, ExitMenuItem });
        FileMenuItem.Name = "FileMenuItem";
        FileMenuItem.Size = new System.Drawing.Size(37, 20);
        FileMenuItem.Text = "File";
        // 
        // LoadConfigurationMenuItem
        // 
        LoadConfigurationMenuItem.Name = "LoadConfigurationMenuItem";
        LoadConfigurationMenuItem.Size = new System.Drawing.Size(221, 22);
        LoadConfigurationMenuItem.Text = "Load device configuration...";
        LoadConfigurationMenuItem.Click += LoadConfigurationStripMenuItem_Click;
        // 
        // SaveConfigurationMenuItem
        // 
        SaveConfigurationMenuItem.Name = "SaveConfigurationMenuItem";
        SaveConfigurationMenuItem.Size = new System.Drawing.Size(221, 22);
        SaveConfigurationMenuItem.Text = "Save device configuration...";
        SaveConfigurationMenuItem.Click += SaveConfigurationStripMenuItem_Click;
        // 
        // FileMenuSeparator1
        // 
        FileMenuSeparator1.Name = "FileMenuSeparator1";
        FileMenuSeparator1.Size = new System.Drawing.Size(218, 6);
        // 
        // OpenImagesMenuItem
        // 
        OpenImagesMenuItem.Name = "OpenImagesMenuItem";
        OpenImagesMenuItem.Size = new System.Drawing.Size(221, 22);
        OpenImagesMenuItem.Text = "Open Image Sequence...";
        OpenImagesMenuItem.Click += OpenImagesStripMenuItem_Click;
        // 
        // CloseImagesMenuItem
        // 
        CloseImagesMenuItem.Name = "CloseImagesMenuItem";
        CloseImagesMenuItem.Size = new System.Drawing.Size(221, 22);
        CloseImagesMenuItem.Text = "Close Image Sequence";
        CloseImagesMenuItem.Click += CloseImagesStripMenuItem_Click;
        // 
        // FileMenuSeparator2
        // 
        FileMenuSeparator2.Name = "FileMenuSeparator2";
        FileMenuSeparator2.Size = new System.Drawing.Size(218, 6);
        // 
        // ExitMenuItem
        // 
        ExitMenuItem.Name = "ExitMenuItem";
        ExitMenuItem.Size = new System.Drawing.Size(221, 22);
        ExitMenuItem.Text = "Exit";
        ExitMenuItem.Click += ExitButton_Click;
        // 
        // ViewMenuItem
        // 
        ViewMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { ShowStatusControlMenuItem });
        ViewMenuItem.Name = "ViewMenuItem";
        ViewMenuItem.Size = new System.Drawing.Size(44, 20);
        ViewMenuItem.Text = "View";
        // 
        // ShowStatusControlMenuItem
        // 
        ShowStatusControlMenuItem.Checked = true;
        ShowStatusControlMenuItem.CheckOnClick = true;
        ShowStatusControlMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
        ShowStatusControlMenuItem.Name = "ShowStatusControlMenuItem";
        ShowStatusControlMenuItem.Size = new System.Drawing.Size(146, 22);
        ShowStatusControlMenuItem.Text = "StatusControl";
        ShowStatusControlMenuItem.CheckedChanged += ShowDataStreamStats_CheckedChanged;
        // 
        // DisplayMenuItem
        // 
        DisplayMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { DisplayFPSCheckBox, DisplayFrameCounterCheckBox, DisplayTimeStampCheckBox, DisplayAllCheckBox, DisplayMenuSeparator1, LimitFPSCheckBox });
        DisplayMenuItem.Name = "DisplayMenuItem";
        DisplayMenuItem.Size = new System.Drawing.Size(57, 20);
        DisplayMenuItem.Text = "Display";
        // 
        // DisplayFPSCheckBox
        // 
        DisplayFPSCheckBox.CheckOnClick = true;
        DisplayFPSCheckBox.Name = "DisplayFPSCheckBox";
        DisplayFPSCheckBox.Size = new System.Drawing.Size(204, 22);
        DisplayFPSCheckBox.Text = "Show FPS";
        DisplayFPSCheckBox.CheckedChanged += ShowFrameRateMenuItem_CheckChanged;
        // 
        // DisplayFrameCounterCheckBox
        // 
        DisplayFrameCounterCheckBox.CheckOnClick = true;
        DisplayFrameCounterCheckBox.Name = "DisplayFrameCounterCheckBox";
        DisplayFrameCounterCheckBox.Size = new System.Drawing.Size(204, 22);
        DisplayFrameCounterCheckBox.Text = "Show Frame Counter";
        DisplayFrameCounterCheckBox.CheckedChanged += ShowFrameIDMenuItem_CheckChanged;
        // 
        // DisplayTimeStampCheckBox
        // 
        DisplayTimeStampCheckBox.CheckOnClick = true;
        DisplayTimeStampCheckBox.Name = "DisplayTimeStampCheckBox";
        DisplayTimeStampCheckBox.Size = new System.Drawing.Size(204, 22);
        DisplayTimeStampCheckBox.Text = "Show Timestamp";
        DisplayTimeStampCheckBox.CheckedChanged += ShowTimeStampMenuItem_CheckChanged;
        // 
        // DisplayAllCheckBox
        // 
        DisplayAllCheckBox.CheckOnClick = true;
        DisplayAllCheckBox.Name = "DisplayAllCheckBox";
        DisplayAllCheckBox.Size = new System.Drawing.Size(204, 22);
        DisplayAllCheckBox.Text = "Show All";
        DisplayAllCheckBox.CheckedChanged += ShowAllMenuItem_CheckChanged;
        // 
        // DisplayMenuSeparator1
        // 
        DisplayMenuSeparator1.Name = "DisplayMenuSeparator1";
        DisplayMenuSeparator1.Size = new System.Drawing.Size(201, 6);
        // 
        // LimitFPSCheckBox
        // 
        LimitFPSCheckBox.CheckOnClick = true;
        LimitFPSCheckBox.Name = "LimitFPSCheckBox";
        LimitFPSCheckBox.Size = new System.Drawing.Size(204, 22);
        LimitFPSCheckBox.Text = "Limit Display FPS (30 Hz)";
        LimitFPSCheckBox.CheckedChanged += LimitFPSCheckBox_CheckedChanged;
        // 
        // HelpMenuItem
        // 
        HelpMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { AboutMenuItem });
        HelpMenuItem.Name = "HelpMenuItem";
        HelpMenuItem.Size = new System.Drawing.Size(44, 20);
        HelpMenuItem.Text = "Help";
        // 
        // AboutMenuItem
        // 
        AboutMenuItem.Name = "AboutMenuItem";
        AboutMenuItem.Size = new System.Drawing.Size(116, 22);
        AboutMenuItem.Text = "About...";
        AboutMenuItem.Click += AboutStripMenuItem_Click;
        // 
        // OpenFileDialog
        // 
        OpenFileDialog.FileName = "openFileDialog";
        // 
        // DisconnectButton
        // 
        DisconnectButton.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
        DisconnectButton.Location = new System.Drawing.Point(182, 73);
        DisconnectButton.Name = "DisconnectButton";
        DisconnectButton.Size = new System.Drawing.Size(100, 28);
        DisconnectButton.TabIndex = 30;
        DisconnectButton.Text = "Disconnect";
        DisconnectButton.UseVisualStyleBackColor = true;
        DisconnectButton.Click += DisconnectButton_Click;
        // 
        // ParameterGridView
        // 
        ParameterGridView.Location = new System.Drawing.Point(1017, 38);
        ParameterGridView.Name = "ParameterGridView";
        ParameterGridView.Size = new System.Drawing.Size(360, 600);
        ParameterGridView.TabIndex = 31;
        // 
        // WinFormsDemoApp
        // 
        AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        BackColor = System.Drawing.SystemColors.GradientActiveCaption;
        ClientSize = new System.Drawing.Size(1384, 644);
        Controls.Add(ParameterGridView);
        Controls.Add(DisconnectButton);
        Controls.Add(DeviceInfoPanel);
        Controls.Add(ConnectButton);
        Controls.Add(RecordButton);
        Controls.Add(StopButton);
        Controls.Add(PlayButton);
        Controls.Add(AcquisitionPanel);
        Controls.Add(DisplayPanel);
        Controls.Add(MainMenu);
        Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
        MainMenuStrip = MainMenu;
        Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
        Name = "WinFormsDemoApp";
        Text = "WinFormsDemoApp";
        FormClosing += TestGUI_FormClosing;
        DeviceInfoPanel.ResumeLayout(false);
        DeviceInfoPanel.PerformLayout();
        AcquisitionPanel.ResumeLayout(false);
        AcquisitionPanel.PerformLayout();
        DisplayPanel.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)DisplayControl).EndInit();
        MainMenu.ResumeLayout(false);
        MainMenu.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion
    private BorderedGroupBox DeviceInfoPanel;
    private BorderedGroupBox AcquisitionPanel;
    private System.Windows.Forms.Button PlayButton;
    private System.Windows.Forms.Button StopButton;
    private System.Windows.Forms.Button RecordButton;
    private System.Windows.Forms.ComboBox AcquisitionModeComboBox;
    private System.Windows.Forms.Label AcquisitionModeTitle;
    private System.Windows.Forms.TextBox AcquisitionFrameCountTextBox;
    private System.Windows.Forms.Label MultiFrameTitle;
    private System.Windows.Forms.Button ConnectButton;
    private System.Windows.Forms.Label ModelTitle;
    private System.Windows.Forms.Label VendorTitle;
    private System.Windows.Forms.Label SerialTitle;
    private System.Windows.Forms.Label VendorNameLabel;
    private System.Windows.Forms.Label UniqueIDTitle;
    private System.Windows.Forms.Label SerialLabel;
    private System.Windows.Forms.Label ModelLabel;
    private System.Windows.Forms.Label UniqueIDLabel;
    private System.Windows.Forms.Label ClassLabel;
    private System.Windows.Forms.Label ClassTitle;
    private System.Windows.Forms.SaveFileDialog SaveFileDialog;
    private System.Windows.Forms.Button SaveImagesButton;
    private System.Windows.Forms.TextBox SaveImagesTextBox;
    private System.Windows.Forms.Panel DisplayPanel;
    private System.Windows.Forms.MenuStrip MainMenu;
    private System.Windows.Forms.ToolStripMenuItem LoadConfigurationMenuItem;
    private System.Windows.Forms.ToolStripMenuItem SaveConfigurationMenuItem;
    private System.Windows.Forms.ToolStripMenuItem OpenImagesMenuItem;
    private System.Windows.Forms.ToolStripMenuItem CloseImagesMenuItem;
    private System.Windows.Forms.ToolStripMenuItem AboutMenuItem;
    private System.Windows.Forms.OpenFileDialog OpenFileDialog;
    private System.Windows.Forms.Button DisconnectButton;
    private System.Windows.Forms.ToolStripMenuItem ExitMenuItem;
    private GcParameterGridView ParameterGridView;
    private System.Windows.Forms.ToolStripMenuItem ShowStatusControlMenuItem;
    private GcDisplayControl DisplayControl;
    private System.Windows.Forms.ToolStripMenuItem FileMenuItem;
    private System.Windows.Forms.ToolStripMenuItem ViewMenuItem;
    private System.Windows.Forms.ToolStripMenuItem DisplayMenuItem;
    private System.Windows.Forms.ToolStripMenuItem DisplayFPSCheckBox;
    private System.Windows.Forms.ToolStripMenuItem DisplayFrameCounterCheckBox;
    private System.Windows.Forms.ToolStripMenuItem DisplayTimeStampCheckBox;
    private System.Windows.Forms.ToolStripMenuItem HelpMenuItem;
    private System.Windows.Forms.ToolStripSeparator DisplayMenuSeparator1;
    private System.Windows.Forms.ToolStripMenuItem LimitFPSCheckBox;
    private System.Windows.Forms.ToolStripSeparator FileMenuSeparator1;
    private System.Windows.Forms.ToolStripSeparator FileMenuSeparator2;
    private System.Windows.Forms.ToolStripMenuItem DisplayAllCheckBox;
    private GcPlayBackControl PlayBackControl;
    private StatusControl StatusControl;
    private System.Windows.Forms.Button ExitPlaybackButton;
}

