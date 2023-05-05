using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Pipelines.Upload;
using SitecoreMediaTagging.Common;

namespace SitecoreMediaTagging.Pipeline
{
    public class CheckMediaUpload
    {
        public void Process(UploadArgs args)
        {

            Assert.ArgumentNotNull(args, "args");
            if (args.UploadedItems != null && args.UploadedItems.Count > 0)
            {
                foreach (var uploadedItem in args.UploadedItems)
                {
                    if (uploadedItem != null)
                    {
                        MediaItem mediaItem = new MediaItem(uploadedItem);

                        if (mediaItem != null && TagImage.IsValidImageRequest(mediaItem.MimeType))
                        {
                            TagImage.GetImageTagsFromAzureAPI(mediaItem.InnerItem, uploadedItem);

                        }
                    }
                }
            }
        }
    }
}