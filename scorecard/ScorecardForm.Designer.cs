﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

partial class ScorecardForm
{
    private void InitializeComponent()
    {
            this.webView2 = new Microsoft.Web.WebView2.WinForms.WebView2();
            ((System.ComponentModel.ISupportInitialize)(this.webView2)).BeginInit();
            this.SuspendLayout();
            // 
            // webView2
            // 
            this.webView2.AllowExternalDrop = true;
            this.webView2.CreationProperties = null;
            this.webView2.DefaultBackgroundColor = System.Drawing.Color.White;
            this.webView2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.webView2.Location = new System.Drawing.Point(0, 0);
            this.webView2.Name = "webView2";
            this.webView2.Size = new System.Drawing.Size(1216, 604);
            this.webView2.TabIndex = 0;
            this.webView2.ZoomFactor = 1D;
            // 
            // ScorecardForm
            // 
            this.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.ClientSize = new System.Drawing.Size(1216, 604);
            this.Controls.Add(this.webView2);
            this.Name = "ScorecardForm";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            ((System.ComponentModel.ISupportInitialize)(this.webView2)).EndInit();
            this.ResumeLayout(false);

    }

    private Microsoft.Web.WebView2.WinForms.WebView2 webView2;
}