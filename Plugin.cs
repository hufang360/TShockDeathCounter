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
        public override string Author => "Discoveri��hufang360";

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
            Commands.ChatCommands.Add(new Command(new List<string>() { "deathcounter" }, ResetCommand, "deathcounter", "dc"));
            Commands.ChatCommands.Add(new Command(new List<string>() { "" }, WhoKillMe, "whokillme", "wkm"));
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

        private void ResetCommand(CommandArgs args)
        {
            if (args.Parameters.Count == 0)
            {
                ShowHelpText(args);
                return;
            }

            switch (args.Parameters[0].ToLowerInvariant())
            {
                default:
                case "help":
                    ShowHelpText(args);
                    return;

                case "clear":
                    deathRecords.ClearDeath();
                    args.Player.SendSuccessMessage("����������¼�������");
                    return;
                
                case "player":
                    if(args.Parameters.Count<2)
                    {
                        args.Player.SendErrorMessage("�﷨�����÷���/dc player [player]");
                        return;
                    }
                    args.Player.SendInfoMessage("{0}��������¼��{1}",args.Parameters[1], deathRecords.GetRecord(args.Parameters[1]));
                    // if(password != "")
                    // {
                    //     args.Player.SendSuccessMessage("��ɫ��{0}", args.Parameters[1]);
                    //     args.Player.SendSuccessMessage("���룺{0}", password);
                    // }
                    // else
                    //     args.Player.SendErrorMessage("�û� {0} δ�ҵ���", args.Parameters[1]);
                    return;
            }
        }

        private void WhoKillMe(CommandArgs args)
        {
            if(args.Player is TSServerPlayer){
                args.Player.SendErrorMessage("������Ϸ�����");
                return;
            }
            args.Player.SendInfoMessage("�ҵ�������¼��{1}",args.Player.Name, deathRecords.GetRecord(args.Player.Name));
        }

        /// <summary>
        /// �����а���
        /// </summary>
        private void ShowHelpText(CommandArgs args)
        {
            args.Player.SendInfoMessage("/dc player [player]����ѯ��ҵ�������¼");
            args.Player.SendInfoMessage("/dc clear��������м�¼");
            args.Player.SendInfoMessage("/dc = /deathcounter");
            args.Player.SendInfoMessage("/wkm [killer]����ѯ��ҵ�������¼");
        }

        private string GetServerMessage(Player player, string killerName, int totalDeathCount)
        {
            StringBuilder message = new StringBuilder();

            if (totalDeathCount == 1)
            {
                //message.Append(string.Format("This is the first time {0} has died to a {1}.", player.name, killerName));
                message.Append(string.Format("����{0}��1������{1}��", player.name, killerName));
            }
            else if (totalDeathCount == 2)
            {
                //message.Append(string.Format("This is now the second time {0} has died to a {1}.", player.name, killerName));
                message.Append(string.Format("����{0}��2������{1}��", player.name, killerName));
            }
            else
            {
                //message.Append(string.Format("{0} has now died to a {1} {2} times.", player.name, killerName, totalDeathCount));
                message.Append(string.Format("{0}��{1}�ɵ���{2}�Ρ�", player.name, killerName, totalDeathCount));
            }

            if (killerName == "deadly fall")
            {
                message.Append(" Don't do that.");
            }
            return message.ToString();
        }

        private string GetNameOfKiller(Player player, PlayerDeathReason reason)
        {
            //StringBuilder message = new StringBuilder();
            //message.Append("NPCIndex:"+ reason._sourceNPCIndex);
            //message.Append("  ProjectileType:" + reason._sourceProjectileType);
            //message.Append("  ProjectileIndex:" + reason._sourceProjectileIndex);
            //message.Append("  PlayerIndex:" + reason._sourcePlayerIndex);
            //message.Append("  ItemType:" + reason._sourceItemType);
            //message.Append("  OtherIndex:" + reason._sourceOtherIndex);
            //TShockAPI.Utils.Instance.Broadcast(message.ToString(), 255, 0, 0);

            if (reason._sourceNPCIndex != -1)
            {
                int NpcId = Main.npc[reason._sourceNPCIndex].netID;
                return Lang.GetNPCNameValue(NpcId);
            }
            else if (reason._sourceProjectileType != 0)
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
                "�߿ձĵ�", //"deadly fall",
                "������ˮԴ", //"deadly water source",
                "������Ӿ", //"overly-hot water source",
                "���", //"strange something",
                "ɱ���¼�", //"slayer event",
                "����ɯ", //"Medusa attack",
                "�����Ķ���", //"sharp object",
                "����ð��", //"no-air adventure",
                "��Դ", //"heat source",
                "��ɫ�ƻ�Դ", //"green damage source",
                "��Դ", //"electric source",
                "Ѫ��ǽ����ʧ��", //"failed Wall of Flesh escape",
                "��ֵĶ���", //"strange something",
                "���δ�ҩˮ����", //"teleportation overdose",
                "���δ�ҩˮ����", //"teleportation overdose",
                "���δ�ҩˮ����", //"teleportation overdose",
            };

            if (sourceOtherIndex >= 0 && sourceOtherIndex < strangeDeathReasons.Count)
            {
                return strangeDeathReasons[sourceOtherIndex];
            }

            //return "very strange something";
            return "�ǳ���ֵĶ���";
        }
    }
}