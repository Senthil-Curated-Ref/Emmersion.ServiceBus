using System.Threading.Tasks;

namespace AdHocs
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var adhoc = new DeadLetterDrainer();
            await adhoc.Drain();
        }
    }
}
