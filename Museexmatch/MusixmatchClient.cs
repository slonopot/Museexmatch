﻿using MusicBeePlugin;
using NLog;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Topten.JsonKit;

namespace Museexmatch
{
    public class MusixmatchClient
    {
        private static Logger Logger = LogManager.GetLogger(Plugin.name);

        private HttpClient client = new HttpClient();

        private string LyricsProviderName;

        private string HmacSHA1Key = "967Pn4)N3&R_GBg5$b('";
        private string ApiURL = "https://apic.musixmatch.com/ws/1.1/";
        private string UserToken = null;

        private int AllowedDistance = 5; //a number of edits needed to get from one title to another
        private char[] Delimiters = { }; //delimiters to remove additional authors from the string
        private bool VerifyAlbum = false;
        private bool AddLyricsSource = false;
        private bool TrimTitle = false;
        private bool PreferSyncedLyrics = false;
        private bool OnlySyncedLyrics = false;
        public MusixmatchClient(string lyricsProviderName = null)
        {
            LyricsProviderName = lyricsProviderName;

            client.DefaultRequestHeaders.Remove("User-Agent");
            client.DefaultRequestHeaders.Add("User-Agent", "Dalvik/2.1.0 (Linux; U; Android 13; Pixel 7 (Whatever))");
            client.DefaultRequestHeaders.Add("x-mxm-endpoint", "default");
  
            if (File.Exists(Plugin.configFile))
            {
                string data = File.ReadAllText(Plugin.configFile);
                dynamic config = Json.Parse<object>(data);
                if (Util.PropertyExists(config, "allowedDistance"))
                    AllowedDistance = (int)config.allowedDistance;
                if (Util.PropertyExists(config, "delimiters"))
                    Delimiters = ((List<object>)config.delimiters).Select(x => char.Parse(x.ToString())).ToArray();
                if (Util.PropertyExists(config, "verifyAlbum"))
                    VerifyAlbum = (bool)config.verifyAlbum;
                if (Util.PropertyExists(config, "addLyricsSource"))
                    AddLyricsSource = (bool)config.addLyricsSource;
                if (Util.PropertyExists(config, "trimTitle"))
                    TrimTitle = (bool)config.trimTitle;
                if (Util.PropertyExists(config, "preferSyncedLyrics"))
                    PreferSyncedLyrics = (bool)config.preferSyncedLyrics;
                if (Util.PropertyExists(config, "onlySyncedLyrics"))
                    OnlySyncedLyrics = (bool)config.onlySyncedLyrics;

                if (Util.PropertyExists(config, "hmacSHA1Key"))
                    HmacSHA1Key = config.hmacSHA1Key;
                if (Util.PropertyExists(config, "apiURL"))
                    ApiURL = config.apiURL;
                
                if (Util.PropertyExists(config, "userToken"))
                    UserToken = config.userToken;

                Logger.Info("Configuration file was used: allowedDistance={allowedDistance}, delimiters={delimiters}, verifyAlbum={verifyAlbum}, addLyricsSource={addLyricsSource}, trimTitle={trimTitle}, preferSyncedLyrics={preferSyncedLyrics}, onlySyncedLyrics={onlySyncedLyrics}", AllowedDistance, Delimiters, VerifyAlbum, AddLyricsSource, TrimTitle, PreferSyncedLyrics, OnlySyncedLyrics);
            }
            else { Logger.Info("No configuration file was provided, defaults were used"); }
            if (string.IsNullOrEmpty(UserToken))
            {
                UserToken = GetUserToken();

                dynamic config;
                if (File.Exists(Plugin.configFile))
                {
                    string data = File.ReadAllText(Plugin.configFile);
                    config = Json.Parse<object>(data);
                    config.userToken = UserToken;
                    Json.WriteFile(Plugin.configFile, config);
                }

                Logger.Info("Got new user token");
            }
           
        }

        private string GetUserToken()
        {
            NameValueCollection parameters = new NameValueCollection();
            parameters.Add("adv_id", Guid.NewGuid().ToString());
            parameters.Add("referal", "utm_source=google-play&utm_medium=organic");
            parameters.Add("root", "0");
            parameters.Add("sideloaded", "0");
            parameters.Add("build_number", "2024050901");
            parameters.Add("guid", Util.GenerateHex(16));
            parameters.Add("lang", "en_US");
            parameters.Add("model", "manufacturer/Google brand/Pixel model/Whatever");
            parameters.Add("timestamp", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ssZ"));

            dynamic result = MusixmatchRequest("token.get", parameters);
            string token = result.user_token;
            return token;
        }

        private dynamic MusixmatchRequest(string path, NameValueCollection parameters = null, bool sign = true)
        {
            string url = this.ApiURL + path;
            if (parameters == null)
                parameters = new NameValueCollection();

            if (!string.IsNullOrEmpty(UserToken))
                parameters.Add("usertoken", UserToken);

            parameters.Add("app_id", "android-player-v1.0");
            parameters.Add("format", "json");

            if (sign)
            {
                parameters.Add("signature", Util.MusixmatchSignature(url, parameters, HmacSHA1Key));
                parameters.Add("signature_protocol", "sha1");
            }

            url += "?" + Util.ToQueryString(parameters);
            HttpResponseMessage response = null;
            try
            {
                var task = Task.Run(() => client.GetAsync(url));
                task.Wait();
                response = task.Result;
            }
            catch (Exception)
            {
                throw;
            }
            dynamic result = null;
            try
            {
                string content = string.Empty;
                var task = Task.Run(() => response.Content.ReadAsStringAsync());
                task.Wait();
                content = task.Result;
                result = Json.Parse<object>(content);
            }
            catch { throw; }
            return result.message.body;
        }

        public string getLyrics(string artist, string title, string album)
        {
            artist = artist.Trim();
            title = title.Trim();
            album = album.Trim();

            if (TrimTitle) { title = Util.Trim(title); }

            Logger.Info("Attempting to search for {aritst} - {title} ({album}) in tracks", artist, title, album);

            dynamic match = findInMatches(searchInTracks(artist, title, album), artist, title, album);

            if (match == null && Delimiters.Length > 0)
            {
                var editedArtist = artist;

                foreach (char delimiter in Delimiters) editedArtist = editedArtist.Split(delimiter)[0].Trim();

                if (editedArtist != artist)
                {
                    Logger.Info("Nothing found, attempting to search for {aritst} - {title} ({album}) in tracks", editedArtist, title, album);

                    match = findInMatches(searchInTracks(editedArtist, title, album), artist, title, album);
                }
            }

            if (match == null)
            {
                Logger.Info("Attempting to search for {aritst} - {title} ({album}) in macro", artist, title, album);
                
                match = findInMatches(searchMacro(artist, title, album), artist, title, album);
            }

            if (match == null && Delimiters.Length > 0)
            {
                var editedArtist = artist;

                foreach (char delimiter in Delimiters) editedArtist = editedArtist.Split(delimiter)[0].Trim();

                if (editedArtist != artist)
                {
                    Logger.Info("Nothing found, attempting to search for {aritst} - {title} ({album}) in macro", editedArtist, title, album);

                    match = findInMatches(searchMacro(editedArtist, title, album), artist, title, album);
                }
            }

            if (match == null) { 
                Logger.Info("Nothing found at all");
                return null;
            }

            string result = loadLyrics(match.track_id.ToString());

            if (result != null) Logger.Info("Got a hit");
            else Logger.Info("Match was found but no lyrics");

            return result;
        }

        private dynamic searchInTracks(string artist, string title, string album)
        {
            Logger.Debug("artist={artist}, title={title}, album={album}", artist, title, album);

            var req = new NameValueCollection();
            req.Add("q_track", title);
            req.Add("q_artist", artist);
            req.Add("part", "track_artist,artist_image");
            req.Add("track_fields_set", "android_track_list");
            req.Add("artist_fields_set", "android_track_list_artist");
            req.Add("page", "1");
            req.Add("page_size", "100");

            dynamic searchResults = MusixmatchRequest("track.search", req);
            
            var matches = searchResults.track_list;

            return matches;
        }


        private dynamic searchMacro(string artist, string title, string album)
        {
            Logger.Debug("artist={artist}, title={title}, album={album}", artist, title, album);

            var req = new NameValueCollection();
            req.Add("q", artist + " " + title);
            req.Add("part", "track_artist,artist_image");
            req.Add("track_fields_set", "android_track_list");
            req.Add("artist_fields_set", "android_track_list_artist");
            req.Add("page", "1");
            req.Add("page_size", "100");

            dynamic searchResults = MusixmatchRequest("macro.search", req);

            var matches = searchResults.macro_result_list.track_list;

            return matches;
        }

        private dynamic findInMatches(dynamic matches, string artist, string title, string album)
        {
            foreach (var _match in matches)
            {
                var match = _match.track;
                if (match.has_lyrics != 1) continue;

                if (VerifyAlbum && match.album_name.ToLower() != album.ToLower()) continue;

                if (Util.ValidateResult(artist, title, match.artist.artist_name, match.track_name, AllowedDistance))
                    return match;

                foreach (var _alias in match.artist.artist_alias_list)
                {
                    var alias = _alias;
                    if (_alias is ExpandoObject) alias = _alias.artist_alias;
                    if (Util.ValidateResult(artist, title, alias, match.track_name, AllowedDistance))
                        return match;
                }
            }

            Logger.Info("No results for this search");

            return null;
        }

        private string loadLyrics(string trackId)
        {

            var req = new NameValueCollection();
            req.Add("track_id", trackId);

            dynamic lyrics = MusixmatchRequest("macro.subtitles.get", req);
            var data = (IDictionary<string, dynamic>)lyrics.macro_calls;

            var result = String.Empty;

            if (PreferSyncedLyrics || OnlySyncedLyrics)
            {
                try
                {
                    var available = data["track.subtitles.get"].message.header.available;
                    if (available > 0)
                    {
                        Logger.Info("Found synced lyrics");
                        result = data["track.subtitles.get"].message.body.subtitle_list[0].subtitle.subtitle_body;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Info($"Error getting richsync lyrics: {ex.Message}");
                }
            }
            if (string.IsNullOrEmpty(result))
            {
                if (OnlySyncedLyrics) return null;
                result = data["track.lyrics.get"].message.body.lyrics.lyrics_body;

                if (AddLyricsSource)
                    result = $"Source: {LyricsProviderName}\n\n" + result;
            }

            return result;
        }
    }
}
