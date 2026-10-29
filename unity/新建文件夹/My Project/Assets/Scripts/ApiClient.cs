using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Inspection
{
    /// 后端通道。所有网络失败都以可读中文回传给 UI，不吞异常。
    public class ApiClient : MonoBehaviour
    {
        [Tooltip("Editor 用 127.0.0.1；Android 真机必须换成电脑局域网 IP")]
        [SerializeField] private string baseUrl = "http://127.0.0.1:8080";

        [SerializeField] private int timeoutSeconds = 8;

        public string BaseUrl => baseUrl;

        /// 链路探针：不依赖业务数据，先验证"能不能连上"
        public IEnumerator Health(Action<string> onResult)
        {
            using (UnityWebRequest www = UnityWebRequest.Get(Url("/api/health")))
            {
                www.timeout = timeoutSeconds;
                yield return www.SendWebRequest();
                if (www.result != UnityWebRequest.Result.Success)
                {
                    onResult?.Invoke("失败：" + Describe(www));
                    yield break;
                }
                onResult?.Invoke("成功：" + www.downloadHandler.text);
            }
        }

        public IEnumerator CreateIssue(CreateIssueRequest req, Action<Issue> onSuccess, Action<string> onError)
        {
            byte[] payload = Encoding.UTF8.GetBytes(JsonUtility.ToJson(req));
            using (UnityWebRequest www = new UnityWebRequest(Url("/api/issues"), UnityWebRequest.kHttpVerbPOST))
            {
                www.uploadHandler = new UploadHandlerRaw(payload);
                www.downloadHandler = new DownloadHandlerBuffer();
                www.SetRequestHeader("Content-Type", "application/json");
                www.timeout = timeoutSeconds;
                yield return www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    onError?.Invoke(Describe(www));
                    yield break;
                }

                Issue issue = null;
                try
                {
                    issue = JsonUtility.FromJson<Issue>(www.downloadHandler.text);
                }
                catch (Exception e)
                {
                    onError?.Invoke("响应解析失败：" + e.Message);
                    yield break;
                }

                if (issue == null || string.IsNullOrEmpty(issue.id))
                {
                    onError?.Invoke("后端返回的数据不完整（缺少 id）");
                    yield break;
                }

                onSuccess?.Invoke(issue);
            }
        }

        public IEnumerator GetIssues(Action<Issue[]> onSuccess, Action<string> onError)
        {
            using (UnityWebRequest www = UnityWebRequest.Get(Url("/api/issues")))
            {
                www.timeout = timeoutSeconds;
                yield return www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    onError?.Invoke(Describe(www));
                    yield break;
                }

                IssueListResponse parsed = null;
                try
                {
                    parsed = JsonUtility.FromJson<IssueListResponse>(www.downloadHandler.text);
                }
                catch (Exception e)
                {
                    onError?.Invoke("响应解析失败：" + e.Message);
                    yield break;
                }

                onSuccess?.Invoke(parsed?.items?.ToArray() ?? Array.Empty<Issue>());
            }
        }

        private string Url(string path) => baseUrl.TrimEnd('/') + path;

        /// 把 UnityWebRequest 的失败翻译成"人话"，方便现场排查
        private static string Describe(UnityWebRequest www)
        {
            if (www.responseCode != 0)
            {
                string body = www.downloadHandler != null ? www.downloadHandler.text : string.Empty;
                string detail = ExtractMessage(body);
                return string.IsNullOrEmpty(detail)
                    ? $"HTTP {www.responseCode}"
                    : $"HTTP {www.responseCode}：{detail}";
            }

            return $"无法连接后端，请检查地址与网络（{www.error}）";
        }

        private static string ExtractMessage(string body)
        {
            if (string.IsNullOrEmpty(body)) return string.Empty;
            try
            {
                ApiErrorResponse err = JsonUtility.FromJson<ApiErrorResponse>(body);
                if (err == null) return string.Empty;
                return string.IsNullOrEmpty(err.message) ? err.error : err.message;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
