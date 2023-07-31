## Museexmatch - Musixmatch for MusicBee
It's just a lyrics provider.

## Disclaimer
This plugin is made and published exclusively for educational purposes. The plugin uses the private Musixmatch API and may stop working whenever the legal owners decide to take action. I take no responsibility, if someone asks then it wasn't me.

### Features
Using private Musixmatch API to get lyrics.

### Installation
Get a release and extract all .dll files into `%APPDATA%\MusicBee\Plugins\` directory.

### Activation
Preferences -> Plugins -> Enable Museexmatch.  
Preferences -> Tags (2) -> Lyrics -> Musixmatch via Museexmatch.

### Configuration
Create museexmatch.conf in the Plugins directory and use this template:

    {
        "allowedDistance": 5,
        "delimiters": ["&", ";", ","],
        "verifyAlbum": false,
        "addLyricsSource": false,
        "trimTitle": false
    }

museexmatch.conf includes several options. You are allowed to use only ones you need, just omit the line and don't forget about commas in JSON.
1. Configurable title distance for minor differences. Defaults to 5. This means that a present N-character difference in search results won't affect the filtering and be considered a hit.
2. Configurable artist delimiters ("A & B, C" => "A"). Defaults to none. Useful when you have several artists for the track but Musixmatch includes only the main one.
3. Configurable album verification. Plugin will check if the album is the same. Names must be identical.
4. Configurable lyrics source marker. Plugin will append "Source: Musixmatch via Museexmatch" to the lyrics' beginning if enabled.
5. Configurable title trim. This option will remove all content in brackets from the title. By default MusicBee removes only features in the round brackets, this option will remove all content in `[]`, `{}`, `<>` and `()`.

Restart MusicBee to apply changes.

### Logic
1. Plugin gets either the "artist" field or the first artist in the extended list if you've edited it manually alongside with the "title".
2. Plugin searches for results just like they are. Results (artist + title) are allowed to differ no more than `allowedDistance` characters.
3. Plugin checks for result artist aliases.
4. Plugin strips down the artist using the delimiters (if provided), searches and handles aliases.

### Log
You can find log at `%APPDATA%\MusicBee\museexmatch.log`.

### Shoutouts
https://github.com/toptensoftware/JsonKit

https://nlog-project.org/
