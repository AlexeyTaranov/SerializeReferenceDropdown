using System;
using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;

namespace SRD.Editor
{
    public class SRDDropdown : AdvancedDropdown
    {
        private readonly Type[] _fieldTypes;
        private readonly Action<int> _onSelectedTypeIndex;

        private readonly Dictionary<AdvancedDropdownItem, int> _itemAndIndexes =
            new Dictionary<AdvancedDropdownItem, int>();

        public SRDDropdown(AdvancedDropdownState state, Type[] fieldTypes, Action<int> onSelectedNewType) :
            base(state)
        {
            _fieldTypes = fieldTypes;
            _onSelectedTypeIndex = onSelectedNewType;
        }

        protected override AdvancedDropdownItem BuildRoot()
        {
            var root = new AdvancedDropdownItem("Types");
            _itemAndIndexes.Clear();
            for (int i = 0; i < _fieldTypes.Length; i++)
            {
                var typeName = _fieldTypes[i]?.Name ?? "null";
                var item = new AdvancedDropdownItem(typeName);
                _itemAndIndexes.Add(item, i);
                root.AddChild(item);
            }

            return root;
        }

        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            base.ItemSelected(item);
            if (_itemAndIndexes.TryGetValue(item, out var index))
            {
                _onSelectedTypeIndex.Invoke(index);
            }
        }
    }
}