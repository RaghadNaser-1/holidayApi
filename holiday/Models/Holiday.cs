namespace holiday.Models
{
    public class Holiday
    {
        public string Name { get; set; }
        public DateTime startDate { get; set; }
        public DateTime endDate { get; set; }
        //public string HijriDate { get; set; }

        public string startDay { get; set; }
        public string endDay { get; set; }= string.Empty;
        public int dayCount { get; set; }
        public int  remainingDays { get; set; }


    }
}
