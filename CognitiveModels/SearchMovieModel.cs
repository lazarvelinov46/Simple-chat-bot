using Antlr4.Runtime.Misc;

namespace DiplomskiRad.CognitiveModels
{
    public class SearchMovieModel
    {
        public SearchMovieModel() 
        {
            Genres = new ArrayList<string>();
        }
        public int MinimumYear { get; set; }
        public int MaximumYear { get; set; }
        public ArrayList<string> Genres { get; set; }
        public int MinimumLengthInMinutes { get; set; }

        public int MaximumLengthInMinutes { get; set; }

        public string Actor { get; set; }

        public int MinimumRating { get; set; }
    }
}
