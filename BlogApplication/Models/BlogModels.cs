using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Web;

using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
//using System.Configuration;
//using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Linq.Expressions;
using System.Net;

namespace BlogApplication.Models
{
    public class BlogPostModel
    {
       
        [JsonProperty(PropertyName = "id")]
        public string ID { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public List<string> Tags { get; set; }
        public DateTime CreateTime { get; set; }
        
    }


    public class PostManager
    {

        private static string PostsFile = HttpContext.Current.Server.MapPath("~/App_Data/Posts.json");
        private static List<BlogPostModel> posts = new List<BlogPostModel>();


        public static void Create(string postJson)
        {
            var obj = JsonConvert.DeserializeObject<BlogPostModel>(postJson);

            if (posts.Count > 0)
            {
                posts = (from post in posts
                         orderby post.CreateTime
                         select post).ToList();
                obj.ID = posts.Last().ID + 1;
            }
            else
            {
                obj.ID = "1";
            }

            

        }


        public static List<BlogPostModel> Read()
        {

            if (!File.Exists(PostsFile))
            {
                File.Create(PostsFile).Close();
                File.WriteAllText(PostsFile, "[]"); 
            }
            posts = JsonConvert.DeserializeObject<List<BlogPostModel>>(File.ReadAllText(PostsFile));
            return posts;
        }

        public static void Update(int id, string postJson)
        {
            Delete(id);
            Create(postJson);
            save();
        }

        public static void Delete(int id)
        {
            posts.Remove(posts.Find(x => x.ID == id.ToString()));
            save();
        }

 
        private static void save()
        {
            File.WriteAllText(PostsFile, JsonConvert.SerializeObject(posts));
        }
    }


    public static class DocumentDBRepository<BlogPostModel> 
    {
        private static readonly string DatabaseId = ConfigurationManager.AppSettings["database"];
        private static readonly string CollectionId = ConfigurationManager.AppSettings["collection"];
        private static DocumentClient client;

        public static void Initialize()
        {
            client = new DocumentClient(new Uri(ConfigurationManager.AppSettings["endpoint"]), ConfigurationManager.AppSettings["authKey"]);
            CreateDatabaseIfNotExistsAsync().Wait();
            CreateCollectionIfNotExistsAsync().Wait();
        }

        private static async Task CreateDatabaseIfNotExistsAsync()
        {
            try
            {
                await client.ReadDatabaseAsync(UriFactory.CreateDatabaseUri(DatabaseId));
            }
            catch (DocumentClientException e)
            {
                if (e.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    await client.CreateDatabaseAsync(new Database { Id = DatabaseId });
                }
                else
                {
                    throw;
                }
            }
        }

        private static async Task CreateCollectionIfNotExistsAsync()
        {
            try
            {
                await client.ReadDocumentCollectionAsync(UriFactory.CreateDocumentCollectionUri(DatabaseId, CollectionId));
            }
            catch (DocumentClientException e)
            {
                if (e.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    await client.CreateDocumentCollectionAsync(
                        UriFactory.CreateDatabaseUri(DatabaseId),
                        new DocumentCollection { Id = CollectionId },
                        new RequestOptions { OfferThroughput = 1000 });
                }
                else
                {
                    throw;
                }
            }
        }

        public static async Task<IEnumerable<BlogPostModel>> GetItemsAsync(Expression<Func<BlogPostModel, bool>> predicate)
        {
            IDocumentQuery<BlogPostModel> query = client.CreateDocumentQuery<BlogPostModel>(
                UriFactory.CreateDocumentCollectionUri(DatabaseId, CollectionId))

                .AsDocumentQuery();

            List<BlogPostModel> results = new List<BlogPostModel>();
            while (query.HasMoreResults)
            {
                results.AddRange(await query.ExecuteNextAsync<BlogPostModel>());
            }

            return results;
        }

        public static async Task<Document> CreateItemAsync(BlogPostModel item)
        {
            return await client.CreateDocumentAsync(UriFactory.CreateDocumentCollectionUri(DatabaseId, CollectionId), item);
        }

        public static async Task<Document> UpdateItemAsync(string id, BlogPostModel item)
        {
            return await client.ReplaceDocumentAsync(UriFactory.CreateDocumentUri(DatabaseId, CollectionId, id), item);
        }

        public static async Task<BlogPostModel> GetItemAsync(string id)
        {
            try
            {
                Document document = await client.ReadDocumentAsync(UriFactory.CreateDocumentUri(DatabaseId, CollectionId, id));
                return (BlogPostModel)(dynamic)document;
            }
            catch (DocumentClientException e)
            {
               
                throw;
            }
        }


    }


}