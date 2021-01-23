using System;

namespace PDFParser
{
    class Program
    {
        static void Main(string[] args)
        {
            var directory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\PDFParser\";

            var menuManager = new MenuManager() { DirectoryPath = directory };
            var lunchMenu = menuManager.LunchMenu;
        }
    }
}
