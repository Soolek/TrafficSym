using System;
using System.Linq;

namespace TrafficSym2D
{
#if WINDOWS || LINUX
    /// <summary>
    /// The main class.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            var parsedArgs = args
                .Select(s => s.Split(':'))
                .ToDictionary(s => s[0].ToLower(), s => s[1].ToLower());

            using (var game = new TrafficSymGame(parsedArgs))
                game.Run();
        }
    }
#endif
}
