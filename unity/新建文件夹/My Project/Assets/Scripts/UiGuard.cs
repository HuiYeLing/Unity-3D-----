using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Inspection
{
    /// 统一判断"这一下点击到底是给 UI 的还是给场景的"。
    /// 题目 3.1.7 的扣分点基本都出在这里。
    public static class UiGuard
    {
        /// 光标是否停在任意 UI 元素上（按钮、输入框、下拉框都算）
        public static bool IsPointerOverUI()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        /// 是否正在输入框里打字。
        /// 注意：不能写成"有 Selectable 被选中"，因为按钮点完会一直保持选中状态，
        /// 那样会在关闭表单后把 WASD 一直拦死。
        public static bool IsUiFocused()
        {
            if (EventSystem.current == null) return false;

            GameObject selected = EventSystem.current.currentSelectedGameObject;
            if (selected == null) return false;

            TMP_InputField tmp = selected.GetComponent<TMP_InputField>();
            if (tmp != null) return tmp.isFocused;

            InputField legacy = selected.GetComponent<InputField>();
            return legacy != null && legacy.isFocused;
        }
    }
}
