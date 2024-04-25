namespace C_Sharp_Example
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
            this.TextBox1 = new System.Windows.Forms.TextBox();
            this.WebView21 = new Microsoft.Web.WebView2.WinForms.WebView2();
            this.btnDownloadFile2 = new System.Windows.Forms.Button();
            this.Label2 = new System.Windows.Forms.Label();
            this.Button1 = new System.Windows.Forms.Button();
            this.btnStopDownload = new System.Windows.Forms.Button();
            this.btnUpload = new System.Windows.Forms.Button();
            this.Label1 = new System.Windows.Forms.Label();
            this.btnDownloadFile = new System.Windows.Forms.Button();
            this.ProgressBar1 = new System.Windows.Forms.ProgressBar();
            this.postDataExample = new System.Windows.Forms.Button();
            this.btnGetWebPageData = new System.Windows.Forms.Button();
            this.OpenFileDialog = new System.Windows.Forms.OpenFileDialog();
            ((System.ComponentModel.ISupportInitialize)(this.WebView21)).BeginInit();
            this.SuspendLayout();
            // 
            // TextBox1
            // 
            this.TextBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.TextBox1.Location = new System.Drawing.Point(12, 383);
            this.TextBox1.Multiline = true;
            this.TextBox1.Name = "TextBox1";
            this.TextBox1.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.TextBox1.Size = new System.Drawing.Size(804, 186);
            this.TextBox1.TabIndex = 4;
            // 
            // WebView21
            // 
            this.WebView21.AllowExternalDrop = true;
            this.WebView21.CreationProperties = null;
            this.WebView21.DefaultBackgroundColor = System.Drawing.Color.White;
            this.WebView21.Location = new System.Drawing.Point(15, 108);
            this.WebView21.Name = "webView21";
            this.WebView21.Size = new System.Drawing.Size(801, 269);
            this.WebView21.TabIndex = 22;
            this.WebView21.ZoomFactor = 1D;
            // 
            // btnDownloadFile2
            // 
            this.btnDownloadFile2.Location = new System.Drawing.Point(558, 41);
            this.btnDownloadFile2.Name = "btnDownloadFile2";
            this.btnDownloadFile2.Size = new System.Drawing.Size(222, 23);
            this.btnDownloadFile2.TabIndex = 21;
            this.btnDownloadFile2.Text = "Multi-Threaded Download File";
            this.btnDownloadFile2.UseVisualStyleBackColor = true;
            this.btnDownloadFile2.Click += new System.EventHandler(this.btnDownloadFile2_Click);
            // 
            // Label2
            // 
            this.Label2.AutoSize = true;
            this.Label2.Location = new System.Drawing.Point(458, 67);
            this.Label2.Name = "Label2";
            this.Label2.Size = new System.Drawing.Size(39, 13);
            this.Label2.TabIndex = 20;
            this.Label2.Text = "Label2";
            // 
            // Button1
            // 
            this.Button1.Location = new System.Drawing.Point(558, 79);
            this.Button1.Name = "Button1";
            this.Button1.Size = new System.Drawing.Size(222, 23);
            this.Button1.TabIndex = 19;
            this.Button1.Text = "Button1";
            this.Button1.UseVisualStyleBackColor = true;
            // 
            // btnStopDownload
            // 
            this.btnStopDownload.Enabled = false;
            this.btnStopDownload.Location = new System.Drawing.Point(672, 12);
            this.btnStopDownload.Name = "btnStopDownload";
            this.btnStopDownload.Size = new System.Drawing.Size(108, 23);
            this.btnStopDownload.TabIndex = 18;
            this.btnStopDownload.Text = "Stop Download";
            this.btnStopDownload.UseVisualStyleBackColor = true;
            this.btnStopDownload.Click += new System.EventHandler(this.btnStopDownload_Click);
            // 
            // btnUpload
            // 
            this.btnUpload.Location = new System.Drawing.Point(340, 12);
            this.btnUpload.Name = "btnUpload";
            this.btnUpload.Size = new System.Drawing.Size(75, 23);
            this.btnUpload.TabIndex = 17;
            this.btnUpload.Text = "Upload File";
            this.btnUpload.UseVisualStyleBackColor = true;
            this.btnUpload.Click += new System.EventHandler(this.btnUpload_Click);
            // 
            // Label1
            // 
            this.Label1.AutoSize = true;
            this.Label1.Location = new System.Drawing.Point(12, 67);
            this.Label1.Name = "Label1";
            this.Label1.Size = new System.Drawing.Size(39, 13);
            this.Label1.TabIndex = 16;
            this.Label1.Text = "Label1";
            // 
            // btnDownloadFile
            // 
            this.btnDownloadFile.Location = new System.Drawing.Point(558, 12);
            this.btnDownloadFile.Name = "btnDownloadFile";
            this.btnDownloadFile.Size = new System.Drawing.Size(108, 23);
            this.btnDownloadFile.TabIndex = 15;
            this.btnDownloadFile.Text = "Download File";
            this.btnDownloadFile.UseVisualStyleBackColor = true;
            this.btnDownloadFile.Click += new System.EventHandler(this.btnDownloadFile_Click);
            // 
            // ProgressBar1
            // 
            this.ProgressBar1.Location = new System.Drawing.Point(12, 41);
            this.ProgressBar1.Name = "ProgressBar1";
            this.ProgressBar1.Size = new System.Drawing.Size(485, 23);
            this.ProgressBar1.TabIndex = 14;
            // 
            // postDataExample
            // 
            this.postDataExample.Location = new System.Drawing.Point(176, 12);
            this.postDataExample.Name = "postDataExample";
            this.postDataExample.Size = new System.Drawing.Size(158, 23);
            this.postDataExample.TabIndex = 13;
            this.postDataExample.Text = "Post Data to Web Site";
            this.postDataExample.UseVisualStyleBackColor = true;
            this.postDataExample.Click += new System.EventHandler(this.postDataExample_Click);
            // 
            // btnGetWebPageData
            // 
            this.btnGetWebPageData.Location = new System.Drawing.Point(12, 12);
            this.btnGetWebPageData.Name = "btnGetWebPageData";
            this.btnGetWebPageData.Size = new System.Drawing.Size(158, 23);
            this.btnGetWebPageData.TabIndex = 12;
            this.btnGetWebPageData.Text = "Get Web Page Data";
            this.btnGetWebPageData.UseVisualStyleBackColor = true;
            this.btnGetWebPageData.Click += new System.EventHandler(this.btnGetWebPageData_Click);
            // 
            // OpenFileDialog
            // 
            this.OpenFileDialog.FileName = "OpenFileDialog1";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(828, 581);
            this.Controls.Add(this.btnDownloadFile2);
            this.Controls.Add(this.Label2);
            this.Controls.Add(this.Button1);
            this.Controls.Add(this.btnStopDownload);
            this.Controls.Add(this.btnUpload);
            this.Controls.Add(this.Label1);
            this.Controls.Add(this.btnDownloadFile);
            this.Controls.Add(this.ProgressBar1);
            this.Controls.Add(this.postDataExample);
            this.Controls.Add(this.btnGetWebPageData);
            this.Controls.Add(this.WebView21);
            this.Controls.Add(this.TextBox1);
            this.Name = "Form1";
            this.Text = "Form1";
            this.Load += new System.EventHandler(this.Form1_Load);
            ((System.ComponentModel.ISupportInitialize)(this.WebView21)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        internal System.Windows.Forms.TextBox TextBox1;
        private Microsoft.Web.WebView2.WinForms.WebView2 WebView21;
        internal System.Windows.Forms.Button btnDownloadFile2;
        internal System.Windows.Forms.Label Label2;
        internal System.Windows.Forms.Button Button1;
        internal System.Windows.Forms.Button btnStopDownload;
        internal System.Windows.Forms.Button btnUpload;
        internal System.Windows.Forms.Label Label1;
        internal System.Windows.Forms.Button btnDownloadFile;
        internal System.Windows.Forms.ProgressBar ProgressBar1;
        internal System.Windows.Forms.Button postDataExample;
        internal System.Windows.Forms.Button btnGetWebPageData;
        internal System.Windows.Forms.OpenFileDialog OpenFileDialog;
    }
}

