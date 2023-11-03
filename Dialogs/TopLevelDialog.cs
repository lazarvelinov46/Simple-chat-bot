using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System.Threading.Tasks;
using System.Threading;
using DiplomskiRad.CognitiveModels;
using Microsoft.Bot.Builder.Dialogs.Choices;
using System.Collections.Generic;

namespace DiplomskiRad.Dialogs
{
    public class TopLevelDialog:ComponentDialog
    {
        public TopLevelDialog(UserState userState)
            : base(nameof(TopLevelDialog))
        {
            AddDialog(new FantasyDialog(userState));
            AddDialog(new MovieRecommenderDialog(userState));
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                ChoiceStepAsync,
                BranchingStepAsync,
                FinishStepAsync
            }));
            InitialDialogId = nameof(WaterfallDialog);
        }
       
        private static async Task<DialogTurnResult> ChoiceStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            //here user is given a choice between two separate dialogs
            return await stepContext.PromptAsync(nameof(ChoicePrompt),
                new PromptOptions
                {
                    Prompt = MessageFactory.Text($"Hello! Please choose reason for contacting dialogs bot."),
                    Choices = ChoiceFactory.ToChoices(new List<string> { "Fantasy search", "Movie recommender" }),
                }, cancellationToken);
        }
        private async Task<DialogTurnResult> BranchingStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            //here new dialog starts and gets pushed to the dialog stack
            var choice = ((FoundChoice)stepContext.Result).Value;
            stepContext.Values["choice"]= choice;
            if (choice=="Fantasy search")
            {
                //startng of fantasy player search dialog
                return await stepContext.BeginDialogAsync(nameof(FantasyDialog), null, cancellationToken);
            }
            else
            {
                //starting of movie recommender dialog
                return await stepContext.BeginDialogAsync(nameof(MovieRecommenderDialog), null, cancellationToken);
            }
        }
        private async Task<DialogTurnResult> FinishStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            //sending goodbye messages to user
            if (stepContext.Values["choice"] == "Fantasy search")
            {
                bool res = (bool)stepContext.Result;
                string msg;
                if (res)
                {
                    msg = "I hope you do well in your mini leagues this gameweek!";
                }
                else
                {
                    msg = "Sorry I wasn't of any help. Lower your expectations for these players and better luck next time mate! :)";
                }
                await stepContext.Context.SendActivityAsync(MessageFactory.Text(msg), cancellationToken);
            }
            else
            {
                bool res = (bool)stepContext.Result;
                string msg;
                if (res)
                {
                    msg = "I hope you enjoy movie recommendations!";
                }
                else
                {
                    msg = "Sorry I wasn't of any help.";
                }
                await stepContext.Context.SendActivityAsync(MessageFactory.Text(msg),cancellationToken);
            }

            return await stepContext.EndDialogAsync(null, cancellationToken);
        }

    
        
        

    }
}
