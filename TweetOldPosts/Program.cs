using Microsoft.Extensions.Configuration;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Threading.Tasks;
using System.Xml;
using Tweetinvi;

namespace TweetOldPosts
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var config = new ConfigurationBuilder().AddEnvironmentVariables().Build();
            using var reader = XmlReader.Create(config["FeedUrl"]);
            var feed = SyndicationFeed.Load(reader);

            var cutOffDate = new DateTime(2017, 01, 01);

            var randomPost = feed.Items.Where(p => p.PublishDate > cutOffDate &&
                                             !p.Categories.Any(cat => cat.Name.ToLower().Contains("books")) &&
                                             !p.Categories.Any(cat => cat.Name.ToLower().Contains("book reviews")) &&
                                             !p.Categories.Any(cat => cat.Name.ToLower().Contains("news")) &&
                                             !p.Categories.Any(cat => cat.Name.ToLower().Contains("arizona technology news")) &&
                                             !p.Categories.Any(cat => cat.Name.ToLower().Contains("technology news")))
                            .OrderBy(p => Guid.NewGuid())
                            .FirstOrDefault();

            if (randomPost == null)
            {
                Console.WriteLine("Could not get a post. Exiting");
                return;
            }

            var hashtags = HashTagList(randomPost.Categories);

            Console.WriteLine($"Good choice! Picking {randomPost.Title.Text}");

            var status =
                $"ICYMI: ({randomPost.PublishDate.Date.ToShortDateString()}): \"{randomPost.Title.Text}.\" RTs and feedback are always appreciated! {randomPost.Links[0].Uri} {hashtags}";

            var consumerKey = config["ConsumerKey"];
            var consumerSecret = config["ConsumerSecret"];
            var accessToken = config["AccessToken"];
            var accessTokenSecret = config["AccessSecret"];

            try
            {
                var client = new TwitterClient(consumerKey, consumerSecret, accessToken, accessTokenSecret);
                var result = await client.Tweets.PublishTweetAsync(status);
                Console.WriteLine($"Successfully sent tweet at: {result.Url}");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message, ex);
            }
        }

        private static string HashTagList(Collection<SyndicationCategory> categories)
        {
            if (categories is null || categories.Count == 0)
            {
                return "#dotnet #csharp #dotnetcore";
            }

            var hashTagCategories = categories.Where(c => !c.Name.Contains("Articles"));

            return hashTagCategories.Aggregate("",
                (current, category) => current + $" #{category.Name.Replace(" ", "").Replace(".", "")}");

        }
    }
}