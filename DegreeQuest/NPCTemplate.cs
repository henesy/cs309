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
    public class NPCTemplate
    {
        public static readonly String table_name = "NPCTemplate";
        public static readonly String NPC_FILE =  DegreeQuest.root+ "\\Content\\NPC.txt";
        public static String[] fields = { "Name",     "HP", "EP", "atk", "def", "spd", "lvl","subjects"};
        public static String[] types = {"VARCHAR(45)","INT","INT","INT", "INT", "INT", "INT","VARCHAR(45)"};
        public static String[] defaults = {
            NPC.name_DFLT,
            NPC.HP_DFLT.ToString(),
            NPC.EP_DFLT.ToString(),
            NPC.atk_DFLT.ToString(),
            NPC.def_DFLT.ToString(),
            NPC.spd_DFLT.ToString(),
            NPC.lvl_DFLT.ToString(),
            "NONE" };
        public String name;
        public Dice HP, EP, atk, def, spd, lvl;
        public BitVector32 subjects;

        NPCTemplate()
        {
            this.name = NPC.name_DFLT;
            this.HP = new Dice(NPC.HP_DFLT);
            this.EP = new Dice(NPC.EP_DFLT);
            this.atk = new Dice(NPC.atk_DFLT);
            this.def = new Dice(NPC.def_DFLT);
            this.spd = new Dice(NPC.spd_DFLT);
            this.lvl = new Dice(NPC.lvl_DFLT);
            this.subjects = new BitVector32(0);
        }

        NPCTemplate(String name,Dice HP, Dice EP, BitVector32 subjects)
        {
            this.name = name;
            this.HP = HP;
            this.EP = EP;
            this.subjects = subjects;
        }

        NPCTemplate(String name, Dice HP, Dice EP, int subjects): this(name,HP,EP,new BitVector32(subjects)) {}

        NPC create()
        {
            return new NPC(this);
        }

        public int subject()
        {
            if (subjects.Data == 0)
                return Subject.NONE;
            Random rand = new Random();
            int first = rand.Next(1, Subject.NUM);
            for (int i = 0; i < Subject.NUM; i++)
            {
                if (i + first > Subject.NUM){ first = first - Subject.NUM; }
                if (subjects[first+i])
                {
                    return first+i;
                }
            }
            return Subject.NONE;
        }

        public String[] toStringArray()
        {
            String[] arr = new String[fields.Length];
            arr[0] = name;
            arr[1] = HP.ToString();
            arr[2] = EP.ToString();
            arr[3] = atk.ToString();
            arr[4] = def.ToString();
            arr[5] = spd.ToString();
            arr[6] = lvl.ToString();
            arr[7] = subjects.ToString();
            return arr;
        }


        public static NPCTemplate Random()
        {
            NPCTemplate t = new NPCTemplate();
            String[] arr = DBReader.random(table_name);

            t.name = arr[1];
            t.HP = new Dice(arr[2]);
            t.EP = new Dice(arr[3]);
            t.atk = new Dice(arr[4]);
            t.def = new Dice(arr[5]);
            t.spd = new Dice(arr[6]);
            t.lvl = new Dice(arr[7]);
            t.subjects = Subject.listToVect(arr[8]);

            return t;
        }

        public static void update()
        {
            DBReader.createTable(table_name, fields, types, defaults);
            //NPCTemplate temp;
            String[] values;
            String[] lines = File.ReadAllLines(NPC_FILE, Encoding.UTF8);
            if (lines[0] != "Version 1.0.0")
                throw new Exception();
            int i = 1;
            while (i < lines.Length-fields.Length)
            {
                if (lines[i].Length<7 ||  //Need to be something in the name slot.
                    lines[i].Substring(0,4) != "Name"|| //Must begin with Name
                    !lines[i+fields.Length].Equals("")) //must end with empty line
                {
                    i++; continue;
                }
                else
                {
                    //temp = new NPCTemplate();
                    //temp.name = lines[i].Substring(6);
                    //temp.HP = new Dice(lines[i+1].Substring(4));
                    //temp.EP = new Dice(lines[i+2].Substring(4));
                    //temp.atk = new Dice(lines[i+3].Substring(5));
                    //temp.def = new Dice(lines[i+4].Substring(5));
                    //temp.spd = new Dice(lines[i+5].Substring(5));
                    //temp.lvl = new Dice(lines[i+6].Substring(5));
                    //temp.subjects = Subject.listToVect(lines[i+7].Substring(10));
                    values = new String[fields.Length];
                    for(int f = 0; f < fields.Length; f++)
                    {
                        values[f] = lines[i + f].Substring(fields[f].Length + 2);
                    }
                    DBReader.insertRecord(table_name,fields, values);
                    i = i + fields.Length;
                }
            }
        }

    }
}
