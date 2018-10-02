using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DegreeQuest
{
    public class Subject
    {
        public static readonly int NUM = 8;
        public static String[] Subjects = { "NONE", "MATH", "BIO","CHEM","COMPE","COMSCI","MED","BIONICS","CHEME"};
        public static int MATH = 1; //Fighter
        public static int BIO = 2;  //Druid
        public static int CHEM = 3; //Shaman
        public static int COMPE = 4; //Sorcerer/Mage
        public static int COMSCI = 5; //MATH*COMPE; //Battle Mage
        public static int MED = 6; //CHEM*BIO; //Cleric/Priest
        public static int BIONICS = 7; //COMPE*BIO; //Warlock/Necromancer
        public static int CHEME = 8; //MATH*CHEM; //Melee Shaman, 'elemental' fighter.
        public static int NONE = 0; 

        //Creates a BitVector to represent a list of subjects given by list
        public static BitVector32 listToVect(String list)
        {
            char[] delims = { ' ', ',' };
            BitVector32 vect = new BitVector32(0);
            String[] subs = list.Split(delims);
            for (int i=0;i<subs.Length;i++)
            {
                int s = Array.IndexOf(Subjects, subs[i]);
                if (s > 0)
                {
                    vect[s] = true;
                }
            }
            return vect;
        }


    }
}
