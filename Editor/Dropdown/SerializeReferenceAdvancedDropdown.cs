using System;
using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;

namespace SerializeReferenceDropdown.Editor.Dropdown
{
    public class SerializeReferenceAdvancedDropdown : AdvancedDropdown
    {
        private readonly IEnumerable<string> typeNames;
        private readonly IEnumerable<Type> types;

        private readonly Dictionary<AdvancedDropdownItem, int> itemAndIndexes =
            new Dictionary<AdvancedDropdownItem, int>();

        private readonly Action<int> onSelectedTypeIndex;

        public SerializeReferenceAdvancedDropdown(AdvancedDropdownState state, IEnumerable<string> typeNames,
            Action<int> onSelectedNewType) :
            base(state)
        {
            this.typeNames = typeNames;
            onSelectedTypeIndex = onSelectedNewType;
        }

        public SerializeReferenceAdvancedDropdown(AdvancedDropdownState state, IReadOnlyList<Type> types,
            Action<Type> onSelectedNewType) :
            base(state)
        {
            this.types = types;
            onSelectedTypeIndex = i => onSelectedNewType.Invoke(types[i]);
        }

        protected override AdvancedDropdownItem BuildRoot()
        {
            var root = new AdvancedDropdownItem("Types");
            itemAndIndexes.Clear();

            var index = 0;
            if (types != null)
            {
                foreach (var type in types)
                {
                    var name = PropertyDrawerTypesUtils.GetTypeName(type);
                    if (type != null)
                    {
                        name += $" ({type.Namespace})";
                    }

                    var item = new AdvancedDropdownItem(name);
                    itemAndIndexes.Add(item, index);
                    root.AddChild(item);
                    index++;
                }
            }
            else
            {
                foreach (var typeName in typeNames)
                {
                    var item = new AdvancedDropdownItem(typeName);
                    itemAndIndexes.Add(item, index);
                    root.AddChild(item);
                    index++;
                }
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