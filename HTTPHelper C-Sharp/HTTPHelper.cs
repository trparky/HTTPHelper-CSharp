using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.IO;

public class FormFile {
    private string m_Name;
    private string m_ContentType;
    private string m_FilePath;

    private string m_uploadedFileName;
    /// <summary>This is the name for the form entry.</summary>
    public string formName {
        get { return m_Name; }
        set { m_Name = value; }
    }

    /// <summary>This is the content type or MIME type.</summary>
    public string contentType {
        get { return m_ContentType; }
        set { m_ContentType = value; }
    }

    /// <summary>This is the path to the file to be uploaded on the local file system.</summary>
    public string localFilePath {
        get { return m_FilePath; }
        set { m_FilePath = value; }
    }

    /// <summary>This sets the name that the uploaded file will be called on the remote server.</summary>
    public string remoteFileName {
        get { return m_uploadedFileName; }
        set { m_uploadedFileName = value; }
    }
}

public class noMimeTypeFoundException : Exception {
    public noMimeTypeFoundException() { }
    public noMimeTypeFoundException(string message) : base(message) { }
    public noMimeTypeFoundException(string message, Exception inner) : base(message, inner) { }
}

public class localFileAlreadyExistsException : Exception {
    public localFileAlreadyExistsException() { }
    public localFileAlreadyExistsException(string message) : base(message) { }
    public localFileAlreadyExistsException(string message, Exception inner) : base(message, inner) { }
}

public class dataMissingException : Exception {
    public dataMissingException() { }
    public dataMissingException(string message) : base(message) { }
    public dataMissingException(string message, Exception inner) : base(message, inner) { }
}

public class dataAlreadyExistsException : Exception {
    public dataAlreadyExistsException() { }
    public dataAlreadyExistsException(string message) : base(message) { }
    public dataAlreadyExistsException(string message, Exception inner) : base(message, inner) { }
}

public class proxyConfigurationErrorException : Exception {
    public proxyConfigurationErrorException() { }
    public proxyConfigurationErrorException(string message) : base(message) { }
    public proxyConfigurationErrorException(string message, Exception inner) : base(message, inner) { }
}

public class dnsLookupError : Exception {
    public dnsLookupError() { }
    public dnsLookupError(string message) : base(message) { }
    public dnsLookupError(string message, Exception inner) : base(message, inner) { }
}

public class noHTTPServerResponseHeadersFoundException : Exception {
    public noHTTPServerResponseHeadersFoundException() { } 
    public noHTTPServerResponseHeadersFoundException(string message) : base(message) { } 
    public noHTTPServerResponseHeadersFoundException(string message, Exception inner) : base(message, inner) { }
}

public class sslErrorException : Exception {
    public sslErrorException() { }
    public sslErrorException(string message) : base(message) { }
    public sslErrorException(string message, Exception inner) : base(message, inner) { }
}

public class credentialsAlreadySet : Exception {
    public credentialsAlreadySet() { }
    public credentialsAlreadySet(string message) : base(message) { }
    public credentialsAlreadySet(string message, Exception inner) : base(message, inner) { }
}

public class httpProtocolException : Exception {
    private System.Net.HttpStatusCode _httpStatusCode = System.Net.HttpStatusCode.NoContent;
    public httpProtocolException() { }
    public httpProtocolException(string message) : base(message) { }
    public httpProtocolException(string message, Exception inner) : base(message, inner) { }
    public System.Net.HttpStatusCode httpStatusCode {
        get { return _httpStatusCode; }
        set { _httpStatusCode = value; }
    }
}

public class noSSLCertificateFoundException : Exception {
    public noSSLCertificateFoundException() { }
    public noSSLCertificateFoundException(string message) : base(message) { }
    public noSSLCertificateFoundException(string message, Exception inner) : base(message, inner) { }
}

class cookieDetails {
    public string cookieData;
    public string cookieDomain;
    public string cookiePath = "/";
}

public class downloadStatusDetails {
    public ulong remoteFileSize;
    public ulong localFileSize;
    public short percentageDownloaded;
}

class credentials {
    public string strUser;
    public string strPassword;
}

/// <summary>Allows you to easily POST and upload files to a remote HTTP server without you, the programmer, knowing anything about how it all works. This class does it all for you. It handles adding a User Agent String, additional HTTP Request Headers, string data to your HTTP POST data, and files to be uploaded in the HTTP POST data.</summary>
public class httpHelper {
    private const string classVersion = "1.302";
    private string strUserAgentString = null;
    private bool boolUseProxy = false;
    private bool boolUseSystemProxy = true;
    private System.Net.IWebProxy customProxy = null;
    private System.Net.WebHeaderCollection httpResponseHeaders = null;
    private short httpDownloadProgressPercentage = 0;
    private ulong remoteFileSize;
    private ulong currentFileSize;
    private long httpTimeOut = 5000;
    private bool boolUseHTTPCompression = true;
    private string lastAccessedURL = null;
    private Exception lastException = null;
    private bool boolRunDownloadStatusUpdatePluginInSeparateThread = true;
    private System.Threading.Thread downloadStatusUpdaterThread = null;
    private int _intDownloadThreadSleepTime = 1000;
    // The default is 8192 bytes or 8 KBs.
    private int intDownloadBufferSize = 8191;

    private string strLastHTTPServerResponse;
    private Dictionary<string, string> additionalHTTPHeaders = new Dictionary<string, string>();
    private Dictionary<string, cookieDetails> httpCookies = new Dictionary<string, cookieDetails>();
    private Dictionary<string, object> postData = new Dictionary<string, object>();
    private Dictionary<string, string> getData = new Dictionary<string, string>();
    private downloadStatusDetails downloadStatusDetails;

    private credentials credentials;
    private System.Security.Cryptography.X509Certificates.X509Certificate2 sslCertificate;
    private Func<string, string> urlPreProcessor;
    private Delegate customErrorHandler;

    private Delegate downloadStatusUpdater;
    /// <summary>Retrieves the downloadStatusDetails data from within the Class instance.</summary>
    /// <returns>A downloadStatusDetails Object.</returns>
    public downloadStatusDetails getDownloadStatusDetails {
        get { return downloadStatusDetails; }
    }

    /// <summary>Sets the size of the download buffer to hold data in memory during the downloading of a file. The default is 8192 bytes or 8 KBs.</summary>
    public int setDownloadBufferSize {
        set { intDownloadBufferSize = value - 1; }
    }

    /// <summary>This allows you to inject your own error handler for HTTP exceptions into the Class instance.</summary>
    /// <value>A Lambda</value>
    /// <example>
    /// A VB.NET Example...
    /// httpHelper.setCustomErrorHandler(Function(ByVal ex As Exception, classInstance As httpHelper)
    /// End Function)
    /// OR A C# Example...
    /// httpHelper.setCustomErrorHandler((Exception ex, httpHelper classInstance) => { }
    /// </example>
    public Delegate setCustomErrorHandler {
        set { customErrorHandler = value; }
    }

    /// <summary>Adds HTTP Authentication headers to your HTTP Request in this HTTPHelper instance.</summary>
    /// <param name="strUsername">The username you want to pass to the server.</param>
    /// <param name="strPassword">The password you want to pass to the server.</param>
    /// <param name="throwExceptionIfAlreadySet">A Boolean value. This tells the function if it should throw an exception if HTTP Authentication settings have already been set.</param>
    public void setHTTPCredentials(string strUsername, string strPassword, bool throwExceptionIfAlreadySet = true) {
        if (credentials == null) {
            credentials = new credentials {
                strUser = strUsername,
                strPassword = strPassword
            };
        }
        else {
            if (throwExceptionIfAlreadySet) throw new credentialsAlreadySet("HTTP Authentication Credentials have already been set for this HTTPHelper Class Instance.");
        }
    }

    /// <summary>Sets up a custom proxy configuration for this class instance.</summary>
    /// <param name="strUsername">The username you want to pass to the server.</param>
    /// <param name="strPassword">The password you want to pass to the server.</param>
    /// <param name="strServer">The proxy server's address, usually an IP address.</param>
    /// <param name="intPort">The proxy port.</param>
    /// <param name="boolByPassOnLocal">This tells the class instance if it should bypass the proxy for local servers. This is an optional value, by default it is True.</param>
    /// <exception cref="proxyConfigurationErrorException">If this function throws a proxyConfigurationError, it means that something went wrong while setting up the proxy configuration for this class instance.</exception>
    public void setProxy(string strServer, int intPort, string strUsername, string strPassword, bool boolByPassOnLocal = true) {
        try {
            customProxy = new System.Net.WebProxy(string.Format("{0}:{1}", strServer, intPort.ToString()), boolByPassOnLocal) { Credentials = new System.Net.NetworkCredential(strUsername, strPassword) };
        }
        catch (UriFormatException ex) {
            throw new proxyConfigurationErrorException("There was an error setting up the proxy for this class instance.", ex);
        }
    }

    /// <summary>Sets up a custom proxy configuration for this class instance.</summary>
    /// <param name="boolByPassOnLocal">This tells the class instance if it should bypass the proxy for local servers. This is an optional value, by default it is True.</param>
    /// <param name="strServer">The proxy server's address, usually an IP address.</param>
    /// <param name="intPort">The proxy port.</param>
    /// <exception cref="proxyConfigurationErrorException">If this function throws a proxyConfigurationError, it means that something went wrong while setting up the proxy configuration for this class instance.</exception>
    public void setProxy(string strServer, int intPort, bool boolByPassOnLocal = true) {
        try {
            customProxy = new System.Net.WebProxy(string.Format("{0}:{1}", strServer, intPort.ToString()), boolByPassOnLocal);
        }
        catch (UriFormatException ex) {
            throw new proxyConfigurationErrorException("There was an error setting up the proxy for this class instance.", ex);
        }
    }

    /// <summary>Sets up a custom proxy configuration for this class instance.</summary>
    /// <param name="boolByPassOnLocal">This tells the class instance if it should bypass the proxy for local servers. This is an optional value, by default it is True.</param>
    /// <param name="strServer">The proxy server's address, usually an IP address followed up by a ":" followed up by a port number.</param>
    /// <exception cref="proxyConfigurationErrorException">If this function throws a proxyConfigurationError, it means that something went wrong while setting up the proxy configuration for this class instance.</exception>
    public void setProxy(string strServer, bool boolByPassOnLocal = true) {
        try {
            customProxy = new System.Net.WebProxy(strServer, boolByPassOnLocal);
        }
        catch (UriFormatException ex) {
            throw new proxyConfigurationErrorException("There was an error setting up the proxy for this class instance.", ex);
        }
    }

    /// <summary>Sets up a custom proxy configuration for this class instance.</summary>
    /// <param name="boolByPassOnLocal">This tells the class instance if it should bypass the proxy for local servers. This is an optional value, by default it is True.</param>
    /// <param name="strServer">The proxy server's address, usually an IP address followed up by a ":" followed up by a port number.</param>
    /// <param name="strUsername">The username you want to pass to the server.</param>
    /// <param name="strPassword">The password you want to pass to the server.</param>
    /// <exception cref="proxyConfigurationErrorException">If this function throws a proxyConfigurationError, it means that something went wrong while setting up the proxy configuration for this class instance.</exception>
    public void setProxy(string strServer, string strUsername, string strPassword, bool boolByPassOnLocal = true) {
        try {
            customProxy = new System.Net.WebProxy(strServer, boolByPassOnLocal) { Credentials = new System.Net.NetworkCredential(strUsername, strPassword) };
        }
        catch (UriFormatException ex) {
            throw new proxyConfigurationErrorException("There was an error setting up the proxy for this class instance.", ex);
        }
    }

    /// <summary>Returns the last Exception that occurred within this Class instance.</summary>
    /// <returns>An Exception Object.</returns>
    public Exception getLastException {
        get { return lastException; }
    }

    /// <summary>This allows you to set up a function to be run while your HTTP download is being processed. This function can be used to update things on the GUI during a download.</summary>
    /// <value>A Lambda</value>
    /// <example>
    /// A VB.NET Example...
    /// httpHelper.setDownloadStatusUpdateRoutine(Function(ByVal downloadStatusDetails As downloadStatusDetails)
    /// End Function)
    /// OR A C# Example...
    /// httpHelper.setDownloadStatusUpdateRoutine((downloadStatusDetails downloadStatusDetails) => { })
    /// </example>
    public Delegate setDownloadStatusUpdateRoutine {
        set { downloadStatusUpdater = value; }
    }

    /// <summary>This allows you to set up a Pre-Processor of sorts for URLs in case you need to add things to the beginning or end of URLs.</summary>
    /// <value>A Lambda</value>
    /// <example>
    /// httpHelper.setURLPreProcessor(Function(ByVal strURLInput As String) As String
    ///   If strURLInput.ToLower.StartsWith("http://") = False Then
    ///     strURLInput = "http://" + strURLInput
    ///   End If
    ///   Return strURLInput
    /// End Function)
    /// </example>
    public Func<string, string> setURLPreProcessor {
        set { urlPreProcessor = value; }
    }

    /// <summary>This wipes out most of the data in this Class instance. Once you have called this function it's recommended to set the name of your class instance to Nothing. For example... httpHelper = Nothing</summary>
    public void dispose() {
        additionalHTTPHeaders.Clear();
        httpCookies.Clear();
        postData.Clear();
        getData.Clear();

        remoteFileSize = 0;
        currentFileSize = 0;

        sslCertificate = null;
        urlPreProcessor = null;
        customErrorHandler = null;
        downloadStatusUpdater = null;
        httpResponseHeaders = null;
        strLastHTTPServerResponse = null;
    }

    /// <summary>Returns the last accessed URL by this Class instance.</summary>
    /// <returns>A String.</returns>
    public string getLastAccessedURL {
        get { return lastAccessedURL; }
    }

    /// <summary>Tells the Class instance if it should use the system proxy.</summary>
    public bool useSystemProxy {
        set { boolUseSystemProxy = value; }
    }

    /// <summary>This function allows you to get a peek inside the Class object instance. It returns many of the things that make up the Class instance like POST and GET data, cookies, additional HTTP headers, if proxy mode and HTTP compression mode is enabled, the user agent string, etc.</summary>
    /// <returns>A String.</returns>
    public string toString() {
        System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder();
        stringBuilder.AppendLine("--== HTTPHelper Class Object ==--");
        stringBuilder.AppendLine("--== Version: " + classVersion + " ==--");
        stringBuilder.AppendLine();
        stringBuilder.AppendLine("Last Accessed URL: " + lastAccessedURL);
        stringBuilder.AppendLine();

        if (getData.Count != 0) {
            foreach (KeyValuePair<string, string> item in getData) {
                stringBuilder.AppendLine("GET Data | " + item.Key + "=" + item.Value);
            }
        }

        if (postData.Count != 0) {
            foreach (KeyValuePair<string, object> item in postData) {
                stringBuilder.AppendLine("POST Data | " + item.Key.ToString() + "=" + item.Value.ToString());
            }
        }

        if (httpCookies.Count != 0) {
            foreach (KeyValuePair<string, cookieDetails> item in httpCookies) {
                stringBuilder.AppendLine("COOKIES | " + item.Key.ToString() + "=" + item.Value.cookieData);
            }
        }

        if (additionalHTTPHeaders.Count != 0) {
            foreach (KeyValuePair<string, string> item in additionalHTTPHeaders) {
                stringBuilder.AppendLine("Additional HTTP Header | " + item.Key.ToString() + "=" + item.Value);
            }
        }

        stringBuilder.AppendLine();

        stringBuilder.AppendLine("User Agent String: " + strUserAgentString);
        stringBuilder.AppendLine("Use HTTP Compression: " + boolUseHTTPCompression.ToString());
        stringBuilder.AppendLine("HTTP Time Out: " + httpTimeOut);
        stringBuilder.AppendLine("Use Proxy: " + boolUseProxy.ToString());

        if (credentials == null) stringBuilder.AppendLine("HTTP Authentication Enabled: False");
        else {
            stringBuilder.AppendLine("HTTP Authentication Enabled: True");
            stringBuilder.AppendLine("HTTP Authentication Details: " + credentials.strUser + "|" + credentials.strPassword);
        }

        if (lastException != null) {
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("--== Raw Exception Data ==--");
            stringBuilder.AppendLine(lastException.ToString());

            if (lastException is System.Net.WebException) {
                stringBuilder.AppendLine("Raw Exception Status Code: " + ((System.Net.WebException)lastException).Status.ToString());
            }
        }

        return stringBuilder.ToString().Trim();
    }

    /// <summary>Gets the remote file size.</summary>
    /// <param name="boolHumanReadable">Optional setting, normally set to True. Tells the function if it should transform the Integer representing the file size into a human readable format.</param>
    /// <returns>Either a String or a Long containing the remote file size.</returns>
    public object getHTTPDownloadRemoteFileSize(bool boolHumanReadable = true) {
        if (boolHumanReadable) {
            return fileSizeToHumanReadableFormat(remoteFileSize);
        }
        else {
            return remoteFileSize;
        }
    }

    /// <summary>This returns the SSL certificate details for the last HTTP request made by this Class instance.</summary>
    /// <returns>System.Security.Cryptography.X509Certificates.X509Certificate2</returns>
    /// <exception cref="noSSLCertificateFoundException">If this function throws a noSSLCertificateFoundException it means that the Class doesn't have an SSL certificate in the memory space of the Class instance. Perhaps the last HTTP request wasn't an HTTPS request.</exception>
    /// <param name="boolThrowException">An optional parameter that tells the function if it should throw an exception if an SSL certificate isn't found in the memory space of this Class instance.</param>
    public System.Security.Cryptography.X509Certificates.X509Certificate2 getCertificateDetails(bool boolThrowException = true) {
        if (sslCertificate == null) {
            if (boolThrowException) {
                lastException = new noSSLCertificateFoundException("No valid SSL certificate found for the last HTTP request. Perhaps the last HTTP request wasn't an HTTPS request.");
                throw lastException;
            }
            return null;
        }
        else return sslCertificate;
    }

    /// <summary>Gets the current local file's size.</summary>
    /// <param name="boolHumanReadable">Optional setting, normally set to True. Tells the function if it should transform the Integer representing the file size into a human readable format.</param>
    /// <returns>Either a String or a Long containing the current local file's size.</returns>
    public object getHTTPDownloadLocalFileSize(bool boolHumanReadable = true) {
        if (boolHumanReadable) return fileSizeToHumanReadableFormat(currentFileSize);
        else return currentFileSize;
    }

    /// <summary>Creates a new instance of the HTTPPost Class. You will need to set things up for the Class instance using the setProxyMode() and setUserAgent() routines.</summary>
    /// <example>Dim httpPostObject As New Tom.HTTPPost()</example>
    public httpHelper() { }

    /// <summary>Creates a new instance of the HTTPPost Class with some required parameters.</summary>
    /// <param name="strUserAgentStringIN">This set the User Agent String for the HTTP Request.</param>
    /// <example>Dim httpPostObject As New Tom.HTTPPost("Microsoft .NET")</example>
    public httpHelper(string strUserAgentStringIN) {
        strUserAgentString = strUserAgentStringIN;
    }

    /// <summary>Creates a new instance of the HTTPPost Class with some required parameters.</summary>
    /// <param name="strUserAgentStringIN">This set the User Agent String for the HTTP Request.</param>
    /// <param name="boolUseProxyIN">This tells the Class if you're going to be using a Proxy or not.</param>
    /// <example>Dim httpPostObject As New Tom.HTTPPost("Microsoft .NET", True)</example>
    public httpHelper(string strUserAgentStringIN, bool boolUseProxyIN) {
        strUserAgentString = strUserAgentStringIN;
        boolUseProxy = boolUseProxyIN;
    }

    /// <summary>Tells the HTTPPost Class if you want to use a Proxy or not.</summary>
    public bool setProxyMode {
        set { boolUseProxy = value; }
    }

    /// <summary>Sets a timeout for any HTTP requests in this Class. Normally it's set for 5 seconds. The input is the amount of time in seconds (NOT milliseconds) that you want your HTTP requests to timeout in. The class will translate the seconds to milliseconds for you.</summary>
    /// <value>The amount of time in seconds (NOT milliseconds) that you want your HTTP requests to timeout in. This function will translate the seconds to milliseconds for you.</value>
    public short setHTTPTimeout {
        set { httpTimeOut = value * 1000; }
    }

    /// <summary>Tells this Class instance if it should use HTTP compression for transport. Using HTTP Compression can save bandwidth. Normally the Class is setup to use HTTP Compression by default.</summary>
    /// <value>Boolean value.</value>
    public bool useHTTPCompression {
        set { boolUseHTTPCompression = value; }
    }

    /// <summary>Sets the User Agent String to be used by the HTTPPost Class.</summary>
    /// <value>Your User Agent String.</value>
    public string setUserAgent {
        set { strUserAgentString = value; }
    }

    /// <summary>This adds a String variable to your POST data.</summary>
    /// <param name="strName">The form name of the data to post.</param>
    /// <param name="strValue">The value of the data to post.</param>
    /// <param name="throwExceptionIfDataAlreadyExists">This tells the function if it should throw an exception if the data already exists in the POST data.</param>
    /// <exception cref="dataAlreadyExistsException">If this function throws a dataAlreadyExistsException, you forgot to add some data for your POST variable.</exception>
    public void addPOSTData(string strName, string strValue, bool throwExceptionIfDataAlreadyExists = false) {
        if (String.IsNullOrEmpty(strValue.Trim())) {
            lastException = new dataMissingException(string.Format("Data was missing for the {0}{1}{0} POST variable.", "\"", strName));
            throw lastException;
        }

        if (postData.ContainsKey(strName) & throwExceptionIfDataAlreadyExists) {
            lastException = new dataAlreadyExistsException(string.Format("The POST data key named {0}{1}{0} already exists in the POST data.", "\"", strName));
            throw lastException;
        }
        else {
            postData.Remove(strName);
            postData.Add(strName, strValue);
        }
    }

    /// <summary>This adds a String variable to your GET data.</summary>
    /// <param name="strName">The form name of the data to post.</param>
    /// <param name="strValue">The value of the data to post.</param>
    /// <exception cref="dataAlreadyExistsException">If this function throws a dataAlreadyExistsException, you forgot to add some data for your POST variable.</exception>
    public void addGETData(string strName, string strValue, bool throwExceptionIfDataAlreadyExists = false) {
        if (String.IsNullOrEmpty(strValue.Trim())) {
            lastException = new dataMissingException(string.Format("Data was missing for the {0}{1}{0} GET variable.", "\"", strName));
            throw lastException;
        }

        if (getData.ContainsKey(strName) & throwExceptionIfDataAlreadyExists) {
            lastException = new dataAlreadyExistsException(string.Format("The GET data key named {0}{1}{0} already exists in the GET data.", "\"", strName));
            throw lastException;
        }
        else {
            getData.Remove(strName);
            getData.Add(strName, strValue);
        }
    }

    /// <summary>Allows you to add additional headers to your HTTP Request Headers.</summary>
    /// <param name="strHeaderName">The name of your new HTTP Request Header.</param>
    /// <param name="strHeaderContents">The contents of your new HTTP Request Header. Be careful with adding data here, invalid data can cause your HTTP Request to fail thus throwing an httpPostException.</param>
    /// <param name="urlEncodeHeaderContent">Optional setting, normally set to False. Tells the function if it should URLEncode the HTTP Header Contents before setting it.</param>
    /// <example>httpPostObject.addHTTPHeader("myheader", "my header value")</example>
    /// <exception cref="dataAlreadyExistsException">If this function throws an dataAlreadyExistsException, it means that this Class instance already has an Additional HTTP Header of that name in the Class instance.</exception>
    public void addHTTPHeader(string strHeaderName, string strHeaderContents, bool urlEncodeHeaderContent = false) {
        if (!doesAdditionalHeaderExist(strHeaderName)) {
            if (urlEncodeHeaderContent) {
                additionalHTTPHeaders.Add(strHeaderName.ToLower(), System.Web.HttpUtility.UrlEncode(strHeaderContents));
            }
            else {
                additionalHTTPHeaders.Add(strHeaderName.ToLower(), strHeaderContents);
            }
        }
        else {
            lastException = new dataAlreadyExistsException(string.Format("The additional HTTP Header named {0}{1}{0} already exists in the Additional HTTP Headers settings for this Class instance.", "\"", strHeaderName));
            throw lastException;
        }
    }

    /// <summary>Allows you to add HTTP cookies to your HTTP Request with a specific path for the cookie.</summary>
    /// <param name="strCookieName">The name of your cookie.</param>
    /// <param name="strCookieValue">The value for your cookie.</param>
    /// <param name="strCookiePath">The path for the cookie.</param>
    /// <param name="strDomainDomain">The domain for the cookie.</param>
    /// <param name="urlEncodeHeaderContent">Optional setting, normally set to False. Tells the function if it should URLEncode the cookie contents before setting it.</param>
    /// <exception cref="dataAlreadyExistsException">If this function throws a dataAlreadyExistsException, it means that the cookie already exists in this Class instance.</exception>
    public void addHTTPCookie(string strCookieName, string strCookieValue, string strDomainDomain, string strCookiePath, bool urlEncodeHeaderContent = false) {
        if (!doesCookieExist(strCookieName)) {
            cookieDetails cookieDetails = new cookieDetails {
                cookieDomain = strDomainDomain,
                cookiePath = strCookiePath
            };

            if (urlEncodeHeaderContent) cookieDetails.cookieData = System.Web.HttpUtility.UrlEncode(strCookieValue);
            else cookieDetails.cookieData = strCookieValue;

            httpCookies.Add(strCookieName.ToLower(), cookieDetails);
        }
        else {
            lastException = new dataAlreadyExistsException(string.Format("The HTTP Cookie named {0}{1}{0} already exists in the settings for this Class instance.", "\"", strCookieName));
            throw lastException;
        }
    }

    /// <summary>Allows you to add HTTP cookies to your HTTP Request with a default path of "/".</summary>
    /// <param name="strCookieName">The name of your cookie.</param>
    /// <param name="strCookieValue">The value for your cookie.</param>
    /// <param name="strCookieDomain">The domain for the cookie.</param>
    /// <param name="urlEncodeHeaderContent">Optional setting, normally set to False. Tells the function if it should URLEncode the cookie contents before setting it.</param>
    /// <exception cref="dataAlreadyExistsException">If this function throws a dataAlreadyExistsException, it means that the cookie already exists in this Class instance.</exception>
    public void addHTTPCookie(string strCookieName, string strCookieValue, string strCookieDomain, bool urlEncodeHeaderContent = false) {
        if (!doesCookieExist(strCookieName)) {
            cookieDetails cookieDetails = new cookieDetails {
                cookieDomain = strCookieDomain,
                cookiePath = "/"
            };

            if (urlEncodeHeaderContent) cookieDetails.cookieData = System.Web.HttpUtility.UrlEncode(strCookieValue);
            else cookieDetails.cookieData = strCookieValue;

            httpCookies.Add(strCookieName.ToLower(), cookieDetails);
        }
        else {
            lastException = new dataAlreadyExistsException(string.Format("The HTTP Cookie named {0}{1}{0} already exists in the settings for this Class instance.", "\"", strCookieName));
            throw lastException;
        }
    }

    /// <summary>Checks to see if the GET data key exists in this GET data.</summary>
    /// <param name="strName">The name of the GET data variable you are checking the existance of.</param>
    /// <returns></returns>
    public bool doesGETDataExist(string strName) {
        return getData.ContainsKey(strName);
    }

    /// <summary>Checks to see if the POST data key exists in this POST data.</summary>
    /// <param name="strName">The name of the POST data variable you are checking the existance of.</param>
    /// <returns></returns>
    public bool doesPOSTDataExist(string strName) {
        return postData.ContainsKey(strName);
    }

    /// <summary>Checks to see if an additional HTTP Request Header has been added to the Class.</summary>
    /// <param name="strHeaderName">The name of the HTTP Request Header to check the existance of.</param>
    /// <returns>Boolean value; True if found, False if not found.</returns>
    public bool doesAdditionalHeaderExist(string strHeaderName) {
        return additionalHTTPHeaders.ContainsKey(strHeaderName.ToLower());
    }

    /// <summary>Checks to see if a cookie has been added to the Class.</summary>
    /// <param name="strCookieName">The name of the cookie to check the existance of.</param>
    /// <returns>Boolean value; True if found, False if not found.</returns>
    public bool doesCookieExist(string strCookieName) {
        return httpCookies.ContainsKey(strCookieName.ToLower());
    }

    /// <summary>This adds a file to be uploaded to your POST data.</summary>
    /// <param name="strFormName">The form name of the data to post.</param>
    /// <param name="strLocalFilePath">The path to the file you want to upload.</param>
    /// <param name="strRemoteFileName">This is the name that the uploaded file will be called on the remote server. If set to Nothing the program will fill the name in.</param>
    /// <param name="strContentType">The Content Type of the file you want to upload. You can leave it blank (or set to Nothing) and the program will try and determine what the MIME type of the file you're attaching is.</param>
    /// <exception cref="FileNotFoundException">If this function throws a FileNotFoundException, the Class wasn't able to find the file that you're trying to attach to the POST data on the local file system.</exception>
    /// <exception cref="noMimeTypeFoundException">If this function throws a noMimeTypeFoundException, the Class wasn't able to automatically determine the MIME type of the file you're trying to attach to the POST data.</exception>
    /// <example>httpPostObject.addFileToUpload("file", "C:\My File.txt", "My File.txt", Nothing)</example>
    /// <example>httpPostObject.addFileToUpload("file", "C:\My File.txt", "My File.txt", "text/plain")</example>
    /// <example>httpPostObject.addFileToUpload("file", "C:\My File.txt", Nothing, Nothing)</example>
    /// <example>httpPostObject.addFileToUpload("file", "C:\My File.txt", Nothing, "text/plain")</example>
    public void addFileUpload(string strFormName, string strLocalFilePath, string strRemoteFileName, string strContentType, bool throwExceptionIfItemAlreadyExists = false) {
        FileInfo fileInfo = new FileInfo(strLocalFilePath);

        if (!fileInfo.Exists) {
            lastException = new FileNotFoundException("Local file not found.", strLocalFilePath);
            throw lastException;
        }
        else if (postData.ContainsKey(strFormName)) {
            if (throwExceptionIfItemAlreadyExists) {
                lastException = new dataAlreadyExistsException(string.Format("The POST data key named {0}{1}{0} already exists in the POST data.", "\"", strFormName));
                throw lastException;
            }
            else return;
        }
        else {
            FormFile formFileInstance = new FormFile() {
                formName = strFormName,
                localFilePath = strLocalFilePath,
                remoteFileName = strRemoteFileName
            };
            if (String.IsNullOrEmpty(strContentType)) {
                string contentType = null;
                Microsoft.Win32.RegistryKey regPath = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(fileInfo.Extension.ToLower(), false);

                if (regPath == null) {
                    lastException = new noMimeTypeFoundException("No MIME Type found for " + fileInfo.Extension.ToLower());
                    throw lastException;
                }
                else contentType = regPath.GetValue("Content Type", null).ToString();

                if (String.IsNullOrEmpty(contentType)) {
                    lastException = new noMimeTypeFoundException("No MIME Type found for " + fileInfo.Extension.ToLower());
                    throw lastException;
                }
                else formFileInstance.contentType = contentType;
            }
            else formFileInstance.contentType = strContentType;

            postData.Add(strFormName, formFileInstance);
        }
    }

    /// <summary>Gets the HTTP Response Headers that were returned by the HTTP Server after the HTTP request.</summary>
    /// <param name="throwExceptionIfNoHeaders">Optional setting, normally set to False. Tells the function if it should throw an exception if no HTTP Response Headers are contained in this Class instance.</param>
    /// <returns>A collection of HTTP Response Headers in a Net.WebHeaderCollection object.</returns>
    /// <exception cref="noHTTPServerResponseHeadersFoundException">If this function throws a noHTTPServerResponseHeadersFoundException, there are no HTTP Response Headers in this Class instance.</exception>
    public System.Net.WebHeaderCollection getHTTPResponseHeaders(bool throwExceptionIfNoHeaders = false) {
        if (httpResponseHeaders == null) {
            if (throwExceptionIfNoHeaders) {
                lastException = new noHTTPServerResponseHeadersFoundException("No HTTP Server Response Headers found.");
                throw lastException;
            }
            else return null;
        }
        else return httpResponseHeaders;
    }

    /// <summary>Gets the percentage of the file that's being downloaded from the HTTP Server.</summary>
    /// <returns>Returns a Short Integer value.</returns>
    public short getHTTPDownloadProgressPercentage {
        get { return httpDownloadProgressPercentage; }
    }

    /// <summary>This tells the current HTTPHelper Class Instance if it should run the download update status routine in a separate thread. By default this is enabled.</summary>
    public bool enableMultiThreadedDownloadStatusUpdates {
        get { return boolRunDownloadStatusUpdatePluginInSeparateThread; }
        set { boolRunDownloadStatusUpdatePluginInSeparateThread = value; }
    }

    /// <summary>Sets the amount of time in miliseconds that the download status updating thread sleeps. The default is 1000 ms or 1 second, perfect for calculating the amount of data downloaded per second.</summary>
    public int intDownloadThreadSleepTime {
        set { _intDownloadThreadSleepTime = value; }
    }

    private void downloadStatusUpdaterThreadSubroutine() {
        try {
            beginAgain:
            downloadStatusUpdater.DynamicInvoke(downloadStatusDetails);
            System.Threading.Thread.Sleep(_intDownloadThreadSleepTime);
            goto beginAgain;
        }
        catch (System.Threading.ThreadAbortException) { }
        catch (System.Reflection.TargetInvocationException) { }
    }

    /// <summary>This subroutine is used by the downloadFile function to update the download status of the file that's being downloaded by the class instance.</summary>
    private void downloadStatusUpdateInvoker() {
        downloadStatusDetails = new downloadStatusDetails {
            remoteFileSize = remoteFileSize,
            percentageDownloaded = httpDownloadProgressPercentage,
            localFileSize = currentFileSize
        };
        // Update the downloadStatusDetails.

        // Checks to see if we have a status update routine to invoke.
        if (downloadStatusUpdater != null) {
            // We invoke the status update routine if we have one to invoke. This is usually injected
            // into the class instance by the programmer who's using this class in his/her program.
            if (boolRunDownloadStatusUpdatePluginInSeparateThread) {
                if (downloadStatusUpdaterThread == null) {
                    downloadStatusUpdaterThread = new System.Threading.Thread(downloadStatusUpdaterThreadSubroutine) {
                        IsBackground = true,
                        Priority = System.Threading.ThreadPriority.Lowest,
                        Name = "HTTPHelper Class Download Status Updating Thread"
                    };
                    downloadStatusUpdaterThread.Start();
                }
            }
            else downloadStatusUpdater.DynamicInvoke(downloadStatusDetails);
        }
    }

    private void abortDownloadStatusUpdaterThread() {
        try {
            if (downloadStatusUpdaterThread != null & boolRunDownloadStatusUpdatePluginInSeparateThread) {
                downloadStatusUpdaterThread.Abort();
                downloadStatusUpdaterThread = null;
            }
        }
        catch (Exception) { }
    }

    /// <summary>Downloads a file from a web server while feeding back the status of the download. You can find the percentage of the download in the httpDownloadProgressPercentage variable. This function gives you the programmer more control over how HTTP downloads are done. For instance, if you don't want to write the data directly out to disk until the download is complete, this function gives you that ability whereas the downloadFile() function writes the downloaded data directly to disk bypassing system RAM. This is good for those cases you may be writing the data to an SSD in which you only want to write the data to the SSD until the download is known to be successful.</summary>
    /// <param name="fileDownloadURL">The HTTP Path to a file on a remote server to download.</param>
    /// <param name="memStream">This is a IO.MemoryStream, it is passed as a ByRef so that the function will be able to act on the IO.MemoryStream() Object you pass to it. At the end of the download, if it is successful, the function will reset the position back to 0 for writing to whatever stream you choose.</param>
    /// <param name="throwExceptionIfError">Normally True. If True this function will throw an exception if an error occurs. If set to False, the function simply returns False if an error occurs; this is a much more simpler way to handle errors.</param>
    /// <exception cref="Net.WebException">If this function throws a Net.WebException then something failed during the HTTP request.</exception>
    /// <exception cref="localFileAlreadyExistsException">If this function throws a localFileAlreadyExistsException, the path in the local file system already exists.</exception>
    /// <exception cref="Exception">If this function throws a general Exception, something really went wrong; something that the function normally doesn't handle.</exception>
    /// <exception cref="httpProtocolException">This exception is thrown if the server responds with an HTTP Error.</exception>
    /// <exception cref="sslErrorException">If this function throws an sslErrorException, an error occurred while negotiating an SSL connection.</exception>
    /// <exception cref="dnsLookupError">If this function throws a dnsLookupError exception it means that the domain name wasn't able to be resolved properly.</exception>
    public bool downloadFile(string fileDownloadURL, ref MemoryStream memStream, bool throwExceptionIfError = true) {
        System.Net.HttpWebRequest httpWebRequest = null;
        currentFileSize = 0;
        double amountDownloaded;

        try {
            if (urlPreProcessor != null) fileDownloadURL = urlPreProcessor(fileDownloadURL);
            lastAccessedURL = fileDownloadURL;

            // We create a new data buffer to hold the stream of data from the web server.
            byte[] dataBuffer = new byte[intDownloadBufferSize + 1];

            httpWebRequest = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(fileDownloadURL);

            configureProxy(ref httpWebRequest);
            addParametersToWebRequest(ref httpWebRequest);

            System.Net.WebResponse webResponse = httpWebRequest.GetResponse();
            // We now get the web response.
            captureSSLInfo(fileDownloadURL, ref httpWebRequest);

            // Gets the size of the remote file on the web server.
            remoteFileSize = (ulong)webResponse.ContentLength;

            Stream responseStream = webResponse.GetResponseStream();
            // Gets the response stream.

            ulong lngBytesReadFromInternet = (ulong)responseStream.Read(dataBuffer, 0, dataBuffer.Length);
            // Reads some data from the HTTP stream into our data buffer.

            // We keep looping until all of the data has been downloaded.
            while (lngBytesReadFromInternet != 0) {
                // We calculate the current file size by adding the amount of data that we've so far
                // downloaded from the server repeatedly to a variable called "currentFileSize".
                currentFileSize += lngBytesReadFromInternet;

                memStream.Write(dataBuffer, 0, (int)lngBytesReadFromInternet);
                // Writes the data directly to disk.

                amountDownloaded = (currentFileSize / remoteFileSize) * 100;
                httpDownloadProgressPercentage = (short)Math.Round(amountDownloaded, 0);
                // Update the download percentage value.
                downloadStatusUpdateInvoker();

                lngBytesReadFromInternet = (ulong)responseStream.Read(dataBuffer, 0, dataBuffer.Length);
                // Reads more data into our data buffer.
            }

            // Before we return the MemoryStream to the user we have to reset the position back to the beginning of the Stream. This is so that when the
            // user processes the IO.MemoryStream that's returned as part of this function the IO.MemoryStream will be ready to write the data out of
            // memory and into whatever stream the user wants to write the data out to. If this isn't done and the user executes the CopyTo() function
            // on the IO.MemoryStream Object the user will have nothing written out because the IO.MemoryStream will be at the end of the stream.
            memStream.Position = 0;

            abortDownloadStatusUpdaterThread();

            return true;
        }
        catch (System.Threading.ThreadAbortException) {
            abortDownloadStatusUpdaterThread();

            if (httpWebRequest != null) httpWebRequest.Abort();

            if (memStream != null) {
                memStream.Close();
                // Closes the file stream.
                memStream.Dispose();
                // Disposes the file stream.
            }

            return false;
        }
        catch (Exception ex) {
            abortDownloadStatusUpdaterThread();

            lastException = ex;
            if (memStream != null) {
                memStream.Close();
                // Closes the file stream.
                memStream.Dispose();
                // Disposes the file stream.
            }

            if (!throwExceptionIfError) return false;

            if (customErrorHandler != null) {
                customErrorHandler.DynamicInvoke(ex, this);
                // Since we handled the exception with an injected custom error handler, we can now exit the function with the return of a False value.
                return false;
            }

            if (ex is System.Net.WebException) {
                System.Net.WebException ex2 = (System.Net.WebException)ex;

                if (ex2.Status == System.Net.WebExceptionStatus.ProtocolError) {
                    throw handleWebExceptionProtocolError(fileDownloadURL, ex2);
                    return false;
                }
                else if (ex2.Status == System.Net.WebExceptionStatus.TrustFailure) {
                    lastException = new sslErrorException("There was an error establishing an SSL connection.", ex2);
                    throw lastException;
                    return false;
                }
                else if (ex2.Status == System.Net.WebExceptionStatus.NameResolutionFailure) {
                    string strDomainName = System.Text.RegularExpressions.Regex.Match(lastAccessedURL, "(?:http(?:s){0,1}://){0,1}(.*)/", System.Text.RegularExpressions.RegexOptions.Singleline).Groups[1].Value;
                    lastException = new dnsLookupError(string.Format("There was an error while looking up the DNS records for the domain name {0}{1}{0}.", "\"", strDomainName), ex2);
                    throw lastException;
                    return false;
                }

                lastException = new System.Net.WebException(ex.Message, ex2);
                throw lastException;
                return false;
            }

            return false;
        }
    }

    /// <summary>Downloads a file from a web server while feeding back the status of the download. You can find the percentage of the download in the httpDownloadProgressPercentage variable.</summary>
    /// <param name="fileDownloadURL">The HTTP Path to a file on a remote server to download.</param>
    /// <param name="localFileName">The path in the local file system to which you are saving the file that's being downloaded.</param>
    /// <param name="throwExceptionIfLocalFileExists">This tells the function if it should throw an Exception if the local file already exists. If set the False the function will delete the local file if it exists before the download starts.</param>
    /// <param name="throwExceptionIfError">Normally True. If True this function will throw an exception if an error occurs. If set to False, the function simply returns False if an error occurs; this is a much more simpler way to handle errors.</param>
    /// <exception cref="Net.WebException">If this function throws a Net.WebException then something failed during the HTTP request.</exception>
    /// <exception cref="localFileAlreadyExistsException">If this function throws a localFileAlreadyExistsException, the path in the local file system already exists.</exception>
    /// <exception cref="Exception">If this function throws a general Exception, something really went wrong; something that the function normally doesn't handle.</exception>
    /// <exception cref="httpProtocolException">This exception is thrown if the server responds with an HTTP Error.</exception>
    /// <exception cref="sslErrorException">If this function throws an sslErrorException, an error occurred while negotiating an SSL connection.</exception>
    /// <exception cref="dnsLookupError">If this function throws a dnsLookupError exception it means that the domain name wasn't able to be resolved properly.</exception>
    public bool downloadFile(string fileDownloadURL, string localFileName, bool throwExceptionIfLocalFileExists, bool throwExceptionIfError = true) {
        FileStream fileWriteStream = null;
        System.Net.HttpWebRequest httpWebRequest = null;
        currentFileSize = 0;
        double amountDownloaded;

        try {
            if (urlPreProcessor != null) fileDownloadURL = urlPreProcessor(fileDownloadURL);
            lastAccessedURL = fileDownloadURL;

            if (File.Exists(localFileName)) {
                if (throwExceptionIfLocalFileExists) {
                    lastException = new localFileAlreadyExistsException(string.Format("The local file found at {0}{1}{0} already exists.", "\"", localFileName));
                    throw lastException;
                }
                else File.Delete(localFileName);
            }

            // We create a new data buffer to hold the stream of data from the web server.
            byte[] dataBuffer = new byte[intDownloadBufferSize + 1];

            httpWebRequest = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(fileDownloadURL);

            configureProxy(ref httpWebRequest);
            addParametersToWebRequest(ref httpWebRequest);

            System.Net.WebResponse webResponse = httpWebRequest.GetResponse();
            // We now get the web response.
            captureSSLInfo(fileDownloadURL, ref httpWebRequest);

            // Gets the size of the remote file on the web server.
            remoteFileSize = (ulong)webResponse.ContentLength;

            Stream responseStream = webResponse.GetResponseStream();
            // Gets the response stream.
            fileWriteStream = new FileStream(localFileName, FileMode.Create);
            // Creates a file write stream.

            ulong lngBytesReadFromInternet = (ulong)responseStream.Read(dataBuffer, 0, dataBuffer.Length);
            // Reads some data from the HTTP stream into our data buffer.

            // We keep looping until all of the data has been downloaded.
            while (lngBytesReadFromInternet != 0) {
                // We calculate the current file size by adding the amount of data that we've so far
                // downloaded from the server repeatedly to a variable called "currentFileSize".
                currentFileSize += lngBytesReadFromInternet;

                fileWriteStream.Write(dataBuffer, 0, (int)lngBytesReadFromInternet);
                // Writes the data directly to disk.

                amountDownloaded = (currentFileSize / remoteFileSize) * 100;
                httpDownloadProgressPercentage = (short)Math.Round(amountDownloaded, 0);
                // Update the download percentage value.
                downloadStatusUpdateInvoker();

                lngBytesReadFromInternet = (ulong)responseStream.Read(dataBuffer, 0, dataBuffer.Length);
                // Reads more data into our data buffer.
            }

            fileWriteStream.Close();
            // Closes the file stream.
            fileWriteStream.Dispose();
            // Disposes the file stream.

            if (downloadStatusUpdaterThread != null & boolRunDownloadStatusUpdatePluginInSeparateThread) {
                downloadStatusUpdaterThread.Abort();
                downloadStatusUpdaterThread = null;
            }

            abortDownloadStatusUpdaterThread();

            return true;
        }
        catch (System.Threading.ThreadAbortException) {
            abortDownloadStatusUpdaterThread();

            if (httpWebRequest != null) httpWebRequest.Abort();

            if (fileWriteStream != null) {
                fileWriteStream.Close();
                // Closes the file stream.
                fileWriteStream.Dispose();
                // Disposes the file stream.
            }

            return false;
        }
        catch (Exception ex) {
            abortDownloadStatusUpdaterThread();

            lastException = ex;
            if (fileWriteStream != null) {
                fileWriteStream.Close();
                // Closes the file stream.
                fileWriteStream.Dispose();
                // Disposes the file stream.
            }

            if (!throwExceptionIfError) return false;

            if (customErrorHandler != null) {
                customErrorHandler.DynamicInvoke(ex, this);
                // Since we handled the exception with an injected custom error handler, we can now exit the function with the return of a False value.
                return false;
            }

            if (ex is System.Net.WebException) {
                System.Net.WebException ex2 = (System.Net.WebException)ex;

                if (ex2.Status == System.Net.WebExceptionStatus.ProtocolError) {
                    throw handleWebExceptionProtocolError(fileDownloadURL, ex2);
                    return false;
                }
                else if (ex2.Status == System.Net.WebExceptionStatus.TrustFailure) {
                    lastException = new sslErrorException("There was an error establishing an SSL connection.", ex2);
                    throw lastException;
                    return false;
                }
                else if (ex2.Status == System.Net.WebExceptionStatus.NameResolutionFailure) {
                    string strDomainName = System.Text.RegularExpressions.Regex.Match(lastAccessedURL, "(?:http(?:s){0,1}://){0,1}(.*)/", System.Text.RegularExpressions.RegexOptions.Singleline).Groups[1].Value;
                    lastException = new dnsLookupError(string.Format("There was an error while looking up the DNS records for the domain name {0}{1}{0}.", "\"", strDomainName), ex2);
                    throw lastException;
                    return false;
                }

                lastException = new System.Net.WebException(ex.Message, ex2);
                throw lastException;
                return false;
            }

            return false;
        }
    }

    /// <summary>Performs an HTTP Request for data from a web server.</summary>
    /// <param name="url">This is the URL that the program will send to the web server in the HTTP request. Do not include any GET variables in the URL, use the addGETData() function before calling this function.</param>
    /// <param name="httpResponseText">This is a ByRef variable so declare it before passing it to this function, think of this as a pointer. The HTML/text content that the web server on the other end responds with is put into this variable and passed back in a ByRef function.</param>
    /// <returns>A Boolean value. If the HTTP operation was successful it returns a TRUE value, if not FALSE.</returns>
    /// <exception cref="Net.WebException">If this function throws a Net.WebException then something failed during the HTTP request.</exception>
    /// <exception cref="Exception">If this function throws a general Exception, something really went wrong; something that the function normally doesn't handle.</exception>
    /// <exception cref="httpProtocolException">This exception is thrown if the server responds with an HTTP Error.</exception>
    /// <exception cref="sslErrorException">If this function throws an sslErrorException, an error occurred while negotiating an SSL connection.</exception>
    /// <exception cref="dnsLookupError">If this function throws a dnsLookupError exception it means that the domain name wasn't able to be resolved properly.</exception>
    /// <example>httpPostObject.getWebData("http://www.myserver.com/mywebpage", httpResponseText)</example>
    /// <param name="throwExceptionIfError">Normally True. If True this function will throw an exception if an error occurs. If set to False, the function simply returns False if an error occurs; this is a much more simpler way to handle errors.</param>
    /// <param name="shortRangeTo">This controls how much data is downloaded from the server.</param>
    /// <param name="shortRangeFrom">This controls how much data is downloaded from the server.</param>
    public bool getWebData(string url, ref string httpResponseText, short shortRangeFrom, short shortRangeTo, bool throwExceptionIfError = true) {
        System.Net.HttpWebRequest httpWebRequest = null;

        try {
            if (urlPreProcessor != null) url = urlPreProcessor(url);
            lastAccessedURL = url;

            if (getData.Count != 0) url += "?" + this.getGETDataString();

            httpWebRequest = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(url);
            httpWebRequest.AddRange(shortRangeFrom, shortRangeTo);

            configureProxy(ref httpWebRequest);
            addParametersToWebRequest(ref httpWebRequest);
            addPostDataToWebRequest(ref httpWebRequest);

            System.Net.WebResponse httpWebResponse = httpWebRequest.GetResponse();
            captureSSLInfo(url, ref httpWebRequest);

            StreamReader httpInStream = new StreamReader(httpWebResponse.GetResponseStream());
            string httpTextOutput = httpInStream.ReadToEnd().Trim();
            httpResponseHeaders = httpWebResponse.Headers;

            httpInStream.Close();
            httpInStream.Dispose();

            httpWebResponse.Close();
            httpWebResponse = null;
            httpWebRequest = null;

            httpResponseText = convertLineFeeds(httpTextOutput).Trim();
            strLastHTTPServerResponse = httpResponseText;

            return true;
        }
        catch (Exception ex) {
            if (ex is System.Threading.ThreadAbortException) {
                if (httpWebRequest != null) httpWebRequest.Abort();
                return false;
            }

            lastException = ex;
            if (!throwExceptionIfError) return false;

            if (customErrorHandler != null) {
                customErrorHandler.DynamicInvoke(ex, this);
                // Since we handled the exception with an injected custom error handler, we can now exit the function with the return of a False value.
                return false;
            }

            if (ex is System.Net.WebException) {
                System.Net.WebException ex2 = (System.Net.WebException)ex;

                if (ex2.Status == System.Net.WebExceptionStatus.ProtocolError) {
                    throw handleWebExceptionProtocolError(url, ex2);
                    return false;
                }
                else if (ex2.Status == System.Net.WebExceptionStatus.TrustFailure) {
                    lastException = new sslErrorException("There was an error establishing an SSL connection.", ex2);
                    throw lastException;
                    return false;
                }
                else if (ex2.Status == System.Net.WebExceptionStatus.NameResolutionFailure) {
                    string strDomainName = System.Text.RegularExpressions.Regex.Match(lastAccessedURL, "(?:http(?:s){0,1}://){0,1}(.*)/", System.Text.RegularExpressions.RegexOptions.Singleline).Groups[1].Value;
                    lastException = new dnsLookupError(string.Format("There was an error while looking up the DNS records for the domain name {0}{1}{0}.", "\"", strDomainName), ex2);
                    throw lastException;
                    return false;
                }

                lastException = new System.Net.WebException(ex.Message, ex2);
                throw lastException;
                return false;
            }

            lastException = new Exception(ex.Message, ex);
            throw lastException;
            return false;
        }
    }

    /// <summary>Performs an HTTP Request for data from a web server.</summary>
    /// <param name="url">This is the URL that the program will send to the web server in the HTTP request. Do not include any GET variables in the URL, use the addGETData() function before calling this function.</param>
    /// <param name="httpResponseText">This is a ByRef variable so declare it before passing it to this function, think of this as a pointer. The HTML/text content that the web server on the other end responds with is put into this variable and passed back in a ByRef function.</param>
    /// <returns>A Boolean value. If the HTTP operation was successful it returns a TRUE value, if not FALSE.</returns>
    /// <exception cref="Net.WebException">If this function throws a Net.WebException then something failed during the HTTP request.</exception>
    /// <exception cref="Exception">If this function throws a general Exception, something really went wrong; something that the function normally doesn't handle.</exception>
    /// <exception cref="httpProtocolException">This exception is thrown if the server responds with an HTTP Error.</exception>
    /// <exception cref="sslErrorException">If this function throws an sslErrorException, an error occurred while negotiating an SSL connection.</exception>
    /// <exception cref="dnsLookupError">If this function throws a dnsLookupError exception it means that the domain name wasn't able to be resolved properly.</exception>
    /// <example>httpPostObject.getWebData("http://www.myserver.com/mywebpage", httpResponseText)</example>
    /// <param name="throwExceptionIfError">Normally True. If True this function will throw an exception if an error occurs. If set to False, the function simply returns False if an error occurs; this is a much more simpler way to handle errors.</param>
    public bool getWebData(string url, ref string httpResponseText, bool throwExceptionIfError = true) {
        System.Net.HttpWebRequest httpWebRequest = null;

        try {
            if (urlPreProcessor != null) url = urlPreProcessor(url);
            lastAccessedURL = url;

            if (getData.Count != 0) url += "?" + getGETDataString();

            httpWebRequest = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(url);

            configureProxy(ref httpWebRequest);
            addParametersToWebRequest(ref httpWebRequest);
            addPostDataToWebRequest(ref httpWebRequest);

            System.Net.WebResponse httpWebResponse = httpWebRequest.GetResponse();
            captureSSLInfo(url, ref httpWebRequest);

            StreamReader httpInStream = new StreamReader(httpWebResponse.GetResponseStream());
            string httpTextOutput = httpInStream.ReadToEnd().Trim();
            httpResponseHeaders = httpWebResponse.Headers;

            httpInStream.Close();
            httpInStream.Dispose();

            httpWebResponse.Close();
            httpWebResponse = null;
            httpWebRequest = null;

            httpResponseText = convertLineFeeds(httpTextOutput).Trim();
            strLastHTTPServerResponse = httpResponseText;

            return true;
        }
        catch (Exception ex) {
            if (ex is System.Threading.ThreadAbortException) {
                if (httpWebRequest != null) httpWebRequest.Abort();
                return false;
            }

            lastException = ex;
            if (!throwExceptionIfError) return false;

            if (customErrorHandler != null) {
                customErrorHandler.DynamicInvoke(ex, this);
                // Since we handled the exception with an injected custom error handler, we can now exit the function with the return of a False value.
                return false;
            }

            if (ex is System.Net.WebException) {
                System.Net.WebException ex2 = (System.Net.WebException)ex;

                if (ex2.Status == System.Net.WebExceptionStatus.ProtocolError) {
                    throw handleWebExceptionProtocolError(url, ex2);
                    return false;
                }
                else if (ex2.Status == System.Net.WebExceptionStatus.TrustFailure) {
                    lastException = new sslErrorException("There was an error establishing an SSL connection.", ex2);
                    throw lastException;
                    return false;
                }
                else if (ex2.Status == System.Net.WebExceptionStatus.NameResolutionFailure) {
                    string strDomainName = System.Text.RegularExpressions.Regex.Match(lastAccessedURL, "(?:http(?:s){0,1}://){0,1}(.*)/", System.Text.RegularExpressions.RegexOptions.Singleline).Groups[1].Value;
                    lastException = new dnsLookupError(string.Format("There was an error while looking up the DNS records for the domain name {0}{1}{0}.", "\"", strDomainName), ex2);
                    throw lastException;
                    return false;
                }

                lastException = new System.Net.WebException(ex.Message, ex2);
                throw lastException;
                return false;
            }

            lastException = new Exception(ex.Message, ex);
            throw lastException;
            return false;
        }
    }

    /// <summary>Sends data to a URL of your choosing.</summary>
    /// <param name="url">This is the URL that the program will send to the web server in the HTTP request. Do not include any GET variables in the URL, use the addGETData() function before calling this function.</param>
    /// <param name="httpResponseText">This is a ByRef variable so declare it before passing it to this function, think of this as a pointer. The HTML/text content that the web server on the other end responds with is put into this variable and passed back in a ByRef function.</param>
    /// <returns>A Boolean value. If the HTTP operation was successful it returns a TRUE value, if not FALSE.</returns>
    /// <exception cref="Net.WebException">If this function throws a Net.WebException then something failed during the HTTP request.</exception>
    /// <exception cref="dataMissingException">If this function throws an postDataMissingException, the Class has nothing to upload so why continue?</exception>
    /// <exception cref="Exception">If this function throws a general Exception, something really went wrong; something that the function normally doesn't handle.</exception>
    /// <exception cref="httpProtocolException">This exception is thrown if the server responds with an HTTP Error.</exception>
    /// <exception cref="sslErrorException">If this function throws an sslErrorException, an error occurred while negotiating an SSL connection.</exception>
    /// <exception cref="dnsLookupError">If this function throws a dnsLookupError exception it means that the domain name wasn't able to be resolved properly.</exception>
    /// <example>httpPostObject.uploadData("http://www.myserver.com/myscript", httpResponseText)</example>
    /// <param name="throwExceptionIfError">Normally True. If True this function will throw an exception if an error occurs. If set to False, the function simply returns False if an error occurs; this is a much more simpler way to handle errors.</param>
    public bool uploadData(string url, ref string httpResponseText, bool throwExceptionIfError = false) {
        System.Net.HttpWebRequest httpWebRequest = null;

        try {
            if (urlPreProcessor != null) url = urlPreProcessor(url);
            lastAccessedURL = url;

            if (postData.Count == 0) {
                lastException = new dataMissingException("Your HTTP Request contains no POST data. Please add some data to POST before calling this function.");
                throw lastException;
            }
            if (getData.Count != 0) url += "?" + this.getGETDataString();

            string boundary = "---------------------------" + DateTime.Now.Ticks.ToString("x");
            byte[] boundaryBytes = System.Text.Encoding.ASCII.GetBytes((Convert.ToString(Constants.vbCr + Constants.vbLf + "--") + boundary) + Constants.vbCr + Constants.vbLf);

            httpWebRequest = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(url);

            configureProxy(ref httpWebRequest);
            addParametersToWebRequest(ref httpWebRequest);

            httpWebRequest.KeepAlive = true;
            httpWebRequest.ContentType = "multipart/form-data; boundary=" + boundary;
            httpWebRequest.Method = "POST";

            if (postData.Count != 0) {
                Stream httpRequestWriter = httpWebRequest.GetRequestStream();
                string header = null;
                FileInfo fileInfo = default(FileInfo);
                FormFile formFileObjectInstance = null;
                byte[] bytes = null;
                byte[] buffer = null;
                FileStream fileStream = default(FileStream);
                string data = null;

                foreach (KeyValuePair<string, object> entry in postData) {
                    httpRequestWriter.Write(boundaryBytes, 0, boundaryBytes.Length);

                    if (entry.Value is FormFile) {
                        formFileObjectInstance = (FormFile)entry.Value;

                        if (String.IsNullOrEmpty(formFileObjectInstance.remoteFileName)) {
                            fileInfo = new FileInfo(formFileObjectInstance.localFilePath);

                            header = string.Format("Content-Disposition: form-data; name={0}{1}{0}; filename={0}{2}{0}", "\"", entry.Key, fileInfo.Name);
                            header += Constants.vbCrLf + "Content-Type: " + formFileObjectInstance.contentType + Constants.vbCrLf + Constants.vbCrLf;
                        }
                        else {
                            header = string.Format("Content-Disposition: form-data; name={0}{1}{0}; filename={0}{2}{0}", "\"", entry.Key, formFileObjectInstance.remoteFileName);
                            header += Constants.vbCrLf + "Content-Type: " + formFileObjectInstance.contentType + Constants.vbCrLf + Constants.vbCrLf;
                        }

                        bytes = System.Text.Encoding.UTF8.GetBytes(header);
                        httpRequestWriter.Write(bytes, 0, bytes.Length);

                        fileStream = new FileStream(formFileObjectInstance.localFilePath, FileMode.Open);
                        buffer = new byte[32769];

                        while (fileStream.Read(buffer, 0, buffer.Length) != 0) {
                            httpRequestWriter.Write(buffer, 0, buffer.Length);
                        }

                        fileStream.Close();
                        fileStream.Dispose();
                        fileStream = null;
                    }
                    else {
                        data = string.Format("Content-Disposition: form-data; name={0}{1}{0}{2}{2}{3}", "\"", entry.Key, Constants.vbCrLf, entry.Value);
                        bytes = System.Text.Encoding.UTF8.GetBytes(data);
                        httpRequestWriter.Write(bytes, 0, bytes.Length);
                    }
                }

                byte[] trailer = System.Text.Encoding.ASCII.GetBytes(Constants.vbCrLf + "--" + boundary + "--" + Constants.vbCrLf);
                httpRequestWriter.Write(trailer, 0, trailer.Length);
                httpRequestWriter.Close();
            }

            System.Net.WebResponse httpWebResponse = httpWebRequest.GetResponse();
            captureSSLInfo(url, ref httpWebRequest);

            StreamReader httpInStream = new StreamReader(httpWebResponse.GetResponseStream());
            string httpTextOutput = httpInStream.ReadToEnd().Trim();
            httpResponseHeaders = httpWebResponse.Headers;

            httpInStream.Close();
            httpInStream.Dispose();

            httpWebResponse.Close();
            httpWebResponse = null;
            httpWebRequest = null;

            httpResponseText = convertLineFeeds(httpTextOutput).Trim();
            strLastHTTPServerResponse = httpResponseText;

            return true;
        }
        catch (Exception ex)
        {
            if (ex is System.Threading.ThreadAbortException) {
                if (httpWebRequest != null) httpWebRequest.Abort();
            }

            lastException = ex;
            if (!throwExceptionIfError) return false;

            if (customErrorHandler != null) {
                customErrorHandler.DynamicInvoke(ex, this);
                // Since we handled the exception with an injected custom error handler, we can now exit the function with the return of a False value.
                return false;
            }

            if (ex is System.Net.WebException) {
                System.Net.WebException ex2 = (System.Net.WebException)ex;

                if (ex2.Status == System.Net.WebExceptionStatus.ProtocolError) {
                    throw handleWebExceptionProtocolError(url, ex2);
                    return false;
                }
                else if (ex2.Status == System.Net.WebExceptionStatus.TrustFailure) {
                    lastException = new sslErrorException("There was an error establishing an SSL connection.", ex2);
                    throw lastException;
                    return false;
                }
                else if (ex2.Status == System.Net.WebExceptionStatus.NameResolutionFailure) {
                    string strDomainName = System.Text.RegularExpressions.Regex.Match(lastAccessedURL, "(?:http(?:s){0,1}://){0,1}(.*)/", System.Text.RegularExpressions.RegexOptions.Singleline).Groups[1].Value;
                    lastException = new dnsLookupError(string.Format("There was an error while looking up the DNS records for the domain name {0}{1}{0}.", "\"", strDomainName), ex2);
                    throw lastException;
                    return false;
                }

                lastException = new System.Net.WebException(ex.Message, ex2);
                throw lastException;
                return false;
            }

            lastException = new Exception(ex.Message, ex);
            throw lastException;
            return false;
        }
    }

    private void captureSSLInfo(string url, ref System.Net.HttpWebRequest httpWebRequest) {
        if (url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) {
            sslCertificate = new System.Security.Cryptography.X509Certificates.X509Certificate2(httpWebRequest.ServicePoint.Certificate);
        }
        else sslCertificate = null;
    }

    private void addPostDataToWebRequest(ref System.Net.HttpWebRequest httpWebRequest)
    {
        if (postData.Count == 0) httpWebRequest.Method = "GET";
        else {
            httpWebRequest.Method = "POST";
            string postDataString = getPOSTDataString();
            httpWebRequest.ContentType = "application/x-www-form-urlencoded";
            httpWebRequest.ContentLength = postDataString.Length;

            dynamic httpRequestWriter = new StreamWriter(httpWebRequest.GetRequestStream());
            httpRequestWriter.Write(postDataString);
            httpRequestWriter.Close();
            httpRequestWriter.Dispose();
            httpRequestWriter = null;
        }
    }

    private void addParametersToWebRequest(ref System.Net.HttpWebRequest httpWebRequest) {
        if (credentials != null) {
            httpWebRequest.PreAuthenticate = true;
            addHTTPHeader("Authorization", "Basic " + Convert.ToBase64String(System.Text.Encoding.Default.GetBytes(credentials.strUser + ":" + credentials.strPassword)));
        }

        if (strUserAgentString != null) httpWebRequest.UserAgent = strUserAgentString;
        if (httpCookies.Count != 0) getCookies(ref httpWebRequest);
        if (additionalHTTPHeaders.Count != 0) getHeaders(ref httpWebRequest);

        if (boolUseHTTPCompression) {
            // We tell the web server that we can accept a GZIP and Deflate compressed data stream.
            httpWebRequest.Accept = "gzip, deflate";
            httpWebRequest.Headers.Add(System.Net.HttpRequestHeader.AcceptEncoding, "gzip, deflate");
            httpWebRequest.AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate;
        }

        httpWebRequest.Timeout = (int)httpTimeOut;
        httpWebRequest.KeepAlive = true;
    }

    private void getCookies(ref System.Net.HttpWebRequest httpWebRequest) {
        System.Net.CookieContainer cookieContainer = new System.Net.CookieContainer();
        foreach (KeyValuePair<string, cookieDetails> entry in httpCookies) {
            cookieContainer.Add(new System.Net.Cookie(entry.Key, entry.Value.cookieData, entry.Value.cookiePath, entry.Value.cookieDomain));
        }
        httpWebRequest.CookieContainer = cookieContainer;
    }

    private void getHeaders(ref System.Net.HttpWebRequest httpWebRequest) {
        foreach (KeyValuePair<string, string> entry in additionalHTTPHeaders) {
            httpWebRequest.Headers.Add(entry.Key.ToString(), entry.Value);
        }
    }

    private void configureProxy(ref System.Net.HttpWebRequest httpWebRequest) {
        if (boolUseProxy) {
            if (boolUseSystemProxy) httpWebRequest.Proxy = System.Net.WebRequest.GetSystemWebProxy();
            else {
                if (customProxy == null) httpWebRequest.Proxy = System.Net.WebRequest.GetSystemWebProxy();
                else httpWebRequest.Proxy = customProxy;
            }
        }
    }

    private string convertLineFeeds(string input) {
        // Checks to see if the file is in Windows linefeed format or UNIX linefeed format.
        if (input.Contains(Constants.vbCrLf)) {
            return input;
            // It's in Windows linefeed format so we return the output as is.
        }
        else {
            return input.Replace(Constants.vbLf, Constants.vbCrLf);
            // It's in UNIX linefeed format so we have to convert it to Windows before we return the output.
        }
    }

    private string getPOSTDataString() {
        string postDataString = "";
        foreach (KeyValuePair<string, object> entry in postData) {
            if (!entry.Value.GetType().Equals(typeof(FormFile))) {
                postDataString += entry.Key.ToString().Trim() + "=" + System.Web.HttpUtility.UrlEncode((String)entry.Value) + "&";
            }
        }

        if (postDataString.EndsWith("&")) postDataString = postDataString.Substring(0, postDataString.Length - 1);

        return postDataString;
    }

    private string getGETDataString() {
        string getDataString = "";
        foreach (KeyValuePair<string, string> entry in getData) {
            getDataString += entry.Key.ToString().Trim() + "=" + System.Web.HttpUtility.UrlEncode(entry.Value.ToString().Trim()) + "&";
        }

        if (getDataString.EndsWith("&")) getDataString = getDataString.Substring(0, getDataString.Length - 1);

        return getDataString;
    }

    private httpProtocolException handleWebExceptionProtocolError(string url, System.Net.WebException ex) {
        System.Net.HttpWebResponse httpErrorResponse = ex.Response as System.Net.HttpWebResponse;
        httpProtocolException lastException;

        if (httpErrorResponse != null) {
            if (httpErrorResponse.StatusCode == System.Net.HttpStatusCode.InternalServerError) {
                lastException = new httpProtocolException("HTTP Protocol Error (Server 500 Error) while accessing " + url, ex) { httpStatusCode = httpErrorResponse.StatusCode };
            }
            else if (httpErrorResponse.StatusCode == System.Net.HttpStatusCode.NotFound) {
                lastException = new httpProtocolException("HTTP Protocol Error (404 File Not Found) while accessing " + url, ex) { httpStatusCode = httpErrorResponse.StatusCode };
            }
            else if (httpErrorResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized) {
                lastException = new httpProtocolException("HTTP Protocol Error (401 Unauthorized) while accessing " + url, ex) { httpStatusCode = httpErrorResponse.StatusCode };
            }
            else if (httpErrorResponse.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable) {
                lastException = new httpProtocolException("HTTP Protocol Error (503 Service Unavailable) while accessing " + url, ex) { httpStatusCode = httpErrorResponse.StatusCode };
            }
            else if (httpErrorResponse.StatusCode == System.Net.HttpStatusCode.Forbidden) {
                lastException = new httpProtocolException("HTTP Protocol Error (403 Forbidden) while accessing " + url, ex) { httpStatusCode = httpErrorResponse.StatusCode };
            }
            else {
                lastException = new httpProtocolException("HTTP Protocol Error while accessing " + url, ex) { httpStatusCode = httpErrorResponse.StatusCode };
            }
        }
        else {
            lastException = new httpProtocolException("HTTP Protocol Error while accessing " + url, ex);
        }

        return lastException;
    }

    public string fileSizeToHumanReadableFormat(ulong size, bool roundToNearestWholeNumber = false) {
        string result = null;
        short shortRoundNumber;
        if (roundToNearestWholeNumber) { shortRoundNumber = 0; } else { shortRoundNumber = 2; }

        if (size <= (Math.Pow(2, 10))) result = size + " Bytes";
        else if (size > (Math.Pow(2, 10)) & size <= (Math.Pow(2, 20))) {
            result = Math.Round(size / (Math.Pow(2, 10)), shortRoundNumber) + " KBs";
        }
        else if (size > (Math.Pow(2, 20)) & size <= (Math.Pow(2, 30))) {
            result = Math.Round(size / (Math.Pow(2, 20)), shortRoundNumber) + " MBs";
        }
        else if (size > (Math.Pow(2, 30)) & size <= (Math.Pow(2, 40))) {
            result = Math.Round(size / (Math.Pow(2, 30)), shortRoundNumber) + " GBs";
        }
        else if (size > (Math.Pow(2, 40)) & size <= (Math.Pow(2, 50))) {
            result = Math.Round(size / (Math.Pow(2, 40)), shortRoundNumber) + " TBs";
        }
        else if (size > (Math.Pow(2, 50)) & size <= (Math.Pow(2, 60))) {
            result = Math.Round(size / (Math.Pow(2, 50)), shortRoundNumber) + " PBs";
        }
        else result = "(None)";

        return result;
    }

    private bool doWeHaveAnInternetConnection() {
        try {
            System.Net.NetworkInformation.Ping ping = new System.Net.NetworkInformation.Ping();

            if (ping.Send("8.8.8.8").Status == System.Net.NetworkInformation.IPStatus.Success) return true;
            else return false;
        }
        catch (Exception) { return false; }
    }
}