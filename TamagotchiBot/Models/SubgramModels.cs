using System.Collections.Generic;

namespace TamagotchiBot.Models
{
    public class SubgramSponsor
    {
        public string ads_id { get; set; }
        public string link { get; set; }
        public string resource_id { get; set; }
        public string type { get; set; }
        public string status { get; set; }
        public bool available_now { get; set; }
        public string button_text { get; set; }
        public string resource_logo { get; set; }
        public string resource_name { get; set; }
    }

    public class SubgramAdditional
    {
        public List<SubgramSponsor> sponsors { get; set; }
    }

    public class SubgramResponse
    {
        public string status { get; set; }
        public int code { get; set; }
        public string message { get; set; }
        public SubgramAdditional additional { get; set; }
    }
}
