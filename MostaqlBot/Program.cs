using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Telegram.Bot;
namespace MostaqlBot
{
    public class Program
    {
        private static readonly string TelegramBotToken = "7562876384:AAE6-YMU_IqR_ZjL4aSId67M_PYQckdNokA";
        private static readonly string TelegramChatId = "183408080";

        private static HashSet<string> previouslySeenLinks = new HashSet<string>();
        private static readonly HttpClient httpClient = new HttpClient();
        private static readonly TelegramBotClient botClient = new TelegramBotClient(TelegramBotToken);

        // Caching mechanism
        private static DateTime lastCacheClearTime = DateTime.UtcNow;

        static async Task Main(string[] args)
        {
            var keywords = new List<string> { "ASP.NET", "C#", "Angular", "Entity Framework", "Web Development", "مطور برمجيات", "مطور ويب", "net", "ef", "موقع الكتروني", "web form","تطوير" };

            while (true)
            {
                if ((DateTime.UtcNow - lastCacheClearTime).TotalHours >= 24)
                {
                    previouslySeenLinks.Clear();
                    lastCacheClearTime = DateTime.UtcNow;
                }

                var html = await httpClient.GetStringAsync("https://mostaql.com/projects?category=development&budget_max=10000&sort=latest");
                var projects = ExtractProjects(html, keywords);

                foreach (var project in projects)
                {
                    if (!previouslySeenLinks.Contains(project.Link))
                    {
                        await botClient.SendTextMessageAsync(TelegramChatId, project.Message);
                        previouslySeenLinks.Add(project.Link);
                    }
                }

                await Task.Delay(TimeSpan.FromMinutes(1));
            }
        }

        static IEnumerable<Project> ExtractProjects(string html, List<string> keywords)
        {
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(html);
            var projectNodes = htmlDoc.DocumentNode.SelectNodes("//tr[contains(@class, 'project-row')]");
            var projects = new List<Project>();

            foreach (var node in projectNodes)
            {
                var titleNode = node.SelectSingleNode(".//h2/a");
                var projectTitle = titleNode?.InnerText.Trim();
                var projectLink = titleNode?.GetAttributeValue("href", string.Empty);
                var descriptionNode = node.SelectSingleNode(".//p[contains(@class, 'project__brief')]/a");
                var projectDescription = descriptionNode?.InnerText.Trim();

                if (ContainsKeyword(projectTitle, keywords) || ContainsKeyword(projectDescription, keywords))
                {
                    var message = $"New Project Found!\nTitle: {projectTitle}\nDescription: {projectDescription}\nLink: {projectLink}";
                    projects.Add(new Project { Title = projectTitle, Description = projectDescription, Link = projectLink, Message = message });
                }
            }

            return projects;
        }

        static bool ContainsKeyword(string text, List<string> keywords)
        {
            if (string.IsNullOrEmpty(text)) return false;

            var regexPattern = string.Join("|", keywords);
            return Regex.IsMatch(text, regexPattern, RegexOptions.IgnoreCase);
        }

        class Project
        {
            public string Title { get; set; }
            public string Description { get; set; }
            public string Link { get; set; }
            public string Message { get; set; }
        }

    }
}
