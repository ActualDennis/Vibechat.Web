﻿using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Vibechat.Web.ChatData.Messages;
using Vibechat.Web.Services.Images;
using Vibechat.Web.Services.Paths;
using VibeChat.Web;
using VibeChat.Web.ChatData;

namespace Vibechat.Web.Services.FileSystem
{
    public class ImagesService
    {
        private static string FilesLocationRelative = "Uploads/";

        private static string StaticFilesLocation = "ClientApp/dist/";

        private static int MessageImageMaxWidth = 350;

        private static int MessageImageMaxHeight = 350;

        private static int ThumbnailHeight = 140;

        private static int ThumbnailWidth = 140;

        private static string FullSized = "full";

        private static string Compressed = "compr";


        public ImagesService(
            IImageScalingService imageScaling, 
            UniquePathsProvider pathsProvider,
            IImageCompressionService imageCompression)
        {
            ImageScaling = imageScaling;
            PathsProvider = pathsProvider;
            ImageCompression = imageCompression;
        }

        public IImageScalingService ImageScaling { get; }
        public UniquePathsProvider PathsProvider { get; }
        public IImageCompressionService ImageCompression { get; }

        /// <summary>
        /// Takes stream containing image and returns <see cref="MessageAttachment"/> object.
        /// This method doesn't compress image, it just calculates scaled dimensions.
        /// </summary>
        /// <param name="image"></param>
        /// <param name="imageName"></param>
        /// <returns></returns>
        public MessageAttachment GetImageAsAttachment(MemoryStream image, string imageName)
        {
            ValueTuple<int, int> resultDimensions = ImageScaling.GetScaledDimensions(image, MessageImageMaxWidth, MessageImageMaxHeight);

            image.Seek(0, SeekOrigin.Begin);

            var uniquePath = PathsProvider.GetUniquePath(imageName);

            Directory.CreateDirectory(StaticFilesLocation + FilesLocationRelative + uniquePath);

            using (var fStream = new FileStream(StaticFilesLocation + FilesLocationRelative + uniquePath + imageName, FileMode.Create))
            {
                image.CopyTo(fStream);
            }

            return new MessageAttachment()
            {
                AttachmentKind = "img",
                AttachmentName = imageName,
                ContentUrl = FilesLocationRelative + uniquePath + imageName,
                ImageHeight = resultDimensions.Item2,
                ImageWidth = resultDimensions.Item1
            };
        }

        /// <summary>
        /// Returns new thumbnail url and full-sized image url.
        /// </summary>
        /// <param name="image"></param>
        /// <param name="imageName"></param>
        /// <returns></returns>
        public Tuple<string,string> SaveImage(MemoryStream image, string imageName)
        {
            var resized = ImageCompression.Resize(image, ThumbnailWidth, ThumbnailHeight);
            image.Seek(0, SeekOrigin.Begin);
            var uniquePath = PathsProvider.GetUniquePath(imageName);
            Directory.CreateDirectory(StaticFilesLocation + FilesLocationRelative + PathsProvider.GetUniquePath(imageName));

            var uncompressedFileName = StaticFilesLocation 
                + FilesLocationRelative
                + uniquePath
                + Path.GetFileNameWithoutExtension(imageName)
                + FullSized
                + Path.GetExtension(imageName);

            var compressedFileName = StaticFilesLocation 
                + FilesLocationRelative
                + uniquePath
                + Path.GetFileNameWithoutExtension(imageName)
                + Compressed
                + Path.GetExtension(imageName);

            using (var fStream = new FileStream(uncompressedFileName, FileMode.Create))
            {
                image.CopyTo(fStream);
            }

            resized.Seek(0, SeekOrigin.Begin);

            using (var fStream = new FileStream(compressedFileName, FileMode.Create))
            {
                resized.CopyTo(fStream);
            }

            return new Tuple<string, string>(compressedFileName.Replace(StaticFilesLocation, ""), uncompressedFileName.Replace(StaticFilesLocation, ""));
        }
    }
}
