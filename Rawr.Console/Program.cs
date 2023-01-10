using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rawr.Base;
using CsvHelper;

namespace Rawr.ConsoleApp
{
    internal class ItemCsvShape
    {
        public int ItemId{get;set;}
        public string Name{get;set;}
        public int ItemLevel{get;set;}
        
        public ItemCsvShape(){}
    }
    internal class Program
    {
        static void Main(string[] args)
        {
            var mapPath = @"C:\Users\AlexKindle\source\repos\rawr-wotlk\T8-map.csv";
            var logPath = @"C:\Users\AlexKindle\source\repos\rawr-wotlk\logfile.txt";
            try
            {
                File.Delete(mapPath);
            } catch {}

            try
            {
                File.Delete(logPath);
            } catch {}

            using var stream = new StreamWriter(mapPath);
            using var logStream = new StreamWriter(logPath);
            using var writer = new CsvWriter(stream, System.Globalization.CultureInfo.CurrentCulture);
            foreach (var item in Items)
            {
                var result = Wowhead.GetItem(item, true);
                var logline = $"Search input: {item}; wowhead result: {result}";
                Console.WriteLine(logline);
                logStream.WriteLine(logline);
                if (result != null)
                {
                    writer.WriteRecord(new ItemCsvShape{Name = item, ItemLevel = result.ItemLevel, ItemId = result.Id});
                    writer.NextRecord();
                }
                else
                {
                    logline = $"WARN: cant find {item}, pretending its 232";
                    writer.WriteRecord(new ItemCsvShape { Name = item, ItemLevel = 232 });
                    writer.NextRecord();
                    Console.WriteLine(logline);
                    logStream.WriteLine(logline);
                }
            }
        }

        private static readonly string[] Items = {"Boots of Living Scale",
            "Blue Belt of Chaos",
            "Belt of Dragons",
            "Boots of Wintry Endurance",
            "Lightning Grounded Boots",
            "Death-warmed Belt",
            "Footpads of Silence",
            "Sash of Ancient Power",
            "Spellslinger's Slippers",
            "Belt of Arctic Life",
            "Cord of the White Dawn",
            "Savior's Slippers",
            "Battlelord's Plate Boots",
            "Belt of the Titans",
            "Indestructible Plate Girdle",
            "Plate Girdle of Righteousness",
            "Spiked Deathdealers",
            "Treads of Destiny"
        };
    }
}
