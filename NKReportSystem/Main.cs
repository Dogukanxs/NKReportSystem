using Newtonsoft.Json;
using Rocket.API;
using Rocket.Core.Commands;
using Rocket.Core.Logging;
using Rocket.Core.Plugins;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using static NKReportSystem.Main;

namespace NKReportSystem
{
    public class Main : RocketPlugin<NKReportSystemConfiguration>
    {
        protected override void Load()
        {
            Logger.Log("+++++++++++++++++++++++++");
            Logger.Log("NK Report System Loaded!");
            Logger.Log("+++++++++++++++++++++++++");
        }
        protected override void Unload()
        {
            Logger.Log("-------------------------");
            Logger.Log("NK Report System Unloaded!!");
            Logger.Log("-------------------------");
        }

        [RocketCommand("raporla", "Bir oyuncu hakkında bilgi alır.", "<oyuncu ismi> <sebep>",  AllowedCaller.Both)]
        [RocketCommandPermission("nk.raporla")]
        public void Execute(IRocketPlayer caller, string[] command)
        {
            if (command.Length < 2)
            {
                UnturnedChat.Say(caller, "Kullanım: /Raporla <isim> <sebep>");
                return;
            }

            var playerName = command[0];
            var reportReason = string.Join(" ", command, 1, command.Length - 1);

            UnturnedPlayer player = (UnturnedPlayer)caller;

            UnturnedPlayer reportedPlayer = UnturnedPlayer.FromName(playerName);
            if (reportedPlayer == null)
            {
                UnturnedChat.Say(caller, $"{playerName} adlı oyuncu bulunamadı veya çevrimdışı.");
                return;
            }

            string reportedPlayerId = reportedPlayer.CSteamID.ToString();

            sendWebhook(player.DisplayName, reportedPlayer.DisplayName, reportedPlayerId, reportReason);
            UnturnedChat.Say("Rapor Gönderildi!");
        }

        private void sendWebhook(string reporter, string reportedPlayer, string reportedPlayerId, string reportReason)
        {

            var payload = new
            {
                content = $"***Rapor Gönderildi***" +
                $"   Raporlayan: `{reporter}`" +
                $"  -  Raporlanan Oyuncu: `{reportedPlayer}`" +
                $"  -  Raporlanan 64ID: `{reportedPlayerId}`" +
                $"  -  Sebep: `{reportReason}`"
            };

            Task.Run(() =>
            {
                try
                {
                    var jsonPayload = JsonConvert.SerializeObject(payload);
                    var request = (HttpWebRequest)WebRequest.Create(Configuration.Instance.webhookUrl);
                    request.Method = "POST";
                    request.ContentType = "application/json";

                    using (var streamWriter = new StreamWriter(request.GetRequestStream()))
                    {
                        streamWriter.Write(jsonPayload);
                    }

                    var response = (HttpWebResponse)request.GetResponse();
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        Logger.Log("Rapor gönderildi!");
                    }
                    else
                    {
                        Logger.LogError($"Rapor gönderilmedi! StatusCode: {response.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Hata: {ex.Message}");
                }
            });
        }

        public class NKReportSystemConfiguration : IRocketPluginConfiguration
        {
            public string webhookUrl { get; set; }

            public void LoadDefaults()
            {
                webhookUrl = "Webhook URL";
            }
        }
    }
}
