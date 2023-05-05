using Sitecore;
using Sitecore.Data.Items;
using Sitecore.Shell.Framework.Commands;
using SitecoreMediaTagging.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace SitecoreMediaTagging.Pipeline
{
    public class AddCustomCommandforImageTagging : Sitecore.Shell.Framework.Commands.Command
    {
        public override void Execute(CommandContext context)
        {
            var contextItem = context.Items.FirstOrDefault();
            List<Item> children = new List<Item>();
            if (contextItem == null)
                return;
            var includeChildren = context.Parameters["tagchildren"];
            if (includeChildren == "true")
            {
                children = contextItem.Axes.GetDescendants().ToList();
                children.Add(contextItem);
            }
            else
            {
                children.Add(contextItem);
            }
            TagMediaImage(children);
        }

        public void TagMediaImage(List<Item> children)
        {
            foreach (var contextItem in children)
            {
                MediaItem mediaItem = new MediaItem(contextItem);

                if (mediaItem != null && TagImage.IsValidImageRequest(mediaItem.MimeType))
                {
                    TagImage.GetImageTagsFromAzureAPI(mediaItem.InnerItem, contextItem);
                }
            }
            Context.ClientPage.ClientResponse.Alert("Image tagging process has completed successfully");
        }
    }
}