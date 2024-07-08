using Museexmatch;

namespace Museexmatch_Tester
{
    internal class Program
    {
        static void Main(string[] args)
        {
            MusixmatchClient client = new MusixmatchClient();
            var result = client.getLyrics("Lily the Kid", "Ghost", "");
            return;
        }
    }
}
