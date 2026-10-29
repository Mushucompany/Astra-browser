using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

class MyBrowser : Form
{
    TabControl tabs;
    TextBox address;
    TextBox search;
    Button btnBack, btnForward, btnReload, btnHome, btnNew;
    MenuStrip menu;
    ToolStripMenuItem mBookmarks, mHistory;
    ToolTip tooltip;

    List<string[]> bookmarks = new List<string[]>(); // {title, url}
    List<string[]> history = new List<string[]>();   // {title, url}
    string currentTabUrl = "";
    string configFile;
    string userDataFolder;

    const string SEARCH_URL = "https://ya.ru/search/?text=";
    // Домашняя страница: внешний файл home.html рядом с exe (если есть), иначе встроенная
    static string HomeUrl()
    {
        string f = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "home.html");
        return File.Exists(f) ? f : "home:start";
    }

    public MyBrowser()
    {
        configFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mybrowser_data.txt");
        userDataFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "webview2_data");

        Text = "Мой браузер";
        Width = 1200;
        Height = 780;
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9.5f);
        try
        {
            if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "browser.ico")))
                this.Icon = new Icon(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "browser.ico"));
        }
        catch { }

        // Меню
        menu = new MenuStrip();
        mBookmarks = new ToolStripMenuItem("Закладки");
        mHistory = new ToolStripMenuItem("История");
        var mAddBookmark = new ToolStripMenuItem("Добавить в закладки");
        mAddBookmark.Click += (s, e) => AddBookmark();
        mBookmarks.DropDownItems.Add(mAddBookmark);
        mBookmarks.DropDownItems.Add(new ToolStripSeparator());
        mBookmarks.DropDownItems.Add(new ToolStripMenuItem("Показать все", null, (s, e) => ShowListTab("Закладки")));
        mHistory.DropDownItems.Add(new ToolStripMenuItem("Показать историю", null, (s, e) => ShowListTab("История")));
        mHistory.DropDownItems.Add(new ToolStripMenuItem("Очистить историю", null, (s, e) => ClearHistory()));
        menu.Items.Add(mBookmarks);
        menu.Items.Add(mHistory);
        menu.Dock = DockStyle.Top;
        menu.GripStyle = ToolStripGripStyle.Hidden;
        menu.RenderMode = ToolStripRenderMode.System;

        // Панель навигации
        var nav = new Panel();
        nav.Dock = DockStyle.Top;
        nav.Height = 34;
        nav.Padding = new Padding(6, 3, 6, 0);

        btnBack = MakeButton("◀");
        btnForward = MakeButton("▶");
        btnReload = MakeButton("⟳");
        btnHome = MakeButton("⌂");
        btnNew = MakeButton("+");

        address = new TextBox();
        address.Dock = DockStyle.Fill;
        address.Margin = new Padding(4, 0, 4, 0);
        address.KeyDown += OnAddressEnter;
        address.Enter += (s, e) => address.SelectAll();

        search = new TextBox();
        search.Dock = DockStyle.Right;
        search.Width = 160;
        search.Margin = new Padding(4, 0, 0, 0);
        search.Font = new Font("Segoe UI", 9f);
        search.ForeColor = Color.Gray;
        search.Text = "Поиск…";
        search.Enter += (s, e) => { if (search.Text == "Поиск…") { search.Text = ""; search.ForeColor = Color.Black; } };
        search.Leave += (s, e) => { if (search.Text == "") { search.Text = "Поиск…"; search.ForeColor = Color.Gray; } };
        search.KeyDown += OnSearchEnter;

        nav.Controls.Add(search);
        nav.Controls.Add(address);
        PlaceLeft(nav, btnNew);
        PlaceLeft(nav, btnHome);
        PlaceLeft(nav, btnReload);
        PlaceLeft(nav, btnForward);
        PlaceLeft(nav, btnBack);

        btnNew.Click += (s, e) => NewTab(HomeUrl(), "");
        btnHome.Click += (s, e) => { var w3 = CurrentView(); if (w3 != null && w3.CoreWebView2 != null) w3.CoreWebView2.Navigate(HomeUrl()); };
        btnReload.Click += (s, e) => { var w3 = CurrentView(); if (w3 != null) w3.CoreWebView2.Reload(); };
        btnBack.Click += (s, e) => { var w3 = CurrentView(); if (w3 != null && w3.CanGoBack) w3.GoBack(); };
        btnForward.Click += (s, e) => { var w3 = CurrentView(); if (w3 != null && w3.CanGoForward) w3.GoForward(); };

        // Вкладки
        tabs = new TabControl();
        tabs.Dock = DockStyle.Fill;
        tabs.SelectedIndexChanged += OnTabChanged;
        tabs.MouseUp += OnTabsMouseUp;

        Controls.Add(tabs);
        Controls.Add(nav);
        Controls.Add(menu);

        tooltip = new ToolTip();
        tooltip.SetToolTip(btnNew, "Новая вкладка");

        LoadData();
        List<string[]> session = LoadSession();
        string welcomePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "welcome.html");
        bool showWelcome = File.Exists(welcomePath) && !File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "welcome.done"));

        if (showWelcome)
        {
            NewTab(welcomePath, "Добро пожаловать");
            // флаг, чтобы приветствие больше не показывалось
            try { File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "welcome.done"), "1"); } catch { }
        }
        else if (session.Count > 0)
        {
            foreach (var item in session) NewTab(item[1], item[0]);
            if (session.Count > 0) tabs.SelectedIndex = 0;
        }
        else
        {
            NewTab(HomeUrl(), "");
        }
    }

    Button MakeButton(string text)
    {
        var b = new Button();
        b.Text = text;
        b.Width = 32;
        b.Height = 26;
        b.Dock = DockStyle.Left;
        b.Margin = new Padding(1, 0, 1, 0);
        b.UseVisualStyleBackColor = true;
        return b;
    }

    void PlaceLeft(Control parent, Control c)
    {
        parent.Controls.Add(c);
        c.BringToFront();
    }

    // ---------- Вкладки ----------
    WebView2 NewTab(string url, string title)
    {
        var page = new TabPage();
        page.Text = string.IsNullOrEmpty(title) ? "Новая вкладка" : title;

        var wv = new WebView2();
        wv.Dock = DockStyle.Fill;
        wv.Tag = page;

        bool pendingInlineHome = (url == "home:start");
        bool pendingFileHome = (url == HomeUrl());

        string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "browser_log.txt");
        wv.CoreWebView2InitializationCompleted += (s, e) =>
        {
            try { Log(logPath, "INIT fired isSuccess=" + e.IsSuccess + " err=" + (e.InitializationException != null ? e.InitializationException.ToString() : "null")); }
            catch { }
            if (e.IsSuccess && wv.CoreWebView2 != null)
            {
                wv.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
                wv.CoreWebView2.Settings.AreDevToolsEnabled = true;
                wv.CoreWebView2.NavigationCompleted += (s2, e2) =>
                {
                    Log(logPath, "NAVCOMPLETED ok=" + e2.IsSuccess + " err=" + e2.WebErrorStatus + " src=" + wv.CoreWebView2.Source);
                    string t = wv.CoreWebView2.DocumentTitle;
                    page.Text = string.IsNullOrEmpty(t) ? "Новая вкладка" : t;
                    string src = wv.CoreWebView2.Source;
                    address.Text = (src == "about:blank" || src.Contains("home:")) ? "" : src;
                    if (tabs.SelectedTab == page) Text = "Мой браузер — " + page.Text;
                    currentTabUrl = wv.CoreWebView2.Source;
                    if (!src.Contains("home:") && src != "about:blank")
                        AddHistory(string.IsNullOrEmpty(t) ? src : t, src);
                    UpdateNav();
                };

                // Навигация только после готовности ядра
                if (pendingInlineHome)
                {
                    Log(logPath, "NAVIGATE to inline StartPage (NavigateToString)");
                    wv.CoreWebView2.NavigateToString(StartPageHtml());
                }
                else if (pendingFileHome)
                {
                    Log(logPath, "NAVIGATE to start.html file");
                    wv.CoreWebView2.Navigate(HomeUrl());
                }
                else
                {
                    Log(logPath, "NAVIGATE to " + url);
                    wv.CoreWebView2.Navigate(string.IsNullOrEmpty(url) ? "about:blank" : url);
                }
            }
        };

        page.Controls.Add(wv);
        tabs.TabPages.Add(page);
        tabs.SelectedTab = page;

        // Явная инициализация ядра — гарантия, что событие сработает
        try
        {
            Log(logPath, "EnsureCoreWebView2Async calling");
            wv.EnsureCoreWebView2Async(null);
        }
        catch (Exception exInit)
        {
            try { Log(logPath, "EnsureCoreWebView2Async EXCEPTION: " + exInit.ToString()); } catch { }
            MessageBox.Show("Ошибка инициализации WebView2:\n" + exInit.Message, "Мой браузер",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        return wv;
    }

    void CloseTab(int index)
    {
        if (index < 0 || index >= tabs.TabPages.Count) return;
        tabs.TabPages.RemoveAt(index);
        if (tabs.TabPages.Count == 0) NewTab(HomeUrl(), "");
    }

    void OnTabsMouseUp(object sender, MouseEventArgs e)
    {
        var tc = sender as TabControl;
        if (tc == null) return;
        for (int i = 0; i < tc.TabPages.Count; i++)
        {
            Rectangle r = tc.GetTabRect(i);
            if (r.Contains(e.Location))
            {
                Rectangle closeR = new Rectangle(r.Right - 20, r.Top + (r.Height - 12) / 2, 16, 16);
                if (closeR.Contains(e.Location)) { CloseTab(i); return; }
                if (e.Button == MouseButtons.Middle) { CloseTab(i); return; }
                if (e.Button == MouseButtons.Right) { CloseTab(i); return; }
            }
        }
    }

    WebView2 CurrentView()
    {
        if (tabs.SelectedTab == null) return null;
        for (int i = 0; i < tabs.SelectedTab.Controls.Count; i++)
        {
            WebView2 wv = tabs.SelectedTab.Controls[i] as WebView2;
            if (wv != null) return wv;
        }
        return null;
    }

    void OnTabChanged(object sender, EventArgs e)
    {
        var wv = CurrentView();
        address.Text = (wv != null && wv.CoreWebView2 != null) ? wv.CoreWebView2.Source : "";
        if (wv != null && wv.CoreWebView2 != null) currentTabUrl = wv.CoreWebView2.Source;
        UpdateNav();
    }

    void UpdateNav()
    {
        var wv = CurrentView();
        btnBack.Enabled = (wv != null && wv.CanGoBack);
        btnForward.Enabled = (wv != null && wv.CanGoForward);
    }

    // ---------- Навигация ----------
    void OnAddressEnter(object sender, KeyEventArgs e)
    {
        if (e.KeyCode != Keys.Enter) return;
        NavigateAddress(address.Text);
        e.Handled = true;
    }

    void OnSearchEnter(object sender, KeyEventArgs e)
    {
        if (e.KeyCode != Keys.Enter) return;
        string q = search.Text.Trim();
        if (q.Length == 0 || q == "Поиск…") return;
        var wv = CurrentView();
        if (wv != null && wv.CoreWebView2 != null)
            wv.CoreWebView2.Navigate(SEARCH_URL + Uri.EscapeDataString(q));
        e.Handled = true;
    }

    void NavigateAddress(string u)
    {
        u = (u ?? "").Trim();
        if (u.Length == 0 || u == "Поиск…") return;
        if (!u.Contains("://")) u = "http://" + u;
        var wv = CurrentView();
        if (wv != null && wv.CoreWebView2 != null) wv.CoreWebView2.Navigate(u);
    }

    // ---------- Данные ----------
    void LoadData()
    {
        if (!File.Exists(configFile)) return;
        try
        {
            string[] lines = File.ReadAllLines(configFile, Encoding.UTF8);
            string section = "";
            foreach (string line in lines)
            {
                if (line == "[BOOKMARKS]") { section = "b"; continue; }
                if (line == "[HISTORY]") { section = "h"; continue; }
                if (section == "b") bookmarks.Add(SplitPair(line));
                else if (section == "h") history.Add(SplitPair(line));
            }
        }
        catch { }
    }

    string[] SplitPair(string line)
    {
        int idx = line.IndexOf('|');
        if (idx < 0) return new string[] { "", line };
        return new string[] { line.Substring(0, idx), line.Substring(idx + 1) };
    }

    string JoinPair(string a, string b)
    {
        return a + "|" + b;
    }

    List<string[]> LoadSession()
    {
        var result = new List<string[]>();
        if (!File.Exists(configFile)) return result;
        try
        {
            string[] lines = File.ReadAllLines(configFile, Encoding.UTF8);
            bool insession = false;
            foreach (string line in lines)
            {
                if (line == "[SESSION]") { insession = true; continue; }
                if (!insession) continue;
                if (line.StartsWith("[")) break;
                string[] p = SplitPair(line);
                if (!string.IsNullOrEmpty(p[1])) result.Add(p);
            }
        }
        catch { }
        return result;
    }

    void SaveData()
    {
        try
        {
            var sb = new StringBuilder();
            sb.AppendLine("[BOOKMARKS]");
            foreach (var b in bookmarks) sb.AppendLine(JoinPair(b[0], b[1]));
            sb.AppendLine("[HISTORY]");
            foreach (var h in history) sb.AppendLine(JoinPair(h[0], h[1]));
            sb.AppendLine("[SESSION]");
            var urls = new List<string[]>();
            for (int i = 0; i < tabs.TabPages.Count; i++)
            {
                WebView2 wv = TabBrowser(tabs.TabPages[i]);
                if (wv != null && wv.CoreWebView2 != null && !string.IsNullOrEmpty(wv.CoreWebView2.Source)
                    && wv.CoreWebView2.Source != "about:blank")
                    urls.Add(new string[] { tabs.TabPages[i].Text, wv.CoreWebView2.Source });
            }
            foreach (var u in urls) sb.AppendLine(JoinPair(u[0], u[1]));
            File.WriteAllText(configFile, sb.ToString(), Encoding.UTF8);
        }
        catch { }
    }

    WebView2 TabBrowser(TabPage page)
    {
        for (int i = 0; i < page.Controls.Count; i++)
        {
            WebView2 wv = page.Controls[i] as WebView2;
            if (wv != null) return wv;
        }
        return null;
    }

    void SaveSessionAsync()
    {
        // сохраняем при закрытии
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        SaveData();
        base.OnFormClosing(e);
    }

    // ---------- Закладки / история ----------
    void AddBookmark()
    {
        var wv = CurrentView();
        if (wv == null || wv.CoreWebView2 == null || string.IsNullOrEmpty(wv.CoreWebView2.Source)) return;
        string url = wv.CoreWebView2.Source;
        string title = string.IsNullOrEmpty(wv.CoreWebView2.DocumentTitle) ? url : wv.CoreWebView2.DocumentTitle;
        bookmarks.Add(new string[] { title, url });
        MessageBox.Show("Добавлено в закладки:\n" + title, "Закладки",
            MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    void AddHistory(string title, string url)
    {
        if (string.IsNullOrEmpty(url) || url == "about:blank") return;
        history.Add(new string[] { string.IsNullOrEmpty(title) ? url : title, url });
        while (history.Count > 300) history.RemoveAt(0);
    }

    void ClearHistory()
    {
        if (MessageBox.Show("Очистить всю историю?", "История",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
        {
            history.Clear();
        }
    }

    void ShowListTab(string kind)
    {
        var page = new TabPage();
        page.Text = kind;

        var lv = new ListView();
        lv.Dock = DockStyle.Fill;
        lv.View = View.Details;
        lv.FullRowSelect = true;
        lv.Columns.Add("Название", 320);
        lv.Columns.Add("Адрес", 500);
        lv.DoubleClick += (s, e) =>
        {
            if (lv.SelectedItems.Count > 0)
            {
                string url = lv.SelectedItems[0].SubItems[1].Text;
                NewTab(url, lv.SelectedItems[0].SubItems[0].Text);
            }
        };

        List<string[]> data = (kind == "Закладки") ? bookmarks : history;
        if (data.Count == 0)
        {
            var lbl = new Label();
            lbl.Text = (kind == "Закладки") ? "Закладок пока нет. Меню «Закладки → Добавить в закладки»." : "История пуста. Посещённые страницы появятся здесь.";
            lbl.Dock = DockStyle.Top;
            lbl.Padding = new Padding(10, 15, 0, 0);
            lbl.AutoSize = true;
            page.Controls.Add(lbl);
        }
        else
        {
            for (int i = data.Count - 1; i >= 0; i--)
            {
                var item = new ListViewItem(data[i][0]);
                item.SubItems.Add(data[i][1]);
                lv.Items.Add(item);
            }
            page.Controls.Add(lv);
        }

        tabs.TabPages.Add(page);
        tabs.SelectedTab = page;
    }

    // ---------- Встроенная главная страница ----------
    string StartPageHtml()
    {
        var sb = new StringBuilder();
        sb.Append("<html><head><meta charset='utf-8'><title>Главная</title><style>");
        sb.Append("body{margin:0;padding:0;font-family:Segoe UI,Arial,sans-serif;");
        sb.Append("background:linear-gradient(135deg,#1e3c72 0%,#2a5298 55%,#6b8cce 100%);color:#fff;");
        sb.Append("height:100vh;display:flex;flex-direction:column;overflow:hidden}");
        sb.Append(".wrap{flex:1;display:flex;flex-direction:column;align-items:center;justify-content:center;padding:20px}");
        sb.Append("#clock{font-size:64px;font-weight:300;letter-spacing:2px;margin-bottom:8px;text-shadow:0 2px 8px rgba(0,0,0,.3)}");
        sb.Append("#date{font-size:18px;opacity:.85;margin-bottom:24px}");
        sb.Append("form{margin-bottom:30px;display:flex;width:100%;max-width:600px}");
        sb.Append("input[type=text]{flex:1;padding:14px 18px;font-size:16px;border:none;border-radius:24px 0 0 24px;outline:none;color:#333}");
        sb.Append("button{padding:14px 22px;font-size:16px;border:none;border-radius:0 24px 24px 0;background:#ffcc00;color:#333;font-weight:600;cursor:pointer}");
        sb.Append(".links{display:flex;flex-wrap:wrap;gap:12px;justify-content:center;max-width:720px;margin-bottom:28px}");
        sb.Append(".link{background:rgba(255,255,255,.15);color:#fff;padding:12px 20px;border-radius:16px;text-decoration:none;font-size:14px;transition:.2s;display:inline-flex;align-items:center;gap:8px}");
        sb.Append(".link:hover{background:rgba(255,255,255,.28);transform:translateY(-2px)}");
        sb.Append(".link small{opacity:.7}");
        sb.Append(".bmk-title{font-size:13px;opacity:.7;margin-bottom:10px;letter-spacing:1px;text-transform:uppercase}");
        sb.Append(".bmk{display:flex;flex-wrap:wrap;gap:8px;justify-content:center;max-width:760px;min-height:30px}");
        sb.Append(".bmk a{background:rgba(255,255,255,.10);color:#eee;padding:8px 14px;border-radius:12px;text-decoration:none;font-size:13px;transition:.2s}");
        sb.Append(".bmk a:hover{background:rgba(255,255,255,.25);color:#fff}");
        sb.Append("</style></head><body>");
        sb.Append("<div class='wrap'>");
        sb.Append("<div id='clock'></div><div id='date'></div>");
        sb.Append("<form id='f' onsubmit='return go();'>");
        sb.Append("<input type='text' id='q' placeholder='Поиск в Яндексе…' autocomplete='off'/>");
        sb.Append("<button type='submit'>Найти</button></form>");
        sb.Append("<div class='links'>");
        sb.Append("<a class='link' href='https://ya.ru'>Яндекс<small>ya.ru</small></a>");
        sb.Append("<a class='link' href='https://google.com'>Google<small>google.com</small></a>");
        sb.Append("<a class='link' href='https://youtube.com'>YouTube<small>youtube.com</small></a>");
        sb.Append("<a class='link' href='https://vk.com'>ВКонтакте<small>vk.com</small></a>");
        sb.Append("<a class='link' href='https://mail.ru'>Почта<small>mail.ru</small></a>");
        sb.Append("<a class='link' href='https://wikipedia.org'>Википедия<small>wikipedia.org</small></a>");
        sb.Append("</div>");
        // закладки
        sb.Append("<div class='bmk-title'>Мои закладки</div>");
        sb.Append("<div class='bmk'>");
        if (bookmarks.Count == 0)
        {
            sb.Append("<span style='opacity:.6;font-size:13px'>Нет закладок — добавьте через меню «Закладки»</span>");
        }
        else
        {
            for (int i = bookmarks.Count - 1; i >= 0; i--)
            {
                string bt = System.Security.SecurityElement.Escape(bookmarks[i][0]);
                string bu = System.Security.SecurityElement.Escape(bookmarks[i][1]);
                sb.Append("<a href='" + bu + "'>" + bt + "</a>");
            }
        }
        sb.Append("</div></div>");
        sb.Append("<script>");
        sb.Append("function upd(){var n=new Date(),h=n.getHours(),m=n.getMinutes(),s=n.getSeconds();");
        sb.Append("function pad(x){return x<10?'0'+x:''+x}");
        sb.Append("document.getElementById('clock').textContent=pad(h)+':'+pad(m)+':'+pad(s);");
        sb.Append("var days=['Воскресенье','Понедельник','Вторник','Среда','Четверг','Пятница','Суббота'];");
        sb.Append("var months=['января','февраля','марта','апреля','мая','июня','июля','августа','сентября','октября','ноября','декабря'];");
        sb.Append("document.getElementById('date').textContent=days[n.getDay()]+', '+n.getDate()+' '+months[n.getMonth()]+' '+n.getFullYear();}");
        sb.Append("setInterval(upd,1000);upd();");
        sb.Append("function go(){var q=document.getElementById('q').value;if(q.trim()){");
        sb.Append("location.href='https://ya.ru/search/?text='+encodeURIComponent(q);}return false;}");
        sb.Append("</script></body></html>");
        return sb.ToString();
    }

    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "browser_log.txt");
        try
        {
            Log(logPath, "START");
            var b = new MyBrowser();
            Log(logPath, "FORM_CREATED");
            Application.Run(b);
            Log(logPath, "EXITED");
        }
        catch (Exception ex)
        {
            Log(logPath, "ERROR: " + ex.ToString());
            MessageBox.Show("Ошибка запуска:\n" + ex.Message, "Мой браузер",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    static void Log(string path, string msg)
    {
        try { File.AppendAllText(path, DateTime.Now.ToString("HH:mm:ss.fff") + " " + msg + "\r\n"); } catch { }
    }
}