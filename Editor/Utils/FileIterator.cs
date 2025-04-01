using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SerializeReferenceDropdown.Editor.Utils
{
    public enum IteratorResult
    {
        NoFoundedAsset,
        FoundedAsset,
        CanceledByUser,
    }

    public class FileIterator<T> where T : UnityEngine.Object
    {
        private readonly Func<string, bool> filePredicate;
        public string ProgressBarLabel { get; set; }
        public string ProgressBarInfoPrefix { get; set; }
        public List<string> SkipFileExtensions = new List<string>();

        public FileIterator(Func<string, bool> filePredicate)
        {
            this.filePredicate = filePredicate;
        }


        public IteratorResult IterateOnUnityAssetFiles()
        {
            var assetPaths = AssetDatabase.FindAssets($"t: {typeof(T).Name}").Select(AssetDatabase.GUIDToAssetPath);
            var filteredPaths = assetPaths.Where(IsCorrectExtensions).ToArray();
            return IterateOnFiles(filteredPaths);

            bool IsCorrectExtensions(string path)
            {
                var extension = Path.GetExtension(path);
                var needSkip = SkipFileExtensions.Contains(extension);
                return needSkip == false;
            }
        }

        private IteratorResult IterateOnFiles(IReadOnlyList<string> filePaths)
        {
            for (var i = 0; i < filePaths.Count; i++)
            {
                var file = filePaths[i];
                var progressFileText = $"{i}/{filePaths.Count} - {file}";
                var info = string.IsNullOrEmpty(ProgressBarInfoPrefix)
                    ? $"{progressFileText}"
                    : $"{ProgressBarInfoPrefix}: {progressFileText}";
                var result = false;
                try
                {
                    result = filePredicate.Invoke(file);
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }

                if (EditorUtility.DisplayCancelableProgressBar(ProgressBarLabel, info,
                        (float)i / (float)filePaths.Count))
                {
                    EditorUtility.ClearProgressBar();
                    return IteratorResult.CanceledByUser;
                }

                if (result)
                {
                    EditorUtility.ClearProgressBar();
                    return IteratorResult.FoundedAsset;
                }
            }

            EditorUtility.ClearProgressBar();
            return IteratorResult.NoFoundedAsset;
        }
    }
}