using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using DiplomskiRad.CognitiveModels;
using Antlr4.Runtime.Misc;
using System;
using System.IO;

namespace DiplomskiRad.Dialogs
{
    public class FantasyDialog : ComponentDialog
    {

        private static IStatePropertyAccessor<SearchPlayerModel> _searchPlayerModelAccessor;
        private static ArrayList<PlayerModel> players = new ArrayList<PlayerModel>();
        private static string step = "price";
        private static string clubvalid = "Please enter one of the following clubs:"+System.Environment.NewLine;
        private static string[] clubs = {"Arsenal","Aston Vila","Bournemouth","Brentford","Brighton","Burnley","Chelsea",
            "Crystal Palace","Everton","Fulham","Liverpool","Luton","Manchester City","Manchester United","Newcastle United",
            "Nottingham Forest","Sheffield United","Tottenham","West Ham United","Wolverhampton" };
        public FantasyDialog(UserState userState)
            : base(nameof(FantasyDialog))
        {
            _searchPlayerModelAccessor = (IStatePropertyAccessor<SearchPlayerModel>)userState.CreateProperty<SearchPlayerModel>("SearchModel");
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
            AddDialog(new NumberPrompt<float>(nameof(NumberPrompt<float>),FloatPromptValidation));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));
            AddDialog(new TextPrompt(nameof(TextPrompt),PreferedClubValidator));
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
                {
                    EnterPreferedClubStepAsync,
                    PreferedClubAsync,
                    PositionStepAsync,
                    PriceStepAsync,
                    OwnershipStepAsync,
                    MaximumFDRStepAsync,
                    FormStepAsync,
                    FinalStepAsync,
                    EndingStepAsync,
                }));
            InitialDialogId = nameof(WaterfallDialog);
            for (int i=0;i<clubs.Length-1;i++)
            {
                clubvalid += (clubs[i] + ", ");
            }
            clubvalid += (clubs[clubs.Length - 1] + ".");
            fillPlayers();
        }

        private static async Task<DialogTurnResult> EnterPreferedClubStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            //asking user if he wants player from some club he prefers
            return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions { Prompt = MessageFactory.Text("Would you like to enter prefered club?") }, cancellationToken);
        }
        private static async Task<DialogTurnResult> PreferedClubAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if ((bool)stepContext.Result)
            {
                //if yes, user enters prefered club which must be one of 20 premier league clubs
                var promptOptions = new PromptOptions
                {
                    Prompt = MessageFactory.Text("Please enter which club would you prefer your player to play for."),
                    RetryPrompt = MessageFactory.Text(clubvalid),
                };
                return await stepContext.PromptAsync(nameof(TextPrompt), promptOptions, cancellationToken);
            }
            else
            {
                //if no, club prompt is skipped
                return await stepContext.NextAsync("None", cancellationToken);
            }
        }
        private static async Task<DialogTurnResult> PositionStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            //asking for player position
            stepContext.Values["club"] = (string)stepContext.Result;
            return await stepContext.PromptAsync(nameof(ChoicePrompt),
            new PromptOptions
            {
                Prompt = MessageFactory.Text("Please enter position of player."),
                Choices = ChoiceFactory.ToChoices(new List<string> { "Goalkeeper", "Defender", "Midfielder", "Forward"}),
            }, cancellationToken);
        }
        private static async Task<DialogTurnResult> PriceStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            //asking for maximum player price user is willing to pay
            stepContext.Values["position"] = ((FoundChoice)stepContext.Result).Value;
            var promptOptions = new PromptOptions
            {
                Prompt = MessageFactory.Text("Please enter maximum player price."),
                RetryPrompt = MessageFactory.Text("The value entered must be between 4 and 14."),
            };
            return await stepContext.PromptAsync(nameof(NumberPrompt<float>), promptOptions, cancellationToken);
        }
        private static async Task<DialogTurnResult> OwnershipStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            //asking for maximum ownership of searched player amongst other fantasy players
            step = "ownership";
            stepContext.Values["price"] = (float)stepContext.Result;
            var promptOptions = new PromptOptions
            {
                Prompt = MessageFactory.Text("Please enter maximum player ownership percentage."),
                RetryPrompt = MessageFactory.Text("The value entered must be between 0 and 100."),
            };

            return await stepContext.PromptAsync(nameof(NumberPrompt<float>), promptOptions, cancellationToken);

        }
        private static async Task<DialogTurnResult> MaximumFDRStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            //asking user for maximum fixture difficulty rating for next 3 matches which searched player would play
            step = "fdr";
            stepContext.Values["ownership"] = (float)stepContext.Result;
            var promptOptions = new PromptOptions
            {
                Prompt = MessageFactory.Text("Please enter maximum player FDR for next 3 matches (Arithmetic mean)."),
                RetryPrompt = MessageFactory.Text("The value entered must be between 2 and 5."),
            };

            return await stepContext.PromptAsync(nameof(NumberPrompt<float>), promptOptions, cancellationToken);

        }
        private static async Task<DialogTurnResult> FormStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            //asking user for minimum form of searched player
            step = "form";
            stepContext.Values["fdr"] = (float)stepContext.Result;
            var promptOptions = new PromptOptions
            {
                Prompt = MessageFactory.Text("Please enter minimum player form "),
                RetryPrompt = MessageFactory.Text("The value entered must be between 0 and 40."),
            };

            return await stepContext.PromptAsync(nameof(NumberPrompt<float>), promptOptions, cancellationToken);

        }
        private static async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            step = "price";
            stepContext.Values["form"] = (float)stepContext.Result;
            var searchPlayerModel = await _searchPlayerModelAccessor.GetAsync(stepContext.Context, () => new SearchPlayerModel(), cancellationToken);
            searchPlayerModel.Club = (string)stepContext.Values["club"];
            searchPlayerModel.Position = (string)stepContext.Values["position"];
            searchPlayerModel.MaximumPrice = (float)stepContext.Values["price"];
            searchPlayerModel.MaximumOwnershipPercentage = (float)stepContext.Values["ownership"];
            searchPlayerModel.MaximumFixtureDifficultyRating = (float)stepContext.Values["fdr"];
            searchPlayerModel.MinimumForm = (float)stepContext.Values["form"];
            string reccomendations = findPlayer(searchPlayerModel);
            if(reccomendations== "Although it would be perfect to have player of your taste, no player in Premier League is even close to your requirements.")
            {
                stepContext.Values["success"] = false;
            }
            else
            {
                stepContext.Values["success"] = true;
            }
            await stepContext.Context.SendActivityAsync(MessageFactory.Text(reccomendations), cancellationToken);
            return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions { Prompt = MessageFactory.Text("Do you want to search for a new player?") }, cancellationToken);
        }
        private async Task<DialogTurnResult> EndingStepAsync(WaterfallStepContext stepContext,CancellationToken cancellationToken)
        {
            //ending or repeating the dialog, depends on previous choice
            bool again = (bool)stepContext.Result;
            if (again)
            {
                return await stepContext.ReplaceDialogAsync(nameof(FantasyDialog), null, cancellationToken);
            }
            else
            {
                bool ret = (bool)stepContext.Values["success"];
                return await stepContext.EndDialogAsync(ret, cancellationToken);
            }            
        }
        private static Task<bool> PreferedClubValidator(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            bool exists = false;
            string searchText=promptContext.Recognized.Value.ToLower();
            for(int i = 0; i < clubs.Length; i++)
            {
                if (clubs[i].ToLower() == searchText)
                {
                    exists = true;
                }
            }
            return Task.FromResult(promptContext.Recognized.Succeeded && exists);
        }
        private static Task<bool> FloatPromptValidation(PromptValidatorContext<float> promptContext, CancellationToken cancellationToken)
        {
            if (step == "price")
            {
                return Task.FromResult(promptContext.Recognized.Succeeded && promptContext.Recognized.Value >= 4 && promptContext.Recognized.Value <= 14);
            }
            else if(step=="ownership")
            {
                return Task.FromResult(promptContext.Recognized.Succeeded && promptContext.Recognized.Value >= 0 && promptContext.Recognized.Value <= 100);
            }
            else if(step=="fdr")
            {
                return Task.FromResult(promptContext.Recognized.Succeeded && promptContext.Recognized.Value >= 2 && promptContext.Recognized.Value <= 5);
            }
            else
            {
                return Task.FromResult(promptContext.Recognized.Succeeded && promptContext.Recognized.Value >= 0 && promptContext.Recognized.Value <= 40);
            }
            
        }
        private void fillPlayers()
        {
            string textFile = "players.txt";
            string[] lines = { };
            if (File.Exists(textFile))
            {
                lines = File.ReadAllLines(textFile);

            }
            foreach (string line in lines)
            {
                string[] parts = line.Split(';');
                PlayerModel player = new PlayerModel();
                player.PlayerName = parts[0];
                player.Club = parts[1];
                player.Position = parts[2];
                player.Price = (float)Convert.ToDouble(parts[3]);
                player.OwnershipPercentage= (float)Convert.ToDouble(parts[4]);
                player.FixtureDifficultyRating = (float)Convert.ToDouble(parts[5]);
                player.Form = (float)Convert.ToDouble(parts[6]);
                players.Add(player);
            }
        }
        private static string findPlayer(SearchPlayerModel searchPlayerModel)
        {
            ArrayList<int> hits = new ArrayList<int>();
            String recPlayers = "";
            for (int i = 0; i < players.Count; i++)
            {
                if (searchPlayerModel.Club=="None")
                {
                    hits.Add(1);
                }
                else if (searchPlayerModel.Club == players[i].Club)
                {
                    hits.Add(1);
                }
                else
                {
                    hits.Add(0);
                }
                if (players[i].Position == searchPlayerModel.Position)
                {
                    hits[i]++;
                }
                if (players[i].Price <= searchPlayerModel.MaximumPrice)
                {
                    hits[i]++;
                }
                if (players[i].OwnershipPercentage <= searchPlayerModel.MaximumOwnershipPercentage)
                {
                    hits[i]++;
                }
                if (players[i].FixtureDifficultyRating <= searchPlayerModel.MaximumFixtureDifficultyRating)
                {
                    hits[i]++;
                }
                if (players[i].Form >= searchPlayerModel.MinimumForm)
                {
                    hits[i]++;
                }
            }
            int maxhits = 0;
            for (int i = 0; i < hits.Count; i++)
            {
                if (hits[i] > maxhits)
                {
                    maxhits = hits[i];
                }
            }
            if (maxhits <= 3)
            {
                return "Although it would be perfect to have player of your taste, no player in Premier League is even close to your requirements.";
            }
            else if (maxhits == 6)
            {
                recPlayers += "Players that match your requirements are:"+System.Environment.NewLine;
                for (int i = 0; i < hits.Count; i++)
                {
                    if (hits[i] == maxhits)
                    {
                        recPlayers += players[i].PlayerName + " from "+players[i].Club;
                        recPlayers += System.Environment.NewLine;
                    }
                }
                return recPlayers;
            }
            else
            {
                recPlayers += "Sorry, we couldn't find a player that matches all your requirements" +
                    ", but take look at following players which could solve your problems:"+System.Environment.NewLine;
                for (int i = 0; i < hits.Count; i++)
                {
                    if (hits[i] == maxhits)
                    {
                        recPlayers += players[i].PlayerName + " from " + players[i].Club;
                        recPlayers += System.Environment.NewLine;
                    }
                }
                return recPlayers;
            }
            
        }
    }
}
