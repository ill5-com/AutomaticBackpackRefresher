using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Threading;

namespace AutomaticBackpackRefresher
{
    public class UserConfig
    {
        public string steamId64 { get; set; }
        public string backpackToken { get; set; }
        public int requestDelay { get; set; }
        public UserConfig(string _steamId64, string _backpackToken, int _requestDelay = 300000)
        {
            steamId64 = _steamId64;
            backpackToken = _backpackToken;
            requestDelay = _requestDelay;
        }
    }
    internal class Program
    {
        public static WebClient webClient = new WebClient();
        public static UserConfig userConfig;
        public static readonly string configFile = "config.json";
        static void RefreshBackpack()
        {
            Console.WriteLine("Sending refresh request...");

            try
            {
                webClient.UploadString($"https://backpack.tf/api/inventory/{userConfig.steamId64}/refresh?token={userConfig.backpackToken}", string.Empty);
            }
            catch
            {
                // No care me!
            }
        }
        static void GetConfigFromCommandLine()
        {
            Console.Write("Please enter your SteamID64: ");
            string steamId64 = Console.ReadLine();

            Console.Write("Please enter your Backpack.tf token: ");
            string backpackToken = Console.ReadLine();

            userConfig = new UserConfig(steamId64, backpackToken);
        }
        static void LoadConfig()
        {
            if (!File.Exists(configFile))
                GetConfigFromCommandLine();
            else
            {
                userConfig = JsonConvert.DeserializeObject<UserConfig>(File.ReadAllText(configFile));
            }

            File.WriteAllText(configFile, JsonConvert.SerializeObject(userConfig));
        }
        static void Main(string[] args)
        {
            Console.Title = "Automatic Backpack Refresher — www.Ill5.com (c) 2023";
            Console.WriteLine("Automatic Backpack Refresher — www.Ill5.com (c) 2023\n");

            LoadConfig();

            Console.WriteLine("Started!");

            while (true)
            {
                RefreshBackpack();
                Thread.Sleep(userConfig.requestDelay);
            }
        }
    }
}
