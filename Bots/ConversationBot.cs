// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
//
// Generated with Bot Builder V4 SDK Template for Visual Studio CoreBot v4.18.1

using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Microsoft.Recognizers.Text.DateTime;
using Microsoft.Recognizers.Text.Number;
using Microsoft.Recognizers.Text;
using DiplomskiRad.CognitiveModels;
using System.Collections.Generic;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DiplomskiRad.Bots
{
    
    public class ConversationBot<T> : ActivityHandler where T : Dialog
    {
        private BotState _conversationState;
        private BotState _userState;
        private Dialog _dialog;
        private ILogger _logger;

        public ConversationBot(ConversationState conversationState, UserState userState,T dialog, ILogger<ConversationBot<T>> logger)
        {
            _conversationState = conversationState;
            _userState = userState;
            _dialog = dialog;
            _logger = logger;
        }
        private static async Task StartingConversationAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            //Greets user
            var reply = MessageFactory.Text("How can I help you?");
            //Suggests actions available in this conversation bot
            reply.SuggestedActions = new SuggestedActions()
            {
                Actions = new List<CardAction>()
        {
            new CardAction() { Title = "Schedule meeting", Type = ActionTypes.ImBack, Value = "Schedule meeting" },
            new CardAction() { Title = "Recommender", Type = ActionTypes.ImBack, Value = "Recommender" }
        },
            };
            await turnContext.SendActivityAsync(reply, cancellationToken);
        }
        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            //this event occurs when member joins a conversation conversation.Immidiately task is started to welcome user.
            await StartingConversationAsync(turnContext, cancellationToken);
        }

        private static bool ValidateString(string str)
        {
            if (str is null || str == "")
            {
                return false;
            }
            return true;
        }
        private static int ValidateAge(String str)
        {
            int age = -1;
            try
            {
                var number = NumberRecognizer.RecognizeNumber(str, Culture.English);
                foreach (var item in number)
                {
                    if (item.Resolution.TryGetValue("value", out var n))
                    {
                        age = Convert.ToInt32(n);
                        if (!(age > 0 && age < 135))
                        {
                            //not a valid age
                            age = -2;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                age = -1;
            }

            return age;
        }

        private static DateTime ValidateDate(string str)
        {
            DateTime date = DateTime.MinValue;
            try
            {
                var convres = DateTimeRecognizer.RecognizeDateTime(str, Culture.English);
                foreach (var item in convres)
                {
                    var resolutions = item.Resolution["values"] as List<Dictionary<string, string>>;

                    foreach (var r in resolutions)
                    {
                        if (r.TryGetValue("value", out var dateString)
                            || r.TryGetValue("start", out dateString))
                        {
                            if (DateTime.TryParse(dateString, out var candidate)
                                && DateTime.Now < candidate)
                            {
                                date = candidate;
                                return date;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                date = DateTime.MaxValue;
            }
            return date;
        }

        protected async Task userInputAsync(ConversationModel conversationData, UserModel userProfile, ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var input = turnContext.Activity.Text?.Trim();
            if (conversationData.AskedForName&&conversationData.LastQuestion==ConversationModel.questions.NONE)
            {
                //if user already entered his data
                await turnContext.SendActivityAsync($"Hello again {userProfile.Name} {userProfile.Surname}!", null, null, cancellationToken);
                await turnContext.SendActivityAsync("When is your meeting?", null, null, cancellationToken);
                conversationData.LastQuestion = ConversationModel.questions.DATE;
                return;
            }
            //deciding what question user will be asked
            switch (conversationData.LastQuestion)
            {
                case ConversationModel.questions.NONE:
                    //name prompt
                    await turnContext.SendActivityAsync("Could you tell me your name please?", null, null, cancellationToken);
                    conversationData.LastQuestion = ConversationModel.questions.NAME;
                    break;
                case ConversationModel.questions.NAME:
                    if (ValidateString(input))
                    {
                        //saving the name and surname prompt
                        userProfile.Name = input;
                        await turnContext.SendActivityAsync($"Hello {userProfile.Name}!", null, null, cancellationToken);
                        await turnContext.SendActivityAsync("Would you be kind enough to tell me your surname?", null, null, cancellationToken);
                        conversationData.LastQuestion = ConversationModel.questions.SURNAME;
                        break;
                    }
                    else
                    {
                        await turnContext.SendActivityAsync("Sorry, I could not understand that. Could you enter your name again?", null, null, cancellationToken);
                        break;
                    }

                case ConversationModel.questions.SURNAME:
                    if (ValidateString(input))
                    {
                        //saving surname and age prompt
                        userProfile.Surname = input;
                        await turnContext.SendActivityAsync($"So, your full name is {userProfile.Name} {userProfile.Surname}", null, null, cancellationToken);
                        await turnContext.SendActivityAsync("How old are you?", null, null, cancellationToken);
                        conversationData.LastQuestion = ConversationModel.questions.AGE;
                        break;
                    }
                    else
                    {
                        await turnContext.SendActivityAsync("Sorry, I could not understand that. Could you enter your name again?", null, null, cancellationToken);
                        break;
                    }

                case ConversationModel.questions.AGE:
                    int age;
                    if ((age = ValidateAge(input)) > 0)
                    {
                        //saving age and meeting prompt
                        userProfile.Age = age;
                        await turnContext.SendActivityAsync($"Ok so you are {userProfile.Age} years old.", null, null, cancellationToken);
                        await turnContext.SendActivityAsync("When is your meeting?", null, null, cancellationToken);
                        conversationData.AskedForName = true;
                        conversationData.LastQuestion = ConversationModel.questions.DATE;
                        break;
                    }
                    else
                    {
                        string msg = "";
                        if (age == -1)
                        {
                            msg = "Sorry, your input could not be understood as age number. Please try again.";
                        }
                        else
                        {
                            msg = "Please input age between 1 and 135";
                        }
                        await turnContext.SendActivityAsync(msg, null, null, cancellationToken);
                        break;
                    }
                case ConversationModel.questions.DATE:
                    DateTime d = ValidateDate(input);
                    if (!(d.Equals(DateTime.MinValue) || d.Equals(DateTime.MaxValue)))
                    {
                        //saving date
                        userProfile.Meetings.Add(d);
                        await turnContext.SendActivityAsync($"Your meeting is scheduled for {d.ToString()}");
                        await turnContext.SendActivityAsync("It was nice chatting with you, if you want to make new appointment please write New", null, null, cancellationToken);
                        conversationData.LastQuestion = ConversationModel.questions.END;
                        break;
                    }
                    else
                    {
                        string msg = "";
                        if (d.Equals(DateTime.MinValue))
                        {
                            msg = "Please enter date and time that is after current time.";
                        }
                        else
                        {
                            msg = "Please try again. I could not understand what is the date you typed";
                        }
                        await turnContext.SendActivityAsync(msg, null, null, cancellationToken);
                        break;
                    }
                default:
                    if (input == "New" || input == "new")
                    {
                        //prompt for new meeting
                        await turnContext.SendActivityAsync("When is your meeting?", null, null, cancellationToken);
                        conversationData.LastQuestion = ConversationModel.questions.DATE;
                        conversationData.ConversationTopic = ConversationModel.topic.MEETING;
                    }
                    else
                    {
                        //exiting meeting scheduler and returning to starting question
                        conversationData.ConversationTopic = ConversationModel.topic.NONE;
                        conversationData.LastQuestion = ConversationModel.questions.NONE;
                        await StartingConversationAsync(turnContext, cancellationToken);
                    }
                    break;
            }
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            // converting text from message to all lower characters to avoid case sensitivity
            var text = turnContext.Activity.Text.ToLower();
            //accessing conversation data
            var conversationStateAccessors = _conversationState.CreateProperty<ConversationModel>(nameof(ConversationModel));
            var conversationData = await conversationStateAccessors.GetAsync(turnContext, () => new ConversationModel());
            //accessing user data
            var userStateAccessors = _userState.CreateProperty<UserModel>(nameof(UserModel));
            var userProfile = await userStateAccessors.GetAsync(turnContext, () => new UserModel());

            if (conversationData.ConversationTopic == ConversationModel.topic.NONE)
            {
                if(text=="schedule meeting")
                {
                    //if schedule meeting is picked, this starts prompts which gather data from user needed to schedule
                    conversationData.ConversationTopic = ConversationModel.topic.MEETING;
                    await userInputAsync(conversationData, userProfile, turnContext, cancellationToken);
                }
                else if(text== "recommender")
                {
                    //if this is picked, it starts top level dialog where user have a choice between movie recommender and fantasy player search
                    conversationData.ConversationTopic = ConversationModel.topic.DIALOG;
                    await turnContext.SendActivityAsync("You have chosen recommender", null, null, cancellationToken);
                    //runs start dialog
                    await _dialog.RunAsync(turnContext, _conversationState.CreateProperty<DialogState>(nameof(DialogState)), cancellationToken);
                }
                else
                {
                    //if any other input occurs, suggested actions are sent again
                    await StartingConversationAsync(turnContext, cancellationToken);
                }
            }
            else if (conversationData.ConversationTopic == ConversationModel.topic.MEETING)
            {
                //as long as meeting promps don't finish, this is the topic of conversation
                await userInputAsync(conversationData, userProfile, turnContext, cancellationToken);
            }
            else
            {
                //as long as dialogs aren't finished, this is the topic of conversation
                string ret = turnContext.Activity.Text.ToLower();
                if (ret == "exit")
                {
                    conversationData.ConversationTopic = ConversationModel.topic.NONE;
                    await StartingConversationAsync(turnContext, cancellationToken);
                }
                else
                {
                    await _dialog.RunAsync(turnContext, _conversationState.CreateProperty<DialogState>(nameof(DialogState)), cancellationToken);
                }
            }
        }

        public override async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            await base.OnTurnAsync(turnContext, cancellationToken);

            // saving changes after every turn
            await _conversationState.SaveChangesAsync(turnContext, false, cancellationToken);
            await _userState.SaveChangesAsync(turnContext, false, cancellationToken);
        }

    }
}
