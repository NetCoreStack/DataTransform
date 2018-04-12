using DataTransform.SharedLibrary;
using System.Collections.Generic;
using System.IO;

namespace DataTransform.Api.Hosting
{
    public static class PathUtility
    {
        public static List<JsTreeDataModel> WalkDirectoryTree(DirectoryInfo directory, JsTreeDataModel tree)
        {
            var treeList = new List<JsTreeDataModel>();
            FileInfo[] files = directory.GetFiles("*.*");

            if (files != null)
            {
                foreach (FileInfo fi in files)
                {
                    var extension = fi.Extension.Replace(".", "-");
                    tree.Children.Add(new JsTreeDataModel
                    {
                        Text = fi.Name,
                        Id = fi.Name,
                        Icon = "file file" + extension,
                        Type = "file"
                    });
                }
            }

            return treeList;
        }
    }
}