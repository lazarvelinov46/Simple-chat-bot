using Antlr4.Runtime.Misc;
using System;

namespace DiplomskiRad.CognitiveModels
{
    public class UserModel
    {
        public UserModel()
        {
            Meetings = new ArrayList<DateTime>();
        }
        public String Name { get; set; }
        public String Surname { get; set; }

        public int Age { get; set; }

        public ArrayList<DateTime> Meetings { get; set; }

        public bool employed { get; set; }

        public bool married { get; set; }
    }
}
