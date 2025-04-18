using scorecard;
using scorecard.lib;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static log4net.Appender.ColoredConsoleAppender;

public class Climb : BaseGameClimb
{
    private double targetPercentage;
    private int targetCount;
    private List<int> obstacles = new List<int>();
    private List<int> targets = new List<int>();
    private string obstacleColor;
    private CoolDown coolDown = new CoolDown();
    Task targetTask;
    public Climb(GameConfig config) : base(config)
    {
        this.config.MaxPlayers = 1;
        targetColor = config.NoofLedPerdevice != 3 ? ColorPaletteone.White : ColorPalette.White;
        obstacleColor = config.NoofLedPerdevice != 3 ? ColorPaletteone.Red : ColorPalette.Red;
        BlinkAllAsync(5);
        
    }
    protected override void OnIteration()
    {
        coolDown.SetFlagTrue(200);
        climbHandler.activeDevices.Clear();
        SendSameColorToAllDevice(ColorPaletteone.Red, true);
        obstacles.Clear();
        targets.Clear();
        for (int i = 0; i < climbHandler.DeviceList.Count(); i++)
        {
            climbHandler.DeviceList[i] = config.NoofLedPerdevice == 3 ? ColorPalette.Green : ColorPaletteone.Green;
        }
        ActivateLevel();
        ActivateRandomLights();
    }
    protected override void OnStart()
    {
        climbHandler.BeginReceive(data => ReceiveCallback(data));
        foreach (var handler in udpHandlers)
        {
            handler.BeginReceive(data => ReceiveCallback(data, handler));
        }
        if (targetTask == null || targetTask.IsCompleted)
        {
            if (targetTask != null && !targetTask.IsCompleted)
            {
                logger.Log("targetTask task still running");
            }
            logger.Log("Starting targetTask task");
            targetTask = Task.Run(() => BlinkTargetLight());
        }
    }
    private void BlinkTargetLight()
    {
        if (!isGameRunning)
            return;

        BlinkLights(targets, 1, config.NoofLedPerdevice == 1 ? ColorPaletteone.Blue : ColorPalette.Blue);
        if (isGameRunning)
        {
            Thread.Sleep(2000);
            BlinkTargetLight();
        }
    }
    private void ActivateLevel()
    {
        int totalColumns = config.columns;
        int totalRows = climbHandler.DeviceList.Count / totalColumns;

        switch (Level)
        {
            case 1:
                // Level 1: Simple horizontal line obstacle at mid-height
                for (int row = 0; row < totalRows; row++)
                {
                    int index = GetIndexFromRowCol(row, totalColumns / 2, totalRows);
                    if (index >= 0 && index < climbHandler.DeviceList.Count)
                    {
                        obstacles.Add(index);
                        climbHandler.activeDevices.Add(index);
                    }
                }
                break;

            case 2:
                // Level 2: Two horizontal barriers at different heights
                for (int row = 0; row < totalRows; row++)
                {
                    int index = GetIndexFromRowCol(row, totalColumns / 3, totalRows);
                    if (index >= 0 && index < climbHandler.DeviceList.Count)
                    {
                        obstacles.Add(index);
                        climbHandler.activeDevices.Add(index);
                    }
                    index = GetIndexFromRowCol(row, 2 * totalColumns / 3, totalRows);
                    if (index >= 0 && index < climbHandler.DeviceList.Count)
                    {
                        obstacles.Add(index);
                        climbHandler.activeDevices.Add(index);
                    }
                }
                break;

            case 3:
                // Level 3: Zigzag pattern
                for (int row = 0; row < totalRows; row++)
                {
                    for (int col = 0; col < totalColumns; col++)
                    {
                        if ((row + col) % 2 == 0) // Create a zigzag pattern
                        {
                            int index = GetIndexFromRowCol(row, col, totalRows);
                            if (index >= 0 && index < climbHandler.DeviceList.Count)
                            {
                                obstacles.Add(index);
                                climbHandler.activeDevices.Add(index);
                            }
                        }
                    }
                }
                break;

            case 4:
                // Level 4: X pattern
                for (int i = 0; i < Math.Min(totalRows, totalColumns); i++)
                {
                    int index = GetIndexFromRowCol(i, i, totalRows);
                    if (index >= 0 && index < climbHandler.DeviceList.Count)
                    {
                        obstacles.Add(index); // Top-left to bottom-right diagonal
                        climbHandler.activeDevices.Add(index);
                    }
                    index = GetIndexFromRowCol(totalRows - 1 - i, i, totalRows);
                    if (index >= 0 && index < climbHandler.DeviceList.Count)
                    {
                        obstacles.Add(index); // Bottom-left to top-right diagonal
                        climbHandler.activeDevices.Add(index);
                    }
                }
                break;

            case 5:
                // Level 5: Random gaps in vertical barriers
                for (int col = 0; col < totalColumns; col += 2)
                {
                    for (int row = 0; row < totalRows; row++)
                    {
                        if (random.NextDouble() > 0.2) // Leave some gaps
                        {
                            int index = GetIndexFromRowCol(row, col, totalRows);
                            if (index >= 0 && index < climbHandler.DeviceList.Count)
                            {
                                obstacles.Add(index);
                            }
                        }
                    }
                }
                break;

            default:
                // Higher levels: Dense obstacles with small gaps
                for (int col = totalColumns / 4; col < 3 * totalColumns / 4; col++)
                {
                    for (int row = 0; row < totalRows; row++)
                    {
                        if (random.NextDouble() > 0.3) // Make it more difficult
                        {
                            int index = GetIndexFromRowCol(row, col, totalRows);
                            if (index >= 0 && index < climbHandler.DeviceList.Count)
                            {
                                obstacles.Add(index);
                            }
                        }
                    }
                }
                break;
        }

        // Apply obstacle colors
        foreach (var index in obstacles)
        {
            if (index >= 0 && index < climbHandler.DeviceList.Count)
            {
                climbHandler.DeviceList[index] = obstacleColor;
            }
        }

        climbHandler.SendColorsToUdp(climbHandler.DeviceList);
    }

    // Utility function to convert row and column to index considering snake-wise numbering
    private int GetIndexFromRowCol(int row, int col, int totalRows)
    {
        return col % 2 == 0 ? (row * config.columns + col) : ((totalRows - 1 - row) * config.columns + col);
    }

    private void ActivateRandomLights()
    {
        targetCount = this.config.MaxPlayers * 5 ;
        

        while (targets.Count < targetCount)
        {
            int index = random.Next(climbHandler.DeviceList.Count());
            if (!climbHandler.activeDevices.Contains(index))
            {
                climbHandler.DeviceList[index] = targetColor; // Green indicates the target light
                climbHandler.activeDevices.Add(index);
                targets.Add(index);
            }
        }

        climbHandler.SendColorsToUdp(climbHandler.DeviceList);

    }

    // For walls
    private void ReceiveCallback(byte[] receivedBytes)

    {
        if (!isGameRunning && coolDown.Flag)
            return;
        string receivedData = Encoding.UTF8.GetString(receivedBytes);
        //LogData($"Received data from {this.handler.RemoteEndPoint}: {BitConverter.ToString(receivedBytes)}");
        byte[] filteredBytes = ProcessByteArray(receivedBytes);
        List<int> positions = filteredBytes
                                .Select((b, index) => new { Byte = b, Index = index }) // Select byte and index
                                .Where(x => x.Byte == 0xCC) // Filter where byte equals 0xCC
                                .Select(x => x.Index-1) // Select only the indices
                                .ToList();
        
        //LogData($"Received data from {String.Join(",", positions)}=");
        var touchedActiveDevices = climbHandler.activeDevices.FindAll(x => positions.Contains(x));
        
        if (touchedActiveDevices.Count > 0)
        {
            if (!isGameRunning && coolDown.Flag)
                return;
            foreach (var device in touchedActiveDevices) { climbHandler.DeviceList[device] = ColorPaletteone.NoColor; }
            climbHandler.SendColorsToUdp(climbHandler.DeviceList);
            climbHandler.activeDevices.RemoveAll(x => touchedActiveDevices.Contains(x));
            foreach (var device in touchedActiveDevices)
            {
                if (targets.Contains(device) && !coolDown.Flag)
                {
                    targets.Remove(device);
                    updateScore(Score + Level + LifeLine);
                    LogData($"Score updated: {Score}  position {String.Join(",", positions)} active positions:{string.Join(",", climbHandler.activeDevices)}");
                    
                }
                else if(obstacles.Contains(device) && !coolDown.Flag)
                {
                    coolDown.SetFlagTrue(500);
                    obstacles.Remove(device);
                    updateScore(Score - level);
                    LogData($"Iteration Lost:Score updated: {Score}  position {String.Join(",", positions)} active positions:{string.Join(",", climbHandler.activeDevices)}");
                    
                    IterationLost(null);
                }
            }
        }



        if (targets.Count == 0)
        {
            int random = new Random().Next(0, 9);

            IterationWon();
        }
        else
        {

            climbHandler.BeginReceive(data => ReceiveCallback(data));
        }

    }


    // For floor
    private void ReceiveCallback(byte[] receivedBytes, UdpHandler handler)
    {
        if (!isGameRunning)
            return;

        string receivedData = Encoding.UTF8.GetString(receivedBytes);
        var positions = receivedData
            .Select((value, index) => new { value, index })
            .Where(x => x.value == 0x0A)
            .Select(x => x.index - 2)
            .Where(position => position >= 0)
            .ToList();

        try
        {
            if (positions.Count > 0 && !coolDown.Flag)
            {
                foreach (var position in positions)
                {
                    handler.DeviceList[position] = ColorPaletteone.NoColor;
                }
                handler.SendColorsToUdp(handler.DeviceList);
                IterationLost(null);
            }
        }
        catch (Exception ex)
        {
            LogData($"Exception in ReceiveCallback: {ex.Message}");
        }

        handler.BeginReceive(data => ReceiveCallback(data, handler));

    }
}