using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using System.Threading.Tasks;
using System.Threading;
using DiplomskiRad.CognitiveModels;

namespace DiplomskiRad.Dialogs
{
    public class StartDialog : ComponentDialog
    {
        private UserState _userState;

        public StartDialog(UserState userState)
            : base(nameof(StartDialog))
        {
            _userState = userState;

            AddDialog(new TopLevelDialog(userState));

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                StartingStepAsync,
                FinalStepAsync,
            }));

            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> StartingStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            //runs top level dialog and pushes it to the dialog stack
            return await stepContext.BeginDialogAsync(nameof(TopLevelDialog), null, cancellationToken);
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

            string status = "Thanks for using our dialog chatbot. If you want to exit type: Exit. If you want to continue, write whatever you want.";

            await stepContext.Context.SendActivityAsync(status);


            return await stepContext.EndDialogAsync("Thanks for using our dialog chatbot.", cancellationToken);
        }
    }
}
