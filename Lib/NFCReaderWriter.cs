﻿using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using PCSC;
using PCSC.Iso7816;
using PCSC.Monitoring;

namespace Lib
{
    public class NFCReaderWriter : IDisposable
    {
        private readonly string readerName;
        private ISCardContext context;
        private ISCardMonitor monitor;
        private ClientWebSocket webSocket;

        public event EventHandler<string> StatusChanged;
        protected virtual void OnStatusChanged(string newStatus)
        {
            StatusChanged?.Invoke(this, newStatus);
        }
        AsyncLogger logger = new AsyncLogger("NFCReaderWriter.log");
        public NFCReaderWriter(string mode)
        {
            var availableReaders = ContextFactory.Instance.Establish(SCardScope.System).GetReaders();
            if (availableReaders.Length == 0)
            {
                Console.WriteLine("No readers found.");
                return;
            }


            this.readerName = availableReaders[0];
            context = ContextFactory.Instance.Establish(SCardScope.System);

            monitor = new SCardMonitor(ContextFactory.Instance, SCardScope.System);
            monitor.CardInserted += (sender, args) =>
            {
               
                logger.Log($"Card inserted, processing...{args.ReaderName}");

                string uid = WriteData(args.ReaderName);

                if (!string.IsNullOrEmpty(uid))
                {
                    string result = "";
                    if (mode == "I")
                    {
                        result = InsertRecord(uid);
                    }
                    else if (mode == "R")
                    {
                        result = ifCardRegisted(uid);
                    }
                    else if (mode == "V")
                    {
                        result = ifPlayerHaveTime(uid);
                    }
                    logger.Log($"uuid:{uid} result:{result}");
                    Console.WriteLine(result);
                    OnStatusChanged(result.Length == 0 ? uid : "");
                
                    // SendUidToWebSocket(uid).Wait();
                }
            };
            monitor.Start(readerName);
            //while (true)
            //{
            //    Thread.Sleep(1000); // Sleep to reduce CPU usage, adjust as needed.
            //}
        }

        private string ifPlayerHaveTime(string uid)
        {
            string query = $"SELECT count(*) FROM [dbo].[WristbandTrans] WHERE wristbandCode = '{uid}' AND playerEndDate > GETDATE() and wristbandStatusFlag='R' ";


            logger.Log(query);

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    int result =  (int)cmd.ExecuteScalar();
                    return result <= 0 ? "Error:Wristband Not in db!" : "";
                }
            }
        }

        private string connectionString = "Server=192.186.105.194;Initial Catalog=Games;User Id=admin;Password=Aero@password1;TrustServerCertificate=True";

        public string ifCardRegisted(string uid)
        {
            string query = $"SELECT count(*) FROM [dbo].[WristbandTrans] WHERE wristbandCode = '{uid}' AND WristbandTranDate > DATEADD(HOUR, -1, GETDATE()) and wristbandStatusFlag='I' ";


            logger.Log(query);

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    int result = (int)cmd.ExecuteScalar();
                    return result <= 0 ? $"Error:Wristband Not in db! count:{result}" : "";


                }
            }
        }

        public string InsertRecord(string uid)
        {
            string query = $"IF NOT EXISTS (SELECT * FROM [dbo].[WristbandTrans] WHERE wristbandCode = '{uid}' AND WristbandTranDate > DATEADD(HOUR, -1, GETDATE())) " +
                           "INSERT INTO [dbo].[WristbandTrans] (wristbandCode, wristbandStatusFlag, WristbandTranDate, createdat, updatedat) " +
                           $"VALUES('{uid}', 'I', GETDATE(), GETDATE(), GETDATE())";
            logger.Log(query);

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    int result = cmd.ExecuteNonQuery();
                    return result < 0 ? "Error inserting data into Database!" : "";

                }
            }
        }

        private string WriteData(string readerName)
        {
            try
            {
                using (var r = context.ConnectReader(readerName, SCardShareMode.Shared, SCardProtocol.Any))
                {
                    var apdu = new CommandApdu(IsoCase.Case2Short, r.Protocol)
                    {
                        CLA = 0xFF,
                        Instruction = InstructionCode.GetData,
                        P1 = 0x00,
                        P2 = 0x00,
                        Le = 0
                    };

                    using (r.Transaction(SCardReaderDisposition.Leave))
                    {
                        Console.WriteLine("Retrieving the UID .... ");
                        var sendPci = SCardPCI.GetPci(r.Protocol);
                        var receivePci = new SCardPCI();

                        var receiveBuffer = new byte[256];
                        var command = apdu.ToArray();

                        var bytesReceived = r.Transmit(
                            sendPci,
                            command,
                            command.Length,
                            receivePci,
                            receiveBuffer,
                            receiveBuffer.Length);

                        var responseApdu = new ResponseApdu(receiveBuffer, bytesReceived, IsoCase.Case2Short, r.Protocol);
                        Console.WriteLine($"SW1: {responseApdu.SW1:X2}, SW2: {responseApdu.SW2:X2}\nUid: {BitConverter.ToString(responseApdu.GetData())}");
                        return BitConverter.ToString(responseApdu.GetData()).Replace("-", "");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: " + ex.Message);
            }
            return "";
        }

        private async Task SendUidToWebSocket(string uid)
        {
            try
            {
                if (webSocket == null || webSocket.State != WebSocketState.Open)
                {
                    webSocket = new ClientWebSocket();
                    await webSocket.ConnectAsync(new Uri("ws://localhost:8080"), CancellationToken.None);
                }

                var data = Encoding.UTF8.GetBytes(uid);
                var buffer = new ArraySegment<byte>(data);
                await webSocket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
                Console.WriteLine($"UID sent to WebSocket: {uid}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("WebSocket error: " + ex.Message);
            }
        }

        public void Dispose()
        {
            monitor.Cancel();
            context.Dispose();
            webSocket?.Dispose();
        }
    }

}