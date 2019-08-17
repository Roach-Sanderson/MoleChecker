
namespace MoleChecker
{
    using HtmlAgilityPack;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    class Program
    {
        private const string baseUrl = "https://www.dofus.com";
        private const string defaultUrl = "https://www.dofus.com/fr/mmorpg/communaute/annuaires/pages-alliances/236200208-time-goes/guildes"; 

        static void Main(string[] args)
        {
            if (!Uri.TryCreate(defaultUrl, UriKind.RelativeOrAbsolute, out Uri defaultUri))
            {
                Console.WriteLine("Error : Invalid defaultUrl");
                return;
            }

            StringBuilder sb = new StringBuilder();

            HtmlDocument mainDocument = GetHtml(defaultUrl);

            var collection = mainDocument.DocumentNode.SelectNodes("//span[@class='ak-guild-name']");
            var allGuildWebsites = new string[collection.Count];

            for (int i = 0; i < collection.Count; i++)
            {
                var guild = collection[i];
                allGuildWebsites[i] = string.Concat(baseUrl, guild.ChildNodes.First().Attributes.First().Value);
            }

            Console.WriteLine("Main document retrieved.");
            foreach (var guildWebsite in allGuildWebsites)
            {
                var guildName = string.Concat(guildWebsite.Split('/').Last().SkipWhile(c => c != '-').Skip(1));
                var guildNameUFL = string.Concat(guildName.First().ToString().ToUpper(), guildName.Substring(1));
                sb.AppendLine(guildNameUFL + ":");
                Console.WriteLine("Retrieving actions from guild " + guildNameUFL + ".");
                if (!Uri.IsWellFormedUriString(guildWebsite, UriKind.RelativeOrAbsolute))
                {
                    Console.WriteLine("Error : Failed to create guild URL for guild " + guildName + ".");
                    continue;
                }

                HtmlDocument guildDocument = GetHtml(guildWebsite);
                var lastPage = GetActionPage(guildDocument);
                for (int i = 1; i <= lastPage; i++)
                {
                    guildDocument = GetHtml(guildWebsite + "?actions-page=" + i);
                    var actionsList = guildDocument.DocumentNode.SelectNodes("//div[@class='ak-actions-list']").Single().ChildNodes.Where(c => c.Name.Equals("div")).Single().ChildNodes;
                    foreach (var action in actionsList)
                    {
                        var actionText = action.InnerText.Trim();
                        if (actionText.EndsWith("la guilde."))
                        {
                            sb.AppendLine(actionText);
                        }
                    }
                }

                sb.AppendLine("\n");
                Console.WriteLine("Done.");
            }

            var tempPath = Path.GetTempPath();
            var fileName = "/moleCheckerReport.txt";
            File.WriteAllText(tempPath + fileName, sb.ToString());
            Console.WriteLine("Success! Report can be found at location : " + tempPath + fileName.Substring(1));
            Console.WriteLine("Press any key to close the console.");
            Console.ReadKey();
        }

        private static int GetActionPage(HtmlDocument guildDocument)
        {
            var actionsCollection = guildDocument.DocumentNode.SelectNodes("//div[@class='ak-actions-pages']");
            var actionList = actionsCollection.Single();
            var validChild = actionList.ChildNodes.Where(c => c.Name.Equals("div")).Last().ChildNodes.Where(c => c.Name.Equals("nav")).Single().ChildNodes
                .Where(c => c.Name.Equals("ul")).Single().ChildNodes.Where(c => c.Name.Equals("li")).Last().ChildNodes.Where(c => c.Name.Equals("a")).Single()
                .Attributes.Single();
            return Convert.ToInt32(validChild.Value.Trim().Split('=').Last());
        }

        private static HtmlDocument GetHtml(string defaultUrl)
        {
            var request = WebRequest.CreateHttp(defaultUrl);
            var response = request.GetResponse();

            HtmlDocument mainDocument = new HtmlDocument();

            using (var responseStream = response.GetResponseStream())
            using (var reader = new StreamReader(responseStream))
            {
                mainDocument.LoadHtml(reader.ReadToEnd());
            }

            return mainDocument;
        }
    }
}
