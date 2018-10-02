using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Specialized;
using System.IO;

namespace DegreeQuest
{
    public class ItemTemplate
    {
        public static readonly String table_name = "ItemTemplate";
        public static readonly String Item_FILE = DegreeQuest.root + "\\Content\\Item.txt";
        public static String[] fields = { "Name", "Type", "Atk", "Def", "Spd", "HP", "EP", "Value" , "Rarity","Logic", "Life", "Chem", "Tech"};
        public static String[] types = { "VARCHAR(45)", "VARCHAR(45)", "VARCHAR(45)", "VARCHAR(45)", "VARCHAR(45)", "VARCHAR(45)", "VARCHAR(45)", "VARCHAR(45)", "VARCHAR(45)", "INT", "INT", "INT", "INT" };
        public static String[] defaults = {
            Item.name_DFLT,
            Item.type_DFLT.ToString(),
            Item.atk_DFLT.ToString(),
            Item.def_DFLT.ToString(),
            Item.spd_DFLT.ToString(),
            Item.HP_DFLT.ToString(),
            Item.EP_DFLT.ToString(),
            Item.value_DFLT.ToString(),
            Item.rarity_DFLT.ToString(),
            Item.logic_DFLT.ToString(),
            Item.life_DFLT.ToString(),
            Item.chem_DFLT.ToString(),
            Item.tech_DFLT.ToString()
        };
        public String name;
        public Item.IType type;
        public int atk,def,spd,HP,EP, value, rarity;
        public int[] stats;

        ItemTemplate()
        {
            
            this.name = Item.name_DFLT;
            this.type = Item.type_DFLT;
            this.atk = Item.atk_DFLT;
            this.def = Item.def_DFLT;
            this.spd = Item.spd_DFLT;
            this.HP = Item.HP_DFLT;
            this.EP = Item.EP_DFLT;
            this.value = Item.value_DFLT;
            this.rarity = Item.rarity_DFLT;

            this.stats = new int[Stat.NUM];
            this.stats[Stat.LOGIC] = Item.logic_DFLT;
            this.stats[Stat.LIFE] = Item.life_DFLT;
            this.stats[Stat.CHEM] = Item.chem_DFLT;
            this.stats[Stat.TECH] = Item.tech_DFLT;
        }

        public static ItemTemplate Random()
        {
            ItemTemplate t = new ItemTemplate();
            String[] arr = DBReader.random(table_name);

            t.name = arr[1];
            Enum.TryParse(arr[2], out t.type);
            t.atk = int.Parse(arr[3]);
            t.def = int.Parse(arr[4]);
            t.spd = int.Parse(arr[5]);
            t.HP = int.Parse(arr[6]);
            t.EP = int.Parse(arr[7]);
            t.value = int.Parse(arr[8]);
            t.rarity = int.Parse(arr[9]);
            t.stats[0] = int.Parse(arr[10]);
            t.stats[1] = int.Parse(arr[11]);
            t.stats[2] = int.Parse(arr[12]);
            t.stats[3] = int.Parse(arr[13]);

            return t;
        }

        public static void update()
        {
            DBReader.createTable(table_name, fields, types, defaults);
            //ItemTemplate temp;
            String[] values;
            String[] lines = File.ReadAllLines(Item_FILE, Encoding.UTF8);
            if (lines[0] != "Version 1.0.0")
                throw new Exception();
            int i = 1;
            while (i < lines.Length - fields.Length)
            {
                if (lines[i].Length < 7 ||  //Need to be something in the name slot.
                    lines[i].Substring(0, 4) != "Name" || //Must begin with Name
                    !lines[i + fields.Length].Equals("")) //must end with empty line
                {
                    i++; continue;
                }
                else
                {
                    //temp = new ItemTemplate();
                    //temp.name = lines[i].Substring(6);
                    //temp.HP = new Dice(lines[i+1].Substring(4));
                    //temp.EP = new Dice(lines[i+2].Substring(4));
                    //temp.atk = new Dice(lines[i+3].Substring(5));
                    //temp.def = new Dice(lines[i+4].Substring(5));
                    //temp.spd = new Dice(lines[i+5].Substring(5));
                    //temp.lvl = new Dice(lines[i+6].Substring(5));
                    //temp.subjects = Subject.listToVect(lines[i+7].Substring(10));
                    values = new String[fields.Length];
                    for (int f = 0; f < fields.Length; f++)
                    {
                        values[f] = lines[i + f].Substring(fields[f].Length + 2);
                    }
                    DBReader.insertRecord(table_name, fields, values);
                    i = i + fields.Length;
                }
            }
        }

    }
}