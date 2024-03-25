using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net.Http;

namespace holiday.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HolidaysController : ControllerBase
    {
        private readonly IHttpClientFactory _clientFactory;

        public HolidaysController(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
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
            var holidays = new List<Holiday>
            {
                new Holiday { Name = "New Year's Day", Date = new DateTime(DateTime.Now.Year, 1, 1) },
                new Holiday { Name = "Eid Al Fitr", Date = new DateTime(DateTime.Now.Year, 4, 9) },
                new Holiday { Name = "Independence Day", Date = new DateTime(DateTime.Now.Year, 5, 25) },
                new Holiday { Name = "Christmas Day", Date = new DateTime(DateTime.Now.Year, 12, 25) }
                // Add more holidays as needed
            };

            // Filter holidays to get only upcoming ones
            var upcomingHolidays = new List<Holiday>();
            foreach (var holiday in holidays)
            {
                if (holiday.Date >= DateTime.Today)
                {
                    upcomingHolidays.Add(holiday);
                }
            }

            return upcomingHolidays;
        }

        [HttpGet("external")]
        public async Task<IActionResult> GetExternalHolidays(string country, int year)
        {
            // Create HttpClient instance
            var client = _clientFactory.CreateClient();

            // Add API key to request headers
            client.DefaultRequestHeaders.Add("X-Api-Key", "ckwzdZRYsSW3acIQxnDYoA==k3iyIgP3TcjA9ZyO");

            // Build the API endpoint URL
            string apiUrl = $"https://api.api-ninjas.com/v1/holidays?country={country}&year={year}";

            try
            {
                // Make GET request to the external API
                HttpResponseMessage response = await client.GetAsync(apiUrl);

                // Check if the request was successful
                if (response.IsSuccessStatusCode)
                {
                    // Read response content
                    string responseContent = await response.Content.ReadAsStringAsync();

                    // Return the response content
                    return Ok(responseContent);
                }
                else
                {
                    // If the request failed, return appropriate status code
                    return StatusCode((int)response.StatusCode, "Failed to retrieve data from external API.");
                }
            }
            catch (HttpRequestException ex)
            {
                // If an error occurred during the HTTP request, return a 500 Internal Server Error
                return StatusCode(500, $"An error occurred while calling the external API: {ex.Message}");
            }
        }
    }

    public class Holiday
    {
        public string Name { get; set; }
        public DateTime Date { get; set; }
    }
}
