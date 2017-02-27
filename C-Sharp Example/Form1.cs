using Microsoft.VisualBasic;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Windows.Forms;

namespace C_Sharp_Example
{
    public partial class Form1 : Form
    {
        delegate void myCustomErrorHandlerDelegate(Exception ex, httpHelper classInstance);
        delegate void myDownloadStatusRoutine(downloadStatusDetails downloadStatusDetails);
        System.Threading.Thread downloadThread;
        System.Threading.Thread statusThread;
        ulong oldFileSize = 0;
        string urlToDownload = "http://releases.ubuntu.com/16.04.2/ubuntu-16.04.2-desktop-amd64.iso";
        string localFilePathToDownloadFileTo = "S:\\ubuntu-16.04.2-desktop-amd64.iso";

        public Form1()
        {
            InitializeComponent();
        }

        private void btnGetWebPageData_Click(object sender, EventArgs e)
        {
            try {
                string strServerResponse = null;

                httpHelper httpHelper = new httpHelper();
                httpHelper.setUserAgent = "Microsoft .NET"; // Set our User Agent String.
                httpHelper.addGETData("test3", "value3");
                httpHelper.addHTTPCookie("mycookie", "my cookie contents", "www.toms-world.org", "/");
                httpHelper.addHTTPHeader("myheader", "my header contents");
                httpHelper.setHTTPCredentials("test", "test");

                // This sets up our download status updating delegate to be injected like a plugin into the HTTPHelper class instance.
                myCustomErrorHandlerDelegate setCustomErrorHandler = (Exception ex, httpHelper classInstance) => { Interaction.MsgBox(ex.Message); };
                    httpHelper.setCustomErrorHandler = setCustomErrorHandler;
                // This sets up our download status updating delegate to be injected like a plugin into the HTTPHelper class instance.

                if (httpHelper.getWebData("https://www.toms-world.org/php/phpinfo.php", ref strServerResponse)) {
                    WebBrowser1.DocumentText = strServerResponse;
                    TextBox1.Text = httpHelper.getHTTPResponseHeaders(true).ToString();

                    X509Certificate2 certDetails = httpHelper.getCertificateDetails(false);
                    if (certDetails != null) {
                        TextBox1.Text += certDetails.ToString();
                    }
                }
            }
            catch (httpProtocolException ex) {
                // You can handle httpProtocolExceptions different than normal exceptions with this code.
            }
            catch (System.Net.WebException ex) {
                // You can handle web exceptions different than normal exceptions with this code.
            }
            catch (Exception ex) {
                Interaction.MsgBox(ex.Message + " " + ex.StackTrace);
            }
        }

        private void postDataExample_Click(object sender, EventArgs e)
        {
            try
            {
                string strServerResponse = null;

                httpHelper httpHelper = new httpHelper();
                httpHelper.setUserAgent = "Microsoft .NET"; // Set our User Agent String.
                httpHelper.addHTTPCookie("mycookie", "my cookie contents", "www.toms-world.org", "/");
                httpHelper.addHTTPHeader("myheader", "my header contents");
                httpHelper.addPOSTData("test1", "value1");
                httpHelper.addPOSTData("test2", "value2");
                httpHelper.addGETData("test3", "value3");
                httpHelper.addPOSTData("major", "3");
                httpHelper.addPOSTData("minor", "9");
                httpHelper.addPOSTData("build", "6");

                if (httpHelper.getWebData("https://www.toms-world.org/httphelper.php", ref strServerResponse))
                {
                    WebBrowser1.DocumentText = strServerResponse;
                    TextBox1.Text = httpHelper.getHTTPResponseHeaders().ToString();

                    X509Certificate2 certDetails = httpHelper.getCertificateDetails(false);
                    if (certDetails != null)
                    {
                        TextBox1.Text += certDetails.ToString();
                    }

                    //For Each strHeaderName As String In httpHelper.getHTTPResponseHeaders
                    //    MsgBox(strHeaderName & " = " & httpHelper.getHTTPResponseHeaders.Item(strHeaderName))
                    //Next
                }
            }
            catch (httpProtocolException ex)
            {
                // You can handle httpProtocolExceptions different than normal exceptions with this code.
            }
            catch (System.Net.WebException ex)
            {
                // You can handle web exceptions different than normal exceptions with this code.
            }
            catch (Exception ex)
            {
                Interaction.MsgBox(ex.Message + " " + ex.StackTrace);
            }
        }

        private void btnUpload_Click(object sender, EventArgs e)
        {
            try
            {
                OpenFileDialog.Title = "Browse for file to upload...";
                OpenFileDialog.FileName = null;
                OpenFileDialog.Filter = "Image Files (JPEG, PNG)|*.png;*.jpg;*.jpeg";

                if (OpenFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string strServerResponse = null;

                    httpHelper httpHelper = new httpHelper();
                    httpHelper.setHTTPTimeout = 10;
                    httpHelper.setUserAgent = "Microsoft .NET"; // Set our User Agent String.
                    httpHelper.addHTTPCookie("mycookie", "my cookie contents", "www.toms-world.org", "/");
                    httpHelper.addHTTPHeader("myheader", "my header contents");
                    httpHelper.addPOSTData("test1", "value1");
                    httpHelper.addPOSTData("test2", "value2");
                    httpHelper.addGETData("test3", "value3");
                    httpHelper.addFileUpload("myfileupload", OpenFileDialog.FileName, null, null);

                    if (httpHelper.uploadData("https://www.toms-world.org/httphelper.php", ref strServerResponse))
                    {
                        WebBrowser1.DocumentText = strServerResponse;
                        TextBox1.Text = httpHelper.getHTTPResponseHeaders().ToString();

                        X509Certificate2 certDetails = httpHelper.getCertificateDetails(false);
                        if (certDetails != null)
                        {
                            TextBox1.Text += certDetails.ToString();
                        }
                    }
                }
            }
            catch (System.Net.WebException ex)
            {
                Interaction.MsgBox(ex.Message + " " + ex.StackTrace);
            }
        }

        private void btnStopDownload_Click(object sender, EventArgs e)
        {
            downloadThread.Abort();
            if (statusThread != null) { statusThread.Abort(); }
        }

        private void btnDownloadFile_Click(object sender, EventArgs e)
        {
            // First we create our httpHelper Class instance.
            {
                httpHelper httpHelper = new httpHelper();
                httpHelper.setUserAgent = "Microsoft .NET"; // Set our User Agent String.
                httpHelper.enableMultiThreadedDownloadStatusUpdates = true;
                ulong oldFileSize = 0;
                
                myDownloadStatusRoutine myDownloadStatusUpdater = (downloadStatusDetails downloadStatusDetails) => {
                    if (httpHelper.enableMultiThreadedDownloadStatusUpdates) {
                        Label1.Text = string.Format("Downloaded {0} of {1} ({2}/s)", httpHelper.fileSizeToHumanReadableFormat(downloadStatusDetails.localFileSize), httpHelper.fileSizeToHumanReadableFormat(downloadStatusDetails.remoteFileSize), httpHelper.fileSizeToHumanReadableFormat(downloadStatusDetails.localFileSize - oldFileSize));
                        oldFileSize = (ulong)downloadStatusDetails.localFileSize;
                    }
                    else {
                        Label1.Text = string.Format("Downloaded {0} of {1}", httpHelper.fileSizeToHumanReadableFormat(downloadStatusDetails.localFileSize), httpHelper.fileSizeToHumanReadableFormat(downloadStatusDetails.remoteFileSize));
                    }

                    Label2.Text = downloadStatusDetails.percentageDownloaded.ToString() + "%";
                    ProgressBar1.Value = downloadStatusDetails.percentageDownloaded;
                };
                httpHelper.setDownloadStatusUpdateRoutine = myDownloadStatusUpdater;

                // Now we need to create our download thread.
                downloadThread = new System.Threading.Thread(() => {
                    string urlToFileToBeDownloaded = urlToDownload;
                    string pathToDownloadFileTo = localFilePathToDownloadFileTo;
                    System.IO.MemoryStream memStream = new System.IO.MemoryStream();

                    try {
                        btnStopDownload.Enabled = true;
                        btnDownloadFile.Enabled = false;
                        btnDownloadFile2.Enabled = false;

                        // We use the downloadFile() function which first calls for the URL and then the path to a place on the local file system to save it. This function is why we need multithreading, this will take a long time to do.
                        if (httpHelper.downloadFile(urlToFileToBeDownloaded, ref memStream, true)) {
                            System.IO.FileStream fileStream = new System.IO.FileStream(pathToDownloadFileTo, System.IO.FileMode.Create);
                            memStream.CopyTo(fileStream);
                            memStream.Close();
                            memStream.Dispose();
                            fileStream.Close();
                            fileStream.Dispose();

                            btnDownloadFile.Enabled = true;
                            btnStopDownload.Enabled = false;
                            Interaction.MsgBox("Download complete.");
                            // And tell the user that the download is complete.
                        }

                    }
                    catch (System.Net.WebException ex) {
                        btnDownloadFile.Enabled = true;
                        btnDownloadFile2.Enabled = true;
                        btnStopDownload.Enabled = false;
                        Interaction.MsgBox(ex.Message + " " + ex.StackTrace);

                    }
                    catch (System.Threading.ThreadAbortException ex) {
                        btnDownloadFile.Enabled = true;
                        btnDownloadFile2.Enabled = true;
                        btnStopDownload.Enabled = false;

                        if (System.IO.File.Exists(pathToDownloadFileTo)) System.IO.File.Delete(pathToDownloadFileTo);
                        Interaction.MsgBox("Download aborted.");
                        // And tell the user that the download is aborted.
                    }
                });

                // Starts our download thread.
                downloadThread.IsBackground = true;
                downloadThread.Start();
            }
        }

        private void btnDownloadFile2_Click(object sender, EventArgs e)
        {
            // First we create our httpHelper Class instance.
            {
                httpHelper httpHelper = new httpHelper();
                httpHelper.setUserAgent = "Microsoft .NET"; // Set our User Agent String.

                // Now we need to create our download thread.
                downloadThread = new System.Threading.Thread(() => {
                    string urlToFileToBeDownloaded = urlToDownload;
                    string pathToDownloadFileTo = localFilePathToDownloadFileTo;
                    System.IO.MemoryStream memStream = new System.IO.MemoryStream();

                    try {
                        btnStopDownload.Enabled = true;
                        btnDownloadFile.Enabled = false;
                        btnDownloadFile2.Enabled = false;

                        // We use the downloadFile() function which first calls for the URL and then the path to a place on the local file system to save it. This function is why we need multithreading, this will take a long time to do.
                        if (httpHelper.downloadFile(urlToFileToBeDownloaded, ref memStream, true)) {
                            System.IO.FileStream fileStream = new System.IO.FileStream(pathToDownloadFileTo, System.IO.FileMode.Create);
                            memStream.CopyTo(fileStream);
                            memStream.Close();
                            memStream.Dispose();
                            fileStream.Close();
                            fileStream.Dispose();

                            btnDownloadFile.Enabled = true;
                            btnStopDownload.Enabled = false;
                            Interaction.MsgBox("Download complete.");
                            // And tell the user that the download is complete.
                        }

                    }
                    catch (System.Net.WebException ex) {
                        btnDownloadFile.Enabled = true;
                        btnDownloadFile2.Enabled = true;
                        btnStopDownload.Enabled = false;
                        Interaction.MsgBox(ex.Message + " " + ex.StackTrace);

                    }
                    catch (System.Threading.ThreadAbortException ex) {
                        btnDownloadFile.Enabled = true;
                        btnDownloadFile2.Enabled = true;
                        btnStopDownload.Enabled = false;
                        if (System.IO.File.Exists(pathToDownloadFileTo)) System.IO.File.Delete(pathToDownloadFileTo);
                        Interaction.MsgBox("Download aborted.");
                        // And tell the user that the download is aborted.
                    }
                });

                downloadThread.IsBackground = true;
                downloadThread.Start();
                // Starts our download thread.

                statusThread = new System.Threading.Thread(() => {
                    downloadStatusDetails downloadStatusDetails;
                    startAgain:
                    downloadStatusDetails = httpHelper.getDownloadStatusDetails;

                    if (downloadStatusDetails != null) {
                        Label1.Text = string.Format("Downloaded {0} of {1} ({2}/s)", httpHelper.fileSizeToHumanReadableFormat(downloadStatusDetails.localFileSize), httpHelper.fileSizeToHumanReadableFormat(downloadStatusDetails.remoteFileSize), httpHelper.fileSizeToHumanReadableFormat(downloadStatusDetails.localFileSize - oldFileSize));

                        oldFileSize = (ulong)downloadStatusDetails.localFileSize;

                        Label2.Text = downloadStatusDetails.percentageDownloaded.ToString() + "%";
                        ProgressBar1.Value = downloadStatusDetails.percentageDownloaded;
                    }
                
                    System.Threading.Thread.Sleep(1000);
                    goto startAgain;
                });

                statusThread.IsBackground = true;
                statusThread.Start();
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Control.CheckForIllegalCrossThreadCalls = false;
        }
    }
}
