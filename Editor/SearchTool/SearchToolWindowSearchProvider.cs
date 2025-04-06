using System;
using System.Collections.Generic;
using System.Linq;
using SerializeReferenceDropdown.Editor.Utils;
using UnityEditor;
using UnityEditor.Search;
using UnityEngine;

namespace SerializeReferenceDropdown.Editor.SearchTool
{
    public class SearchToolWindowSearchProvider
    {
        static readonly string providerId = "typeselect";

        private static IReadOnlyList<Type> targetTypes;
        private static Action<Type> selectType;

        private static (ISearchView view, SearchProvider provider, SearchItem item) last;

        
        public static void Show(Action<Type> onSelectedType)
        {
            if (last.view == null)
            {
                var provider = CreateProvider();
                last.provider = provider;
                last.view = SearchService.ShowContextual(provider.id);
            }
            else
            {
                last.view.Focus();
            }

            selectType = onSelectedType;
        }

        [SearchItemProvider]
        internal static SearchProvider CreateProvider()
        {
            var icon = EditorGUIUtility.IconContent("cs Script Icon").image;
            if (targetTypes == null)
            {
                var typesList = new List<Type>();
                var allTypes = TypeUtils.GetAllTypesInCurrentDomain();
                var interfaces = allTypes.Where(t => (t.IsInterface || t.IsAbstract) && t.IsGenericType == false);
                var nonUnityObjectTypes =
                    allTypes.Where(t =>
                        t.IsClass && t.IsSubclassOf(typeof(UnityEngine.Object)) == false);
                typesList.AddRange(interfaces);
                typesList.AddRange(nonUnityObjectTypes);
                targetTypes = typesList;
            }

            return new SearchProvider(providerId, "Search Target Type")
            {
                isEnabledForContextualSearch = () => false,
                fetchItems = FetchItems,
                fetchLabel = (item, context) => item.label,
                fetchDescription = (item, context) => item.description,
                trackSelection = (item, context) => last.item = item,
                onDisable = Disable,
                isExplicitProvider = true,
            };

            IEnumerable<SearchItem> FetchItems(SearchContext context, List<SearchItem> items,
                SearchProvider provider)
            {
                var query = context.searchQuery.ToLowerInvariant();
                var newItems = targetTypes.Where(t => t.FullName.ToLowerInvariant().Contains(query))
                    .Select(CreateSearchItem);
                foreach (var item in newItems)
                {
                    yield return item;
                }

                SearchItem CreateSearchItem(Type t)
                {
                    var item = new SearchItem(t.FullName)
                    {
                        label = t.FullName,
                        thumbnail = (Texture2D)icon,
                        context = context,
                        provider = last.provider,
                        options = SearchItemOptions.Compacted,
                        data = t
                    };
                    return item;
                }
            }
        }

        private static void Disable()
        {
            if (last.item?.data is Type type)
            {
                selectType.Invoke(type);
            }

            last.view = null;
            last.item = null;
            selectType = null;
        }
    }
}