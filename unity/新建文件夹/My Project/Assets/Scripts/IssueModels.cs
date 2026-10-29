using System;
using System.Collections.Generic;
using UnityEngine;

namespace Inspection
{
    // 三端必须完全一致的枚举字符串，禁止在别处写裸字符串
    public static class Priorities
    {
        public const string Low = "low";
        public const string Medium = "medium";
        public const string High = "high";
        public static readonly string[] All = { Low, Medium, High };

        public static bool IsValid(string v) => v == Low || v == Medium || v == High;
    }

    public static class Statuses
    {
        public const string Open = "open";
        public const string InProgress = "in_progress";
        public const string Resolved = "resolved";
    }

    [Serializable]
    public class Vector3Dto
    {
        public float x;
        public float y;
        public float z;

        public Vector3Dto() { }

        public Vector3Dto(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public static Vector3Dto From(Vector3 v) => new Vector3Dto(v.x, v.y, v.z);

        public Vector3 ToVector3() => new Vector3(x, y, z);

        public override string ToString() => $"({x:F2}, {y:F2}, {z:F2})";
    }

    // 与 README 中的数据结构逐字段对应；JsonUtility 只认字段，不认属性
    [Serializable]
    public class Issue
    {
        public string id;
        public string title;
        public string description;
        public string priority;
        public string status;
        public Vector3Dto position;
        public string createdAt;
        public string updatedAt;
    }

    [Serializable]
    public class CreateIssueRequest
    {
        public string title;
        public string description;
        public string priority;
        public Vector3Dto position;
    }

    // JsonUtility 不支持顶层数组，列表必须包一层
    [Serializable]
    public class IssueListResponse
    {
        public List<Issue> items;
    }

    [Serializable]
    public class ApiErrorResponse
    {
        public string error;
        public string message;
    }
}
