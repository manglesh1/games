﻿using scorecard.lib;
using System.Net.Sockets;
using System.Net;
using System.Text.Json;
using Microsoft.Web.WebView2.Core;
using Microsoft.Win32;
using System.Configuration;
using System.ComponentModel;

public partial class ScorecardForm : Form
{
    private UdpClient udpClientReceiver;
    private System.Threading.Timer relayTimer;
    private IPEndPoint remoteEndPoint;
    private string currentState = GameStatus.NotStarted;
    private string gameType = "";
    private string gameVariant;

    public ScorecardForm()
    {
        InitializeComponent();
        InitializeWebView();
        SetBrowserFeatureControl();
        UpdateScoreBoard(0, 0, 5, 0);
    }

    private async void InitializeWebView()
    {
        // Initialize the WebView2 control and set its source
        webView2.Source = new Uri(ConfigurationSettings.AppSettings["scorecardurl"]);
        await webView2.EnsureCoreWebView2Async(null);

        // Set up the NavigationCompleted event handler
        webView2.CoreWebView2.NavigationCompleted += CoreWebView2_NavigationCompleted;
    }

    private void CoreWebView2_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        if (e.IsSuccess)
        {
            logger.Log("Navigation completed successfully.");
        }
        else
        {
            logger.Log($"Navigation failed with error: {e.WebErrorStatus}");
        }
    }

    private void WebView2_CoreWebView2InitializationCompleted(object sender, CoreWebView2InitializationCompletedEventArgs e)
    {
        webView2.CoreWebView2.WebMessageReceived += WebView2_WebMessageReceived;
    }

    private void WebView2_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        // Handle messages received from the WebView2
    }

    private void SetBrowserFeatureControl()
    {
        string appName = System.Diagnostics.Process.GetCurrentProcess().ProcessName + ".exe";
        using (var key = Registry.CurrentUser.CreateSubKey($@"Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION"))
        {
            key.SetValue(appName, 11001, RegistryValueKind.DWord);
        }
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);
    }

    public void UpdateScoreBoard(int timer, int level, int lives, int score)
    {
        UiUpdate("updateTimer", timer);
        UiUpdate("updateLevel", level);
        UiUpdate("updateLives", lives);
        UiUpdate("updateScore", score);


    }

    private void UiUpdate(string func, int value)
    {
        util.uiupdate($"window.{func}({value})", webView2);
    }
}