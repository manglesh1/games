﻿using Microsoft.Win32;
using System;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Web.WebView2.Core;

namespace WpfApp1
{
    public partial class MainWindow : Window
    {
        AsyncLogger logger = new AsyncLogger("wpf.log");
        public MainWindow()
        {
            InitializeComponent();
            InitializeWebView();
           // PlayScreensaver();
            SetBrowserFeatureControl();
            webView2.Source = new Uri("http://localhost:3002/registration");
            Lib.NFCReaderWriter readerWriter = new Lib.NFCReaderWriter("R");

            readerWriter.StatusChanged += (s, uid) =>
            {
                if (ifWebaskedtoShow == "ScanCard")
                {
                    Dispatcher.Invoke(() =>
                    {
                        if (uid.Length > 0)
                        {
                            logger.Log($"Card detected: {uid}");
                            if (webView2.CoreWebView2 != null)
                            {
                                string script = $"window.receiveMessageFromWPF('{uid}');";
                                webView2.CoreWebView2.ExecuteScriptAsync(script);
                                readerWriter.updateStatus(uid,"R");
                            }
                        }
                        else
                        {
                            string script = $"window.receiveMessageFromWPF('');";
                        }
                    });
                }
            };
        }
        string ifWebaskedtoShow = "N";
        private void InitializeWebView()
        {
            webView2.CoreWebView2InitializationCompleted += WebView2_CoreWebView2InitializationCompleted;
            
        }

        private void WebView2_CoreWebView2InitializationCompleted(object sender, CoreWebView2InitializationCompletedEventArgs e)
        {
            webView2.CoreWebView2.WebMessageReceived += WebView2_WebMessageReceived;
        }

        private void WebView2_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            var message = e.TryGetWebMessageAsString();
           
                ifWebaskedtoShow = message;
                // Restart the video

          
        }
        private void SetBrowserFeatureControl()
        {
            string appName = System.Diagnostics.Process.GetCurrentProcess().ProcessName + ".exe";
            using (var key = Registry.CurrentUser.CreateSubKey($@"Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION"))
            {
                key.SetValue(appName, 11001, RegistryValueKind.DWord);
            }
        }
        private void PlayScreensaver()
        {
            //videoPlayer.MediaEnded += VideoPlayer_MediaEnded;
            //videoPlayer.Play();
        }

        private void VideoPlayer_MediaEnded(object sender, RoutedEventArgs e)
        {
            //Restart the video
            //videoPlayer.Position = TimeSpan.Zero;
            //videoPlayer.Play();
        }

        private void OnCardDetected(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                //videoPlayer.Visibility = Visibility.Collapsed;
                webView2.Visibility = Visibility.Visible;
                webView2.Source = new Uri("http://localhost:8081/");
                logger.Log("web visible");
                
                // Delay visibility change to ensure the content is loaded

            });
        }

    }
}