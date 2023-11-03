using Antlr4.Runtime.Misc;

namespace DiplomskiRad.CognitiveModels
{
    public class MovieModel
    {
        public MovieModel() 
        {
            Genres = new ArrayList<string>();
            Actors= new ArrayList<string>();
        }
        public string MovieName { get; set; }
        public int ReleaseYear { get; set; }
        public ArrayList<string> Genres { get; set; }
        public int LengthInMinutes { get; set; }
           
        public ArrayList<string> Actors { get; set; }

        public int Rating { get; set; }
    }
}
