using Museexmatch;
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

        public static string configFile;
        public static string logFile;
        public static string name = "Museexmatch";

        private MusicBeeApiInterface musicBee;
        private PluginInfo info = new PluginInfo();
        private MusixmatchClient musixmatchClient;

        public PluginInfo Initialise(IntPtr apiPtr)
        {
            musicBee = new MusicBeeApiInterface();
            musicBee.Initialise(apiPtr);

            configFile = Path.Combine(musicBee.Setting_GetPersistentStoragePath(), @"museexmatch.conf");
            logFile = Path.Combine(musicBee.Setting_GetPersistentStoragePath(), @"museexmatch.log");

            info.PluginInfoVersion = PluginInfoVersion;
            info.Name = name;

            info.VersionMajor = 1;
            info.VersionMinor = 1;
            info.Revision = 0;

            info.Description = $"Musixmatch support for MusicBee [{info.VersionMajor}.{info.VersionMinor}.{info.Revision}]";
            info.Author = "slonopot";
            info.TargetApplication = "MusicBee";
            info.Type = PluginType.LyricsRetrieval;

            info.MinInterfaceVersion = 20;
            info.MinApiRevision = 25;
            info.ReceiveNotifications = ReceiveNotificationFlags.StartupOnly;
            info.ConfigurationPanelHeight = 20;

            try
            {
                var target = new NLog.Targets.FileTarget(name)
                {
                    FileName = logFile,
                    Layout = "${date} | ${level} | ${callsite} | ${message} ${exception:format=tostring}",
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

        private (string, string, string) TryGetFileMetadata(String source)
        {
            var tfile = TagLib.File.Create(source);
            string title = tfile.Tag.Title;
            string artist = String.Join(" & ", tfile.Tag.AlbumArtists);
            string album = tfile.Tag.Album;
            //int duration = (int)tfile.Properties.Duration.TotalSeconds;
            Logger.Debug("Extracted metadata from {source}: artist={artist}, title={title}, album={album}", source, artist, title, album);
            return (artist, title, album);
        }

        public String RetrieveLyrics(String source, String artist, String title, String album, bool preferSynced, String providerName)
        {
            Logger.Debug("source={source}, artist={artist}, title={title}, album={album}, preferSynced={preferSynced}, providerName={providerName}", source, artist, title, album, preferSynced, providerName);

            if (providerName != MuseexmatchLyricsProvider) return null;

            if (source != string.Empty)
            {
                try { (artist, title, album) = TryGetFileMetadata(source); }
                catch { Logger.Debug("Failed to extract metadata from {source}", source); }
            }

            try
            {
                var lyrics = musixmatchClient.getLyrics(artist, title, album);
                return lyrics;
            }
            catch (Exception ex)
            {
                Logger.Debug(ex);
                return null;
            }

        }

        public void ReceiveNotification(String source, NotificationType type) { }
        public void SaveSettings() { }
        public bool Configure(IntPtr panelHandle) { return false; } //fixes the popup
        public void Uninstall() { MessageBox.Show("Just delete the plugin files from the Plugins folder yourself, this plugin is not very sophisticated to handle it itself."); }

    }
}
