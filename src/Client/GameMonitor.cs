﻿using System;
using System.Diagnostics;
using System.IO;
using System.Timers;
using EFTLauncher.ClientData;
using EFTLauncher.Utility;

namespace EFTLauncher.ClientLogic
{
    public class GameMonitor
    {
        private string gameDirectory;
        private string SClientDirectory;
        private string originalDomain;
        private Timer gameAlive;

        public GameMonitor(string gameDirectory)
        {
            this.gameDirectory = gameDirectory;
        }

        public void LaunchGame(string address)
        {
            if (gameAlive != null)
            {
                return;
            }

            // get client config
            Logger.Log("INFO: Client preparing for launch");

            // set client address
            SetGameDomain(address);

            // rename SClient.dll if it exists
            SClientDirectory = gameDirectory + @"/EscapeFromTarkov_Data/Managed/";
            if (File.Exists(SClientDirectory + "SClient.dll"))
            {
                Logger.Log("INFO: Client contains SClient.dll, renaming dll");
                File.Move(SClientDirectory + "SClient.dll", SClientDirectory + "SClient.dll.disabled");
            }

            // launch game
            ProcessStartInfo game = new ProcessStartInfo();
            game.FileName = gameDirectory + @"/EscapeFromTarkov.exe";
            if (File.Exists(gameDirectory + @"/EscapeFromTarkov.exe"))
            {
                Process gameProcess = Process.Start(game);
                Logger.Log("INFO: Game started");
            }

            // initialize game watchdog
            gameAlive = new Timer(1000);
            gameAlive.Elapsed += OnUpdate;
            gameAlive.AutoReset = true;
            gameAlive.Enabled = true;
        }

        private void OnUpdate(Object source, ElapsedEventArgs e)
        {
            // check if game is alive
            Process[] gameProcess = Process.GetProcessesByName("EscapeFromTarkov");
            if (gameProcess.Length == 0)
            {
                Logger.Log("INFO: Game terminated");
                gameAlive.Enabled = false;
                ResetGameFiles();
            }
        }

        public void ResetGameFiles()
        {
            // rename SClient.dll if it exists
            if (File.Exists(SClientDirectory + "SClient.dll.disabled"))
            {
                Logger.Log("INFO: Client contains SClient.dll.disabled, renaming dll");
                File.Move(SClientDirectory + "SClient.dll.disabled", SClientDirectory + "SClient.dll");
            }

            // reset client address
            SetGameDomain(originalDomain);
        }

        public void SetGameDomain(string domain)
        {
            ConfigData configData = JsonHelper.LoadJson<ConfigData>(gameDirectory + @"/client.config.json");

            // override backendurl if it doesn't match
            if (configData.BackendUrl != domain)
            {
                Logger.Log("INFO: Client BackendUrl doesn't match domain" + domain + ", overwriting config");
                originalDomain = configData.BackendUrl;
                configData.BackendUrl = domain;
                JsonHelper.SaveJson<ConfigData>(gameDirectory + @"/client.config.json", configData);
            }
        }
    }
}
