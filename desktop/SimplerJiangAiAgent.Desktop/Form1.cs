using Microsoft.Web.WebView2.WinForms;

namespace SimplerJiangAiAgent.Desktop;

public partial class Form1 : Form
{
    private readonly WebView2 _webView;
#if DEBUG
    private readonly TextBox _debugTextBox;
    private readonly Button _debugButton;
    private bool _debugVisible;
#endif

    public Form1()
    {
        InitializeComponent();

        Text = "SimplerJiang AI Agent";
        WindowState = FormWindowState.Maximized;

        _webView = new WebView2
        {
            Dock = DockStyle.Fill
        };

        Controls.Add(_webView);
#if DEBUG
        _debugTextBox = new TextBox
        {
            Dock = DockStyle.Bottom,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            Height = 160,
            Visible = false
        };

        _debugButton = new Button
        {
            Text = "开发模式",
            AutoSize = true,
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Top = 10,
            Left = ClientSize.Width - 110
        };

        _debugButton.Click += (_, _) => ToggleDebugPanel();
        Controls.Add(_debugTextBox);
        Controls.Add(_debugButton);

        Resize += (_, _) =>
        {
            _debugButton.Left = ClientSize.Width - _debugButton.Width - 12;
        };
#endif
        Load += OnLoadAsync;
    }

    private async void OnLoadAsync(object? sender, EventArgs e)
    {
        // TODO: 生产环境可改为本地静态文件或内置资源
        await _webView.EnsureCoreWebView2Async();
#if DEBUG
        _webView.CoreWebView2.ProcessFailed += (_, args) => AppendDebug($"WebView2 进程异常: {args.Reason}");
        _webView.CoreWebView2.NavigationCompleted += (_, args) =>
        {
            if (!args.IsSuccess)
            {
                AppendDebug($"页面加载失败: {args.WebErrorStatus}");
            }
        };

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            AppendDebug($"未处理异常: {args.ExceptionObject}");
        };

        Application.ThreadException += (_, args) =>
        {
            AppendDebug($"线程异常: {args.Exception}");
        };
#endif
        _webView.CoreWebView2.Navigate("http://localhost:5119");
    }

#if DEBUG
    private void ToggleDebugPanel()
    {
        _debugVisible = !_debugVisible;
        _debugTextBox.Visible = _debugVisible;
    }

    private void AppendDebug(string message)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => AppendDebug(message));
            return;
        }

        _debugTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
    }
#endif
}
