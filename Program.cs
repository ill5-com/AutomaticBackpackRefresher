using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Diagnostics;
using System.Threading;
using System.Runtime.CompilerServices;

namespace AutomaticBackpackRefresher
{
    public class APIResponse
    {
        public int? current_time { get; set; }
        public int? last_update { get; set; }
        public int? timestamp { get; set; }
        public int? next_update { get; set; }
        public int? refresh_interval { get; set; }
    }

    public class UserConfig
    {
        public string steamId64 { get; set; }
        public string backpackToken { get; set; }
        public int fallbackDelay { get; set; }
        public UserConfig(string steamId64, string backpackToken, int fallbackDelay)
        {
            this.steamId64 = steamId64;
            this.backpackToken = backpackToken;
            this.fallbackDelay = fallbackDelay;
        }
    }
    internal class Program
    {
        public static WebClient webClient = new WebClient();
        public static APIResponse lastResponse;
        public static UserConfig userConfig;
        public static readonly string configFile = "config.json";

        [MethodImpl(MethodImplOptions.NoInlining)] // https://stackoverflow.com/a/2652481
        static string GetMethodName(int frameIndex)
        {
            var st = new StackTrace();
            var sf = st.GetFrame(frameIndex);

            return sf.GetMethod().Name;
        }

        static void DebugPrint(string message)
        {
#if DEBUG
            // Get two methods up in the call stack since we use up one call here
            Console.WriteLine($"{GetMethodName(2)}: {message}");
#endif
        }

        static void SleepUntilUnixTimestamp(int timestamp) // BUG: 2038
        {
            int secondsToSleep = timestamp - Convert.ToInt32(DateTimeOffset.Now.ToUnixTimeSeconds());

            if (secondsToSleep <= 0)
            {
                // this should only happen if the API returns a bad next_update value
                // to prevent a flood of requests if this happens just sleep for a user defined amount of time
                secondsToSleep = userConfig.fallbackDelay;
            }

            DebugPrint($"Sleeping for {secondsToSleep} seconds...");

            Thread.Sleep(secondsToSleep * 1000);
        }

        static void ParseAPIException(Exception exception)
        {
            // TODO: implement this
            DebugPrint(exception.ToString());
        }

        static void ParseAPIResponse(string apiResponse)
        {
            APIResponse newResponse;
            try
            {
                newResponse = JsonConvert.DeserializeObject<APIResponse>(apiResponse);
            }
            catch (Exception ex)
            {
                DebugPrint(ex.ToString());
                return;
            }

            if (newResponse.last_update == newResponse.current_time)
            {
                DebugPrint("Refreshed!");
            }

            lastResponse = newResponse;
        }

        static void RefreshBackpack()
        {
            DebugPrint("Sending refresh request...");

            try
            {
                string webResponse = webClient.UploadString($"https://backpack.tf/api/inventory/{userConfig.steamId64}/refresh?token={userConfig.backpackToken}", string.Empty);
                ParseAPIResponse(webResponse);
            }
            catch (Exception ex)
            {
                ParseAPIException(ex);
                return;
            }

            DebugPrint("Sent!");
        }

        static void GetConfigFromCommandLine()
        {
            Console.Write("Please enter your SteamID64: ");
            string steamId64 = Console.ReadLine();

            Console.Write("Please enter your Backpack.tf token: ");
            string backpackToken = Console.ReadLine();

            userConfig = new UserConfig(steamId64, backpackToken, 10);
        }

        static void LoadConfig()
        {
            if (!File.Exists(configFile))
            {
                GetConfigFromCommandLine();
            }
            else
            {
                userConfig = JsonConvert.DeserializeObject<UserConfig>(File.ReadAllText(configFile));
            }

            File.WriteAllText(configFile, JsonConvert.SerializeObject(userConfig));
        }

        static void Main(string[] args)
        {
            Console.Title = "Automatic Backpack Refresher ??? www.Ill5.com (c) 2023";
            Console.WriteLine("Automatic Backpack Refresher ??? www.Ill5.com (c) 2023\n");

            LoadConfig();

            DebugPrint("Started!");

            while (true)
            {
                RefreshBackpack();

                // i don't like how i'm doing this
                if (lastResponse?.next_update.HasValue == false)
                {
                    continue;
                }

                SleepUntilUnixTimestamp(lastResponse.next_update.Value);
            }
        }
    }
}
