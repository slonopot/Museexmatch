using Museexmatch;

namespace Museexmatch_Tester
{
    internal class Program
    {
        static void Main(string[] args)
        {
            MusixmatchClient client = new MusixmatchClient();
            var result = client.getLyrics("Metallica", "Hit The Lights", "");
            return;
        }
    }
}
