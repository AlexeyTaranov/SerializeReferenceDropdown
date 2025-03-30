using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace SerializeReferenceDropdown.Editor.Utils
{
    public static class LinqExtensions
    {
        public static void ForEach<T>(this IReadOnlyList<T> items, Action<T> action)
        {
            foreach (var item in items)
            {
                action.Invoke(item);
            }
        }

        public static void RefreshListViewData<T>(this ListView listView, IReadOnlyList<T> list)
        {
            listView.itemsSource.Clear();
            foreach (var element in list)
            {
                listView.itemsSource.Add(element);
            }
            listView.RefreshItems();
        }
    }
}