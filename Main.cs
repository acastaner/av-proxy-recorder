using System;
using System.Collections.Specialized;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Fiddler;
[assembly: Fiddler.RequiredVersion("2.1.8.5")]


public class Main : IAutoTamper
{
    #region Private Fields

    TabPage _page = new TabPage("SpirentScript");
    RichTextBox _text = new RichTextBox();
    Button _newButton = new Button();
    Button _showButton = new Button();
    Button _keepChangesButton = new Button();
    Button _recordButton = new Button();
    Button _insertCommentButton = new Button();
    StringCollection _lines = new StringCollection();
    bool recording = false;

    #endregion

    #region Constructor

    public Main()
    {
        /* NOTE: It's possible that Fiddler UI isn't fully loaded yet, so don't add any UI in the constructor.

           But it's also possible that AutoTamper* methods are called before OnLoad (below), so be
           sure any needed data structures are initialized to safe values here in this constructor */
        ClearScript();
        ShowScript();
    }

    #endregion

    #region Fiddler

    #region Fiddler Methods

    public void OnLoad()
    {
        AddControlsToFiddler();
    }
    public void OnBeforeUnload() { }

    public void AutoTamperRequestBefore(Session session) { }
    public void AutoTamperRequestAfter(Session session) { }
    public void AutoTamperResponseBefore(Session session)
    {
        if (recording == false || session.oRequest.headers == null || session.oRequest.headers.HTTPMethod == "CONNECT")
            return;

        ////////// Request //////////
        string line = String.Empty;
        // Host tokens
        string host = session.host.Replace("dvt001.hn2test", "{UrlPart}").Replace("dvt002.hn2test", "{UrlPart}").Replace("dvt005.hn2test", "{UrlPart}").Replace("dvt006.hn2test", "{UrlPart}").Replace("dvt021.hn2test", "{UrlPart}");

        // Url and headers
        // Level 1 or 2
        string level = LevelOneOrTwo(session);
        // Rest of url
        line = RestOfUrl(session, line, host, level);
        // Check for latest images
        line = LatestImages(session, line);
        if (String.IsNullOrEmpty(line) == false)
            _lines.Add(line);

        // Body
        if (session.requestBodyBytes != null && session.requestBodyBytes.Length > 0)
            _lines.Add(Encoding.ASCII.GetString(session.requestBodyBytes, 0, session.requestBodyBytes.Length));
        
        ////////// Response //////////
        if (session.oResponse.headers == null)
            return;

        _lines.Add("# " + session.responseCode.ToString());

    }
    public void AutoTamperResponseAfter(Session session) { }
    public void OnBeforeReturningError(Session session) { }

    #endregion
    
    #region Private Methods

    private string LevelOneOrTwo(Session session)
    {
        string level;
        if (
            session.url.ToUpper().Contains(".CSS") ||
            session.url.ToUpper().Contains(".JPG") ||
            session.url.ToUpper().Contains(".GIF") ||
            session.url.ToUpper().Contains(".PNG") ||
            session.url.ToUpper().Contains(".JS")  ||
            session.url.ToUpper().Contains(".SWF") ||
            session.url.ToUpper().Contains(".XML") ||
            session.url.ToUpper().Contains("IMG.") ||
            session.url.ToUpper().Contains("USERDETAILSHANDLER")
            )
            // If this is a level 2 get then it's easy
            level = "2 ";
        else
        {
            level = "1 ";
            // Now let's get the comment to put in to the script
            _lines.Add(String.Empty);
            int indexOfQMark = session.PathAndQuery.IndexOf("?");
            if (indexOfQMark > 0)
            {
                _lines.Add("# " + session.PathAndQuery.Substring(0, indexOfQMark));
            }
            else
            {
                _lines.Add("# " + session.PathAndQuery);
            }
        }
        return level;
    }
    private static string RestOfUrl(Session session, string line, string host, string level)
    {
        // Spirent assumes a GET if no verb supplied, so only put one in if not a GET
        string method = session.oRequest.headers.HTTPMethod == "GET" ? String.Empty : session.oRequest.headers.HTTPMethod + " ";
        // Fiddler gives us all the nice parts of the request as fields on the Session object
        // We can now just pipe them out
        line += level +
                method +
                String.Format("{0}://{1}:{2}{3}", session.oRequest.headers.UriScheme, host, session.port.ToString(), session.PathAndQuery);// +
                //" <ADDITIONAL_HEADER_FROMVAR=Euphoria>";
        return line;
    }
    private static string LatestImages(Session session, string line)
    {
        // We need to throw some additional headers in for images.
        // Once again, the Fiddler Session object allows us to do this easily...
        if (session.oRequest.headers.Exists("If-Modified-Since"))
        {
            line += " <ADDITIONAL_HEADER=\"If-Modified-Since: " + session.oRequest.headers["If-Modified-Since"].Replace("\"", string.Empty) + "\">";
        }
        if (session.oRequest.headers.Exists("If-None-Match"))
        {
            line += " <ADDITIONAL_HEADER=\"If-None-Match: " + session.oRequest.headers["If-None-Match"].Replace("\"", string.Empty) + "\">";
        }
        return line;
    }

    #endregion

    #endregion

    #region Form

    #region Button Clicks

    private void NewButton_Click(object sender, EventArgs e)
    {
        ClearScript();
        ShowScript();
    }
    private void ShowButton_Click(object sender, EventArgs e)
    {
        ShowScript();
    }
    private void KeepChangesButton_Click(object sender, EventArgs e)
    {
        string[] separators = { Environment.NewLine };
        string[] lines = _text.Text.Split(separators, StringSplitOptions.RemoveEmptyEntries);

        _lines.Clear();

        for (int i = 0; i < lines.Length; i++)
            _lines.Add(lines[i]);

        ShowScript();
    }
    private void RecordButton_Click(object sender, EventArgs e)
    {
        if (recording)
        {
            _recordButton.Text = "Not Recording...";
            _recordButton.BackColor = Color.Red;
            recording = false;
        }
        else
        {
            _recordButton.Text = "Recording...";
            _recordButton.BackColor = Color.Green;
            recording = true;
        }
    }
    private void InsertCommentButton_Click(object sender, EventArgs e)
    {
        _text.Text += Environment.NewLine + "### InsertCommentHere ###" + Environment.NewLine;
    }
    
    #endregion

    #region Private Methods

    private void AddControlsToFiddler()
    {
        _page.ImageIndex = (int)Fiddler.SessionIcons.Script;
        int x = _page.Size.Width;
        int y = _page.Size.Height;

        _newButton.Location = new Point(10, 10);
        _newButton.Size = new System.Drawing.Size(100, 25);
        _newButton.Text = "New";
        _newButton.UseVisualStyleBackColor = true;
        _newButton.Click += new System.EventHandler(this.NewButton_Click);

        _showButton.Location = new Point(115, 10);
        _showButton.Size = new System.Drawing.Size(100, 25);
        _showButton.Text = "Refresh";
        _showButton.UseVisualStyleBackColor = true;
        _showButton.Click += new System.EventHandler(this.ShowButton_Click);

        _keepChangesButton.Location = new Point(220, 10);
        _keepChangesButton.Size = new System.Drawing.Size(100, 25);
        _keepChangesButton.Text = "Save Changes";
        _keepChangesButton.UseVisualStyleBackColor = true;
        _keepChangesButton.Click += new System.EventHandler(this.KeepChangesButton_Click);

        _recordButton.Location = new Point(325, 10);
        _recordButton.Size = new System.Drawing.Size(100, 25);
        _recordButton.Text = "Not Recording...";
        _recordButton.BackColor = Color.Red;
        _recordButton.UseVisualStyleBackColor = true;
        _recordButton.Click += new System.EventHandler(this.RecordButton_Click);

        _insertCommentButton.Location = new Point(430, 10);
        _insertCommentButton.Size = new System.Drawing.Size(100, 25);
        _insertCommentButton.Text = "Insert Comment";
        _insertCommentButton.UseVisualStyleBackColor = true;
        _insertCommentButton.Click += new System.EventHandler(this.InsertCommentButton_Click);

        _text.Location = new Point(10, 50);
        _text.Size = new System.Drawing.Size(x - 20, y - 60);
        _text.Anchor = ((AnchorStyles)((((AnchorStyles.Top |
                                          AnchorStyles.Bottom) |
                                          AnchorStyles.Left) |
                                          AnchorStyles.Right)));

        _page.Name = "SpirentScript";
        _page.Controls.Add(_text);
        _page.Controls.Add(_newButton);
        _page.Controls.Add(_showButton);
        _page.Controls.Add(_keepChangesButton);
        _page.Controls.Add(_recordButton);
        _page.Controls.Add(_insertCommentButton);
        FiddlerApplication.UI.tabsViews.TabPages.Add(_page);
        FiddlerApplication.UI.tabsViews.SelectedTab = FiddlerApplication.UI.tabsViews.TabPages["SpirentScript"];
    }
    private void ClearScript()
    {
        _lines.Clear();
        _lines.Add("#####################################");
        _lines.Add("# Spirent Script");
        _lines.Add(String.Format("# Recorded by:\t{0}", Environment.UserName));
        _lines.Add(String.Format("# DateTime:\t{0}:{1}", DateTime.Now.ToShortDateString(), DateTime.Now.ToShortTimeString()));
        _lines.Add("#####################################");
        _lines.Add(String.Empty);
        _lines.Add("### InsertCommentHere ###");
    }
    private void ShowScript()
    {
        _text.Text = string.Empty;
        foreach (var line in _lines)
            _text.Text += line + Environment.NewLine;
    }

    #endregion

    #endregion
}
