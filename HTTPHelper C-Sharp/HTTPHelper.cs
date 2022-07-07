using System;
using System.Collections.Generic;
using System.IO;

public class FormFile
{
    private string m_Name;
    private string m_ContentType;
    private string m_FilePath;

    private string m_uploadedFileName;
    /// <summary>This is the name for the form entry.</summary>
    public string FormName
    {
        get { return m_Name; }
        set { m_Name = value; }
    }

    /// <summary>This is the content type or MIME type.</summary>
    public string ContentType
    {
        get { return m_ContentType; }
        set { m_ContentType = value; }
    }

    /// <summary>This is the path to the file to be uploaded on the local file system.</summary>
    public string LocalFilePath
    {
        get { return m_FilePath; }
        set { m_FilePath = value; }
    }

    /// <summary>This sets the name that the uploaded file will be called on the remote server.</summary>
    public string RemoteFileName
    {
        get { return m_uploadedFileName; }
        set { m_uploadedFileName = value; }
    }
}

public class NoMimeTypeFoundException : Exception
{
    public NoMimeTypeFoundException() { }
    public NoMimeTypeFoundException(string message) : base(message) { }
    public NoMimeTypeFoundException(string message, Exception inner) : base(message, inner) { }
}

public class LocalFileAlreadyExistsException : Exception
{
    public LocalFileAlreadyExistsException() { }
    public LocalFileAlreadyExistsException(string message) : base(message) { }
    public LocalFileAlreadyExistsException(string message, Exception inner) : base(message, inner) { }
}

public class DataMissingException : Exception
{
    public DataMissingException() { }
    public DataMissingException(string message) : base(message) { }
    public DataMissingException(string message, Exception inner) : base(message, inner) { }
}

public class DataAlreadyExistsException : Exception
{
    public DataAlreadyExistsException() { }
    public DataAlreadyExistsException(string message) : base(message) { }
    public DataAlreadyExistsException(string message, Exception inner) : base(message, inner) { }
}

public class ProxyConfigurationErrorException : Exception
{
    public ProxyConfigurationErrorException() { }
    public ProxyConfigurationErrorException(string message) : base(message) { }
    public ProxyConfigurationErrorException(string message, Exception inner) : base(message, inner) { }
}

public class DNSLookupError : Exception
{
    public DNSLookupError() { }
    public DNSLookupError(string message) : base(message) { }
    public DNSLookupError(string message, Exception inner) : base(message, inner) { }
}

public class NoHTTPServerResponseHeadersFoundException : Exception
{
    public NoHTTPServerResponseHeadersFoundException() { }
    public NoHTTPServerResponseHeadersFoundException(string message) : base(message) { }
    public NoHTTPServerResponseHeadersFoundException(string message, Exception inner) : base(message, inner) { }
}

public class SSLErrorException : Exception
{
    public SSLErrorException() { }
    public SSLErrorException(string message) : base(message) { }
    public SSLErrorException(string message, Exception inner) : base(message, inner) { }
}

public class CredentialsAlreadySet : Exception
{
    public CredentialsAlreadySet() { }
    public CredentialsAlreadySet(string message) : base(message) { }
    public CredentialsAlreadySet(string message, Exception inner) : base(message, inner) { }
}

public class HTTPProtocolException : Exception
{
    private System.Net.HttpStatusCode _httpStatusCode = System.Net.HttpStatusCode.NoContent;
    public HTTPProtocolException() { }
    public HTTPProtocolException(string message) : base(message) { }
    public HTTPProtocolException(string message, Exception inner) : base(message, inner) { }
    public System.Net.HttpStatusCode HTTPStatusCode
    {
        get { return _httpStatusCode; }
        set { _httpStatusCode = value; }
    }
}

public class NoSSLCertificateFoundException : Exception
{
    public NoSSLCertificateFoundException() { }
    public NoSSLCertificateFoundException(string message) : base(message) { }
    public NoSSLCertificateFoundException(string message, Exception inner) : base(message, inner) { }
}

class CookieDetails
{
    public string cookieData;
    public string cookieDomain;
    public string cookiePath = "/";
}

public class DownloadStatusDetails
{
    public long remoteFileSize;
    public long localFileSize;
    public short percentageDownloaded;
}

class Credentials
{
    public string strUser;
    public string strPassword;
}

/// <summary>Allows you to easily POST and upload files to a remote HTTP server without you, the programmer, knowing anything about how it all works. This class does it all for you. It handles adding a User Agent String, additional HTTP Request Headers, string data to your HTTP POST data, and files to be uploaded in the HTTP POST data.</summary>
public class HTTPHelper
{
    private const string classVersion = "1.314";
    private string strUserAgentString = null;
    private bool boolUseProxy = false;
    private bool boolUseSystemProxy = true;
    private System.Net.IWebProxy customProxy = null;
    private System.Net.WebHeaderCollection httpResponseHeaders = null;
    private short httpDownloadProgressPercentage = 0;
    private long remoteFileSize;
    private long currentFileSize;
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
    private readonly Dictionary<string, string> additionalHTTPHeaders = new Dictionary<string, string>();
    private readonly Dictionary<string, CookieDetails> httpCookies = new Dictionary<string, CookieDetails>();
    private readonly Dictionary<string, object> postData = new Dictionary<string, object>();
    private readonly Dictionary<string, string> getData = new Dictionary<string, string>();
    private DownloadStatusDetails downloadStatusDetails;

    private Credentials credentials;
    private System.Security.Cryptography.X509Certificates.X509Certificate2 sslCertificate;
    private Func<string, string> urlPreProcessor;
    private Delegate customErrorHandler;

    private const string strLF = "\n";
    private const string strCRLF = "\r\n";

    private Delegate downloadStatusUpdater;
    /// <summary>Retrieves the downloadStatusDetails data from within the Class instance.</summary>
    /// <returns>A downloadStatusDetails Object.</returns>
    public DownloadStatusDetails GetDownloadStatusDetails
    {
        get { return downloadStatusDetails; }
    }

    /// <summary>Sets the size of the download buffer to hold data in memory during the downloading of a file. The default is 8192 bytes or 8 KBs.</summary>
    public int SetDownloadBufferSize
    {
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
    public Delegate SetCustomErrorHandler
    {
        set { customErrorHandler = value; }
    }

    /// <summary>Adds HTTP Authentication headers to your HTTP Request in this HTTPHelper instance.</summary>
    /// <param name="strUsername">The username you want to pass to the server.</param>
    /// <param name="strPassword">The password you want to pass to the server.</param>
    /// <param name="throwExceptionIfAlreadySet">A Boolean value. This tells the function if it should throw an exception if HTTP Authentication settings have already been set.</param>
    public void SetHTTPCredentials(string strUsername, string strPassword, bool throwExceptionIfAlreadySet = true)
    {
        if (credentials == null)
        {
            credentials = new Credentials
            {
                strUser = strUsername,
                strPassword = strPassword
            };
        }
        else
        {
            if (throwExceptionIfAlreadySet) throw new CredentialsAlreadySet("HTTP Authentication Credentials have already been set for this HTTPHelper Class Instance.");
        }
    }

    /// <summary>Sets up a custom proxy configuration for this class instance.</summary>
    /// <param name="strUsername">The username you want to pass to the server.</param>
    /// <param name="strPassword">The password you want to pass to the server.</param>
    /// <param name="strServer">The proxy server's address, usually an IP address.</param>
    /// <param name="intPort">The proxy port.</param>
    /// <param name="boolByPassOnLocal">This tells the class instance if it should bypass the proxy for local servers. This is an optional value, by default it is True.</param>
    /// <exception cref="proxyConfigurationErrorException">If this function throws a proxyConfigurationError, it means that something went wrong while setting up the proxy configuration for this class instance.</exception>
    public void SetProxy(string strServer, int intPort, string strUsername, string strPassword, bool boolByPassOnLocal = true)
    {
        try
        {
            customProxy = new System.Net.WebProxy(string.Format("{0}:{1}", strServer, intPort.ToString()), boolByPassOnLocal) { Credentials = new System.Net.NetworkCredential(strUsername, strPassword) };
        }
        catch (UriFormatException ex)
        {
            throw new ProxyConfigurationErrorException("There was an error setting up the proxy for this class instance.", ex);
        }
    }

    /// <summary>Sets up a custom proxy configuration for this class instance.</summary>
    /// <param name="boolByPassOnLocal">This tells the class instance if it should bypass the proxy for local servers. This is an optional value, by default it is True.</param>
    /// <param name="strServer">The proxy server's address, usually an IP address.</param>
    /// <param name="intPort">The proxy port.</param>
    /// <exception cref="proxyConfigurationErrorException">If this function throws a proxyConfigurationError, it means that something went wrong while setting up the proxy configuration for this class instance.</exception>
    public void SetProxy(string strServer, int intPort, bool boolByPassOnLocal = true)
    {
        try
        {
            customProxy = new System.Net.WebProxy(string.Format("{0}:{1}", strServer, intPort.ToString()), boolByPassOnLocal);
        }
        catch (UriFormatException ex)
        {
            throw new ProxyConfigurationErrorException("There was an error setting up the proxy for this class instance.", ex);
        }
    }

    /// <summary>Sets up a custom proxy configuration for this class instance.</summary>
    /// <param name="boolByPassOnLocal">This tells the class instance if it should bypass the proxy for local servers. This is an optional value, by default it is True.</param>
    /// <param name="strServer">The proxy server's address, usually an IP address followed up by a ":" followed up by a port number.</param>
    /// <exception cref="proxyConfigurationErrorException">If this function throws a proxyConfigurationError, it means that something went wrong while setting up the proxy configuration for this class instance.</exception>
    public void SetProxy(string strServer, bool boolByPassOnLocal = true)
    {
        try
        {
            customProxy = new System.Net.WebProxy(strServer, boolByPassOnLocal);
        }
        catch (UriFormatException ex)
        {
            throw new ProxyConfigurationErrorException("There was an error setting up the proxy for this class instance.", ex);
        }
    }

    /// <summary>Sets up a custom proxy configuration for this class instance.</summary>
    /// <param name="boolByPassOnLocal">This tells the class instance if it should bypass the proxy for local servers. This is an optional value, by default it is True.</param>
    /// <param name="strServer">The proxy server's address, usually an IP address followed up by a ":" followed up by a port number.</param>
    /// <param name="strUsername">The username you want to pass to the server.</param>
    /// <param name="strPassword">The password you want to pass to the server.</param>
    /// <exception cref="proxyConfigurationErrorException">If this function throws a proxyConfigurationError, it means that something went wrong while setting up the proxy configuration for this class instance.</exception>
    public void SetProxy(string strServer, string strUsername, string strPassword, bool boolByPassOnLocal = true)
    {
        try
        {
            customProxy = new System.Net.WebProxy(strServer, boolByPassOnLocal) { Credentials = new System.Net.NetworkCredential(strUsername, strPassword) };
        }
        catch (UriFormatException ex)
        {
            throw new ProxyConfigurationErrorException("There was an error setting up the proxy for this class instance.", ex);
        }
    }

    /// <summary>Returns the last Exception that occurred within this Class instance.</summary>
    /// <returns>An Exception Object.</returns>
    public Exception GetLastException
    {
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
    public Delegate SetDownloadStatusUpdateRoutine
    {
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
    public Func<string, string> SetURLPreProcessor
    {
        set { urlPreProcessor = value; }
    }

    /// <summary>This wipes out most of the data in this Class instance. Once you have called this function it's recommended to set the name of your class instance to Nothing. For example... httpHelper = Nothing</summary>
    public void Dispose()
    {
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
    public string GetLastAccessedURL
    {
        get { return lastAccessedURL; }
    }

    /// <summary>Tells the Class instance if it should use the system proxy.</summary>
    public bool UseSystemProxy
    {
        set { boolUseSystemProxy = value; }
    }

    /// <summary>This function allows you to get a peek inside the Class object instance. It returns many of the things that make up the Class instance like POST and GET data, cookies, additional HTTP headers, if proxy mode and HTTP compression mode is enabled, the user agent string, etc.</summary>
    /// <returns>A String.</returns>
    public override string ToString()
    {
        System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder();
        stringBuilder.AppendLine("--== HTTPHelper Class Object ==--");
        stringBuilder.AppendLine("--== Version: " + classVersion + " ==--");
        stringBuilder.AppendLine();
        stringBuilder.AppendLine("Last Accessed URL: " + lastAccessedURL);
        stringBuilder.AppendLine();

        if (getData.Count != 0)
        {
            foreach (KeyValuePair<string, string> item in getData)
            {
                stringBuilder.AppendLine("GET Data | " + item.Key + "=" + item.Value);
            }
        }

        if (postData.Count != 0)
        {
            foreach (KeyValuePair<string, object> item in postData)
            {
                stringBuilder.AppendLine("POST Data | " + item.Key.ToString() + "=" + item.Value.ToString());
            }
        }

        if (httpCookies.Count != 0)
        {
            foreach (KeyValuePair<string, CookieDetails> item in httpCookies)
            {
                stringBuilder.AppendLine("COOKIES | " + item.Key.ToString() + "=" + item.Value.cookieData);
            }
        }

        if (additionalHTTPHeaders.Count != 0)
        {
            foreach (KeyValuePair<string, string> item in additionalHTTPHeaders)
            {
                stringBuilder.AppendLine("Additional HTTP Header | " + item.Key.ToString() + "=" + item.Value);
            }
        }

        stringBuilder.AppendLine();

        stringBuilder.AppendLine("User Agent String: " + strUserAgentString);
        stringBuilder.AppendLine("Use HTTP Compression: " + boolUseHTTPCompression.ToString());
        stringBuilder.AppendLine("HTTP Time Out: " + httpTimeOut);
        stringBuilder.AppendLine("Use Proxy: " + boolUseProxy.ToString());

        if (credentials == null) stringBuilder.AppendLine("HTTP Authentication Enabled: False");
        else
        {
            stringBuilder.AppendLine("HTTP Authentication Enabled: True");
            stringBuilder.AppendLine("HTTP Authentication Details: " + credentials.strUser + "|" + credentials.strPassword);
        }

        if (lastException != null)
        {
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("--== Raw Exception Data ==--");
            stringBuilder.AppendLine(lastException.ToString());

            if (lastException is System.Net.WebException exception)
            {
                stringBuilder.AppendLine("Raw Exception Status Code: " + exception.Status.ToString());
            }
        }

        return stringBuilder.ToString().Trim();
    }

    /// <summary>Gets the remote file size.</summary>
    /// <param name="boolHumanReadable">Optional setting, normally set to True. Tells the function if it should transform the Integer representing the file size into a human readable format.</param>
    /// <returns>Either a String or a Long containing the remote file size.</returns>
    public object GetHTTPDownloadRemoteFileSize(bool boolHumanReadable = true)
    {
        if (boolHumanReadable) return FileSizeToHumanReadableFormat(remoteFileSize);
        else return remoteFileSize;
    }

    /// <summary>This returns the SSL certificate details for the last HTTP request made by this Class instance.</summary>
    /// <returns>System.Security.Cryptography.X509Certificates.X509Certificate2</returns>
    /// <exception cref="noSSLCertificateFoundException">If this function throws a noSSLCertificateFoundException it means that the Class doesn't have an SSL certificate in the memory space of the Class instance. Perhaps the last HTTP request wasn't an HTTPS request.</exception>
    /// <param name="boolThrowException">An optional parameter that tells the function if it should throw an exception if an SSL certificate isn't found in the memory space of this Class instance.</param>
    public System.Security.Cryptography.X509Certificates.X509Certificate2 GetCertificateDetails(bool boolThrowException = true)
    {
        if (sslCertificate == null)
        {
            if (boolThrowException)
            {
                lastException = new NoSSLCertificateFoundException("No valid SSL certificate found for the last HTTP request. Perhaps the last HTTP request wasn't an HTTPS request.");
                throw lastException;
            }
            return null;
        }
        else return sslCertificate;
    }

    /// <summary>Gets the current local file's size.</summary>
    /// <param name="boolHumanReadable">Optional setting, normally set to True. Tells the function if it should transform the Integer representing the file size into a human readable format.</param>
    /// <returns>Either a String or a Long containing the current local file's size.</returns>
    public object GetHTTPDownloadLocalFileSize(bool boolHumanReadable = true)
    {
        if (boolHumanReadable) return FileSizeToHumanReadableFormat(currentFileSize);
        else return currentFileSize;
    }

    /// <summary>Creates a new instance of the HTTPPost Class. You will need to set things up for the Class instance using the setProxyMode() and setUserAgent() routines.</summary>
    /// <example>Dim httpPostObject As New Tom.HTTPPost()</example>
    public HTTPHelper() { }

    /// <summary>Creates a new instance of the HTTPPost Class with some required parameters.</summary>
    /// <param name="strUserAgentStringIN">This set the User Agent String for the HTTP Request.</param>
    /// <example>Dim httpPostObject As New Tom.HTTPPost("Microsoft .NET")</example>
    public HTTPHelper(string strUserAgentStringIN)
    {
        strUserAgentString = strUserAgentStringIN;
    }

    /// <summary>Creates a new instance of the HTTPPost Class with some required parameters.</summary>
    /// <param name="strUserAgentStringIN">This set the User Agent String for the HTTP Request.</param>
    /// <param name="boolUseProxyIN">This tells the Class if you're going to be using a Proxy or not.</param>
    /// <example>Dim httpPostObject As New Tom.HTTPPost("Microsoft .NET", True)</example>
    public HTTPHelper(string strUserAgentStringIN, bool boolUseProxyIN)
    {
        strUserAgentString = strUserAgentStringIN;
        boolUseProxy = boolUseProxyIN;
    }

    /// <summary>Tells the HTTPPost Class if you want to use a Proxy or not.</summary>
    public bool SetProxyMode
    {
        set { boolUseProxy = value; }
    }

    /// <summary>Sets a timeout for any HTTP requests in this Class. Normally it's set for 5 seconds. The input is the amount of time in seconds (NOT milliseconds) that you want your HTTP requests to timeout in. The class will translate the seconds to milliseconds for you.</summary>
    /// <value>The amount of time in seconds (NOT milliseconds) that you want your HTTP requests to timeout in. This function will translate the seconds to milliseconds for you.</value>
    public short SetHTTPTimeout
    {
        set { httpTimeOut = value * 1000; }
    }

    /// <summary>Tells this Class instance if it should use HTTP compression for transport. Using HTTP Compression can save bandwidth. Normally the Class is setup to use HTTP Compression by default.</summary>
    /// <value>Boolean value.</value>
    public bool UseHTTPCompression
    {
        set { boolUseHTTPCompression = value; }
    }

    /// <summary>Sets the User Agent String to be used by the HTTPPost Class.</summary>
    /// <value>Your User Agent String.</value>
    public string SetUserAgent
    {
        set { strUserAgentString = value; }
    }

    /// <summary>This adds a String variable to your POST data.</summary>
    /// <param name="strName">The form name of the data to post.</param>
    /// <param name="strValue">The value of the data to post.</param>
    /// <param name="throwExceptionIfDataAlreadyExists">This tells the function if it should throw an exception if the data already exists in the POST data.</param>
    /// <exception cref="dataAlreadyExistsException">If this function throws a dataAlreadyExistsException, you forgot to add some data for your POST variable.</exception>
    public void AddPOSTData(string strName, string strValue, bool throwExceptionIfDataAlreadyExists = false)
    {
        if (string.IsNullOrEmpty(strValue.Trim()))
        {
            lastException = new DataMissingException(string.Format("Data was missing for the {0}{1}{0} POST variable.", "\"", strName));
            throw lastException;
        }

        if (postData.ContainsKey(strName) & throwExceptionIfDataAlreadyExists)
        {
            lastException = new DataAlreadyExistsException(string.Format("The POST data key named {0}{1}{0} already exists in the POST data.", "\"", strName));
            throw lastException;
        }
        else
        {
            postData.Remove(strName);
            postData.Add(strName, strValue);
        }
    }

    /// <summary>This adds a String variable to your GET data.</summary>
    /// <param name="strName">The form name of the data to post.</param>
    /// <param name="strValue">The value of the data to post.</param>
    /// <exception cref="dataAlreadyExistsException">If this function throws a dataAlreadyExistsException, you forgot to add some data for your POST variable.</exception>
    public void AddGETData(string strName, string strValue, bool throwExceptionIfDataAlreadyExists = false)
    {
        if (string.IsNullOrEmpty(strValue.Trim()))
        {
            lastException = new DataMissingException(string.Format("Data was missing for the {0}{1}{0} GET variable.", "\"", strName));
            throw lastException;
        }

        if (getData.ContainsKey(strName) & throwExceptionIfDataAlreadyExists)
        {
            lastException = new DataAlreadyExistsException(string.Format("The GET data key named {0}{1}{0} already exists in the GET data.", "\"", strName));
            throw lastException;
        }
        else
        {
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
    public void AddHTTPHeader(string strHeaderName, string strHeaderContents, bool urlEncodeHeaderContent = false)
    {
        if (!DoesAdditionalHeaderExist(strHeaderName))
        {
            additionalHTTPHeaders.Add(strHeaderName.ToLower(), urlEncodeHeaderContent ? System.Web.HttpUtility.UrlEncode(strHeaderContents) : strHeaderContents);
        }
        else
        {
            lastException = new DataAlreadyExistsException(string.Format("The additional HTTP Header named {0}{1}{0} already exists in the Additional HTTP Headers settings for this Class instance.", "\"", strHeaderName));
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
    public void AddHTTPCookie(string strCookieName, string strCookieValue, string strDomainDomain, string strCookiePath, bool urlEncodeHeaderContent = false)
    {
        if (!DoesCookieExist(strCookieName))
        {
            CookieDetails cookieDetails = new CookieDetails
            {
                cookieDomain = strDomainDomain,
                cookiePath = strCookiePath,
                cookieData = urlEncodeHeaderContent ? System.Web.HttpUtility.UrlEncode(strCookieValue) : strCookieValue
            };

            httpCookies.Add(strCookieName.ToLower(), cookieDetails);
        }
        else
        {
            lastException = new DataAlreadyExistsException(string.Format("The HTTP Cookie named {0}{1}{0} already exists in the settings for this Class instance.", "\"", strCookieName));
            throw lastException;
        }
    }

    /// <summary>Allows you to add HTTP cookies to your HTTP Request with a default path of "/".</summary>
    /// <param name="strCookieName">The name of your cookie.</param>
    /// <param name="strCookieValue">The value for your cookie.</param>
    /// <param name="strCookieDomain">The domain for the cookie.</param>
    /// <param name="urlEncodeHeaderContent">Optional setting, normally set to False. Tells the function if it should URLEncode the cookie contents before setting it.</param>
    /// <exception cref="dataAlreadyExistsException">If this function throws a dataAlreadyExistsException, it means that the cookie already exists in this Class instance.</exception>
    public void AddHTTPCookie(string strCookieName, string strCookieValue, string strCookieDomain, bool urlEncodeHeaderContent = false)
    {
        if (!DoesCookieExist(strCookieName))
        {
            CookieDetails cookieDetails = new CookieDetails
            {
                cookieDomain = strCookieDomain,
                cookiePath = "/",
                cookieData = urlEncodeHeaderContent ? System.Web.HttpUtility.UrlEncode(strCookieValue) : strCookieValue
            };

            httpCookies.Add(strCookieName.ToLower(), cookieDetails);
        }
        else
        {
            lastException = new DataAlreadyExistsException(string.Format("The HTTP Cookie named {0}{1}{0} already exists in the settings for this Class instance.", "\"", strCookieName));
            throw lastException;
        }
    }

    /// <summary>Checks to see if the GET data key exists in this GET data.</summary>
    /// <param name="strName">The name of the GET data variable you are checking the existance of.</param>
    /// <returns></returns>
    public bool DoesGETDataExist(string strName)
    {
        return getData.ContainsKey(strName);
    }

    /// <summary>Checks to see if the POST data key exists in this POST data.</summary>
    /// <param name="strName">The name of the POST data variable you are checking the existance of.</param>
    /// <returns></returns>
    public bool DoesPOSTDataExist(string strName)
    {
        return postData.ContainsKey(strName);
    }

    /// <summary>Checks to see if an additional HTTP Request Header has been added to the Class.</summary>
    /// <param name="strHeaderName">The name of the HTTP Request Header to check the existance of.</param>
    /// <returns>Boolean value; True if found, False if not found.</returns>
    public bool DoesAdditionalHeaderExist(string strHeaderName)
    {
        return additionalHTTPHeaders.ContainsKey(strHeaderName.ToLower());
    }

    /// <summary>Checks to see if a cookie has been added to the Class.</summary>
    /// <param name="strCookieName">The name of the cookie to check the existance of.</param>
    /// <returns>Boolean value; True if found, False if not found.</returns>
    public bool DoesCookieExist(string strCookieName)
    {
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
    public void AddFileUpload(string strFormName, string strLocalFilePath, string strRemoteFileName, string strContentType, bool throwExceptionIfItemAlreadyExists = false)
    {
        FileInfo fileInfo = new FileInfo(strLocalFilePath);

        if (!fileInfo.Exists)
        {
            lastException = new FileNotFoundException("Local file not found.", strLocalFilePath);
            throw lastException;
        }
        else if (postData.ContainsKey(strFormName))
        {
            if (throwExceptionIfItemAlreadyExists)
            {
                lastException = new DataAlreadyExistsException(string.Format("The POST data key named {0}{1}{0} already exists in the POST data.", "\"", strFormName));
                throw lastException;
            }
            else return;
        }
        else
        {
            FormFile formFileInstance = new FormFile()
            {
                FormName = strFormName,
                LocalFilePath = strLocalFilePath,
                RemoteFileName = strRemoteFileName
            };
            if (string.IsNullOrEmpty(strContentType))
            {
                Microsoft.Win32.RegistryKey regPath = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(fileInfo.Extension.ToLower(), false);

                string contentType;
                if (regPath == null)
                {
                    lastException = new NoMimeTypeFoundException("No MIME Type found for " + fileInfo.Extension.ToLower());
                    throw lastException;
                }
                else contentType = regPath.GetValue("Content Type", null).ToString();

                if (string.IsNullOrEmpty(contentType))
                {
                    lastException = new NoMimeTypeFoundException("No MIME Type found for " + fileInfo.Extension.ToLower());
                    throw lastException;
                }
                else formFileInstance.ContentType = contentType;
            }
            else formFileInstance.ContentType = strContentType;

            postData.Add(strFormName, formFileInstance);
        }
    }

    /// <summary>Gets the HTTP Response Headers that were returned by the HTTP Server after the HTTP request.</summary>
    /// <param name="throwExceptionIfNoHeaders">Optional setting, normally set to False. Tells the function if it should throw an exception if no HTTP Response Headers are contained in this Class instance.</param>
    /// <returns>A collection of HTTP Response Headers in a Net.WebHeaderCollection object.</returns>
    /// <exception cref="noHTTPServerResponseHeadersFoundException">If this function throws a noHTTPServerResponseHeadersFoundException, there are no HTTP Response Headers in this Class instance.</exception>
    public System.Net.WebHeaderCollection GetHTTPResponseHeaders(bool throwExceptionIfNoHeaders = false)
    {
        if (httpResponseHeaders == null)
        {
            if (throwExceptionIfNoHeaders)
            {
                lastException = new NoHTTPServerResponseHeadersFoundException("No HTTP Server Response Headers found.");
                throw lastException;
            }
            else return null;
        }
        else return httpResponseHeaders;
    }

    /// <summary>Gets the percentage of the file that's being downloaded from the HTTP Server.</summary>
    /// <returns>Returns a Short Integer value.</returns>
    public short GetHTTPDownloadProgressPercentage
    {
        get { return httpDownloadProgressPercentage; }
    }

    /// <summary>This tells the current HTTPHelper Class Instance if it should run the download update status routine in a separate thread. By default this is enabled.</summary>
    public bool EnableMultiThreadedDownloadStatusUpdates
    {
        get { return boolRunDownloadStatusUpdatePluginInSeparateThread; }
        set { boolRunDownloadStatusUpdatePluginInSeparateThread = value; }
    }

    /// <summary>Sets the amount of time in miliseconds that the download status updating thread sleeps. The default is 1000 ms or 1 second, perfect for calculating the amount of data downloaded per second.</summary>
    public int IntDownloadThreadSleepTime
    {
        set { _intDownloadThreadSleepTime = value; }
    }

    private void DownloadStatusUpdaterThreadSubroutine()
    {
        try
        {
        beginAgain:
            downloadStatusUpdater.DynamicInvoke(downloadStatusDetails);
            System.Threading.Thread.Sleep(_intDownloadThreadSleepTime);
            goto beginAgain;
        }
        catch (System.Threading.ThreadAbortException) { }
        catch (System.Reflection.TargetInvocationException) { }
    }

    /// <summary>This subroutine is used by the downloadFile function to update the download status of the file that's being downloaded by the class instance.</summary>
    private void DownloadStatusUpdateInvoker()
    {
        downloadStatusDetails = new DownloadStatusDetails
        {
            remoteFileSize = remoteFileSize,
            percentageDownloaded = httpDownloadProgressPercentage,
            localFileSize = currentFileSize
        };
        // Update the downloadStatusDetails.

        // Checks to see if we have a status update routine to invoke.
        if (downloadStatusUpdater != null)
        {
            // We invoke the status update routine if we have one to invoke. This is usually injected
            // into the class instance by the programmer who's using this class in his/her program.
            if (boolRunDownloadStatusUpdatePluginInSeparateThread)
            {
                if (downloadStatusUpdaterThread == null)
                {
                    downloadStatusUpdaterThread = new System.Threading.Thread(DownloadStatusUpdaterThreadSubroutine)
                    {
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

    private void AbortDownloadStatusUpdaterThread()
    {
        try
        {
            if (downloadStatusUpdaterThread != null & boolRunDownloadStatusUpdatePluginInSeparateThread)
            {
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
    public bool DownloadFile(string fileDownloadURL, ref MemoryStream memStream, bool throwExceptionIfError = true)
    {
        System.Net.HttpWebRequest httpWebRequest = null;
        currentFileSize = 0;
        double amountDownloaded;

        try
        {
            if (urlPreProcessor != null) fileDownloadURL = urlPreProcessor(fileDownloadURL);
            lastAccessedURL = fileDownloadURL;

            // We create a new data buffer to hold the stream of data from the web server.
            byte[] dataBuffer = new byte[intDownloadBufferSize + 1];

            httpWebRequest = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(fileDownloadURL);

            ConfigureProxy(ref httpWebRequest);
            AddParametersToWebRequest(ref httpWebRequest);

            System.Net.WebResponse webResponse = httpWebRequest.GetResponse();
            // We now get the web response.
            CaptureSSLInfo(fileDownloadURL, ref httpWebRequest);

            // Gets the size of the remote file on the web server.
            remoteFileSize = (long)webResponse.ContentLength;

            Stream responseStream = webResponse.GetResponseStream();
            // Gets the response stream.

            long lngBytesReadFromInternet = (long)responseStream.Read(dataBuffer, 0, dataBuffer.Length);
            // Reads some data from the HTTP stream into our data buffer.

            // We keep looping until all of the data has been downloaded.
            while (lngBytesReadFromInternet != 0)
            {
                // We calculate the current file size by adding the amount of data that we've so far
                // downloaded from the server repeatedly to a variable called "currentFileSize".
                currentFileSize += lngBytesReadFromInternet;

                memStream.Write(dataBuffer, 0, (int)lngBytesReadFromInternet);
                // Writes the data directly to disk.

                amountDownloaded = currentFileSize / remoteFileSize * 100;
                httpDownloadProgressPercentage = (short)Math.Round(amountDownloaded, 0);
                // Update the download percentage value.
                DownloadStatusUpdateInvoker();

                lngBytesReadFromInternet = (long)responseStream.Read(dataBuffer, 0, dataBuffer.Length);
                // Reads more data into our data buffer.
            }

            // Before we return the MemoryStream to the user we have to reset the position back to the beginning of the Stream. This is so that when the
            // user processes the IO.MemoryStream that's returned as part of this function the IO.MemoryStream will be ready to write the data out of
            // memory and into whatever stream the user wants to write the data out to. If this isn't done and the user executes the CopyTo() function
            // on the IO.MemoryStream Object the user will have nothing written out because the IO.MemoryStream will be at the end of the stream.
            memStream.Position = 0;

            AbortDownloadStatusUpdaterThread();

            return true;
        }
        catch (System.Threading.ThreadAbortException)
        {
            AbortDownloadStatusUpdaterThread();
            if (httpWebRequest != null) httpWebRequest.Abort();
            if (memStream != null) memStream.Dispose(); // Disposes the file stream.
            return false;
        }
        catch (Exception ex)
        {
            AbortDownloadStatusUpdaterThread();

            lastException = ex;
            if (memStream != null) memStream.Dispose(); // Disposes the file stream.

            if (!throwExceptionIfError) return false;

            if (customErrorHandler != null)
            {
                customErrorHandler.DynamicInvoke(ex, this);
                // Since we handled the exception with an injected custom error handler, we can now exit the function with the return of a False value.
                return false;
            }

            if (ex is System.Net.WebException)
            {
                System.Net.WebException ex2 = (System.Net.WebException)ex;

                if (ex2.Status == System.Net.WebExceptionStatus.ProtocolError)
                {
                    throw HandleWebExceptionProtocolError(fileDownloadURL, ex2);
                }
                else if (ex2.Status == System.Net.WebExceptionStatus.TrustFailure)
                {
                    lastException = new SSLErrorException("There was an error establishing an SSL connection.", ex2);
                    throw lastException;
                }
                else if (ex2.Status == System.Net.WebExceptionStatus.NameResolutionFailure)
                {
                    string strDomainName = System.Text.RegularExpressions.Regex.Match(lastAccessedURL, "(?:http(?:s){0,1}://){0,1}(.*)/", System.Text.RegularExpressions.RegexOptions.Singleline).Groups[1].Value;
                    lastException = new DNSLookupError(string.Format("There was an error while looking up the DNS records for the domain name {0}{1}{0}.", "\"", strDomainName), ex2);
                    throw lastException;
                }

                lastException = new System.Net.WebException(ex.Message, ex2);
                throw lastException;
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
    public bool DownloadFile(string fileDownloadURL, string localFileName, bool throwExceptionIfLocalFileExists, bool throwExceptionIfError = true)
    {
        FileStream fileWriteStream = null;
        System.Net.HttpWebRequest httpWebRequest = null;
        currentFileSize = 0;
        double amountDownloaded;

        try
        {
            if (urlPreProcessor != null) fileDownloadURL = urlPreProcessor(fileDownloadURL);
            lastAccessedURL = fileDownloadURL;

            if (File.Exists(localFileName))
            {
                if (throwExceptionIfLocalFileExists)
                {
                    lastException = new LocalFileAlreadyExistsException(string.Format("The local file found at {0}{1}{0} already exists.", "\"", localFileName));
                    throw lastException;
                }
                else File.Delete(localFileName);
            }

            // We create a new data buffer to hold the stream of data from the web server.
            byte[] dataBuffer = new byte[intDownloadBufferSize + 1];

            httpWebRequest = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(fileDownloadURL);

            ConfigureProxy(ref httpWebRequest);
            AddParametersToWebRequest(ref httpWebRequest);

            System.Net.WebResponse webResponse = httpWebRequest.GetResponse();
            // We now get the web response.
            CaptureSSLInfo(fileDownloadURL, ref httpWebRequest);

            // Gets the size of the remote file on the web server.
            remoteFileSize = (long)webResponse.ContentLength;

            Stream responseStream = webResponse.GetResponseStream();
            // Gets the response stream.
            fileWriteStream = new FileStream(localFileName, FileMode.Create);
            // Creates a file write stream.

            long lngBytesReadFromInternet = (long)responseStream.Read(dataBuffer, 0, dataBuffer.Length);
            // Reads some data from the HTTP stream into our data buffer.

            // We keep looping until all of the data has been downloaded.
            while (lngBytesReadFromInternet != 0)
            {
                // We calculate the current file size by adding the amount of data that we've so far
                // downloaded from the server repeatedly to a variable called "currentFileSize".
                currentFileSize += lngBytesReadFromInternet;

                fileWriteStream.Write(dataBuffer, 0, (int)lngBytesReadFromInternet);
                // Writes the data directly to disk.

                amountDownloaded = currentFileSize / remoteFileSize * 100;
                httpDownloadProgressPercentage = (short)Math.Round(amountDownloaded, 0);
                // Update the download percentage value.
                DownloadStatusUpdateInvoker();

                lngBytesReadFromInternet = (long)responseStream.Read(dataBuffer, 0, dataBuffer.Length);
                // Reads more data into our data buffer.
            }

            fileWriteStream.Dispose();
            // Disposes the file stream.

            if (downloadStatusUpdaterThread != null & boolRunDownloadStatusUpdatePluginInSeparateThread)
            {
                downloadStatusUpdaterThread.Abort();
                downloadStatusUpdaterThread = null;
            }

            AbortDownloadStatusUpdaterThread();

            return true;
        }
        catch (System.Threading.ThreadAbortException)
        {
            AbortDownloadStatusUpdaterThread();
            if (httpWebRequest != null) httpWebRequest.Abort();
            if (fileWriteStream != null) fileWriteStream.Dispose(); // Disposes the file stream.
            return false;
        }
        catch (Exception ex)
        {
            AbortDownloadStatusUpdaterThread();

            lastException = ex;
            if (fileWriteStream != null)
            {
                fileWriteStream.Close();
                // Closes the file stream.
                fileWriteStream.Dispose();
                // Disposes the file stream.
            }

            if (!throwExceptionIfError) return false;

            if (customErrorHandler != null)
            {
                customErrorHandler.DynamicInvoke(ex, this);
                // Since we handled the exception with an injected custom error handler, we can now exit the function with the return of a False value.
                return false;
            }

            if (ex is System.Net.WebException)
            {
                System.Net.WebException ex2 = (System.Net.WebException)ex;

                if (ex2.Status == System.Net.WebExceptionStatus.ProtocolError)
                {
                    throw HandleWebExceptionProtocolError(fileDownloadURL, ex2);
                }
                else if (ex2.Status == System.Net.WebExceptionStatus.TrustFailure)
                {
                    lastException = new SSLErrorException("There was an error establishing an SSL connection.", ex2);
                    throw lastException;
                }
                else if (ex2.Status == System.Net.WebExceptionStatus.NameResolutionFailure)
                {
                    string strDomainName = System.Text.RegularExpressions.Regex.Match(lastAccessedURL, "(?:http(?:s){0,1}://){0,1}(.*)/", System.Text.RegularExpressions.RegexOptions.Singleline).Groups[1].Value;
                    lastException = new DNSLookupError(string.Format("There was an error while looking up the DNS records for the domain name {0}{1}{0}.", "\"", strDomainName), ex2);
                    throw lastException;
                }

                lastException = new System.Net.WebException(ex.Message, ex2);
                throw lastException;
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
    public bool GetWebData(string url, ref string httpResponseText, short shortRangeFrom, short shortRangeTo, bool throwExceptionIfError = true)
    {
        System.Net.HttpWebRequest httpWebRequest = null;

        try
        {
            if (urlPreProcessor != null) url = urlPreProcessor(url);
            lastAccessedURL = url;

            if (getData.Count != 0) url += "?" + GetGETDataString();

            httpWebRequest = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(url);
            httpWebRequest.AddRange(shortRangeFrom, shortRangeTo);

            ConfigureProxy(ref httpWebRequest);
            AddParametersToWebRequest(ref httpWebRequest);
            AddPostDataToWebRequest(ref httpWebRequest);

            System.Net.WebResponse httpWebResponse = httpWebRequest.GetResponse();
            CaptureSSLInfo(url, ref httpWebRequest);

            StreamReader httpInStream = new StreamReader(httpWebResponse.GetResponseStream());
            string httpTextOutput = httpInStream.ReadToEnd().Trim();
            httpResponseHeaders = httpWebResponse.Headers;

            httpInStream.Dispose();

            httpWebResponse.Close();
            httpWebResponse = null;
            httpWebRequest = null;

            httpResponseText = ConvertLineFeeds(httpTextOutput).Trim();
            strLastHTTPServerResponse = httpResponseText;

            return true;
        }
        catch (Exception ex)
        {
            if (ex is System.Threading.ThreadAbortException)
            {
                if (httpWebRequest != null) httpWebRequest.Abort();
                return false;
            }

            lastException = ex;
            if (!throwExceptionIfError) return false;

            if (customErrorHandler != null)
            {
                customErrorHandler.DynamicInvoke(ex, this);
                // Since we handled the exception with an injected custom error handler, we can now exit the function with the return of a False value.
                return false;
            }

            if (ex is System.Net.WebException)
            {
                System.Net.WebException ex2 = (System.Net.WebException)ex;

                if (ex2.Status == System.Net.WebExceptionStatus.ProtocolError)
                {
                    throw HandleWebExceptionProtocolError(url, ex2);
                }
                else if (ex2.Status == System.Net.WebExceptionStatus.TrustFailure)
                {
                    lastException = new SSLErrorException("There was an error establishing an SSL connection.", ex2);
                    throw lastException;
                }
                else if (ex2.Status == System.Net.WebExceptionStatus.NameResolutionFailure)
                {
                    string strDomainName = System.Text.RegularExpressions.Regex.Match(lastAccessedURL, "(?:http(?:s){0,1}://){0,1}(.*)/", System.Text.RegularExpressions.RegexOptions.Singleline).Groups[1].Value;
                    lastException = new DNSLookupError(string.Format("There was an error while looking up the DNS records for the domain name {0}{1}{0}.", "\"", strDomainName), ex2);
                    throw lastException;
                }

                lastException = new System.Net.WebException(ex.Message, ex2);
                throw lastException;
            }

            lastException = new Exception(ex.Message, ex);
            throw lastException;
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
    public bool GetWebData(string url, ref string httpResponseText, bool throwExceptionIfError = true)
    {
        System.Net.HttpWebRequest httpWebRequest = null;

        try
        {
            if (urlPreProcessor != null) url = urlPreProcessor(url);
            lastAccessedURL = url;

            if (getData.Count != 0) url += "?" + GetGETDataString();

            httpWebRequest = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(url);

            ConfigureProxy(ref httpWebRequest);
            AddParametersToWebRequest(ref httpWebRequest);
            AddPostDataToWebRequest(ref httpWebRequest);

            System.Net.WebResponse httpWebResponse = httpWebRequest.GetResponse();
            CaptureSSLInfo(url, ref httpWebRequest);

            StreamReader httpInStream = new StreamReader(httpWebResponse.GetResponseStream());
            string httpTextOutput = httpInStream.ReadToEnd().Trim();
            httpResponseHeaders = httpWebResponse.Headers;

            httpInStream.Dispose();

            httpWebResponse.Close();
            httpWebResponse = null;
            httpWebRequest = null;

            httpResponseText = ConvertLineFeeds(httpTextOutput).Trim();
            strLastHTTPServerResponse = httpResponseText;

            return true;
        }
        catch (Exception ex)
        {
            if (ex is System.Threading.ThreadAbortException)
            {
                if (httpWebRequest != null) httpWebRequest.Abort();
                return false;
            }

            lastException = ex;
            if (!throwExceptionIfError) return false;

            if (customErrorHandler != null)
            {
                customErrorHandler.DynamicInvoke(ex, this);
                // Since we handled the exception with an injected custom error handler, we can now exit the function with the return of a False value.
                return false;
            }

            if (ex is System.Net.WebException)
            {
                System.Net.WebException ex2 = (System.Net.WebException)ex;

                if (ex2.Status == System.Net.WebExceptionStatus.ProtocolError)
                {
                    throw HandleWebExceptionProtocolError(url, ex2);
                }
                else if (ex2.Status == System.Net.WebExceptionStatus.TrustFailure)
                {
                    lastException = new SSLErrorException("There was an error establishing an SSL connection.", ex2);
                    throw lastException;
                }
                else if (ex2.Status == System.Net.WebExceptionStatus.NameResolutionFailure)
                {
                    string strDomainName = System.Text.RegularExpressions.Regex.Match(lastAccessedURL, "(?:http(?:s){0,1}://){0,1}(.*)/", System.Text.RegularExpressions.RegexOptions.Singleline).Groups[1].Value;
                    lastException = new DNSLookupError(string.Format("There was an error while looking up the DNS records for the domain name {0}{1}{0}.", "\"", strDomainName), ex2);
                    throw lastException;
                }

                lastException = new System.Net.WebException(ex.Message, ex2);
                throw lastException;
            }

            lastException = new Exception(ex.Message, ex);
            throw lastException;
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
    public bool UploadData(string url, ref string httpResponseText, bool throwExceptionIfError = false)
    {
        System.Net.HttpWebRequest httpWebRequest = null;

        try
        {
            if (urlPreProcessor != null) url = urlPreProcessor(url);
            lastAccessedURL = url;

            if (postData.Count == 0)
            {
                lastException = new DataMissingException("Your HTTP Request contains no POST data. Please add some data to POST before calling this function.");
                throw lastException;
            }
            if (getData.Count != 0) url += "?" + GetGETDataString();

            string boundary = "---------------------------" + DateTime.Now.Ticks.ToString("x");
            byte[] boundaryBytes = System.Text.Encoding.ASCII.GetBytes(Convert.ToString(strCRLF + "--") + boundary + strCRLF);

            httpWebRequest = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(url);

            ConfigureProxy(ref httpWebRequest);
            AddParametersToWebRequest(ref httpWebRequest);

            httpWebRequest.KeepAlive = true;
            httpWebRequest.ContentType = "multipart/form-data; boundary=" + boundary;
            httpWebRequest.Method = "POST";

            if (postData.Count != 0)
            {
                Stream httpRequestWriter = httpWebRequest.GetRequestStream();
                string header = null;
                FileInfo fileInfo = default;
                FormFile formFileObjectInstance = null;
                byte[] bytes = null;
                byte[] buffer = null;
                FileStream fileStream = default;
                string data = null;

                foreach (KeyValuePair<string, object> entry in postData)
                {
                    httpRequestWriter.Write(boundaryBytes, 0, boundaryBytes.Length);

                    if (entry.Value is FormFile file)
                    {
                        formFileObjectInstance = file;

                        if (string.IsNullOrEmpty(formFileObjectInstance.RemoteFileName))
                        {
                            fileInfo = new FileInfo(formFileObjectInstance.LocalFilePath);

                            header = string.Format("Content-Disposition: form-data; name={0}{1}{0}; filename={0}{2}{0}", "\"", entry.Key, fileInfo.Name);
                            header += strCRLF + "Content-Type: " + formFileObjectInstance.ContentType + strCRLF + strCRLF;
                        }
                        else
                        {
                            header = string.Format("Content-Disposition: form-data; name={0}{1}{0}; filename={0}{2}{0}", "\"", entry.Key, formFileObjectInstance.RemoteFileName);
                            header += strCRLF + "Content-Type: " + formFileObjectInstance.ContentType + strCRLF + strCRLF;
                        }

                        bytes = System.Text.Encoding.UTF8.GetBytes(header);
                        httpRequestWriter.Write(bytes, 0, bytes.Length);

                        fileStream = new FileStream(formFileObjectInstance.LocalFilePath, FileMode.Open);
                        buffer = new byte[32769];

                        while (fileStream.Read(buffer, 0, buffer.Length) != 0)
                        {
                            httpRequestWriter.Write(buffer, 0, buffer.Length);
                        }

                        fileStream.Dispose();
                        fileStream = null;
                    }
                    else
                    {
                        data = string.Format("Content-Disposition: form-data; name={0}{1}{0}{2}{2}{3}", "\"", entry.Key, strCRLF, entry.Value);
                        bytes = System.Text.Encoding.UTF8.GetBytes(data);
                        httpRequestWriter.Write(bytes, 0, bytes.Length);
                    }
                }

                byte[] trailer = System.Text.Encoding.ASCII.GetBytes(strCRLF + "--" + boundary + "--" + strCRLF);
                httpRequestWriter.Write(trailer, 0, trailer.Length);
                httpRequestWriter.Close();
            }

            System.Net.WebResponse httpWebResponse = httpWebRequest.GetResponse();
            CaptureSSLInfo(url, ref httpWebRequest);

            StreamReader httpInStream = new StreamReader(httpWebResponse.GetResponseStream());
            string httpTextOutput = httpInStream.ReadToEnd().Trim();
            httpResponseHeaders = httpWebResponse.Headers;

            httpInStream.Dispose();

            httpWebResponse.Dispose();
            httpWebResponse = null;
            httpWebRequest = null;

            httpResponseText = ConvertLineFeeds(httpTextOutput).Trim();
            strLastHTTPServerResponse = httpResponseText;

            return true;
        }
        catch (Exception ex)
        {
            if (ex is System.Threading.ThreadAbortException)
            {
                if (httpWebRequest != null) httpWebRequest.Abort();
            }

            lastException = ex;
            if (!throwExceptionIfError) return false;

            if (customErrorHandler != null)
            {
                customErrorHandler.DynamicInvoke(ex, this);
                // Since we handled the exception with an injected custom error handler, we can now exit the function with the return of a False value.
                return false;
            }

            if (ex is System.Net.WebException)
            {
                System.Net.WebException ex2 = (System.Net.WebException)ex;

                if (ex2.Status == System.Net.WebExceptionStatus.ProtocolError)
                {
                    throw HandleWebExceptionProtocolError(url, ex2);
                }
                else if (ex2.Status == System.Net.WebExceptionStatus.TrustFailure)
                {
                    lastException = new SSLErrorException("There was an error establishing an SSL connection.", ex2);
                    throw lastException;
                }
                else if (ex2.Status == System.Net.WebExceptionStatus.NameResolutionFailure)
                {
                    string strDomainName = System.Text.RegularExpressions.Regex.Match(lastAccessedURL, "(?:http(?:s){0,1}://){0,1}(.*)/", System.Text.RegularExpressions.RegexOptions.Singleline).Groups[1].Value;
                    lastException = new DNSLookupError(string.Format("There was an error while looking up the DNS records for the domain name {0}{1}{0}.", "\"", strDomainName), ex2);
                    throw lastException;
                }

                lastException = new System.Net.WebException(ex.Message, ex2);
                throw lastException;
            }

            lastException = new Exception(ex.Message, ex);
            throw lastException;
        }
    }

    private void CaptureSSLInfo(string url, ref System.Net.HttpWebRequest httpWebRequest)
    {
        sslCertificate = url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ? new System.Security.Cryptography.X509Certificates.X509Certificate2(httpWebRequest.ServicePoint.Certificate) : null;
    }

    private void AddPostDataToWebRequest(ref System.Net.HttpWebRequest httpWebRequest)
    {
        if (postData.Count == 0) httpWebRequest.Method = "GET";
        else
        {
            httpWebRequest.Method = "POST";
            string postDataString = GetPOSTDataString();
            httpWebRequest.ContentType = "application/x-www-form-urlencoded";
            httpWebRequest.ContentLength = postDataString.Length;

            dynamic httpRequestWriter = new StreamWriter(httpWebRequest.GetRequestStream());
            httpRequestWriter.Write(postDataString);
            httpRequestWriter.Close();
            httpRequestWriter.Dispose();
        }
    }

    private void AddParametersToWebRequest(ref System.Net.HttpWebRequest httpWebRequest)
    {
        if (credentials != null)
        {
            httpWebRequest.PreAuthenticate = true;
            AddHTTPHeader("Authorization", "Basic " + Convert.ToBase64String(System.Text.Encoding.Default.GetBytes(credentials.strUser + ":" + credentials.strPassword)));
        }

        if (!string.IsNullOrWhiteSpace(strUserAgentString)) httpWebRequest.UserAgent = strUserAgentString;
        if (httpCookies.Count != 0) GetCookies(ref httpWebRequest);
        if (additionalHTTPHeaders.Count != 0) GetHeaders(ref httpWebRequest);

        if (boolUseHTTPCompression)
        {
            // We tell the web server that we can accept a GZIP and Deflate compressed data stream.
            httpWebRequest.Accept = "gzip, deflate";
            httpWebRequest.Headers.Add(System.Net.HttpRequestHeader.AcceptEncoding, "gzip, deflate");
            httpWebRequest.AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate;
        }

        httpWebRequest.Timeout = (int)httpTimeOut;
        httpWebRequest.KeepAlive = true;
    }

    private void GetCookies(ref System.Net.HttpWebRequest httpWebRequest)
    {
        System.Net.CookieContainer cookieContainer = new System.Net.CookieContainer();
        foreach (KeyValuePair<string, CookieDetails> entry in httpCookies)
        {
            cookieContainer.Add(new System.Net.Cookie(entry.Key, entry.Value.cookieData, entry.Value.cookiePath, entry.Value.cookieDomain));
        }
        httpWebRequest.CookieContainer = cookieContainer;
    }

    private void GetHeaders(ref System.Net.HttpWebRequest httpWebRequest)
    {
        foreach (KeyValuePair<string, string> entry in additionalHTTPHeaders)
        {
            httpWebRequest.Headers.Add(entry.Key.ToString(), entry.Value);
        }
    }

    private void ConfigureProxy(ref System.Net.HttpWebRequest httpWebRequest)
    {
        if (boolUseProxy)
        {
            if (boolUseSystemProxy) httpWebRequest.Proxy = System.Net.WebRequest.GetSystemWebProxy();
            else
            {
                httpWebRequest.Proxy = customProxy ?? System.Net.WebRequest.GetSystemWebProxy();
            }
        }
    }

    private string ConvertLineFeeds(string input)
    {
        // Checks to see if the file is in Windows linefeed format or UNIX linefeed format.
        if (input.Contains(strCRLF))
        {
            return input;
            // It's in Windows linefeed format so we return the output as is.
        }
        else
        {
            return input.Replace(strLF, strCRLF);
            // It's in UNIX linefeed format so we have to convert it to Windows before we return the output.
        }
    }

    private string GetPOSTDataString()
    {
        string postDataString = "";
        foreach (KeyValuePair<string, object> entry in postData)
        {
            if (!entry.Value.GetType().Equals(typeof(FormFile)))
            {
                postDataString += entry.Key.ToString().Trim() + "=" + System.Web.HttpUtility.UrlEncode((string)entry.Value) + "&";
            }
        }

        if (postDataString.EndsWith("&")) postDataString = postDataString.Substring(0, postDataString.Length - 1);

        return postDataString;
    }

    private string GetGETDataString()
    {
        string getDataString = "";
        foreach (KeyValuePair<string, string> entry in getData)
        {
            getDataString += entry.Key.ToString().Trim() + "=" + System.Web.HttpUtility.UrlEncode(entry.Value.ToString().Trim()) + "&";
        }

        if (getDataString.EndsWith("&")) getDataString = getDataString.Substring(0, getDataString.Length - 1);

        return getDataString;
    }

    private HTTPProtocolException HandleWebExceptionProtocolError(string url, System.Net.WebException ex)
    {
        HTTPProtocolException lastException;

        if (ex.Response is System.Net.HttpWebResponse httpErrorResponse)
        {
            if (httpErrorResponse.StatusCode == System.Net.HttpStatusCode.InternalServerError)
            {
                lastException = new HTTPProtocolException("HTTP Protocol Error (Server 500 Error) while accessing " + url, ex) { HTTPStatusCode = httpErrorResponse.StatusCode };
            }
            else if (httpErrorResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                lastException = new HTTPProtocolException("HTTP Protocol Error (404 File Not Found) while accessing " + url, ex) { HTTPStatusCode = httpErrorResponse.StatusCode };
            }
            else if (httpErrorResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                lastException = new HTTPProtocolException("HTTP Protocol Error (401 Unauthorized) while accessing " + url, ex) { HTTPStatusCode = httpErrorResponse.StatusCode };
            }
            else if (httpErrorResponse.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
            {
                lastException = new HTTPProtocolException("HTTP Protocol Error (503 Service Unavailable) while accessing " + url, ex) { HTTPStatusCode = httpErrorResponse.StatusCode };
            }
            else if (httpErrorResponse.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                lastException = new HTTPProtocolException("HTTP Protocol Error (403 Forbidden) while accessing " + url, ex) { HTTPStatusCode = httpErrorResponse.StatusCode };
            }
            else
            {
                lastException = new HTTPProtocolException("HTTP Protocol Error while accessing " + url, ex) { HTTPStatusCode = httpErrorResponse.StatusCode };
            }
        }
        else
        {
            lastException = new HTTPProtocolException("HTTP Protocol Error while accessing " + url, ex);
        }

        return lastException;
    }

    public string FileSizeToHumanReadableFormat(long size, bool roundToNearestWholeNumber = false)
    {
        short shortRoundNumber;
        if (roundToNearestWholeNumber) { shortRoundNumber = 0; } else { shortRoundNumber = 2; }

        string result;
        if (size <= Math.Pow(2, 10)) result = size + " Bytes";
        else if (size > Math.Pow(2, 10) & size <= Math.Pow(2, 20))
        {
            result = Math.Round(size / Math.Pow(2, 10), shortRoundNumber) + " KBs";
        }
        else if (size > Math.Pow(2, 20) & size <= Math.Pow(2, 30))
        {
            result = Math.Round(size / Math.Pow(2, 20), shortRoundNumber) + " MBs";
        }
        else if (size > Math.Pow(2, 30) & size <= Math.Pow(2, 40))
        {
            result = Math.Round(size / Math.Pow(2, 30), shortRoundNumber) + " GBs";
        }
        else if (size > Math.Pow(2, 40) & size <= Math.Pow(2, 50))
        {
            result = Math.Round(size / Math.Pow(2, 40), shortRoundNumber) + " TBs";
        }
        else if (size > Math.Pow(2, 50) & size <= Math.Pow(2, 60))
        {
            result = Math.Round(size / Math.Pow(2, 50), shortRoundNumber) + " PBs";
        }
        else result = "(None)";

        return result;
    }
}