using UnityEngine;

namespace Inspection
{
    /// 射线拾取 + 待提交标记的生命周期管理。
    /// 约束：一次有效点击最多一个待提交标记；取消要真的删掉。
    public class MarkerPlacer : MonoBehaviour
    {
        [Header("引用")]
        [SerializeField] private Camera viewCamera;
        [SerializeField] private IssueFormUI form;
        [SerializeField] private FirstPersonController controller;

        [Header("标记")]
        [SerializeField] private GameObject markerPrefab;
        [SerializeField] private LayerMask raycastMask = ~0;
        [SerializeField] private float maxDistance = 60f;
        [SerializeField] private float surfaceOffset = 0.06f;

        private GameObject pendingMarker;
        private Vector3 pendingPoint;

        public bool HasPending => pendingMarker != null;

        /// 提交时用的是"射线命中点"，不是标记物体的 transform（两者差一个表面偏移）
        public Vector3 PendingPoint => pendingPoint;

        private void Update()
        {
            if (form != null && form.IsOpen) return;          // 填表时不放置
            if (Cursor.lockState != CursorLockMode.Locked) return; // 未进入视角模式
            if (UiGuard.IsPointerOverUI()) return;            // 点到按钮/输入框不算
            if (!Input.GetMouseButtonDown(0)) return;

            if (!TryRaycast(out RaycastHit hit))
            {
                Toast.Show("这里没有可放置的表面，请对准地面或设备", Toast.Kind.Warn);
                return;
            }

            ClearPending(); // 先清旧的，保证"一次点击只有一个待提交标记"
            pendingPoint = hit.point;
            pendingMarker = Instantiate(
                markerPrefab,
                hit.point + hit.normal * surfaceOffset,
                Quaternion.LookRotation(hit.normal));
            pendingMarker.name = "Marker_Pending";

            form.Open(pendingPoint);
        }

        private bool TryRaycast(out RaycastHit hit)
        {
            Ray ray = viewCamera.ScreenPointToRay(Input.mousePosition);
            return Physics.Raycast(ray, out hit, maxDistance, raycastMask, QueryTriggerInteraction.Ignore);
        }

        /// 取消填报：销毁未保存标记
        public void ClearPending()
        {
            if (pendingMarker != null)
            {
                Destroy(pendingMarker);
            }
            pendingMarker = null;
        }

        /// 提交成功：标记转正（改名为已保存，并按优先级上色）
        public void Confirm(Issue issue)
        {
            if (pendingMarker == null) return;

            pendingMarker.name = "Marker_" + issue.id;
            ApplyStyle(pendingMarker, issue.priority);
            pendingMarker = null;
        }

        private static void ApplyStyle(GameObject marker, string priority)
        {
            Color color;
            switch (priority)
            {
                case Priorities.High: color = new Color(0.89f, 0.29f, 0.27f); break;   // 红
                case Priorities.Medium: color = new Color(0.94f, 0.62f, 0.15f); break; // 橙
                default: color = new Color(0.24f, 0.54f, 0.87f); break;                // 蓝
            }

            foreach (Renderer r in marker.GetComponentsInChildren<Renderer>())
            {
                r.material.color = color;
            }
        }
    }
}
