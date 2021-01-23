using System;
using System.Linq;
using System.Net;
using Newtonsoft.Json;
using System.Xml;
using System.IO;
using System.Text.RegularExpressions;

namespace PDFParser
{
    /// <summary>
    /// Manages the state of the menu within memory.
    /// </summary>
    public class MenuManager
    {
        private string _directoryPath;

        /// <summary>
        /// Path to the menu directory
        /// </summary>
        /// 
        public string DirectoryPath
        {
            get { return _directoryPath; }
            set
            {
                Directory.CreateDirectory(value);
                _directoryPath = value;
            }
        }

        private string FileName => DirectoryPath + "menu.pdf";

        private LunchMenu _lunchMenu;
        public LunchMenu LunchMenu
        {
            get
            {
                // Check if menu stored in memory is valid
                if (_lunchMenu == null || _lunchMenu.ExpirationDate.IsExpired())
                {
                    // Get latest menu
                    //  1. Try storage
                    //  2. Try online
                    _lunchMenu = RetrieveMenu();
                }

                return _lunchMenu;
            }
        }

        /// <summary>
        /// Retrieves the lunch menu.
        /// 
        /// Initially check for local file, if outdated, grabs from URL
        /// </summary>
        /// <returns>The newest lunch menu</returns>
        public LunchMenu RetrieveMenu()
        {
            // Valid file path
            if (string.IsNullOrEmpty(FileName))
                throw new Exception($"{nameof(this.GetType)}'s FilePath must be set.");

            // Get menu from file
            var menu = FileManager.OpenJSON(FileName);

            // If file is outdated, get from website
            if (menu == null || menu.ExpirationDate.IsExpired())
            {
                // Download the new menu
                menu = FileManager.DownloadMenu(FileName);
            }

            // Return the menu object
            return menu;
        }
    }

    /// <summary>
    /// Manages the fetching and serialization.
    /// </summary>
    static class FileManager
    {
        /// <summary>
        /// Download and convert/serialize the menu.
        /// </summary>
        /// <param name="filepath">Path to the files to be created.</param>
        /// <returns>A serialized LunchMenu</returns>
        public static LunchMenu DownloadMenu(string filepath)
        {
            // Get the pdf
            DownloadLatestPDF(filepath);

            // Convert to xml and query for node
            var xml = OpenAndConvertPDFToXML(filepath);

            // deserialize
            var menu = DeserializePDF(xml);

            // Save to file
            SaveObjectToFile(menu, filepath);

            return menu;
        }

        private static void DownloadLatestPDF(string filepath)
        {
            var client = new WebClient();

            var megaBytesSite = client.DownloadString(@"http://305.intergraph.com/?page_id=796");

            var regexCapture = Regex.Match(megaBytesSite, "[\\w-\\/.:]*.pdf").Captures[0];

            var cafeMenuLink = regexCapture.Value;

            client.DownloadFile(cafeMenuLink, filepath);
        }

        private static XmlNodeList OpenAndConvertPDFToXML(string filepath)
        {
            string output = filepath.Remove(filepath.LastIndexOf('.')) + ".xml";

            SautinSoft.PdfFocus f = new SautinSoft.PdfFocus();
            f.XmlOptions.ConvertNonTabularDataToSpreadsheet = false;
            f.OpenPdf(filepath);
            f.ToXml(output);
            f.ClosePdf();

            var xmlDoc = new XmlDocument();
            xmlDoc.Load(output);

            return xmlDoc.SelectNodes("document/page/table/row");
        }

        private static LunchMenu DeserializePDF(XmlNodeList xmlNodeList)
        {
            var lunchMenu = new LunchMenu();

            for (int i = 0; i < xmlNodeList.Count; i += 6)
            {
                var newDay = new Day() { DayName = xmlNodeList[i].FirstChild.InnerXml };

                for (int j = 1; j < 6; j++)
                {
                    var cellValues = xmlNodeList[i + j].ChildNodes
                        .Cast<XmlNode>()
                        .Select(x => x.InnerXml)
                        .ToArray();

                    var foodStation = new FoodStation()
                    {
                        StationName = cellValues[0],
                        FoodName = cellValues[1]
                    };


                    // In the case that there are two options (ex. Bowl vs Cup), take the first
                    if (cellValues[2].Split(' ').Count() >= 2)
                        cellValues[2] = cellValues[2].Split(' ')[0];

                    // In case the station is CLOSED, don't set the price
                    if (!string.IsNullOrEmpty(cellValues[2]))
                        foodStation.Price = Double.Parse(cellValues[2].Substring(1));

                    newDay.FoodStations.Add(foodStation);
                }

                lunchMenu.Add(newDay);
            }

            return lunchMenu;
        }

        private static void SaveObjectToFile(LunchMenu lunchMenu, string filepath)
        {
            var outputFilepath = filepath.Remove(filepath.LastIndexOf('.')) + ".json";

            var json = JsonConvert.SerializeObject(lunchMenu);

            File.WriteAllText(outputFilepath, json);
        }

        /// <summary>
        /// Open and deserialize the lunch menu.
        /// </summary>
        /// <param name="filepath">Path to the JSON file.</param>
        /// <returns>A LunchMenu object.</returns>
        public static LunchMenu OpenJSON(string filepath)
        {

            try
            {
                var jsonPath = filepath.Remove(filepath.LastIndexOf('.')) + ".json";
                var fileLunchMenu = JsonConvert.DeserializeObject<LunchMenu>(File.ReadAllText(jsonPath));
                return fileLunchMenu;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
