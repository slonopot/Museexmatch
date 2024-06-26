﻿using Museexmatch;
using NLog;
using System;
using System.IO;
using System.Windows;
using System.Windows.Forms;

namespace MusicBeePlugin
{
    public partial class Plugin
    {
        private static Logger Logger;

        public static string configFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"MusicBee\Plugins\museexmatch.conf");
        public static string logFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"MusicBee\museexmatch.log");
        public static string name = "Museexmatch";

        private MusicBeeApiInterface musicBee;
        private PluginInfo info = new PluginInfo();
        private MusixmatchClient musixmatchClient;

        public PluginInfo Initialise(IntPtr apiPtr)
        {
            musicBee = new MusicBeeApiInterface();
            musicBee.Initialise(apiPtr);

            info.PluginInfoVersion = PluginInfoVersion;
            info.Name = name;
            info.Description = "Musixmatch support for MusicBee";
            info.Author = "slonopot";
            info.TargetApplication = "MusicBee";
            info.Type = PluginType.LyricsRetrieval;
            info.VersionMajor = 1;
            info.VersionMinor = 0;
            info.Revision = 6;
            info.MinInterfaceVersion = 20;
            info.MinApiRevision = 25;
            info.ReceiveNotifications = ReceiveNotificationFlags.StartupOnly;
            info.ConfigurationPanelHeight = 20;

            try
            {
                var target = new NLog.Targets.FileTarget(name)
                {
                    FileName = logFile,
                    Layout = "${date} | ${level} | ${callsite} | ${message}",
                    DeleteOldFileOnStartup = true,
                    Name = name
                };
                if (LogManager.Configuration == null)
                {
                    var config = new NLog.Config.LoggingConfiguration();
                    config.AddTarget(target);
                    config.AddRuleForAllLevels(target, name);
                    LogManager.Configuration = config;

                }
                else
                {
                    LogManager.Configuration.AddTarget(target);
                    LogManager.Configuration.AddRuleForAllLevels(target, name);
                }

                LogManager.ReconfigExistingLoggers();

                Logger = LogManager.GetLogger(name);

                musixmatchClient = new MusixmatchClient(MuseexmatchLyricsProvider);
            }
            catch (Exception e)
            {
                MessageBox.Show("An error occurred during Museexmatch startup: " + e.Message);
                throw;
            }

            return info;
        }

        private string MuseexmatchLyricsProvider = "Musixmatch via Museexmatch";

        public String[] GetProviders()
        {
            return new string[] { MuseexmatchLyricsProvider };
        }

        public String RetrieveLyrics(String source, String artist, String title, String album, bool preferSynced, String providerName)
        {
            Logger.Debug("source={source}, artist={artist}, title={title}, album={album}, preferSynced={preferSynced}, providerName={providerName}", source, artist, title, album, preferSynced, providerName);

            if (providerName != MuseexmatchLyricsProvider) return null;

            var lyrics = musixmatchClient.getLyrics(artist, title, album);
            return lyrics;
        }

        public void ReceiveNotification(String source, NotificationType type) { }
        public void SaveSettings() { }
        public bool Configure(IntPtr panelHandle) { return false; } //fixes the popup
        public void Uninstall() { MessageBox.Show("Just delete the plugin files from the Plugins folder yourself, this plugin is not very sophisticated to handle it itself."); }

    }
}
