using Newtonsoft.Json.Linq;
using Sitecore.Configuration;
using Sitecore.Data.Items;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;


namespace SitecoreMediaTagging.Common
{
    public static class TagImage
    {
        public static string altTag { get; set; }
        public static Sitecore.Text.ListString referencesValue { get; set; }
        public static bool GetImageTagsFromAzureAPI(MediaItem mediaItem, Item mainItem)
        {
            //string altTag = string.Empty;
            string[] tags = new string[50];

            HttpClient client = new HttpClient();

            string uriBase = Sitecore.Configuration.Settings.GetSetting("uriBase");//ConfigurationManager.AppSettings["uriBase"].ToString();
            string subKey = Sitecore.Configuration.Settings.GetSetting("subscriptionKey");// ConfigurationManager.AppSettings["subscriptionKey"].ToString();

            // Request headers.
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subKey);

            // Request parameters. A third optional parameter is "details".
            string requestParameters = "visualFeatures=Categories,Description,Color&language=en";

            // Assemble the URI for the REST API Call.
            string uri = uriBase + "?" + requestParameters;

            HttpResponseMessage response;
           // var myTask = Task.Run(() => GetImageAsByteArray(mediaItem));
            // Request body. Posts a locally stored JPEG image.
            byte[] byteData =  GetImageAsByteArray(mediaItem);
           // byte[] byteData = await myTask;

            using (ByteArrayContent content = new ByteArrayContent(byteData))
            {
                // This example uses content type "application/octet-stream".
                // The other content types you can use are "application/json" and "multipart/form-data".
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                // Execute the REST API call.
                response = client.PostAsync(uri, content).Result;

                // Get the JSON response - Made it sync
                string contentString = response.Content.ReadAsStringAsync().Result;

                dynamic jsonObject = JObject.Parse(contentString);

                if (jsonObject != null)
                {
                    try
                    {
                        altTag = jsonObject.description["captions"] != null ? jsonObject.description["captions"][0]["text"] : string.Empty;
                        JArray tagArray = jsonObject.description["tags"];
                        CreateTaggingItem(tagArray, mainItem);

                    }
                    catch (Exception ex)
                    {
                        Sitecore.Diagnostics.Log.Error("Something went wrong during parsing JSON", ex, new object());
                        return false;
                    }

                }
            }
            return true;
        }

        public static bool IsValidImageRequest(string mimeType)
        {
            return ValidMimeTypes.Any(v => v.Equals(mimeType, StringComparison.InvariantCultureIgnoreCase));
        }
        public static IEnumerable<string> ValidMimeTypes
        {
            get
            {
                // Credits : https://laubplusco.net/make-sitecore-deliver-images-fits-screen-part-2/
                var validMimetypes = Settings.GetSetting("ValidMimeTypes",
                    "image/jpeg|image/pjpeg|image/png|image/gif|image/tiff|image/bmp");
                return validMimetypes.Split(new[] { ",", "|", ";" }, StringSplitOptions.RemoveEmptyEntries);
            }
        }
        private static byte[] GetImageAsByteArray(MediaItem mediaItem)
        {
            // https://stackoverflow.com/questions/26094326/convert-images-to-png-after-reading-media-item-from-sitecore
            if (mediaItem != null)
            {
                Stream stream = mediaItem.GetMediaStream();
                stream.Seek(0, SeekOrigin.Begin);
                Byte[] bytes = new Byte[stream.Length];
                stream.Read(bytes, 0, bytes.Length);
                return bytes;
            }
            return null;
        }

        public static void CreateTaggingItem(JArray tags, Item mainItem)
        {
            using (new Sitecore.SecurityModel.SecurityDisabler())
            {
                //string tagTemplatepath = Sitecore.Configuration.Settings.GetSetting("tagTemplate");
                string parentItempath = Sitecore.Configuration.Settings.GetSetting("parentItem");
                string tagTemplate = Sitecore.Configuration.Settings.GetSetting("tagTemplate");
                // Get the master database
                Sitecore.Data.Database master = Sitecore.Data.Database.GetDatabase("master");
                // Get the template for which you need to create item
                Sitecore.Data.Items.TemplateItem template = master.GetItem(tagTemplate);

                // Get the place in the site tree where the new item must be inserted
                Item parentItem = master.GetItem(parentItempath);
                //"{68BA23FD-8270-4675-97EA-4FAFC7CF3AB9} Tag Template
                var childItems = parentItem.Axes.GetDescendants().Where(x => x.TemplateID.ToString().Trim() == tagTemplate.ToString());
                referencesValue = new Sitecore.Text.ListString();
                Parallel.ForEach(tags, tag =>
                {
                    // Set the new item in editing mode
                    // Fields can only be updated when in editing mode
                    // (It's like the begin transaction on a database)
                    var tagitems = childItems.Where(x => x.Name.ToLower().Equals(tag.ToString().ToLower()));
                    if (tagitems.Count() == 0)
                    {
                        // Add the item to the site tree
                        Item newItem = parentItem.Add(tag.ToString(), template);
                        try
                        {
                            // Assign values to the fields of the new item
                            referencesValue.Add(newItem.ID.ToString());
                        }
                        catch (System.Exception ex)
                        {
                            // Log the message on any failure to sitecore log
                            Sitecore.Diagnostics.Log.Error("Could not update item " + newItem.Paths.FullPath + ": " + ex.Message, "CreateTaggingItem");
                        }
                    }
                    else
                    {
                        referencesValue.Add(tagitems.First().ID.ToString());
                    }
                });
                //Update tags in Sitecore media item
                UpdateTagInMediaItem(mainItem);
            }
        }

        public static void UpdateTagInMediaItem(Item mainItem)
        {
            mainItem.Editing.BeginEdit();
            mainItem.Fields["__Semantics"].Value = referencesValue.ToString();
            mainItem.Fields["Alt"].Value = altTag;
            mainItem.Editing.EndEdit();
        }
    }
}