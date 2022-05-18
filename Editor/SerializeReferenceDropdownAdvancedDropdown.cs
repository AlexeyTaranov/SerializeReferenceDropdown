using System;
using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;

namespace SerializeReferenceDropdown.Editor
{
    public class SerializeReferenceDropdownAdvancedDropdown : AdvancedDropdown
    {
        private readonly Type[] fieldTypes;
        private readonly Action<int> onSelectedTypeIndex;

        private readonly Dictionary<AdvancedDropdownItem, int> itemAndIndexes =
            new Dictionary<AdvancedDropdownItem, int>();

        public SerializeReferenceDropdownAdvancedDropdown(AdvancedDropdownState state, Type[] fieldTypes, Action<int> onSelectedNewType) :
            base(state)
        {
            this.fieldTypes = fieldTypes;
            onSelectedTypeIndex = onSelectedNewType;
        }

        protected override AdvancedDropdownItem BuildRoot()
        {
            var root = new AdvancedDropdownItem("Types");
            itemAndIndexes.Clear();
            for (int i = 0; i < fieldTypes.Length; i++)
            {
                var typeName = fieldTypes[i]?.Name ?? "null";
                var item = new AdvancedDropdownItem(typeName);
                itemAndIndexes.Add(item, i);
                root.AddChild(item);
            }

            return root;
        }

        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            base.ItemSelected(item);
            if (itemAndIndexes.TryGetValue(item, out var index))
            {
                onSelectedTypeIndex.Invoke(index);
            }
        }
    }
}
