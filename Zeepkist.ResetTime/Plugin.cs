using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Linq;
using System.Security.Cryptography;
using UnityEngine;
using ZeepkistClient;
using ZeepkistNetworking;
using ZeepSDK.Cosmetics;
using ZeepSDK.Racing;
using ZeepSDK.Workshop;

namespace Zeepkist.ResetTime
{

    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInDependency("ZeepSDK")]
    public class Plugin : BaseUnityPlugin
    {
        private Harmony harmony;

        public static ConfigEntry<bool> ModEnabled { get; private set; }
        public static ConfigEntry<int> PercentageRequired { get; private set; }

        public static string JOIN_MESSAGE = "Resettime voting is enabled, enter !resettime into the chat window to vote to reset time";
        public static string SERVER_MESSAGE = "Resettime has been voted on.  {0} of {1} votes.";

        private void Awake()
        {
            harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
            harmony.PatchAll();

            // Plugin startup logic
            Debug.Log($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

            ModEnabled = Config.Bind<bool>("Mod", "Enable Mod", true);
            PercentageRequired = Config.Bind<int>("Mod", "Percentage Required", 50);

            ModEnabled.SettingChanged += ModEnabled_SettingChanged;

            ZeepSDK.Multiplayer.MultiplayerApi.PlayerLeft += MultiplayerApi_PlayerLeft;
            ZeepSDK.Racing.RacingApi.RoundEnded += RacingApi_RoundEnded;
            ZeepSDK.Multiplayer.MultiplayerApi.CreatedRoom += MultiplayerApi_CreatedRoom;
            ZeepSDK.Chat.ChatApi.ChatMessageReceived += ChatApi_ChatMessageReceived;

            VotingManager.VoteChanges += VotingManager_VoteChanges;
        }

        private void ChatApi_ChatMessageReceived(ulong playerId, string username, string message)
        {
            string unformatted = message.Replace("<noparse>", "");
            if (unformatted.StartsWith("!resettime"))
            {
                VotingManager.NewVote(playerId);
            }
        }

        private void MultiplayerApi_CreatedRoom()
        {
            if (!ZeepkistNetwork.LocalPlayerHasHostPowers() || ModEnabled.Value == false)
            {
                return;
            }

            ZeepSDK.Chat.ChatApi.SendMessage($"/joinmessage yellow {JOIN_MESSAGE}");
            ZeepSDK.Chat.ChatApi.SendMessage(JOIN_MESSAGE);
        }

        private void VotingManager_VoteChanges(object sender, NewVoteArgs e)
        {
            if (!ZeepkistNetwork.LocalPlayerHasHostPowers() || ModEnabled.Value == false)
            {
                return;
            }

            int requiredVotes = (PlayerManager.Instance.amountOfPlayers * PercentageRequired.Value) / 100;
            if (PlayerManager.Instance.amountOfPlayers <= 3)
            {
                requiredVotes = 2;
            }

            if (e.votes > 0)
            {
                ZeepSDK.Chat.ChatApi.SendMessage($"/servermessage yellow 120 {string.Format(SERVER_MESSAGE, e.votes, requiredVotes)}");

                if (e.votes >= requiredVotes)
                {
                    ZeepSDK.Chat.ChatApi.SendMessage("/resettime");
                    ZeepSDK.Chat.ChatApi.SendMessage("/servermessage remove");
                    VotingManager.Clear();
                }
            }
        }

        private void RacingApi_RoundEnded()
        {
            VotingManager.Clear();
            ZeepSDK.Chat.ChatApi.SendMessage("/servermessage remove");
        }

        private void MultiplayerApi_PlayerLeft(ZeepkistNetworkPlayer player)
        {
            VotingManager.RemoveVote(player.SteamID);
        }

        private void ModEnabled_SettingChanged(object sender, EventArgs e)
        {
            // If disabled, disable the server and join messages
            if (ModEnabled.Value == false)
            {
                ZeepSDK.Chat.ChatApi.SendMessage("/servermessage remove");
                ZeepSDK.Chat.ChatApi.SendMessage("/joinmessage remove");
            } else
            {
                ZeepSDK.Chat.ChatApi.SendMessage($"/joinmessage yellow {JOIN_MESSAGE}");
                ZeepSDK.Chat.ChatApi.SendMessage(JOIN_MESSAGE);
            }
        }

        public void OnDestroy()
        {
            harmony?.UnpatchSelf();
            harmony = null;
        }
    }
}