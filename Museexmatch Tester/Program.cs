using Museexmatch;

namespace Museexmatch_Tester
{
    internal class Program
    {
        static void Main(string[] args)
        {
            MusixmatchClient client = new MusixmatchClient();
            var result = client.getLyrics("Coldplay", "Princess Of China [feat. Rihanna]", "tha playa$ manual [jej]");
            return;
        }
    }
}
