using BlogApplication.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Net;

namespace BlogApplication.Controllers
{
    public class BlogController : Controller
    {
       

        [ActionName("Index")]
        public async Task<ActionResult> IndexAsync()
        {
            var items = await DocumentDBRepository<BlogPostModel>.GetItemsAsync(d => d.Title != "");
            return View(items);
        }

        [Route("blog/read/{id}")] 
        public ActionResult Read(int id)
        {

            var blogs = PostManager.Read();
            BlogPostModel post = null;

            if (blogs != null && blogs.Count > 0)
            {
                post = blogs.Find(x => x.ID == id.ToString());
            }

            if (post == null)
            {
                ViewBag.PostFound = false;
                return View();
            }
            else
            {
                ViewBag.PostFound = true;
                return View(post);
            }
        }

        
        public ActionResult Create()
        {
            return View();

        }

        

        [HttpPost]
        public async Task<ActionResult> Create(FormCollection collection)
        {
            

            if (Request.HttpMethod == "POST")
            {

                var title = Request.Form["title"].ToString();
                var tags = Request.Form["tags"].ToString().Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                var content = Request.Form["content"].ToString();


                var post = new BlogPostModel { Title = title, CreateTime = DateTime.Now, Content = content, Tags = tags.ToList() };


                await DocumentDBRepository<BlogPostModel>.CreateItemAsync(post);


                Response.Redirect("~/blog");
            }
            return View();
        }

        

        [ActionName("Edit")]
        public async Task<ActionResult> EditAsync(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            BlogPostModel item = await DocumentDBRepository<BlogPostModel>.GetItemAsync(id);
            if (item == null)
            {

                ViewBag.Found = false;
            }
            else
            {

                ViewBag.Found = true;
                ViewBag.Id = item.ID;
                ViewBag.PostTitle = item.Title;
                ViewBag.Tags = item.Tags;
                ViewBag.Content = item.Content;
            }
                return View();

        }

        [HttpPost]
        [ActionName("Edit")]
        public async Task<ActionResult> EditAsync([Bind(Include = "Id,Name,Description,Completed")] BlogPostModel item)
        {
            if (ModelState.IsValid)
            {

                var id = Request.Form["hdnID"].ToString();
                var title = Request.Form["title"].ToString();
                var tags = Request.Form["tags"].ToString().Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                var content = Request.Form["content"].ToString();
                var post = new BlogPostModel { ID = id, Title = title, CreateTime = DateTime.Now, Content = content, Tags = tags.ToList() };

                await DocumentDBRepository<BlogPostModel>.UpdateItemAsync(post.ID, post);
                return RedirectToAction("Index");
            }

            return View(item);
        }



        public ActionResult Delete(int id)
        {
            return View();
        }


        [HttpPost]
        public ActionResult Delete(int id, FormCollection collection)
        {
            try
            {


                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }
    }
}
