using System;
namespace TelegramBot.Model
{
    public class JobSearch
    {
        public List<JobsResult> jobs_results { get; set; }

    }


    public class JobsResult
    {
        public string title { get; set; }
        public string company_name { get; set; }
        public string location { get; set; }
        public string via { get; set; }
        public string description { get; set; }

    }

}

