namespace BlueQ.Bot.Models
{
    using System;
    using System.Collections.Generic;

    [Serializable]
    public class ParticipantPreferences
    {
        public string ParticipantEmail { get; set; }

        public string ParticipantPhoneNumber { get; set; }

        public Dictionary<string, string> ParticipantLocations { get; set; }
    }
}