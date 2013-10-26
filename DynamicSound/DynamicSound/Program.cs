using System;

namespace DynamicSound
{
#if WINDOWS || XBOX
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            using (DynamicSound game = new DynamicSound())
            {
                game.Run();
            }
        }
    }
#endif
}

