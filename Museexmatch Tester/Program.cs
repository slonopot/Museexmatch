using Museexmatch;

namespace Museexmatch_Tester
{
    internal class Program
    {
        static void Main(string[] args)
        {
            MusixmatchClient client = new MusixmatchClient();
            var result = client.getLyrics("ULTRA SUNN", "Can You Believe It", "");
            return;
        }
    }
}
