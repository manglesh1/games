using System;
using System.Collections.Generic;
using System.Management;

public class ComPortInfo
{
    public string Port { get; set; }
    public string Name { get; set; }
    public string DeviceID { get; set; }
}

public class ComPortScanner
{
    public static List<ComPortInfo> GetAvailableComPorts()
    {
        var portList = new List<ComPortInfo>();
        var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE Name LIKE '%(COM%'");

        foreach (ManagementObject obj in searcher.Get())
        {
            try
            {
                var name = obj["Name"]?.ToString();       // Friendly Name (e.g., USB-SERIAL CH340 (COM4))
                var deviceId = obj["DeviceID"]?.ToString(); // e.g., USB\VID_1A86&PID_7523\...
                var portMatch = System.Text.RegularExpressions.Regex.Match(name, @"\(COM\d+\)");

                if (portMatch.Success)
                {
                    var portName = portMatch.Value.Trim('(', ')');
                    portList.Add(new ComPortInfo
                    {
                        Port = portName,
                        Name = name,
                        DeviceID = deviceId
                    });
                }
            }
            catch (Exception ex)
            {
                // Optional: log or ignore
            }
        }

        return portList;
    }
}
