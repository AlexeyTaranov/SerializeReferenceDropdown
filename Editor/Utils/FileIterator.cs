using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SerializeReferenceDropdown.Editor.Utils
{
    public class FileIterator
    {
        private readonly Func<string, bool> filePredicate;

        public string ProgressBarLabel { get; set; }
        public string ProgressBarInfoPrefix { get; set; }
        public string FileNameExtension { get; set; }

        public FileIterator(Func<string, bool> filePredicate)
        {
            this.filePredicate = filePredicate;
        }


        public bool IterateOnUnityProjectFiles()
        {
            var files = Directory.GetFiles(Application.dataPath, $"*.{FileNameExtension}", SearchOption.AllDirectories);
            for (var i = 0; i < files.Length; i++)
            {
                var file = files[i];
                var info = $"{ProgressBarInfoPrefix}: ({i}/{files.Length} - {file})";
                var result = filePredicate.Invoke(file);
                if (EditorUtility.DisplayCancelableProgressBar(ProgressBarLabel, info, (float)i / (float)files.Length) || result)
                {
                    EditorUtility.ClearProgressBar();
                    return result;
                }
            }

            EditorUtility.ClearProgressBar();
            return false;
        }
    }
}