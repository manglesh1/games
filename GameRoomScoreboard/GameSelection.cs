﻿using Microsoft.Web.WebView2.Core;
using Microsoft.Win32;
using System.Configuration;
using scorecard.lib;
using System.Diagnostics;
using System.Text;
using System.Net.Http;
using System.Text.Json;
namespace scorecard
{
    public partial class GameSelection : Form
    {
        private ScorecardForm scorecardForm;
        List<Player> players = new List<Player>();
        List<Player> Waitingplayers = new List<Player>();
        // Logger logger = new AsyncLogger("scorecard");
        ScoreboardListener udpHandler = new ScoreboardListener();
        public GameSelection()
        {
           
             InitializeComponent();
            StartCheckInTimer();
            if (!Debugger.IsAttached)
            {
                this.FormBorderStyle = FormBorderStyle.None;
                this.WindowState = FormWindowState.Maximized;
                this.TopMost = true;
            }
            logger.Log("application started");
            InitializeWebView();

            SetBrowserFeatureControl();
            InitializeScorecardForm();
            Lib.NFCReaderWriter readerWriter = new Lib.NFCReaderWriter("V", ConfigurationSettings.AppSettings["server"]);
            webView2.Source = new Uri(ConfigurationSettings.AppSettings["gameurl"])  ;
            // webView2.Visibility = Visibility.Visible;
            readerWriter.StatusChanged += (s, uid1) =>
            {
                string uid = uid1.Split(':')[0];
                string result = uid1.Split(':')[1];
                if (uid.Length == 0)
                {
                    logger.Log($"card uid not detected {uid}"); return;
                }
                if (result.Length > 0)
                {
                    logger.Log($"card not valid {uid}");
                    return;
                }
                if (Waitingplayers.FindAll(x => x.wristbandCode == uid).Count > 0)
                {

                    logger.Log($"card already added {uid}");
                    return;
                }
                logger.Log($"card uid detected {uid}");
                //wristbandCode, playerStartTime, playerEndTime, gameType, points, LevelPlayed
                Waitingplayers.Add(new Player { wristbandCode = uid, CheckInTime = DateTime.Now });
                    if (webView2.InvokeRequired)
                    {
                        webView2.Invoke(new Action(() =>
                            webView2.CoreWebView2.ExecuteScriptAsync($"window.receiveMessageFromWPF('{uid}')")
                        ));
                    }
                    else
                    {
                        webView2.CoreWebView2.ExecuteScriptAsync($"window.receiveMessageFromWPF('{uid}')");
                    }


               
                };
            udpHandler.BeginReceive(data => ReceiveCallback(data));

        }

        private void ReceiveCallback(byte[] receivedBytes)
        {
            string receivedData = Encoding.UTF8.GetString(receivedBytes);

            var gameMessage = Newtonsoft.Json.JsonConvert.DeserializeObject<GameMessage>(receivedData);
            scorecardForm.UpdateScoreBoard( gameMessage.IterationTime, gameMessage.Level, gameMessage.LifeLine, gameMessage.Score);
            foreach (var p in players)
            {
                p.LevelPlayed = gameMessage.Level;
                p.Points = gameMessage.Score;
                if (gameMessage.Status == GameStatus.Completed)
                {
                    p.playerEndTime = DateTime.Now;

                }

            }
            HandleSattusChange(gameMessage.Status);
            udpHandler.BeginReceive(data => ReceiveCallback(data));
            //  CurrentGame_StatusChanged(null, gameMessage.Status);
        }
        private void UpdateWristBandStatus(List<Player> players)
        {
            foreach (var item in players)
            {
                item.playerEndTime = DateTime.Now;
            }
            var request = new
            {
                players = players
            };

            // Serialize the object to JSON
            string jsonRequest = JsonSerializer.Serialize(request, new JsonSerializerOptions { WriteIndented = true });
            logger.Log(jsonRequest);

            var httpClient = new HttpClient { BaseAddress = new Uri(ConfigurationSettings.AppSettings["server"]) };
            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

            try
            {
                var response = httpClient.PostAsync("playerScore/addPlayerScores", content);
                logger.Log(response.Result.IsSuccessStatusCode ? "" : "Error inserting data into Database!");
            }
            catch (Exception ex)
            {
                logger.Log("An error occurred: " + ex.Message);
                // "Error communicating with API";
            }

        }
        private System.Windows.Forms.Timer checkInTimer;

        private void StartCheckInTimer()
        {
            
            checkInTimer = new System.Windows.Forms.Timer();
            checkInTimer.Interval = 300000; // 5 minutes
            checkInTimer.Tick += CheckInTimer_Tick;
            checkInTimer.Start();
        }

        private void CheckInTimer_Tick(object sender, EventArgs e)
        {
          //  if (!scorecardForm.currentGame.IsRunning)
            {
                if (Waitingplayers.FindAll(x => x.CheckInTime > DateTime.Now.AddMinutes(-5)).Count > 0)
                {
                    logger.Log($"player did play game minute so clearing them");
                    Waitingplayers.Clear();
                    RefreshWebView();
                }
            }
        }

        private void RefreshWebView()
        {
            if (webView2.InvokeRequired)
            {
                webView2.Invoke(new Action(() =>
                {
                    webView2.CoreWebView2.Reload();
                }));
            }
            else
            {
                webView2.CoreWebView2.Reload();
            }
        }
      
        private void InitializeScorecardForm()
        {
            // Find the secondary screen
            Screen[] screens = Screen.AllScreens;
            Screen secondaryScreen = screens.Length > 1 ? screens[1] : null;

            if (secondaryScreen != null)
            {
               
                scorecardForm = new ScorecardForm();
                
                // Set the position of the scorecard form to the secondary screen
                scorecardForm.StartPosition = FormStartPosition.Manual;
                scorecardForm.Location = new Point(secondaryScreen.Bounds.Left, secondaryScreen.Bounds.Top);
                scorecardForm.Size = new Size(secondaryScreen.Bounds.Width, secondaryScreen.Bounds.Height);

                // Show the scorecard form on the secondary screen
                scorecardForm.Show();
            }
            else
            {
                scorecardForm = new ScorecardForm();
                scorecardForm.Show();
                MessageBox.Show("Secondary monitor not detected.");
            }
        }
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
            logger.Log($"receive message from front end message{message}");
            if (message.StartsWith("start"))
            {
                string game = message.Split(':')[1];               
                int noofplayers = int.Parse(message.Split(':')[2]);
                
                if (ConfigurationSettings.AppSettings["gamingEnginePath"].Length>0)
                {
                    Task.Run(() => StartGame(new string[] { game, noofplayers.ToString() }, ConfigurationSettings.AppSettings["gamingEnginePath"]));
                    Thread.Sleep(1000);
                }
                udpHandler.SendStartGameMessage(message);
            }
        }
        static void StartGame(string[] args,string  exePath)
        {
            // Path to the executable
            //string exePath = @"C:\Path\To\YourExecutable.exe";

            // Arguments to pass to the executable
            string arguments = $"\"{args[0]}\" {args[1]} {ConfigurationSettings.AppSettings["isTestMode"]}";

            // Create a new process start information
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = exePath,     // The path of the executable
                Arguments = arguments,  // The arguments to pass
                UseShellExecute = false, // Set to true if you need to use the shell to start the process
                RedirectStandardOutput = true, // To capture the output
                RedirectStandardError = true,  // To capture errors
                CreateNoWindow = true, // Set to true if you don't want a new window to be created
                
        };
            startInfo.WorkingDirectory = Path.GetDirectoryName(exePath);
            // Start the process
            using (Process process = Process.Start(startInfo))
            {
                // Optionally, capture the output
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

               logger.LogError("Output: " + output);
                logger.LogError("Error: " + error);
            }
        }
        private void HandleSattusChange(string status)
        {
           
            if (scorecardForm != null)
            {
                util.uiupdate($"window.updateStaus('{status}')", webView2);
                logger.Log($"gameselection-receive status change message in select form status:{status}");
                if (status == GameStatus.Completed)
                {

                    UpdateWristBandStatus(players);
                    players.Clear();
                 //   Waitingplayers.Clear(); // Clear the waiting list
                    RefreshWebView(); // Refresh WebView2
                }
                if (status.StartsWith(GameStatus.Running))
                {
                    players.AddRange(Waitingplayers);
                    Waitingplayers.Clear();
                } 
            }
        }
        
       

        private void SetBrowserFeatureControl()
        {
            string appName = System.Diagnostics.Process.GetCurrentProcess().ProcessName + ".exe";
            using (var key = Registry.CurrentUser.CreateSubKey($@"Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION"))
            {
                key.SetValue(appName, 11001, RegistryValueKind.DWord);
            }
        }


        private string GetGameDescription(string gameType)
        {
            switch (gameType)
            {
                case "Target":
                    return "In the Target Game, hit the highlighted targets as quickly as possible. Each successful hit scores points.";
                case "Smash":
                    return "In the Smash Game, smash the targets that light up. The faster you smash, the higher your score.";
                case "Chaser":
                    return "In the Chaser Game, chase and hit the moving targets. Stay quick and keep up to score points.";
                case "FloorGame":
                    return "Welcome to the LED Floor Game! Here's how to play: Avoid the blue line as it moves across the grid. Step on the green tiles to score points. Each level gets faster, so stay sharp! Touch the blue line and it's game over. Survive all iterations to win! Good luck, and have fun!";
                case "PatternBuilder":
                    return "Players must recreate a pattern based off memory as quickly as possible. Each correct pattern earns a point.";
                case "wipeout":
                    return "Welcome to the LED Wipeout Game! Here's how to play: Your goal is to avoid the rotating obstacles and survive as long as possible. Obstacles will move around the center of the grid. Each full rotation without a collision increases your score. Be careful, the speed and direction of rotation can change, so stay alert! If you touch an obstacle, the game ends. Survive through all iterations to win the game. Good luck, and get ready for the challenge!";
                default:
                    return "";
            }
        }


    }
    class GameMessage
    {
        public int Score;
        public int LifeLine;
        public int Level;
        public string Status;
        public int IterationTime;
    }
}