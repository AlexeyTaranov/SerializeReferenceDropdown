using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace SerializeReferenceDropdown.Editor.Utils
{
    public static class LinqExtensions
    {
        public static void ForEach<T>(this IEnumerable<T> items, Action<T> action)
        {
            foreach (var item in items)
            {
                action.Invoke(item);
            }
        }

        public static void RefreshListViewData<T>(this ListView listView, IEnumerable<T> list)
        {
            listView.ClearSelection();
            listView.itemsSource.Clear();
            foreach (var element in list)
            {
                listView.itemsSource.Add(element);
            }
            listView.RefreshItems();
        }
    }
}