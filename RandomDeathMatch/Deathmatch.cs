﻿using System;
using System.Collections.Generic;

using MEC;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Enums;
using PluginAPI.Events;
using PlayerRoles;
using UnityEngine;
using System.ComponentModel;

//todo voice and spectate cmd
namespace TheRiptide
{
    public class MainConfig
    {
        public bool IsEnabled { get; set; } = true;

        [Description("round time in minutes")]
        public float RoundTime { get; set; } = 30.0f;

        public string DummyPlayerName { get; set; } = "[THE RIPTIDE]";
    }

    public class Deathmatch
    {
        public static Deathmatch Singleton { get; private set; }

        [PluginConfig("main_config.yml")]
        public MainConfig config;

        [PluginConfig("rooms_config.yml")]
        public RoomsConfig rooms_config;

        [PluginConfig("killstreak_config.yml")]
        public KillstreakConfig killstreak_config;

        [PluginConfig("loadout_config.yml")]
        public LoadoutConfig loadout_config;

        [PluginConfig("lobby_config.yml")]
        public LobbyConfig lobby_config;

        [PluginConfig("menu_config.yml")]
        public MenuConfig menu_config;

        [PluginConfig("experience_config.yml")]
        public ExperienceConfig experience_config;

        [PluginConfig("rank_config.yml")]
        public RankConfig rank_config;

        [PluginConfig("tracking_config.yml")]
        public TrackingConfig tracking_config;

        [PluginConfig("translation_config.yml")]
        public TranslationConfig translation_config;

        [PluginConfig("attachment_blacklist_config.yml")]
        public AttachmentBlacklistConfig attachment_blacklist_config;

        //[PluginConfig("voice_chat_config.yml")]
        //public VoiceChatConfig voice_chat_config;

        private static bool game_started = false;
        public static SortedSet<int> players = new SortedSet<int>();

        public static bool GameStarted
        {
            get => game_started;
            set
            {
                if (value == true)
                {
                    foreach (var player in Player.GetPlayers())
                        if (player.IsAlive)
                            Killstreaks.Singleton.AddKillstreakEffects(player);
                }
                else
                {
                    foreach (var player in Player.GetPlayers())
                        if (player.IsAlive)
                            Lobby.ApplyGameNotStartedEffects(player);
                }
                game_started = value;
            }
        }

        public Deathmatch()
        {
            Singleton = this;
            Killfeeds.Init(2, 5, 20);
        }

        public void Start()
        {
            Database.Singleton.Load();

            EventManager.RegisterEvents(this);
            //dependencies
            EventManager.RegisterEvents<InventoryMenu>(this);
            EventManager.RegisterEvents<BroadcastOverride>(this);
            EventManager.RegisterEvents<FacilityManager>(this);
            EventManager.RegisterEvents<BadgeOverride>(this);
            EventManager.RegisterEvents<HintOverride>(this);
            BadgeOverride.Singleton.Init(2);

            //features
            EventManager.RegisterEvents<Statistics>(this);
            EventManager.RegisterEvents<Killfeeds>(this);
            EventManager.RegisterEvents<Killstreaks>(this);
            EventManager.RegisterEvents<Loadouts>(this);
            EventManager.RegisterEvents<Lobby>(this);
            EventManager.RegisterEvents<Rooms>(this);
            if (rank_config.IsEnabled)
                EventManager.RegisterEvents<Ranks>(this);
            if (experience_config.IsEnabled)
                EventManager.RegisterEvents<Experiences>(this);
            if (tracking_config.IsEnabled)
                EventManager.RegisterEvents<Tracking>(this);
            if (attachment_blacklist_config.IsEnabled)
                EventManager.RegisterEvents<AttachmentBlacklist>(this);
            //if (voice_chat_config.IsEnabled)
            //    EventManager.RegisterEvents<VoiceChat>(this);


            Rooms.Singleton.Init(rooms_config);
            Killstreaks.Singleton.Init(killstreak_config);
            Loadouts.Singleton.Init(loadout_config);
            Lobby.Singleton.Init(lobby_config);
            DeathmatchMenu.Singleton.Init(menu_config);
            if (rank_config.IsEnabled)
                Ranks.Singleton.Init(rank_config);
            if (experience_config.IsEnabled)
                Experiences.Singleton.Init(experience_config);
            if (tracking_config.IsEnabled)
                Tracking.Singleton.Init(tracking_config);
            if (attachment_blacklist_config.IsEnabled)
                AttachmentBlacklist.Singleton.Init(attachment_blacklist_config, this);
            //if (voice_chat_config.IsEnabled)
            //    VoiceChat.Singleton.Init(voice_chat_config);

            Translation.translation = translation_config;
            DeathmatchMenu.Singleton.SetupMenus();
        }

        public void Stop()
        {
            Database.Singleton.UnLoad();

            //features
            //EventManager.UnregisterEvents<VoiceChat>(this);
            EventManager.UnregisterEvents<AttachmentBlacklist>(this);
            EventManager.UnregisterEvents<Tracking>(this);
            EventManager.UnregisterEvents<Experiences>(this);
            EventManager.UnregisterEvents<Ranks>(this);
            EventManager.UnregisterEvents<Rooms>(this);
            EventManager.UnregisterEvents<Lobby>(this);
            EventManager.UnregisterEvents<Loadouts>(this);
            EventManager.UnregisterEvents<Killstreaks>(this);
            EventManager.UnregisterEvents<Killfeeds>(this);
            EventManager.UnregisterEvents<Statistics>(this);

            //dependencies
            EventManager.UnregisterEvents<HintOverride>(this);
            EventManager.UnregisterEvents<BadgeOverride>(this);
            EventManager.UnregisterEvents<FacilityManager>(this);
            EventManager.UnregisterEvents<BroadcastOverride>(this);
            EventManager.UnregisterEvents<InventoryMenu>(this);

            EventManager.UnregisterEvents(this);

            DeathmatchMenu.Singleton.ClearMenus();
        }

        [PluginEntryPoint("Deathmatch", "1.0", "needs no explanation", "The Riptide")]
        void EntryPoint()
        {
            if (config.IsEnabled)
                Start();
        }

        [PluginUnload]
        void Unload()
        {
            Stop();
        }

        [PluginEvent(ServerEventType.WaitingForPlayers)]
        void WaitingForPlayers()
        {
            Database.Singleton.Checkpoint();
        }

        [PluginEvent(ServerEventType.RoundStart)]
        void OnRoundStart()
        {
            Server.Instance.SetRole(RoleTypeId.Scp939);
            Server.Instance.ReferenceHub.nicknameSync.SetNick(config.DummyPlayerName);
            Server.Instance.Position = new Vector3(128.8f, 994.0f, 18.0f);
            Server.FriendlyFire = true;
            FriendlyFireConfig.PauseDetector = true;
            Server.IsHeavilyModded = true;

            Timing.CallDelayed(1.0f, () =>
            {
                try
                {
                    Server.Instance.ReferenceHub.serverRoles.Permissions = (ulong)PlayerPermissions.FacilityManagement;
                    CommandSystem.Commands.RemoteAdmin.Cleanup.ItemsCommand cmd = new CommandSystem.Commands.RemoteAdmin.Cleanup.ItemsCommand();
                    string response = "";
                    string[] empty = { "" };
                    cmd.Execute(new ArraySegment<string>(empty, 0, 0), new RemoteAdmin.PlayerCommandSender(Server.Instance.ReferenceHub), out response);
                    ServerConsole.AddLog(response);
                }
                catch (Exception ex)
                {
                    ServerConsole.AddLog(ex.ToString());
                }
            });
            if (config.RoundTime > 5.0f)
                Timing.CallDelayed(60.0f * (config.RoundTime - 5.0f), () => { BroadcastOverride.BroadcastLine(1, 30, BroadcastPriority.Medium, "<color=#43BFF0>Round Ends in 5 minutes</color>"); });
            if (config.RoundTime > 1.0f)
                Timing.CallDelayed(60.0f * (config.RoundTime - 1.0f), () => { BroadcastOverride.BroadcastLine(1, 30, BroadcastPriority.Medium, "<color=#43BFF0>Round Ends in 1 minute</color>"); });
            Timing.CallDelayed(60.0f * config.RoundTime, () => 
            {
                try
                {
                    Timing.CallDelayed(20.0f, () => Round.Restart(false));
                    Timing.CallPeriodically(20.0f, 0.2f, () =>
                    {
                        foreach (var p in Player.GetPlayers())
                            p.IsGodModeEnabled = true;
                    });
                    Statistics.DisplayRoundStats();
                    Experiences.Singleton.SaveExperiences();
                    Ranks.Singleton.CalculateAndSaveRanks();
                    HintOverride.Refresh();
                }
                catch(Exception ex)
                {
                    Log.Error("round end Error: " + ex.ToString());
                }
            });
        }

        [PluginEvent(ServerEventType.PlayerJoined)]
        void OnPlayerJoined(Player player)
        {
            players.Add(player.PlayerId);
            Database.Singleton.LoadConfig(player);
        }

        [PluginEvent(ServerEventType.PlayerLeft)]
        void OnPlayerLeft(Player player)
        {
            players.Remove(player.PlayerId);
        }

        [PluginEvent(ServerEventType.RoundEndConditionsCheck)]
        RoundEndConditionsCheckCancellationData OnRoundEndConditionsCheck(bool baseGameConditionsSatisfied)
        {
            return RoundEndConditionsCheckCancellationData.Override(false);
        }
        [PluginEvent(ServerEventType.RoundRestart)]
        void OnRoundRestart()
        {
            Timing.KillCoroutines();
        }

        public static bool IsPlayerValid(Player player)
        {
            return players.Contains(player.PlayerId);
        }
    }
}
