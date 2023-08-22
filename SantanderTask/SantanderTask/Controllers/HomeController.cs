using Microsoft.AspNetCore.Mvc;
using SantanderTask.Constants;
using SantanderTask.Models;
using System.Diagnostics;
using Newtonsoft.Json;

namespace SantanderTask.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public IEnumerable<StoryModel?> Stories { get; set; }
        public IEnumerable<int> StoryIds { get; set; }

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;

            StoryIds = GetStoryIds();
            FetchStoriesAsync(StoryIds).Wait();
        }

        private async Task FetchStoriesAsync(IEnumerable<int> storyIds)
        {
            Stories = await GetStoriesAsync(storyIds);
        }

        [HttpGet]
        public async Task<IEnumerable<StoryModel?>> GetStoriesAsync(IEnumerable<int> storyIds)
        {
            var stories = new List<StoryModel>();
            var tasks = new List<Task<StoryModel>>();

            foreach (var storyId in storyIds)
            {
                var task = GetStoryWithIdAsync(storyId);
                if(task != null)
                {
                    tasks.Add(task);
                }
                else
                {
                    _logger.LogError($"Was not able to fetch data for storyId: {storyId}, that one will be skipped.");
                }
            }

            await Task.WhenAll(tasks);
            return tasks.Select(t => t.Result);
        }

        [HttpGet]
        public IEnumerable<int> GetStoryIds()
        {
            var storyIds = new List<int>();

            var url = CommonConsts.BestStoryIdListUrl;
            using (HttpClient httpClient = new HttpClient())
            {
                try
                {
                    HttpResponseMessage response = httpClient.GetAsync(url).Result;
                    if (response.IsSuccessStatusCode)
                    {
                        string jsonContent = response.Content.ReadAsStringAsync().Result;
                        return System.Text.Json.JsonSerializer.Deserialize<List<int>>(jsonContent);
                    }
                } 
                catch (Exception ex)
                {
                    _logger.LogError($"Exception occured on getting the list of story ids.", ex.Message);

                }
            }

            return storyIds;
        }

        public async Task<StoryModel> GetStoryWithIdAsync(int storyId)
        {
            var url = string.Format("https://hacker-news.firebaseio.com/v0/item/{0}.json", storyId);

            using (var httpClient = new HttpClient())
            {
                var response = httpClient.GetAsync(url).Result;
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<StoryModel>();
                }
            }

            return null;
        }

        [HttpGet("{n}")]
        public string GetBestStories(int n)
        {
            var stories = Stories.ToList();
            var bestStories = stories.OrderByDescending(x => x.Score).Take(n);

            /*
            // A second approach can be used here not to deserialize the json
            JArray jsonArray = JArray.Parse(jsonString);
            // Sort the array by the "Score" property
            JArray sortedArray = new JArray(jsonArray.OrderBy(item => item["Score"]));
            */

            return BeautifyBestStories(bestStories);
        }

        /// <summary>
        /// A function to display the best stories in beautiful format
        /// </summary>
        /// <param name="selectedStories"></param>
        /// <returns></returns>
        private string BeautifyBestStories(IEnumerable<StoryModel?> selectedStories)
        {
            // Here we can use another approach to put endlines after each object and each comma
            // but as far as Newtonsoft has such feature I decided to use the existing one.
            return JsonConvert.SerializeObject(selectedStories, Newtonsoft.Json.Formatting.Indented);
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}