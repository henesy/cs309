using System;

namespace DegreeQuest
{
    /// <summary>
    /// The main class.
    /// </summary>
    public static class Program
    {

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
       
       
            
               
        
        [STAThread]
        static void Main()
        {
            //NPCTemplate.update();
            //ItemTemplate.update();
            using (var game = new DegreeQuest())
                game.Run();
        }
    }
}
