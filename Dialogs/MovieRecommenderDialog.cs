using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using System.Threading.Tasks;
using System.Threading;
using DiplomskiRad.CognitiveModels;
using System.Collections.Generic;
using System.Collections;
using Antlr4.Runtime.Misc;
using System.IO;
using System;
using NuGet.ContentModel;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DiplomskiRad.Dialogs
{
    public class MovieRecommenderDialog : ComponentDialog
    {

        private static IStatePropertyAccessor<SearchMovieModel> _searchModelAccessor;
        private static int minyear = 0;
        private static int minlength = 0;
        private static string step = "year";
        private static string chosen = "None";
        private static ArrayList<MovieModel> movies;
        private const string API = "http://www.omdbapi.com/?t=";
        private const string USERID = "&apikey=fa44e010";
        public MovieRecommenderDialog(UserState userState):base(nameof(MovieRecommenderDialog)) 
        {
            _searchModelAccessor = (IStatePropertyAccessor<SearchMovieModel>)userState.CreateProperty<SearchMovieModel>("SearchMovieModel");
            movies = new ArrayList<MovieModel>();
            fillMovies();
            var waterfallDialog = new WaterfallStep[]
            {
                MinYearStepAsync,
                MaxYearStepAsync,
                GenreStepAsync,
                AnotherGenreStepAsync,
                Genre2StepAsync,
                MinLengthStepAsync,
                MaxLengthStepAsync,
                ActorStepAsync,
                RatingStepAsync,
                FinalStepAsync
            };
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallDialog));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new NumberPrompt<int>(nameof(NumberPrompt<int>), NumberPromptValidator));
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt),ChoicePromptValidator));
            InitialDialogId = nameof(WaterfallDialog);
        }

        private static async Task<DialogTurnResult> MinYearStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            //prompts user for minimum movie release year
            var promptOptions = new PromptOptions
            {
                Prompt = MessageFactory.Text("Please enter minimum movie release year."),
                RetryPrompt = MessageFactory.Text("The value entered must be greater than 1887 and less than 2024."),
            };
            return await stepContext.PromptAsync(nameof(NumberPrompt<int>), promptOptions, cancellationToken);
        }
        private static async Task<DialogTurnResult> MaxYearStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            //prompts user for maximum movie release year
            minyear = (int)stepContext.Result;
            stepContext.Values["minimumYear"] = minyear;
            var promptOptions = new PromptOptions
            {
                Prompt = MessageFactory.Text("Please enter maximum movie release year."),
                RetryPrompt = MessageFactory.Text("The value entered must be greater than minimum year and less than 2024."),
            };

            return await stepContext.PromptAsync(nameof(NumberPrompt<int>), promptOptions, cancellationToken);
        }
        private static async Task<DialogTurnResult> GenreStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            //prompts user for preferred genre
            step = "length";
            stepContext.Values["maximumYear"] = (int)stepContext.Result;
            return await stepContext.PromptAsync(nameof(ChoicePrompt),
            new PromptOptions
            {
                Prompt = MessageFactory.Text("Please enter genre you are interested in."),
                Choices = ChoiceFactory.ToChoices(new List<string> { "Drama","Crime","Comedy", "Horror", "Action", "Adventure", "Sci-Fi", "Romance", "Sport", "War", "Cartoon" }),
            }, cancellationToken);
        }
        private static async Task<DialogTurnResult> AnotherGenreStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            //asks user if he wants to add another preferred genre
            stepContext.Values["genre"] = ((FoundChoice)stepContext.Result).Value;
            chosen = ((FoundChoice)stepContext.Result).Value;
            return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions { Prompt = MessageFactory.Text("Would you like to add another genre?") }, cancellationToken);
        }
        private static async Task<DialogTurnResult> Genre2StepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            //prompts user for adding another genre
            stepContext.Values["skip"] = (bool)stepContext.Result;
            if ((bool)stepContext.Result)
            {
                return await stepContext.PromptAsync(nameof(ChoicePrompt),
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("Please enter another genre."),
                    Choices = ChoiceFactory.ToChoices(new List<string> { "Drama", "Crime", "Comedy", "Horror", "Action", "Adventure", "Sci-Fi", "Romance", "Sport", "War", "Cartoon" }),
                    RetryPrompt=MessageFactory.Text("Please choose different genre from first one.")
                }, cancellationToken);
            }
            else
            {
                return await stepContext.NextAsync(null, cancellationToken);
            }


        }
        private static async Task<DialogTurnResult> MinLengthStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            chosen = "None";
            //prompts user for minimum length of a movie
            if ((bool)stepContext.Values["skip"])
            {
                stepContext.Values["genre2"] = ((FoundChoice)stepContext.Result).Value;
            }
            var promptOptions = new PromptOptions
            {
                Prompt = MessageFactory.Text("Please enter minimum length of movie in minutes."),
                RetryPrompt = MessageFactory.Text("The value entered must be greater than 30 and less than 300."),
            };

            return await stepContext.PromptAsync(nameof(NumberPrompt<int>), promptOptions, cancellationToken);
        }
        private static async Task<DialogTurnResult> MaxLengthStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            //prompts user for maximum length of a movie
            stepContext.Values["minimumLength"] = (int)stepContext.Result;
            minlength = (int)stepContext.Result;
            var promptOptions = new PromptOptions
            {
                Prompt = MessageFactory.Text("Please enter maximum length of movie in minutes."),
                RetryPrompt = MessageFactory.Text("The value entered must be greater than minimum length and less than 300."),
            };

            return await stepContext.PromptAsync(nameof(NumberPrompt<int>), promptOptions, cancellationToken);
        }
        private static async Task<DialogTurnResult> ActorStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            //prompts user for preferred actor
            step = "rating";
            stepContext.Values["maximumLength"] = (int)stepContext.Result;
            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Please enter name of the actor you like.") }, cancellationToken);
        }
        private static async Task<DialogTurnResult> RatingStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            //prompts user for minimum metascore critic rating of a movie
            stepContext.Values["actor"] = (string)stepContext.Result;
            var promptOptions = new PromptOptions
            {
                Prompt = MessageFactory.Text("Please enter minimum Metascore critic rating of movie."),
                RetryPrompt = MessageFactory.Text("The value entered must be greater than 0 and less than 101."),
            };

            return await stepContext.PromptAsync(nameof(NumberPrompt<float>), promptOptions, cancellationToken);
            
        }
        private static async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            //searching for a movie and returning a response
            step = "year";
            stepContext.Values["rating"] = (int)stepContext.Result;
            var searchModel = await _searchModelAccessor.GetAsync(stepContext.Context, () => new SearchMovieModel(), cancellationToken);
            searchModel.MinimumYear = (int)stepContext.Values["minimumYear"];
            searchModel.MaximumYear = (int)stepContext.Values["maximumYear"];
            searchModel.Genres.Insert(0,(string)stepContext.Values["genre"]);
            if (stepContext.Values.ContainsKey("genre2"))
            {
                searchModel.Genres.Insert(1,((string)stepContext.Values["genre2"]));
            }
            searchModel.MinimumLengthInMinutes = (int)stepContext.Values["minimumLength"];
            searchModel.MaximumLengthInMinutes = (int)stepContext.Values["maximumLength"];
            searchModel.Actor = (string)stepContext.Values["actor"];
            searchModel.MinimumRating = (int)stepContext.Values["rating"];

            var msg = $"You are searching for a movie between {searchModel.MinimumYear} and {searchModel.MaximumYear},";

            if (searchModel.Genres.Count != 1)
            {
                msg += $" and with these two genres: {searchModel.Genres[0]}, {searchModel.Genres[1]}.";
            }
            else
            {
                msg += $" and with {searchModel.Genres[0]} genre.";
            }

            msg += $" You are prefering movie that is minimum {searchModel.MinimumLengthInMinutes} minutes long and maximum {searchModel.MaximumLengthInMinutes} minutes long.";
            msg += $" Actor or actress you like is: {searchModel.Actor}.";
            msg += $" Minimum critic rating you would like is {searchModel.MinimumRating}.";
            minyear = 0;
            minlength = 0;
            await stepContext.Context.SendActivityAsync(MessageFactory.Text(msg), cancellationToken);
            string reccomendations = calculateRecommendations(searchModel);
            bool ret = true;
            if (reccomendations == "Database have no movie that is similar to your taste.") ret = false;
            await stepContext.Context.SendActivityAsync(MessageFactory.Text(reccomendations), cancellationToken);
            return await stepContext.EndDialogAsync(ret, cancellationToken);
        }

        //validator for numbers
        private static Task<bool> NumberPromptValidator(PromptValidatorContext<int> promptContext, CancellationToken cancellationToken)
        {
            if (step == "year")
            {
                if (minyear > 0)
                {
                    return Task.FromResult(promptContext.Recognized.Succeeded && promptContext.Recognized.Value > 1887 && promptContext.Recognized.Value < 2024 && promptContext.Recognized.Value >= minyear);
                }
                return Task.FromResult(promptContext.Recognized.Succeeded && promptContext.Recognized.Value > 1887 && promptContext.Recognized.Value < 2024);
            } 
            else if(step=="length")
            {
                if (minlength > 0)
                {
                    return Task.FromResult(promptContext.Recognized.Succeeded && promptContext.Recognized.Value > 30 && promptContext.Recognized.Value < 300 && promptContext.Recognized.Value >= minlength);
                }
                return Task.FromResult(promptContext.Recognized.Succeeded && promptContext.Recognized.Value > 30 && promptContext.Recognized.Value < 300);
            }
            else
            {
                return Task.FromResult(promptContext.Recognized.Succeeded && promptContext.Recognized.Value > 0 && promptContext.Recognized.Value < 101);
            }
            
        }
        //validator for preventing user to choose 2 same genres
        private static Task<bool> ChoicePromptValidator(PromptValidatorContext<FoundChoice> promptContext, CancellationToken cancellationToken)
        {
            string second = promptContext.Recognized.Value.Value;
            if (chosen == second)
            {
                return Task.FromResult(false);
            }
            else
            {
                return Task.FromResult(true);
            }
        }
        //filling list with movie models
        private void fillMovies()
        {
            string textFile = "movies.txt";
            string[] lines= { };
            if (File.Exists(textFile))
            {
                lines=File.ReadAllLines(textFile);

            }
            foreach (string line in lines)
            {
                string[] parts = line.Split(';');
                MovieModel mov = new MovieModel();
                mov.MovieName = parts[0];
                mov.ReleaseYear = Convert.ToInt32(parts[1]);
                string[] genres = parts[2].Split(',');
                foreach (string g in genres)
                {
                    mov.Genres.Add(g);
                }
                mov.LengthInMinutes = Convert.ToInt32(parts[3]);
                string[] actors = parts[4].Split(",");
                foreach (string a in actors)
                {
                    mov.Actors.Add(a);
                }
                mov.Rating = Convert.ToInt32(parts[5]);
                movies.Add(mov);
            }
        }
        //calculating recommendations
        private static string calculateRecommendations(SearchMovieModel searchModel)
        {
            string recMovies = "Results are here:"+System.Environment.NewLine;
            ArrayList<int> hits = new ArrayList<int>();
            for(int i = 0; i < movies.Count; i++)
            {
                if (movies[i].ReleaseYear >= searchModel.MinimumYear)
                {
                    hits.Add(1);
                }
                else
                {
                    hits.Add(0);
                }
                if (movies[i].ReleaseYear <= searchModel.MaximumYear)
                {
                    hits[i]++;
                }
                for (int j=0;j< movies[i].Genres.Count; j++)
                {
                    for(int k = 0; k < searchModel.Genres.Count; k++)
                    {
                        if (movies[i].Genres[j] == searchModel.Genres[k])
                        {
                            hits[i]++;
                        }
                    }
                }
                if (movies[i].LengthInMinutes >= searchModel.MinimumLengthInMinutes)
                {
                    hits[i]++;
                }
                if (movies[i].LengthInMinutes <= searchModel.MaximumLengthInMinutes)
                {
                    hits[i]++;
                }
                for(int j = 0; j < movies[i].Actors.Count; j++)
                {
                    if (movies[i].Actors[j] == searchModel.Actor)
                    {
                        hits[i]++;
                    }
                }
                if (movies[i].Rating >= searchModel.MinimumRating)
                {
                    hits[i]++;
                }
            }
            int maxhits = 0;
            for(int i = 0; i < hits.Count; i++)
            {
                if (hits[i] > maxhits)
                {   
                    maxhits = hits[i];
                }
            }
            if (maxhits <= 4)
            {
                return "Database have no movie that is similar to your taste.";
            }
            else
            {
                recMovies += System.Environment.NewLine;
                for (int i = 0; i < hits.Count; i++)
                {
                    if (hits[i] == maxhits)
                    {
                        string movieId = "";
                        try
                        {
                            using (var client = new HttpClient())
                            {
                                string movie = movies[i].MovieName.ToLower().Replace(" ", "+");
                                string toSend = API + movie + USERID;
                                var uri = new Uri(toSend);
                                var returnResult = client.GetAsync(uri).Result.Content.ReadAsStringAsync().Result;
                                JObject jsonResult = JObject.Parse(returnResult);
                                movieId = jsonResult["imdbID"].ToString();
                            }
                        }catch(Exception e)
                        {
                            movieId = "no connection";
                        }
                        recMovies += movies[i].MovieName + " ";
                        if(movieId!="no connection")
                        {
                            recMovies += "https://www.imdb.com/title/" + movieId;
                        }
                        recMovies += System.Environment.NewLine;
                    }
                }
            }
            

            return recMovies;
        }
    }
}
