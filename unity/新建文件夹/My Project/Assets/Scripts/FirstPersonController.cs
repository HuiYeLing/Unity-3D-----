using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Inspection
{
    /// 第一人称巡检视角：WASD 移动 + 鼠标转向。
    /// 关键约束：输入框获得焦点时不移动；打开表单时释放光标。
    [RequireComponent(typeof(CharacterController))]
    public class FirstPersonController : MonoBehaviour
    {
        //[SerializeField]是Unity的一个属性，用于在Inspector面板中显示私有字段，
        // 使其可以在编辑器中进行设置和调整。它允许开发者在不改变字段访问修饰符的情况下，
        // 将私有字段暴露给Unity编辑器，从而方便地进行调试和配置。
        [SerializeField] private float moveSpeed = 3.5f;// 移动速度
        [SerializeField] private float sprintMultiplier = 1.8f;// 冲刺速度倍率
        [SerializeField] private float lookSensitivity = 2.0f;// 鼠标灵敏度
        [SerializeField] private float pitchLimit = 85f;// 俯仰角限制，避免翻转
        [SerializeField] private float gravity = -9.81f;// 重力加速度

        /// 表单打开时置 false，彻底交出输入控制权
        [System.NonSerialized] public bool InputEnabled = true;// 是否允许输入

        private CharacterController cc;// 角色控制器组件
        private float yaw;// 偏航角（水平旋转）
        private float pitch;// 俯仰角（垂直旋转）
        private float verticalVelocity;// 垂直速度，用于模拟重力和跳跃

        private void Awake() // Awake 在 Start 之前调用，适合初始化组件引用
        {
            cc = GetComponent<CharacterController>();// 获取角色控制器组件
            yaw = transform.eulerAngles.y;//    初始化偏航角为当前物体的 y 轴旋转角度
            pitch = 0f;// 初始化俯仰角为 0
        }

        private void Start()
        {
            LockCursor(true);// 锁定光标，隐藏鼠标指针
        }

        private void Update()
        {
            if (!InputEnabled) return;// 如果输入被禁用，直接返回，不处理输入

            HandleCursor();// 处理光标锁定和释放逻辑
            if (Cursor.lockState == CursorLockMode.Locked)// 如果光标被锁定，处理视角旋转
            {
                HandleLook();
            }
            HandleMove();// 处理移动逻辑
        }

        public void LockCursor(bool locked)// 锁定或释放光标
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;// 设置光标锁定状态
            Cursor.visible = !locked;// 设置光标可见性
        }

        private void HandleCursor()// 处理光标锁定和释放逻辑
        {
            if (Input.GetKeyDown(KeyCode.Escape))// 按下 Esc 键时释放光标
            {
                LockCursor(false);// 释放光标，显示鼠标指针
                return;// 直接返回，不再处理后续逻辑
            }

            // 点到 UI 上的那次点击不恢复视角锁定，避免“点了按钮顺带跑进视角模式”
            if (Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked 
            && !UiGuard.IsPointerOverUI())
            {
                LockCursor(true);
            }
        }

        private void HandleLook() // 处理视角旋转逻辑
        {
            yaw += Input.GetAxis("Mouse X") * lookSensitivity; // 获取鼠标水平移动量，乘以灵敏度，累加到偏航角
            pitch -= Input.GetAxis("Mouse Y") * lookSensitivity; // 获取鼠标垂直移动量，乘以灵敏度，累加到俯仰角（注意是减号，因为屏幕坐标系与世界坐标系相反）
            pitch = Mathf.Clamp(pitch, -pitchLimit, pitchLimit); // 限制俯仰角在-pitchLimit到pitchLimit之间，避免翻转
            transform.rotation = Quaternion.Euler(pitch, yaw, 0f); // 将俯仰角和偏航角转换为四元数，应用到物体的旋转上
        }

        private void HandleMove() // 处理移动逻辑
        {
            if (UiGuard.IsUiFocused()) return; // 正在输入框打字

            float h = Input.GetAxisRaw("Horizontal");   // 获取水平和垂直输入轴的原始值（未经过平滑处理），用于计算移动方向
            float v = Input.GetAxisRaw("Vertical"); // 获取水平和垂直输入轴的原始值（未经过平滑处理），用于计算移动方向
            Vector3 dir = transform.right * h + transform.forward * v; // 根据输入轴计算移动方向，transform.right表示物体的右方向，transform.forward表示物体的前方向
            if (dir.sqrMagnitude > 1f) dir.Normalize();// 如果移动方向的平方长度大于1，归一化方向向量，避免斜向移动速度过快

            float speed = moveSpeed * (Input.GetKey(KeyCode.LeftShift) ? sprintMultiplier : 1f);// 根据是否按下左 Shift 键，决定使用冲刺速度还是普通速度
            if (cc.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;// 如果角色在地面上且垂直速度小于0，将垂直速度设置为-2，确保角色贴地，避免悬空
            verticalVelocity += gravity * Time.deltaTime;// 根据重力加速度和时间增量，更新垂直速度，实现自由落体效果

            Vector3 motion = dir * speed + Vector3.up * verticalVelocity;// 计算最终的移动向量，包含水平移动和垂直移动
            cc.Move(motion * Time.deltaTime);// 使用角色控制器的 Move 方法移动角色，乘以时间增量，确保移动速度不受帧率影响
        }
    }
}
