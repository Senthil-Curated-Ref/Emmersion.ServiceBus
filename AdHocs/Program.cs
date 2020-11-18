namespace AdHocs
{
    class Program
    {
        static void Main(string[] args)
        {
            var adhoc = new DeadLetterDrainer();
            adhoc.Drain();
        }
    }
}
