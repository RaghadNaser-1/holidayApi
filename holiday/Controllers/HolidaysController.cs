using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net.Http;
using holiday.Models;
using Newtonsoft.Json;
using System.Globalization;
using Microsoft.Extensions.Logging; // Add this namespace

namespace holiday.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HolidaysController : ControllerBase
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly HttpClient _httpClient;
        private readonly ILogger<HolidaysController> _logger; // Add ILogger dependency

        public HolidaysController(IHttpClientFactory clientFactory, ILogger<HolidaysController> logger)
        {
            _clientFactory = clientFactory;
            _httpClient = clientFactory.CreateClient();
            _logger = logger; // Initialize ILogger

        }

        [HttpGet("{country}/{year}")]
        public async Task<ActionResult<IEnumerable<Holiday>>> GetHoliday(string country, int year)
        {
            try
            {
                // Get external holidays for the given country and year
                var externalHolidaysResponse = await GetExternalHolidays(country, year);

                // Check if the request was successful
                if (externalHolidaysResponse.Result is OkObjectResult okResult)
                {
                    // Deserialize the response content to get the list of external holidays
                    var externalHolidays = okResult.Value as IEnumerable<ExternalHoliday>;

                    if (externalHolidays != null && externalHolidays.Any())
                    {
                        var holidays = new List<Holiday>();

                        // Group external holidays by name and count occurrences
                        var holidayNameCounts = externalHolidays.GroupBy(h => h.Name)
                                                                .ToDictionary(g => g.Key, g => g.Count());

                        foreach (var externalHoliday in externalHolidays)
                        {
                            // Check if a holiday with the same name already exists in the list
                            if (!holidays.Any(h => h.Name == externalHoliday.Name))
                            {
                                // Get the count of the current holiday name
                                var dayCount = holidayNameCounts.ContainsKey(externalHoliday.Name) ? holidayNameCounts[externalHoliday.Name] : 0;
                                var startDate = Convert.ToDateTime(externalHoliday.Date);
                                var endDate = startDate.AddDays(dayCount);
                                var hijriDate = await ConvertToHijri(externalHoliday.Date);
                                startDate = ConvertHijriToDateTime(hijriDate);
                                var hijriEndDate = await ConvertToHijri(endDate.ToString("yyyy-MM-dd"));
                                var endDateInHijri = ConvertHijriToDateTime(hijriEndDate);

                                var holiday = new Holiday
                                {
                                    Name = externalHoliday.Name,
                                    startDate = startDate,
                                    startDay = externalHoliday.Day,
                                    dayCount = dayCount, // Set dayCount to the count of occurrences
                                    endDate = endDateInHijri,
                                    // Adjust this as per your requirement
                                    endDay = endDate.DayOfWeek.ToString(),
                                    remainingDays = CalculateRemainingDays(Convert.ToDateTime(externalHoliday.Date))
                                };

                                holidays.Add(holiday);
                            }
                        }

                        return holidays;
                    }
                    else
                    {
                        return NotFound("No holidays found for the given country and year.");
                    }
                }
                else
                {
                    return StatusCode(500, "Failed to retrieve external holidays data.");
                }
            }
            catch (Exception ex)
            {
                // Log the exception
                _logger.LogError($"An error occurred: {ex.Message}");
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }




        //private async Task<DateTime> ConvertToHijriAndCalculateRemainingDaysAsync(string gregorianDate)
        //{
        //    // Convert Gregorian date to Hijri date using Aladhan's API
        //    var hijriDate = await ConvertToHijri(gregorianDate);

        //    // Calculate remaining days until the holiday
        //    var remainingDays = CalculateRemainingDays(DateTime.Parse(gregorianDate));

        //    // Return the Hijri date with remaining days
        //    return hijriDate.AddDays(remainingDays);
        //}


        private int CalculateRemainingDays(DateTime holidayDate)
        {
            var currentDate = DateTime.Now;
            // Ensure the holiday date is in the "dd-MM-yyyy" format
            var holidayDateString = holidayDate.ToString("dd-MM-yyyy");
            // Parse the holiday date string back to DateTime using the same format
            var parsedHolidayDate = DateTime.ParseExact(holidayDateString, "dd-MM-yyyy", CultureInfo.InvariantCulture);

            // Calculate remaining days until the holiday
            var remainingDays = (parsedHolidayDate.Date - currentDate.Date).Days;
            return Math.Max(0, remainingDays); // Use the absolute value of the difference
        }


        private DateTime ConvertHijriToDateTime(string hijriDate)
        {
            return DateTime.ParseExact(hijriDate, "dd-MM-yyyy", CultureInfo.InvariantCulture);
        }

        private async Task<string> ConvertToHijri(string gregorianDate)
        {
            try
            {
                // Parse the Gregorian date and format it as "dd-MM-yyyy"
                DateTime parsedDate = DateTime.ParseExact(gregorianDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                string formattedDate = parsedDate.ToString("dd-MM-yyyy");

                // Make HTTP request to Aladhan's API for Hijri date conversion
                var response = await _httpClient.GetAsync($"http://api.aladhan.com/v1/gToH/{formattedDate}");
                response.EnsureSuccessStatusCode();
                var responseBody = await response.Content.ReadAsStringAsync();

                // Parse response to get Hijri date
                dynamic responseData = JsonConvert.DeserializeObject(responseBody);
                string hijriDate = responseData.data.hijri.date;

                return hijriDate;
            }
            catch (Exception ex)
            {
                // Handle errors
                Console.WriteLine($"Error converting to Hijri date: {ex.Message}");
                throw;
            }
        }


        // GET: api/holidays
        [HttpGet("local")]
        public IEnumerable<Holiday> Get()
        {
            // Get upcoming holidays data
            var holidays = GetUpcomingHolidays();
            return holidays;
        }

        private IEnumerable<Holiday> GetUpcomingHolidays()
        {
            // Hardcoded list of holidays (replace this with your data source)
            //Range date - range day - (holiday long) - days remind to it

            var holidays = new List<Holiday>
            {
                new Holiday { Name = "New Year's Day", startDate = new DateTime(DateTime.Now.Year, 1, 1) },
                new Holiday { Name = "Eid Al Fitr", startDate = new DateTime(DateTime.Now.Year, 4, 10) , endDate = new DateTime(DateTime.Now.Year, 4, 13) , dayCount =3, startDay= "Wednesday" },
                new Holiday { Name = "Independence Day", startDate = new DateTime(DateTime.Now.Year, 5, 25) },
                new Holiday { Name = "Christmas Day", startDate = new DateTime(DateTime.Now.Year, 12, 25) }
                // Add more holidays as needed
            };

            // Filter holidays to get only upcoming ones
            var upcomingHolidays = new List<Holiday>();
            foreach (var holiday in holidays)
            {
                if (holiday.startDate >= DateTime.Today)
                {
                    upcomingHolidays.Add(holiday);
                }
            }

            return upcomingHolidays;
        }


        // GET: api/holidays/external
        [HttpGet("external")]
        public async Task<ActionResult<IEnumerable<ExternalHoliday>>> GetExternalHolidays(string country, int year)
        {
            var client = _clientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Api-Key", "ckwzdZRYsSW3acIQxnDYoA==k3iyIgP3TcjA9ZyO");

            string apiUrl = $"https://api.api-ninjas.com/v1/holidays?country={country}&year={year}";

            try
            {
                HttpResponseMessage response = await client.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    string responseContent = await response.Content.ReadAsStringAsync();
                    var holidays = JsonConvert.DeserializeObject<IEnumerable<ExternalHoliday>>(responseContent);
                    return Ok(holidays);
                }
                else
                {
                    return StatusCode((int)response.StatusCode, "Failed to retrieve data from external API.");
                }
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(500, $"An error occurred while calling the external API: {ex.Message}");
            }
        }
    }

    
}
