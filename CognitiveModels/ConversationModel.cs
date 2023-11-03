namespace DiplomskiRad.CognitiveModels
{
    public class ConversationModel
    {
        public enum questions { NAME, SURNAME, AGE, DATE, EMPLOYED, MARRIED, NONE, END };
        
        public enum topic { NONE,MEETING,DIALOG};
        public bool AskedForName { get; set; } = false;
        public string TimeStamp { get; set; }
        public string channelID { get; set; }

        public topic ConversationTopic { get; set; } = topic.NONE;
        public questions LastQuestion { get; set; } = questions.NONE;
    }
}
