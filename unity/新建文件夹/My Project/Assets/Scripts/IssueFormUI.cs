using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Inspection
{
    /// 填报面板。必须挂在"常驻激活"的物体上，panel 作为它的子物体被 SetActive 开关，
    /// 否则 panel 初始隐藏时 Awake 不会执行，按钮监听全部失效。
    public class IssueFormUI : MonoBehaviour
    {
        [Header("面板")]
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_InputField titleInput;
        [SerializeField] private TMP_InputField descInput;
        [SerializeField] private TMP_Dropdown priorityDropdown;
        [SerializeField] private Button submitButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private TextMeshProUGUI positionText;
        [SerializeField] private TextMeshProUGUI hintText;

        [Tooltip("可选：屏幕中心准星，填表时隐藏、关闭表单时恢复")]
        [SerializeField] private GameObject crosshair;

        [Header("依赖")]
        [SerializeField] private ApiClient api;
        [SerializeField] private MarkerPlacer placer;
        [SerializeField] private FirstPersonController controller;

        public bool IsOpen => panel != null && panel.activeSelf;

        private bool submitting;

        private void Awake()
        {
            // 选项由代码写入，避免 Inspector 里手填漏项或顺序错
            priorityDropdown.ClearOptions();
            priorityDropdown.AddOptions(new System.Collections.Generic.List<string>(Priorities.All));
            priorityDropdown.SetValueWithoutNotify(1); // 默认 medium

            submitButton.onClick.AddListener(OnSubmit);
            cancelButton.onClick.AddListener(OnCancel);

            if (crosshair != null) crosshair.SetActive(true);
            panel.SetActive(false);
        }

        public void Open(Vector3 worldPoint)
        {
            titleInput.text = string.Empty;
            descInput.text = string.Empty;
            priorityDropdown.SetValueWithoutNotify(1);
            hintText.text = string.Empty;
            positionText.text = $"位置  x {worldPoint.x:F2}   y {worldPoint.y:F2}   z {worldPoint.z:F2}";

            submitting = false;
            SetInteractable(true);
            panel.SetActive(true);
            if (crosshair != null) crosshair.SetActive(false);

            controller.InputEnabled = false;
            controller.LockCursor(false);
            StartCoroutine(FocusTitleNextFrame());
        }

        public void Close()
        {
            // 关表单必须结束输入法焦点并清空 EventSystem 选中，
            // 否则按钮会保持选中，导致 WASD 一直被判定为"正在输入"
            if (titleInput != null) titleInput.DeactivateInputField();
            if (descInput != null) descInput.DeactivateInputField();
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);

            panel.SetActive(false);
            if (crosshair != null) crosshair.SetActive(true);
            controller.InputEnabled = true;
            controller.LockCursor(true);
        }

        private IEnumerator FocusTitleNextFrame()
        {
            yield return null; // 等一帧，否则 ActivateInputField 在刚激活的 Canvas 上无效
            titleInput.ActivateInputField();
        }

        private void Update()
        {
            if (IsOpen && !submitting && Input.GetKeyDown(KeyCode.Escape))
            {
                OnCancel();
            }
        }

        private void OnSubmit()
        {
            if (submitting) return;

            string title = titleInput.text.Trim();
            if (string.IsNullOrEmpty(title))
            {
                hintText.text = "标题不能为空";
                titleInput.ActivateInputField();
                return;
            }

            string priority = Priorities.All[Mathf.Clamp(priorityDropdown.value, 0, Priorities.All.Length - 1)];
            if (!Priorities.IsValid(priority))
            {
                hintText.text = "优先级取值非法";
                return;
            }

            CreateIssueRequest request = new CreateIssueRequest
            {
                title = title,
                description = descInput.text.Trim(),
                priority = priority,
                position = Vector3Dto.From(placer.PendingPoint)
            };

            submitting = true;
            SetInteractable(false);
            hintText.text = "提交中…";
            StartCoroutine(api.CreateIssue(request, OnCreated, OnCreateFailed));
        }

        private void OnCreated(Issue issue)
        {
            submitting = false;
            placer.Confirm(issue);
            Close();
            Toast.Show($"上报成功：{issue.title}（{issue.priority} / {issue.status}）", Toast.Kind.Success);
        }

        private void OnCreateFailed(string message)
        {
            submitting = false;
            SetInteractable(true);
            hintText.text = "提交失败：" + message;
            Toast.Show("上报失败，请检查后端是否在运行", Toast.Kind.Error);
        }

        private void OnCancel()
        {
            if (submitting) return;

            placer.ClearPending(); // 取消要真的删掉未保存标记
            Close();
            Toast.Show("已取消，未保存的标记已移除");
        }

        private void SetInteractable(bool value)
        {
            submitButton.interactable = value;
            cancelButton.interactable = value;
            titleInput.interactable = value;
            descInput.interactable = value;
            priorityDropdown.interactable = value;
        }
    }
}
