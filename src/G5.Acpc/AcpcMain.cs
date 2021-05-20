﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using G5.Logic;


namespace G5.Acpc
{
    class Program
    {
        private static string printTime(TimeSpan time)
        {
            return time.TotalMilliseconds.ToString("f2") + "ms";
        }

        private static void sendMessage(NetworkStream stream, string message)
        {
            byte[] data = System.Text.Encoding.ASCII.GetBytes(message + "\r\n");
            stream.Write(data, 0, data.Length);
        }

        private static int findSplitter(byte[] data, int size)
        {
            byte r = Convert.ToByte('\r');
            byte n = Convert.ToByte('\n');

            for (int i = 0; i < size - 1; i++)
            {
                if (data[i] == r && data[i+1] == n)
                    return i;
            }

            return -1;
        }

        private static void gameLoop(TcpClient tcpClient, NetworkStream stream, AcpcGame game)
        {
            var data = new byte[512];
            int numberOfBytesRead = 0;

            var protocolVersion = "VERSION:2.0.0";
            Console.WriteLine("Sending protocol version to server '{0}'", protocolVersion);
            sendMessage(stream, protocolVersion);

            int nempty = 0;

            while (true)
            {
                Console.WriteLine("Waiting for server input, bytes in buffer {0}", numberOfBytesRead);
                numberOfBytesRead += stream.Read(data, numberOfBytesRead, data.Length - numberOfBytesRead);
                int pos = findSplitter(data, numberOfBytesRead);
                Console.WriteLine("Message arrived, bytes in buffer {0}, fount delimeter at {1}", numberOfBytesRead, pos);

                if (numberOfBytesRead == 0)
                {
                    if (tcpClient.Connected && nempty < 20)
                    {
                        nempty++;
                        Console.WriteLine("Recieved {0} empty message.", nempty);
                        System.Threading.Thread.Sleep(50);
                        continue;
                    }
                    else
                    {
                        Console.WriteLine("Recieved too many empty messages  or tcpClient is disconected, ending the match.");
                        game.finishHand();
                        Console.WriteLine("Total Saldo: {0}", game.TotalSaldo);
                        break;
                    }
                }

                nempty = 0;

                while (pos > -1)
                {
                    if (pos > 0)
                    {
                        string message = System.Text.Encoding.ASCII.GetString(data, 0, pos);
                        //Console.WriteLine("Parsed message: {0}", message);

                        if (message[0] != '#' && message[0] != ';') // Comment or GUI command
                        {
                            string gameResponse = game.acceptMessageFromServer(message);

                            if (gameResponse != null && gameResponse != "")
                                sendMessage(stream, gameResponse);
                        }
                    }

                    for (int i = pos + 2; i < numberOfBytesRead; i++)
                        data[i - pos - 2] = data[i];

                    numberOfBytesRead -= pos + 2;
                    pos = findSplitter(data, numberOfBytesRead);

                    if (pos > -1)
                        Console.WriteLine("Found delimeter at {0}", pos);
                }
            }
        }

        private static void connect(string server, int port)
        {
            try
            {
                using (TcpClient client = new TcpClient())
                {
                    Console.WriteLine("Trying connection on: {0}:{1}", server, port);

                    var connectionsTask = client.ConnectAsync(server, port);
                    connectionsTask.Wait();

                    Console.WriteLine("Connected to a server.");

                    using (NetworkStream stream = client.GetStream())
                    //using (var game = new AcpcGame(TableType.HeadsUp))
                    using (var game = new AcpcGame(TableType.SixMax))
                    {
                        gameLoop(client, stream, game);
                    }
                }
            }
            catch (System.AggregateException e)
            {
                Console.WriteLine("Exception: {0}", e);
            }
            catch (System.Exception e)
            {
                Console.WriteLine("Exception: {0}", e);
            }
        }

        private static void testParse1()
        {
            var mState1 = MatchState.Parse("MATCHSTATE:0:30::9s8h", 2);
            var mState2 = MatchState.Parse("MATCHSTATE:0:30:c:9s8h|", 2);
            var mState3 = MatchState.Parse("MATCHSTATE:0:30:cc/:9s8h|/8c8d5c", 2);
            var mState4 = MatchState.Parse("MATCHSTATE:0:30:cc/r250:9s8h|/8c8d5c", 2);
            var mState5 = MatchState.Parse("MATCHSTATE:0:30:cc/r250c/:9s8h|/8c8d5c/6s", 2);
            var mState6 = MatchState.Parse("MATCHSTATE:0:30:cc/r250c/r500:9s8h|/8c8d5c/6s", 2);
            var mState7 = MatchState.Parse("MATCHSTATE:0:30:cc/r250c/r500c/:9s8h|/8c8d5c/6s/2d", 2);
            var mState8 = MatchState.Parse("MATCHSTATE: 0:30:cc/r250c/r500c/r1250:9s8h|/8c8d5c/6s/2d", 2);
            var mState9 = MatchState.Parse("MATCHSTATE:0:30:cc/r250c/r500c/r1250c:9s8h|9c6h/8c8d5c/6s/2d", 2);
        }

        private static void testParse2()
        {
            var mState1  = MatchState.Parse("MATCHSTATE:1:31::|JdTc", 2);
            var mState2  = MatchState.Parse("MATCHSTATE:1:31:r300:|JdTc", 2);
            var mState3  = MatchState.Parse("MATCHSTATE:1:31:r300r900:|JdTc", 2);
            var mState4  = MatchState.Parse("MATCHSTATE:1:31:r300r900c/:|JdTc/6dJc9c", 2);
            var mState5  = MatchState.Parse("MATCHSTATE:1:31:r300r900c/r1800:|JdTc/6dJc9c", 2);
            var mState6  = MatchState.Parse("MATCHSTATE:1:31:r300r900c/r1800r3600:|JdTc/6dJc9c", 2);
            var mState7  = MatchState.Parse("MATCHSTATE:1:31:r300r900c/r1800r3600r9000:|JdTc/6dJc9c", 2);
            var mState8  = MatchState.Parse("MATCHSTATE:1:31:r300r900c/r1800r3600r9000c/:|JdTc/6dJc9c/Kh", 2);
            var mState9  = MatchState.Parse("MATCHSTATE:1:31:r300r900c/r1800r3600r9000c/r20000:|JdTc/6dJc9c/Kh", 2);
            var mState10 = MatchState.Parse("MATCHSTATE:1:31:r300r900c/r1800r3600r9000c/r20000c/:KsJs|JdTc/6dJc9c/Kh/Qc", 2);
        }

        private static void testParse_SixMax()
        {
            var mState1  = MatchState.Parse("MATCHSTATE:0:90::Ad6h|||||", 6);
            var mState2  = MatchState.Parse("MATCHSTATE:0:90:c:Ad6h|||||", 6);
            var mState3  = MatchState.Parse("MATCHSTATE:0:90:cr300:Ad6h|||||", 6);
            var mState4  = MatchState.Parse("MATCHSTATE:0:90:cr300f:Ad6h|||||", 6);
            var mState5  = MatchState.Parse("MATCHSTATE:0:90:cr300fc/:Ad6h|||||/TsKd7h", 6);
            var mState6  = MatchState.Parse("MATCHSTATE:0:90:cr300fc/r900:Ad6h|||||/TsKd7h", 6);
            var mState7  = MatchState.Parse("MATCHSTATE:0:90:cr300fc/r900c/:Ad6h|||||/TsKd7h/Kh", 6);
            var mState8  = MatchState.Parse("MATCHSTATE:0:90:cr300fc/r900c/r2000:Ad6h|||||/TsKd7h/Kh", 6);
            var mState9  = MatchState.Parse("MATCHSTATE:0:90:cr300fc/r900c/r2000c/:Ad6h|||||/TsKd7h/Kh/6d", 6);
            var mState10 = MatchState.Parse("MATCHSTATE:0:90:cr300fc/r900c/r2000c/r4000:Ad6h|||||/TsKd7h/Kh/6d", 6);
            var mState11 = MatchState.Parse("MATCHSTATE:0:90:cr300fc/r900c/r2000c/r4000c:Ad6h|||||Td2h/TsKd7h/Kh/6d", 6);
        }

        private static void testParse_Log()
        {
            var mState1 = MatchState.ParseLog("STATE:759:r200c/cr400c/cr800f:3s4s|Jc8d/7d3c8c/Kh:-400|400:Intermission_2pn_2017|Feste_2pn_2017", 2);
            var mState2 = MatchState.ParseLog("STATE:946:r273c/cc/r547c/cc:Jc7c|JhQd/9hAc5c/Js/Kh:-547|547:Feste_2pn_2017|Intermission_2pn_2017", 2);
            var mState3 = MatchState.ParseLog("STATE:947:r300f:6s9d|7hAh:-100|100:Intermission_2pn_2017|Feste_2pn_2017", 2);
            var mState5 = MatchState.ParseLog("STATE:949:cr550c/cc/cc/cc:5c5h|4hAd/7cQd3c/6h/9c:550|-550:Intermission_2pn_2017|Feste_2pn_2017", 2);
        }

        private static void testAcpcGame()
        {
            using (var game = new AcpcGame(TableType.HeadsUp))
            {
                string gameResponse;

                /*gameResponse = game.acceptMessageFromServer("MATCHSTATE:0:30::9s8h");
                gameResponse = game.acceptMessageFromServer("MATCHSTATE:0:30:c:9s8h|");
                gameResponse = game.acceptMessageFromServer("MATCHSTATE:0:30:cc/:9s8h|/8c8d5c");
                gameResponse = game.acceptMessageFromServer("MATCHSTATE:0:30:cc/r250:9s8h|/8c8d5c");
                gameResponse = game.acceptMessageFromServer("MATCHSTATE:0:30:cc/r250c/:9s8h|/8c8d5c/6s");
                gameResponse = game.acceptMessageFromServer("MATCHSTATE:0:30:cc/r250c/r500:9s8h|/8c8d5c/6s");
                gameResponse = game.acceptMessageFromServer("MATCHSTATE:0:30:cc/r250c/r500c/:9s8h|/8c8d5c/6s/2d");
                gameResponse = game.acceptMessageFromServer("MATCHSTATE: 0:30:cc/r250c/r500c/r1250:9s8h|/8c8d5c/6s/2d");
                gameResponse = game.acceptMessageFromServer("MATCHSTATE:0:30:cc/r250c/r500c/r1250c:9s8h|9c6h/8c8d5c/6s/2d");

                gameResponse = game.acceptMessageFromServer("MATCHSTATE:1:31::|JdTc");
                gameResponse = game.acceptMessageFromServer("MATCHSTATE:1:31:r300:|JdTc");
                gameResponse = game.acceptMessageFromServer("MATCHSTATE:1:31:r300r900:|JdTc");
                gameResponse = game.acceptMessageFromServer("MATCHSTATE:1:31:r300r900c/:|JdTc/6dJc9c");
                gameResponse = game.acceptMessageFromServer("MATCHSTATE:1:31:r300r900c/r1800:|JdTc/6dJc9c");
                gameResponse = game.acceptMessageFromServer("MATCHSTATE:1:31:r300r900c/r1800r3600:|JdTc/6dJc9c");
                gameResponse = game.acceptMessageFromServer("MATCHSTATE:1:31:r300r900c/r1800r3600r9000:|JdTc/6dJc9c");
                gameResponse = game.acceptMessageFromServer("MATCHSTATE:1:31:r300r900c/r1800r3600r9000c/:|JdTc/6dJc9c/Kh");
                gameResponse = game.acceptMessageFromServer("MATCHSTATE:1:31:r300r900c/r1800r3600r9000c/r20000:|JdTc/6dJc9c/Kh");
                gameResponse = game.acceptMessageFromServer("MATCHSTATE:1:31:r300r900c/r1800r3600r9000c/r20000c/:KsJs|JdTc/6dJc9c/Kh/Qc");*/

                /*gameResponse = game.acceptMessageFromServer("MATCHSTATE:0:2::Ah8s|");
                gameResponse = game.acceptMessageFromServer("MATCHSTATE:0:2:r8381:Ah8s|");
                gameResponse = game.acceptMessageFromServer("MATCHSTATE:0:2:r8381r19900:Ah8s|");
                gameResponse = game.acceptMessageFromServer("MATCHSTATE:0:2:r8381r19900r20000:Ah8s|");*/

                /*gameResponse = game.acceptMessageFromServer("MATCHSTATE:0:18::AsJh|");
                gameResponse = game.acceptMessageFromServer("MATCHSTATE:0:18:r11449:AsJh|");
                gameResponse = game.acceptMessageFromServer("MATCHSTATE:0:18:r11449r20000:AsJh|");
                gameResponse = game.acceptMessageFromServer("MATCHSTATE:0:18:r11449r20000c///:AsJh|8d2c/7dAh5c/3s/3c");
                gameResponse = game.acceptMessageFromServer("MATCHSTATE:1:19::|7dKc");*/

                gameResponse = game.acceptMessageFromServer("MATCHSTATE:1:1571::|QsQd");
                gameResponse = game.acceptMessageFromServer("MATCHSTATE:1:1571:r300:|QsQd");
                gameResponse = game.acceptMessageFromServer("MATCHSTATE:1:1571:r300r4062:|QsQd");
                gameResponse = game.acceptMessageFromServer("MATCHSTATE:1:1571:r300r4062r12186:|QsQd");
                gameResponse = game.acceptMessageFromServer("MATCHSTATE:1:1571:r300r4062r12186r20000:|QsQd");
            }
        }

        static void Main(string[] args)
        {
            //testParse_Log();
            
            //testParse1();
            //testParse2();
            //testParse_SixMax();
            //testAcpcGame();
            //return;

            if (args.Length != 2)
            {
                Console.WriteLine("Acpc application expects exactly two arguments as input. Server IP and port number.");
            }
            else
            {
                string serverIp = args[0];
                int portNumber = 0;

                if (int.TryParse(args[1], out portNumber))
                {
                    connect(serverIp, portNumber);
                }
                else
                {
                    Console.WriteLine("Could not parse the port number: {0}", args[1]);
                }
            }
        }
    }
}
