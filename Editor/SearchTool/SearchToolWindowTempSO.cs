using UnityEngine;

namespace SerializeReferenceDropdown.Editor.SearchTool
{
    //HACK: uhahahaha blya - f*cking amazing!
    //https://t.me/unsafecsharp/78
    public class SearchToolWindowTempSO : ScriptableObject
    {
        [SerializeReference] public object tempObject;
    }
}