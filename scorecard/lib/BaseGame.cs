using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Timer = System.Threading.Timer;
using scorecard.lib;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Reflection;
using log4net;
using static NAudio.Wave.WaveInterop;
using System.Text;
public abstract class BaseGame
{
    public GameStatusPublisher statusPublisher = GameStatusPublisher.Instance;
    TPLinkSmartDevices.Devices.TPLinkSmartPlug plug;
    protected List<UdpHandler> udpHandlers;
    protected Dictionary<UdpHandler, HashSet<int>> activeIndices;
    protected Random random = new Random();
    protected MusicPlayer musicPlayer;
    protected int lifeLine = 5;
    protected Timer iterationTimer;
    protected int iterations = 0;
    protected int score;
    protected GameConfig config;
    protected string status { get; set; }
    protected int level = 1;
    protected List<string> gameColors = new List<string>();
    protected bool isGameRunning = false;
    protected int remainingTime;
    public virtual string Status
    {
        get { return status; }
        set
        {
            status = value;
            statusPublisher.PublishStatus(score, lifeLine, Level, status, remainingTime, config.GameName, iterations);
            OnStatusChanged(status);
        }
    }
    public virtual int Level
    {
        get { return level; }
        set
        {
            level = value;
            statusPublisher.PublishStatus(score, lifeLine, Level, status, remainingTime, config.GameName, iterations);
            OnLevelChanged(level);

        }
    }
    public virtual int Score
    {
        get { return score; }
        set
        {
            score = value;
            statusPublisher.PublishStatus(score, lifeLine, Level, status, remainingTime, config.GameName, iterations);
            OnScoreChanged(score);
            //labelScore.Text = $"Score: {score}";
            LogData($"Score: {score}");
        }
    }
    public virtual int LifeLine
    {
        get { return lifeLine; }
        set
        {
            lifeLine = value; statusPublisher.PublishStatus(score, lifeLine, Level, status, IterationTime, config.GameName, iterations);
            OnLifelineChanged(value);
            //  LogData($"GameLost lifeLine: {lifeLine}");
        }
    }
    public int IterationTime { get { return (config.MaxIterationTime - (Level - 1) * config.ReductionTimeEachLevel) * 1000; } }
    public event EventHandler<int> ScoreChanged;
    public event EventHandler<int> LifeLineChanged;
    public event EventHandler<int> LevelChanged;
    public event EventHandler<string> StatusChanged;
    protected virtual void OnStatusChanged(string newStatus)
    {
        LogData($"status changed to:{newStatus}");
        StatusChanged?.Invoke(this, newStatus);
    }
    protected virtual void OnLevelChanged(int newLevel)
    {
        LogData($"level changed to:{newLevel}");
        LevelChanged?.Invoke(this, newLevel);
    }
    protected virtual void OnScoreChanged(int newScore)
    {
        LogData($"score changed to:{newScore}");
        ScoreChanged?.Invoke(this, newScore);

    }
    protected virtual void OnLifelineChanged(int newLifeline)
    {
        LogData($"score changed to:{newLifeline}");
        LifeLineChanged?.Invoke(this, newLifeline);
    }

    public string targetColor;
    //private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
    //public void lightonoff(bool on)
    //{
    //    try
    //    {
    //        if (!config.isTestMode)
    //        {
    //            var plug = new TPLinkSmartDevices.Devices.TPLinkSmartPlug(config.SmartPlugip);
    //            plug.OutletPowered = !on;
    //            plug.OutletPowered = on;
    //        }
    //    }
    //    catch { }
    //}
    public BaseGame(GameConfig co)
    {
        logger.Log("basegame constructor");
        this.config = co;
        //statusPublisher = new GameStatusPublisher(config.gameEngineIp);
        statusPublisher.PublishStatus(score, config.MaxLifeLines, Level, GameStatus.Running, remainingTime, config.GameName, iterations);
        gameColors = getColorList();
        musicPlayer = new MusicPlayer("content/background_music.wav");


        //lightonoff(true);
        Thread.Sleep(3000); // Countdown 
        udpHandlers = new List<UdpHandler>();
        if (config.IpAddress != "169.254.255.255" && config.GameName != "Climb")
        {

            udpHandlers.Add(new UdpHandler(config.IpAddress, config.LocalPort, config.RemotePort, config.SocketBReceiverPort, config.NoofLedPerdevice, config.columns, "handler-1"));
            for (int i = 1; i < config.NoOfControllers; i++)
            {
                udpHandlers.Add(new UdpHandler(config.IpAddress, config.LocalPort + i, config.RemotePort + i, config.SocketBReceiverPort + i, config.NoofLedPerdevice, config.columns, $"handler-{i + 1}"));
            }
        }
        Console.WriteLine("Game starting in 3... 2... 1...");
        Task.Run(() => StartAnimition());
        musicPlayer.Announcement("content/hit2.wav");
    }
    public BaseGame(GameConfig co, string backgroundMusic)
    {
        logger.Log("basegame constructor");
        this.config = co;
        statusPublisher.PublishStatus(score, config.MaxLifeLines, Level, GameStatus.Running, remainingTime, config.GameName, iterations);
        gameColors = getColorList();
        musicPlayer = new MusicPlayer(backgroundMusic);
        Thread.Sleep(3000); // Countdown 
        udpHandlers = new List<UdpHandler>();
        if (config.IpAddress != "169.254.255.255" && config.GameName != "Climb")
        {

            udpHandlers.Add(new UdpHandler(config.IpAddress, config.LocalPort, config.RemotePort, config.SocketBReceiverPort, config.NoofLedPerdevice, config.columns, "handler-1"));
            for (int i = 1; i < config.NoOfControllers; i++)
            {
                udpHandlers.Add(new UdpHandler(config.IpAddress, config.LocalPort + i, config.RemotePort + i, config.SocketBReceiverPort + i, config.NoofLedPerdevice, config.columns, $"handler-{i + 1}"));
            }
        }
        Console.WriteLine("Game starting in 3... 2... 1...");
        Task.Run(() => StartAnimition());
        musicPlayer.Announcement(config.isTestMode ? "content/hit2.wav" : config.introAudio);
    }


    public void StartGame()
    {

        Initialize();
        RunGameInSequence();


    }
    protected virtual void RunGameInSequence()
    {
        Status = GameStatus.Running;
        udpHandlers.ForEach(x => x.StartReceive());
        remainingTime = IterationTime; // Initialize remaining time
        OnIteration();
        isGameRunning = true;

        if (config.timerPointLoss)
            iterationTimer = new Timer(UpdateRemainingTime, null, 1000, 1000); // Update every second
        OnStart();
    }
    protected void UpdateRemainingTime(object state)
    {
        if (remainingTime > 0)
        {
            remainingTime -= 1000; // Decrease remaining time by 1 second (1000 ms)
            statusPublisher.PublishStatus(score, lifeLine, Level, status, remainingTime, config.GameName, iterations); // Publish remaining time
        }
        else
        {
            // Time's up, handle as iteration loss
            iterationTimer?.Dispose();
            IterationLost(null);
        }
    }
    protected virtual async void StartAnimition()
    {

    }
    //private CancellationTokenSource _cancellationTokenIterationTimer;
    protected virtual void IterationLost(object state)
    {
        isGameRunning = false;
        udpHandlers.ForEach(x => x.StopReceive());
        if (!config.timerPointLoss && state == null)
        {
            IterationWon();
            return;
        }

        LogData($"iteration failed within {IterationTime} second");
        if (config.timerPointLoss)
            iterationTimer.Dispose();
        LifeLine = LifeLine - 1;
        Status = $"{GameStatus.Running} : Lost Lifeline {LifeLine}";
        if (lifeLine <= 0)
        {
            //TexttoSpeech: Oh no! You’ve lost all your lives. Game over! 🎮
            musicPlayer.Announcement("content/voicelines/GameOver.mp3", false);
            LogData("GAME OVER");
            Status = GameStatus.Completed;
            

        }
        else
        {

            musicPlayer.Announcement($"content/voicelines/lives_left_{LifeLine}.mp3");
            //iterations = iterations + 1;
            RunGameInSequence();
        }

    }

    protected virtual void OnIteration()
    {

    }
    public void EndGame()
    {

        if (iterationTimer != null)
            iterationTimer.Dispose();
        // logger.Dispose();
        musicPlayer.Dispose();
        OnEnd();
        BlinkAllAsync(2);
        SendSameColorToAllDevice(config.NoofLedPerdevice == 1 ? ColorPaletteone.NoColor : ColorPalette.noColor3);

        foreach (var handler in udpHandlers)
        {
            if (handler != null)
                handler.Close();

        }
        Thread.Sleep(2000);

        //lightonoff(false);

        //Status = GameStatus.Completed;


    }
    protected virtual void Initialize() { }
    protected virtual void OnStart() { }
    protected virtual void OnEnd() { }

    public void LogData(string message)
    {
        logger.Log($"scorecard: {message}");

    }
    protected void SendDataToDevice(string color, int deviceNo)
    {

        foreach (var handler in udpHandlers)
        {
            handler.DeviceList[deviceNo] = color;
            handler.SendColorsToUdp(handler.DeviceList);
        }
    }
    protected void SendSameColorToAllDevice(string color)
    {
        foreach (var handler in udpHandlers)
        {
            handler.SendColorsToUdp(handler.DeviceList.Select(x => color).ToList());
        }

    }
    protected void SendSameColorToAllDevice(string color, bool reset)
    {
        if (reset)
        {
            foreach (var handler in udpHandlers)
            {
                for (int x = 0; x < handler.DeviceList.Count; x++)
                {
                    handler.DeviceList[x] = color;
                }
                handler.SendColorsToUdp(handler.DeviceList);
            }

        }
        else
        {
            foreach (var handler in udpHandlers)
            {
                handler.SendColorsToUdp(handler.DeviceList.Select(x => color).ToList());
            }
        }

    }
    protected virtual void IterationWon()
    {
        isGameRunning = false;
        udpHandlers.ForEach(x => x.StopReceive());
        LogData($"All targets hit iterations:{iterations} passed");
        if (config.timerPointLoss)
            iterationTimer.Dispose();
        iterations = iterations + 1;



        if (iterations >= config.Maxiterations)
        {

            Status = $"{GameStatus.Running}: Moved to Next Level {Level}";
            LogData($"Game Win level: {Level}");
            Level = Level + 1;
            iterations = 1;
            if (Level >= config.MaxLevel)
            {
                Status = $"Reached to last Level {config.MaxLevel} ending game";
                LogData(Status);
                //Text to speech : Congratulations! 🎉You’ve won the game! You’ve completed all the levels. You’re a champion! 🏆
                musicPlayer.Announcement("content/GameWinAlllevelPassed.mp3");
                Status = GameStatus.Completed;
                EndGame();
                return;
            }
            else
            {
                //   musicPlayer.StopBackgroundMusic();
                //Text to speech: Great job, Team! 🎉You’ve won this level! Now, get ready for the next one.Expect more energy and excitement.  Let’s go! 🚀 one two three go 
                LogData(Status);
                musicPlayer.Announcement($"content/voicelines/level_{Level}.mp3");
                //                musicPlayer.PlayBackgroundMusic("content/background_music.wav", true);
            }
            //labelScore.Text = $"Score: {score}";

        }
        else { BlinkAllAsync(1); }
        Status = $"{GameStatus.Running}: Moved to Next iterations {iterations}";
        //  if (IterationTime > 0)
        // {
        RunGameInSequence();
        LogData($"moving to next iterations: {iterations} Iteration time: {IterationTime} ");
        // }
        // else { Status = $"All iterations completed IterationTime:{IterationTime}";
        //     LogData($"{Status}: {iterations} Iteration time: {IterationTime} ");
        // }

    }
    protected void updateScore(int score)
    {
        Score = score;
        int random = new Random().Next(0, 9);
        if (0 <= random && random < 3) { musicPlayer.PlayEffect("content//hit2.wav"); }
        if (3 <= random && random < 6) { musicPlayer.PlayEffect("content/hit2.wav"); }
        if (6 <= random) { musicPlayer.PlayEffect("content/hit2.wav"); }
        //        musicPlayer.backgroundMusicPlayer.Volume = 0.8f;
    }
    protected void ChnageColorToDevice(string color, int deviceNo, UdpHandler handler)
    {
        ChnageColorToDevice(color, new List<int> { deviceNo }, handler);
    }
    protected void ChnageColorToDevice(string color, List<int> deviceNos, UdpHandler handler)
    {
        try
        {
            //  Console.WriteLine($"before change: {string.Join(",", handler.DeviceList)}");
            foreach (int x in deviceNos)
            {
                handler.DeviceList[x] = color;
            }
            handler.SendColorsToUdp(handler.DeviceList);
            //Console.WriteLine($"after change: {string.Join(",", handler.DeviceList)}");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.StackTrace);
            // ChnageColorToDevice(color, deviceNo, handler);
        }
    }

    protected void SendColorToDevices(string color, bool isDeviceToBeUpdate)
    {

        var tasks = new List<Task>();
        foreach (var handler in udpHandlers)
        {
            if (isDeviceToBeUpdate)
            {
                for (int x = 0; x < handler.DeviceList.Count; x++)
                {
                    handler.DeviceList[x] = color;
                }

            }
            var colors = handler.DeviceList.Select(x => color).ToList();
            tasks.Add(handler.SendColorsToUdpAsync(colors));

        }
        Task.WhenAll(tasks);
    }


    protected virtual void BlinkAllAsync(int nooftimes)
    {
        for (int i = 0; i < nooftimes; i++)
        {
            var tasks = new List<Task>();
            foreach (var handler in udpHandlers)
            {

                var colors = handler.DeviceList.Select(x => config.NoofLedPerdevice == 1 ? ColorPaletteone.Yellow : ColorPalette.yellow).ToList();
                tasks.Add(handler.SendColorsToUdpAsync(colors));
            }
            Task.WhenAll(tasks);
            Thread.Sleep(100);

            foreach (var handler in udpHandlers)
            {
                tasks.Add(handler.SendColorsToUdpAsync(handler.DeviceList));
                // var colors = handler.SendColorsToUdpAsync(handlerDevices[handler]);
            }
            Task.WhenAll(tasks);
            Thread.Sleep(100);
        }
    }
    public virtual void BlinkLights(List<int> lightIndex, int repeation, UdpHandler handler, string Color)
    {
        for (int j = 0; j < repeation; j++)
        {
            handler.SendColorsToUdp(handler.DeviceList.Select((x, i) => lightIndex.Contains(i) ? Color : x).ToList());
            Thread.Sleep(100);
            handler.SendColorsToUdp(handler.DeviceList);
        }
    }
    public virtual void BlinkLights(List<int> lightIndex, int repeation, string color)
    {

    }
    protected void LoopAll()
    {
        LoopAll((config.NoofLedPerdevice == 1 ? ColorPaletteone.NoColor : ColorPalette.noColor3), 1);
    }
    protected virtual void LoopAll(string basecolor, int frequency)
    {
        for (int i = 0; i < frequency; i++)
        {
            foreach (var handler in udpHandlers)
            {
                var deepCopiedList = handler.DeviceList.Select(x => basecolor).ToList();
                // handler.SendColorsToUdp(deepCopiedList);

                var loopColor = gameColors[random.Next(gameColors.Count - 1)];
                for (int j = 0; j < handler.DeviceList.Count; j++)
                {
                    deepCopiedList[j] = loopColor;
                    handler.SendColorsToUdp(deepCopiedList);
                    Thread.Sleep(100);
                    deepCopiedList[j] = basecolor;
                    handler.SendColorsToUdp(deepCopiedList);
                    Thread.Sleep(100);
                }

                LogData($"LoopAll: {string.Join(",", deepCopiedList)}");
            }
        }
    }
    protected int PositionToIndex(int row, int column)
    {
        int index = 0;
        int handler;
        if (row <= (udpHandlers[0].Rows - 1))
        {
            index = row * udpHandlers[0].columns + column;
        }
        else if (row > (udpHandlers[0].Rows - 1))
        {
            handler = 0;
            index = row - udpHandlers[0].DeviceList.Count;

        }

        return index;
    }
    protected int Resequencer(int index, UdpHandler handler)
    {
        if ((index / handler.columns) % 2 == 0)
        {
            return index;
        }

        int columns = handler.columns;
        int row = index / columns;
        int column = index % columns;
        int dest = (row + 1) * columns - 1 - column;
        return dest;
    }
    protected List<string> getColorList()
    {
        List<string> colorList = new List<string>();


        // Get all public static string fields from ColorPalette
        _FieldInfo[] fields;
        if (config.NoofLedPerdevice == 3)
            fields = typeof(ColorPalette).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        else
            fields = typeof(ColorPaletteone).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);



        foreach (var field in fields)
        {
            if (field.FieldType == typeof(string))
            {
                string value = (string)field.GetValue(null); // null because it's a static field
                colorList.Add(value);
            }
        }
        return colorList;
    }

    public void Dispose()
    {
        foreach(var udpHandler in udpHandlers)
        {
            udpHandler.Close();
        }

    }
}