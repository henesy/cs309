using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DegreeQuest
{
    class Settings
        
    {
        private string root = System.AppDomain.CurrentDomain.BaseDirectory + "..\\..\\..\\..";//gotten from DegreeQuest.cs
        private bool isClient;
        private uint res_x;
        private uint res_y;
        private string[] delim = new string[] { "=" }; //maybe make an enum for this

        /**
         * Default constructor
         */
        public Settings()
        {
            readConfig();
        }

        /**
         * Reads config file
         */
        private void readConfig()
        {
            //config changed to isClient
            System.IO.StreamReader config = new System.IO.StreamReader(root + @"/config.txt");
            string[] setting;
            while(config.Peek() >= 0)
            {
                string read = config.ReadLine();
                setting = read.Split(delim, StringSplitOptions.None);
                switch (setting[0])
                {
                    case "isClient":
                        isClient = Convert.ToBoolean(setting[1]);
                        break;
                    case "res_x":
                        res_x = Convert.ToUInt32(setting[1]);
                        break;
                    case "res_y":
                        res_y = Convert.ToUInt32(setting[1]);
                        break;
                    default:
                        break;
                }
            }
            config.Close();      
        }

        /// <summary>
        /// Getter for isclient
        /// </summary>
        public bool IsClient
        {
            get { return isClient; }
        }

        /// <summary>
        /// Getter and setter for res_x
        /// </summary>
        public uint ResX
        {
            get { return res_x; }
            set { res_x = value; }
        }
        
        /// <summary>
        /// Getter and setter for res_y
        /// </summary>
        public uint ResY
        {
            get { return res_y; }
            set { res_y = value; }
        }


        public void writeFile()
        {
            System.IO.StreamWriter file = new System.IO.StreamWriter(root + @"/config.txt");

            file.WriteLine("isClient="+isClient.ToString());
            file.WriteLine("res_x=" + res_x.ToString());
            file.WriteLine("res_y=" + res_y.ToString());
        }

    }
}
