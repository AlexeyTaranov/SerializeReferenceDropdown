using UnityEngine.UIElements;

namespace SerializeReferenceDropdown.Editor.Utils
{
    public static class UIToolkitExtensions
    {
        public static void SetDisplayElement(this VisualElement element, bool isActive)
        {
            element.style.display = new StyleEnum<DisplayStyle>(isActive ? DisplayStyle.Flex : DisplayStyle.None);
        }

        public static void SetActiveEmptyPlaceholder(this VisualElement element, bool isActivePlaceHolder)
        {
            var parent = element.parent;
            foreach (var child in parent.Children())
            {
                var isPlaceholder = child == element;
                if (isPlaceholder)
                {
                    child.SetDisplayElement(isActivePlaceHolder);
                }
                else
                {
                    child.SetDisplayElement(!isActivePlaceHolder);
                }
            }
        }
    }
}