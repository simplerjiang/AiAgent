using Microsoft.Web.WebView2.WinForms;

namespace SimplerJiangAiAgent.Desktop;

public partial class Form1 : Form
{
    private readonly WebView2 _webView;

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
        Load += OnLoadAsync;
    }

    private async void OnLoadAsync(object? sender, EventArgs e)
    {
        // TODO: 生产环境可改为本地静态文件或内置资源
        await _webView.EnsureCoreWebView2Async();
        _webView.CoreWebView2.Navigate("http://localhost:5173");
    }
}
