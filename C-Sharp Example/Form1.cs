using System;
using System.Security.Cryptography.X509Certificates;
using System.Windows.Forms;

namespace C_Sharp_Example
{
    public partial class Form1 : Form {
        // These are the delegates that we will be passing to the HTTPHelper class instance.
        delegate void myCustomErrorHandlerDelegate(Exception ex, HTTPHelper classInstance);
        delegate void myDownloadStatusRoutine(DownloadStatusDetails downloadStatusDetails);

        System.Threading.Thread downloadThread;
        System.Threading.Thread statusThread;
        long oldFileSize = 0;
        readonly string urlToDownload = "http://releases.ubuntu.com/16.04.2/ubuntu-16.04.2-desktop-amd64.iso";
        readonly string localFilePathToDownloadFileTo = "S:\\ubuntu-16.04.2-desktop-amd64.iso";

        public Form1() { InitializeComponent(); }

        private void btnGetWebPageData_Click(object sender, EventArgs e) {
            try {
                string strServerResponse = null;

                HTTPHelper HTTPHelper = new HTTPHelper() { SetUserAgent = "Microsoft .NET" }; // Set our User Agent String.
                HTTPHelper.AddGETData("test3", "value3");
                HTTPHelper.AddHTTPCookie("mycookie", "my cookie contents", "www.toms-world.org", "/");
                HTTPHelper.AddHTTPHeader("myheader", "my header contents");
                HTTPHelper.SetHTTPCredentials("test", "test");
                HTTPHelper.SetURLPreProcessor = (string strURLInput) => { System.Diagnostics.Debug.WriteLine("strURLInput = " + strURLInput); return strURLInput; };

                // This sets up our download status updating delegate to be injected like a plugin into the HTTPHelper class instance.
                myCustomErrorHandlerDelegate setCustomErrorHandler = (Exception ex, HTTPHelper classInstance) => { MessageBox.Show(ex.Message); };
                HTTPHelper.SetCustomErrorHandler = setCustomErrorHandler;
                // This sets up our download status updating delegate to be injected like a plugin into the HTTPHelper class instance.

                if (HTTPHelper.GetWebData("https://www.toms-world.org/php/phpinfo.php", ref strServerResponse)) {
                    WebBrowser1.DocumentText = strServerResponse;
                    TextBox1.Text = HTTPHelper.GetHTTPResponseHeaders(true).ToString();

                    X509Certificate2 certDetails = HTTPHelper.GetCertificateDetails(false);
                    if (certDetails != null) {
                        TextBox1.Text += certDetails.ToString();
                    }
                }
            }
            catch (HTTPProtocolException) {
                // You can handle httpProtocolExceptions different than normal exceptions with this code.
            }
            catch (System.Net.WebException) {
                // You can handle web exceptions different than normal exceptions with this code.
            }
            catch (Exception ex) { MessageBox.Show(ex.Message + " " + ex.StackTrace); }
        }

        private void postDataExample_Click(object sender, EventArgs e) {
            try
            {
                string strServerResponse = null;

                HTTPHelper HTTPHelper = new HTTPHelper() { SetUserAgent = "Microsoft .NET" }; // Set our User Agent String.
                HTTPHelper.AddHTTPCookie("mycookie", "my cookie contents", "www.toms-world.org", "/");
                HTTPHelper.AddHTTPHeader("myheader", "my header contents");
                HTTPHelper.AddPOSTData("test1", "value1");
                HTTPHelper.AddPOSTData("test2", "value2");
                HTTPHelper.AddGETData("test3", "value3");
                HTTPHelper.AddPOSTData("major", "3");
                HTTPHelper.AddPOSTData("minor", "9");
                HTTPHelper.AddPOSTData("build", "6");
                HTTPHelper.SetURLPreProcessor = (string strURLInput) => { System.Diagnostics.Debug.WriteLine("strURLInput = " + strURLInput); return strURLInput; };

                if (HTTPHelper.GetWebData("https://www.toms-world.org/httphelper.php", ref strServerResponse))
                {
                    WebBrowser1.DocumentText = strServerResponse;
                    TextBox1.Text = HTTPHelper.GetHTTPResponseHeaders().ToString();

                    X509Certificate2 certDetails = HTTPHelper.GetCertificateDetails(false);
                    if (certDetails != null) TextBox1.Text += certDetails.ToString();

                    //For Each strHeaderName As String In httpHelper.getHTTPResponseHeaders
                    //    MsgBox(strHeaderName & " = " & httpHelper.getHTTPResponseHeaders.Item(strHeaderName))
                    //Next
                }
            }
            catch (HTTPProtocolException)
            {
                // You can handle httpProtocolExceptions different than normal exceptions with this code.
            }
            catch (System.Net.WebException)
            {
                // You can handle web exceptions different than normal exceptions with this code.
            }
            catch (Exception ex) { MessageBox.Show(ex.Message + " " + ex.StackTrace); }
        }

        private void btnUpload_Click(object sender, EventArgs e) {
            try {
                OpenFileDialog.Title = "Browse for file to upload...";
                OpenFileDialog.FileName = null;
                OpenFileDialog.Filter = "Image Files (JPEG, PNG)|*.png;*.jpg;*.jpeg";

                if (OpenFileDialog.ShowDialog() == DialogResult.OK) {
                    string strServerResponse = null;

                    HTTPHelper HTTPHelper = new HTTPHelper() {
                        SetHTTPTimeout = 10,
                        SetUserAgent = "Microsoft .NET" // Set our User Agent String.
                    };
                    HTTPHelper.AddHTTPCookie("mycookie", "my cookie contents", "www.toms-world.org", "/");
                    HTTPHelper.AddHTTPHeader("myheader", "my header contents");
                    HTTPHelper.AddPOSTData("test1", "value1");
                    HTTPHelper.AddPOSTData("test2", "value2");
                    HTTPHelper.AddGETData("test3", "value3");
                    HTTPHelper.AddFileUpload("myfileupload", OpenFileDialog.FileName, null, null);

                    if (HTTPHelper.UploadData("https://www.toms-world.org/httphelper.php", ref strServerResponse)) {
                        WebBrowser1.DocumentText = strServerResponse;
                        TextBox1.Text = HTTPHelper.GetHTTPResponseHeaders().ToString();

                        X509Certificate2 certDetails = HTTPHelper.GetCertificateDetails(false);
                        if (certDetails != null) TextBox1.Text += certDetails.ToString();
                    }
                }
            }
            catch (System.Net.WebException ex) { MessageBox.Show(ex.Message + " " + ex.StackTrace); }
        }

        private void btnStopDownload_Click(object sender, EventArgs e) {
            downloadThread.Abort();
            if (statusThread != null) { statusThread.Abort(); }
        }

        private void btnDownloadFile_Click(object sender, EventArgs e) {
            // First we create our httpHelper Class instance.
            {
                HTTPHelper HTTPHelper = new HTTPHelper() {
                    SetUserAgent = "Microsoft .NET", // Set our User Agent String.
                    EnableMultiThreadedDownloadStatusUpdates = true
                };
                long oldFileSize = 0;
                
                // First we create our delegate.
                myDownloadStatusRoutine myDownloadStatusUpdater = (DownloadStatusDetails downloadStatusDetails) => {
                    if (HTTPHelper.EnableMultiThreadedDownloadStatusUpdates) {
                        Label1.Text = string.Format("Downloaded {0} of {1} ({2}/s)", HTTPHelper.FileSizeToHumanReadableFormat(downloadStatusDetails.localFileSize), HTTPHelper.FileSizeToHumanReadableFormat(downloadStatusDetails.remoteFileSize), HTTPHelper.FileSizeToHumanReadableFormat(downloadStatusDetails.localFileSize - oldFileSize));
                        oldFileSize = downloadStatusDetails.localFileSize;
                    }
                    else {
                        Label1.Text = string.Format("Downloaded {0} of {1}", HTTPHelper.FileSizeToHumanReadableFormat(downloadStatusDetails.localFileSize), HTTPHelper.FileSizeToHumanReadableFormat(downloadStatusDetails.remoteFileSize));
                    }

                    Label2.Text = downloadStatusDetails.percentageDownloaded.ToString() + "%";
                    ProgressBar1.Value = downloadStatusDetails.percentageDownloaded;
                };
                HTTPHelper.SetDownloadStatusUpdateRoutine = myDownloadStatusUpdater; // And now we pass our delegate to the HTTPHelper class instance.

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
                        if (HTTPHelper.DownloadFile(urlToFileToBeDownloaded, ref memStream, true)) {
                            System.IO.FileStream fileStream = new System.IO.FileStream(pathToDownloadFileTo, System.IO.FileMode.Create);
                            memStream.CopyTo(fileStream);
                            memStream.Close();
                            memStream.Dispose();
                            fileStream.Close();
                            fileStream.Dispose();

                            btnDownloadFile.Enabled = true;
                            btnStopDownload.Enabled = false;
                            MessageBox.Show("Download complete.");
                            // And tell the user that the download is complete.
                        }

                    }
                    catch (System.Net.WebException ex) {
                        btnDownloadFile.Enabled = true;
                        btnDownloadFile2.Enabled = true;
                        btnStopDownload.Enabled = false;
                        MessageBox.Show(ex.Message + " " + ex.StackTrace);

                    }
                    catch (System.Threading.ThreadAbortException) {
                        btnDownloadFile.Enabled = true;
                        btnDownloadFile2.Enabled = true;
                        btnStopDownload.Enabled = false;

                        if (System.IO.File.Exists(pathToDownloadFileTo)) System.IO.File.Delete(pathToDownloadFileTo);
                        MessageBox.Show("Download aborted.");
                        // And tell the user that the download is aborted.
                    }
                })
                {
                    // Starts our download thread.
                    IsBackground = true
                };
                downloadThread.Start();
            }
        }

        private void btnDownloadFile2_Click(object sender, EventArgs e) {
            // First we create our httpHelper Class instance.
            HTTPHelper HTTPHelper = new HTTPHelper() { SetUserAgent = "Microsoft .NET" }; // Set our User Agent String.

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
                    if (HTTPHelper.DownloadFile(urlToFileToBeDownloaded, ref memStream, true)) {
                        System.IO.FileStream fileStream = new System.IO.FileStream(pathToDownloadFileTo, System.IO.FileMode.Create);
                        memStream.CopyTo(fileStream);
                        memStream.Close();
                        memStream.Dispose();
                        fileStream.Close();
                        fileStream.Dispose();

                        btnDownloadFile.Enabled = true;
                        btnStopDownload.Enabled = false;
                        MessageBox.Show("Download complete.");
                        // And tell the user that the download is complete.
                    }
                }
                catch (System.Net.WebException ex) {
                    btnDownloadFile.Enabled = true;
                    btnDownloadFile2.Enabled = true;
                    btnStopDownload.Enabled = false;
                    MessageBox.Show(ex.Message + " " + ex.StackTrace);
                }
                catch (System.Threading.ThreadAbortException) {
                    btnDownloadFile.Enabled = true;
                    btnDownloadFile2.Enabled = true;
                    btnStopDownload.Enabled = false;
                    if (System.IO.File.Exists(pathToDownloadFileTo)) System.IO.File.Delete(pathToDownloadFileTo);
                    MessageBox.Show("Download aborted.");
                    // And tell the user that the download is aborted.
                }
            }) { IsBackground = true };

            downloadThread.Start();
            // Starts our download thread.

            statusThread = new System.Threading.Thread(() => {
                DownloadStatusDetails downloadStatusDetails;
                startAgain:
                downloadStatusDetails = HTTPHelper.GetDownloadStatusDetails;

                if (downloadStatusDetails != null) {
                    Label1.Text = string.Format("Downloaded {0} of {1} ({2}/s)", HTTPHelper.FileSizeToHumanReadableFormat(downloadStatusDetails.localFileSize), HTTPHelper.FileSizeToHumanReadableFormat(downloadStatusDetails.remoteFileSize), HTTPHelper.FileSizeToHumanReadableFormat(downloadStatusDetails.localFileSize - oldFileSize));

                    oldFileSize = downloadStatusDetails.localFileSize;

                    Label2.Text = downloadStatusDetails.percentageDownloaded.ToString() + "%";
                    ProgressBar1.Value = downloadStatusDetails.percentageDownloaded;
                }

                System.Threading.Thread.Sleep(1000);
                goto startAgain;
            }) { IsBackground = true };

            statusThread.Start();
        }

        private void Form1_Load(object sender, EventArgs e) { Control.CheckForIllegalCrossThreadCalls = false; }
    }
}