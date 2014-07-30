using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DSCript
{
    public sealed class DriverPL
    {
        // Temporary way of getting vehicle names by ID
        // This should load from [DPL Directory]\Text\vehnames.txt
        public static readonly Dictionary<int, string> VehicleNames = new Dictionary<int, string>() {
            { 0, "BX-9" },
            { 1, "BX-9 Racer" },
            { 2, "Antilli VO3" },
            { 3, "Atlus" },
            { 4, "Atlus Racer" },
            { 5, "Saxon" },
            { 6, "Saxon Cargo Trailer" },
            { 7, "Saxon Flatbed Trailer" },
            { 8, "Saxon Tanker Trailer" },
            { 9, "Colonna" },
            { 10, "Colonna Racer" },
            { 11, "M700" },
            { 12, "Hotrod" },
            { 13, "Indiana" },
            { 14, "Kramer" },
            { 15, "Kramer Racer" },
            { 16, "Miyagi" },
            { 17, "Montara" },
            { 18, "Mutsumi 1000R" },
            { 19, "MX2000" },
            { 20, "MX2000 Racer" },
            { 21, "Negotiator" },
            { 22, "Torrex" },
            { 23, "Torrex Racer" },
            { 24, "Olympic" },
            { 25, "Paramedic" },
            { 26, "Prestige" },
            { 27, "Prestige Racer" },
            { 28, "Boltus" },
            { 29, "Schweizer" },
            { 30, "Schweizer Racer" },
            { 31, "Security Van" },
            { 32, "Teramo" },
            { 33, "Teramo Racer" },
            { 34, "Zenda" },
            { 35, "Zenda Racer" },
            { 36, "Cerva" },
            { 37, "Cerva Racer" },
            { 38, "Bonsai" },
            { 39, "Bonsai Racer" },
            { 40, "Brooklyn" },
            { 41, "Brooklyn Racer" },
            { 42, "Bus" },
            { 43, "Chauffeur" },
            { 44, "Chopper" },
            { 45, "Zartex" },
            { 46, "Courier" },
            { 47, "Cerrano" },
            { 48, "Cerrano Racer" },
            { 49, "Delivery Van" },
            { 50, "Dolva" },
            { 51, "Dolva Flatbed" },
            { 52, "Dozer" },
            { 53, "Andec" },
            { 54, "Andec Racer" },
            { 55, "Eurotech Lifter" },
            { 56, "Fairview" },
            { 57, "Firetruck" },
            { 58, "Grand Valley" },
            { 59, "Boldius" },
            { 60, "Land Roamer" },
            { 61, "Meat Wagon" },
            { 62, "Melizzano" },
            { 63, "Melizzano Racer" },
            { 64, "Raven" },
            { 65, "Raven Racer" },
            { 66, "Refuse Truck" },
            { 67, "Regina" },
            { 68, "Regina Racer" },
            { 69, "Rhapsody" },
            { 70, "Rosalita" },
            { 71, "San Marino" },
            { 72, "San Marino Racer" },
            { 73, "San Marino Spyder" },
            { 74, "San Marino Spyder Racer" },
            { 75, "School Bus" },
            { 76, "Namorra" },
            { 77, "Pangea" },
            { 78, "Wayfarer" },
            { 79, "Wingar" },
            { 80, "Woody" },
            { 81, "Wrecker" },
            { 82, "Yamashita 900" },
            { 83, "Mission Truck" },
            { 84, "Prison Bus" },
            { 85, "Ram Raider" },
            { 86, "Prison Van" },
            { 87, "Pimp Wagon" },
            { 88, "The Mexican's Ride" },
            { 89, "SWAT Van" },
            { 90, "Antilli VO3 Special" },
            { 91, "Cerva Punk" },
            { 92, "Brooklyn Punk" },
            { 93, "Cerrano Punk" },
            { 94, "Andec Punk" },
            { 95, "Olympic Punk" },
            { 96, "Torrex Turbo" },
            { 97, "Wayfarer Turbo" },

            { 100, "<NULL>" }
        };
    }
}
