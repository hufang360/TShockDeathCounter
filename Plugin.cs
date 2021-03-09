using System;
using System.IO;
using System.Diagnostics;
using System.Text;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using TerrariaApi.Server;
using Newtonsoft.Json;
using TShockAPI;

namespace TerrariaDeathCounter
{
    [ApiVersion(2, 1)]
    public class TerrariaDeathCounterPlugin : TerrariaPlugin
    {
        /// <summary>
        /// The name of the plugin.
        /// </summary>
        public override string Name => "Death Recorder";

        /// <summary>
        /// The version of the plugin in its current state.
        /// </summary>
        public override Version Version => new Version(1, 0);

        /// <summary>
        /// The author(s) of the plugin.
        /// </summary>
        public override string Author => "Discoveri";

        /// <summary>
        /// A short, one-line, description of the plugin's purpose.
        /// </summary>
        public override string Description => "Records and reports player deaths from each source, for laughs and stats.";

        //private static string saveFilename = "DeathRecords.json";
        private static string saveFilename = Path.Combine(TShock.SavePath, "DeathRecords.json");
        private static IDeathRepository deathRecords = new JsonDeathRepository(saveFilename);
        //private static string logFilename = Path.Combine(TShock.SavePath, "DeathOutput.log");
        //private static ILogWriter logWriter = new ServerLogWriter(logFilename);

        public TerrariaDeathCounterPlugin(Main game) : base(game)
        {

        }

        /// <summary>
        /// Performs plugin initialization logic.
        /// </summary>
        public override void Initialize()
        {
            ServerApi.Hooks.NetGetData.Register(this, OnGetData);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.NetGetData.Deregister(this, OnGetData);
            }
            base.Dispose(disposing);
        }

        private void OnGetData(GetDataEventArgs e)
        {
            if (e.Handled)
                return;

            PacketTypes type = e.MsgID;
            if (type != PacketTypes.PlayerDeathV2)
            {
                return;
            }

            using (MemoryStream data = new MemoryStream(e.Msg.readBuffer, e.Index, e.Length - 1))
            {
                var player = Main.player[e.Msg.whoAmI];
                RecordPlayerDeath(player, data);
            }
        }

        private bool RecordPlayerDeath(Player player, MemoryStream data)
        {
            // Unused initial ID, read byte so FromReader is properly aligned in stream.
            data.ReadByte();
            PlayerDeathReason playerDeathReason = PlayerDeathReason.FromReader(new BinaryReader(data));

            // Record failure in current death repository.
            string killerName = GetNameOfKiller(player, playerDeathReason);
            string deathText = playerDeathReason.GetDeathText(player.name).ToString();
            int totalDeathCount = deathRecords.RecordDeath(player.name, killerName);

            // Log objects for reference.
            //logWriter.ServerWriteLine(JsonConvert.SerializeObject(playerDeathReason), TraceLevel.Info);
            //logWriter.ServerWriteLine(string.Format("{0} {1}->{2} ({3})", player.name, deathText, killerName, totalDeathCount), TraceLevel.Info);

            // Broadcast message to server.
            string serverMessage = GetServerMessage(player, killerName, totalDeathCount);
            TShockAPI.Utils.Instance.Broadcast(serverMessage, 255, 0, 0);

            return true;
        }

        private string GetServerMessage(Player player, string killerName, int totalDeathCount)
        {
            StringBuilder message = new StringBuilder();

            if (totalDeathCount == 1)
            {
                //message.Append(string.Format("This is the first time {0} has died to a {1}.", player.name, killerName));
                message.Append(string.Format("这是{0}第1次死于{1}。", player.name, killerName));
            }
            else if (totalDeathCount == 2)
            {
                //message.Append(string.Format("This is now the second time {0} has died to a {1}.", player.name, killerName));
                message.Append(string.Format("这是{0}第2次死于{1}。", player.name, killerName));
            }
            else
            {
                //message.Append(string.Format("{0} has now died to a {1} {2} times.", player.name, killerName, totalDeathCount));
                message.Append(string.Format("{0}被{1}干掉了{2}次。", player.name, killerName, totalDeathCount));
            }

            if (killerName == "deadly fall")
            {
                message.Append(" Don't do that.");
            }

            return message.ToString();
        }

        private string GetNameOfKiller(Player player, PlayerDeathReason reason)
        {
            if (reason._sourceNPCIndex != -1)
            {
                int NpcId = Main.npc[reason._sourceNPCIndex].netID;
                return Lang.GetNPCNameValue(NpcId);
            }
            else if (reason.SourceProjectileType != 0)
            {
                return Lang.GetProjectileName(reason._sourceProjectileType).Value;
            }
            else if (reason._sourcePlayerIndex != -1)
            {
                return Main.player[reason._sourcePlayerIndex].name;
            }
            else if (reason._sourceItemType != 0)
            {
                return Main.item[reason._sourceItemType].Name;
            }
            else if (reason._sourceOtherIndex != -1)
            {
                return GetOtherKiller(reason._sourceOtherIndex);
            }
            else
            {
                return "Unknown Killer?!";
            }
        }

        private string GetOtherKiller(int sourceOtherIndex)
        {
            // Reference: Sources Lang.cs CreateDeathMessage
            var strangeDeathReasons = new List<string>()
            {
                "deadly fall",
                "deadly water source",
                "overly-hot water source",
                "strange something",
                "slayer event",
                "Medusa attack",
                "sharp object",
                "no-air adventure",
                "heat source",
                "green damage source",
                "electric source",
                "failed Wall of Flesh escape",
                "strange something",
                "teleportation overdose",
                "teleportation overdose",
                "teleportation overdose",
            };

            if (sourceOtherIndex >= 0 && sourceOtherIndex < strangeDeathReasons.Count)
            {
                return strangeDeathReasons[sourceOtherIndex];
            }

            return "very strange something";
        }
    }
}