package main

import (
	"bytes"
	"encoding/json"
	"net/http"
	"net/http/httptest"
	"path/filepath"
	"testing"
)

// resetStore 清空全局状态并指向临时文件，保证用例之间互不干扰
func resetStore(t *testing.T) {
	t.Helper()
	dataFile = filepath.Join(t.TempDir(), "issues.json")
	mu.Lock()
	issues = make(map[string]Issue)
	seq = 0
	mu.Unlock()
}

// do 走真实路由链发一次请求，返回 recorder（前身可理解为 MockMvc 的 perform）
func do(t *testing.T, method, path string, body any) *httptest.ResponseRecorder {
	t.Helper()
	buf := bytes.NewBuffer(nil)
	if body != nil {
		b, err := json.Marshal(body)
		if err != nil {
			t.Fatalf("marshal body: %v", err)
		}
		buf = bytes.NewBuffer(b)
	}

	req := httptest.NewRequest(method, path, buf)
	req.Header.Set("Content-Type", "application/json")
	rec := httptest.NewRecorder()

	mux := http.NewServeMux()
	mux.HandleFunc("/api/health", handleHealth)
	mux.HandleFunc("/api/issues", handleIssues)
	mux.HandleFunc("/api/issues/", handleIssue)
	mux.ServeHTTP(rec, req)
	return rec
}

func seedIssue(t *testing.T) Issue {
	t.Helper()
	rec := do(t, http.MethodPost, "/api/issues", map[string]any{
		"title": "占位问题", "priority": "low",
		"position": map[string]float64{"x": 0, "y": 0, "z": 0},
	})
	if rec.Code != http.StatusCreated {
		t.Fatalf("seed 失败: %d %s", rec.Code, rec.Body.String())
	}
	var it Issue
	if err := json.Unmarshal(rec.Body.Bytes(), &it); err != nil {
		t.Fatalf("seed 响应解析失败: %v", err)
	}
	return it
}

func TestHealth(t *testing.T) {
	resetStore(t)
	rec := do(t, http.MethodGet, "/api/health", nil)
	if rec.Code != http.StatusOK {
		t.Fatalf("got %d want 200", rec.Code)
	}
}

func TestCreateIssue(t *testing.T) {
	cases := []struct {
		name string
		body any
		want int
	}{
		{"合法创建", map[string]any{
			"title": "入口墙面破损", "description": "裂缝", "priority": "high",
			"position": map[string]float64{"x": 1.25, "y": 0.8, "z": 2.4},
		}, http.StatusCreated},
		{"标题为空", map[string]any{
			"title": "", "priority": "high",
			"position": map[string]float64{"x": 0, "y": 0, "z": 0},
		}, http.StatusBadRequest},
		{"标题全是空格", map[string]any{
			"title": "   ", "priority": "high",
			"position": map[string]float64{"x": 0, "y": 0, "z": 0},
		}, http.StatusBadRequest},
		{"非法优先级", map[string]any{
			"title": "正常标题", "priority": "urgent",
			"position": map[string]float64{"x": 0, "y": 0, "z": 0},
		}, http.StatusBadRequest},
	}

	for _, c := range cases {
		t.Run(c.name, func(t *testing.T) {
			resetStore(t)
			rec := do(t, http.MethodPost, "/api/issues", c.body)
			if rec.Code != c.want {
				t.Fatalf("状态码 got %d want %d, body=%s", rec.Code, c.want, rec.Body.String())
			}
			if c.want != http.StatusCreated {
				// 校验错误必须带 message，Unity 会直接展示给用户
				var e ErrorResponse
				_ = json.Unmarshal(rec.Body.Bytes(), &e)
				if e.Message == "" {
					t.Error("参数错误响应缺少 message 字段")
				}
				return
			}
			var got Issue
			if err := json.Unmarshal(rec.Body.Bytes(), &got); err != nil {
				t.Fatalf("响应不是合法 Issue JSON: %v", err)
			}
			if got.ID == "" {
				t.Error("id 不能为空")
			}
			if got.Status != "open" {
				t.Errorf("默认状态应为 open, got %q", got.Status)
			}
			if got.CreatedAt == "" || got.UpdatedAt == "" {
				t.Error("createdAt/updatedAt 必须由后端生成")
			}
			if got.Position.X != 1.25 || got.Position.Z != 2.4 {
				t.Errorf("position 回传错误: %+v", got.Position)
			}
		})
	}
}

func TestCreateIssueInvalidJSON(t *testing.T) {
	resetStore(t)
	req := httptest.NewRequest(http.MethodPost, "/api/issues", bytes.NewBufferString("{not json"))
	req.Header.Set("Content-Type", "application/json")
	rec := httptest.NewRecorder()

	mux := http.NewServeMux()
	mux.HandleFunc("/api/issues", handleIssues)
	mux.ServeHTTP(rec, req)

	if rec.Code != http.StatusBadRequest {
		t.Fatalf("非法 JSON 应返回 400, got %d", rec.Code)
	}
}

func TestListIssues(t *testing.T) {
	resetStore(t)
	do(t, http.MethodPost, "/api/issues", map[string]any{
		"title": "第一条", "priority": "low",
		"position": map[string]float64{"x": 1, "y": 1, "z": 1},
	})
	do(t, http.MethodPost, "/api/issues", map[string]any{
		"title": "第二条", "priority": "high",
		"position": map[string]float64{"x": 2, "y": 2, "z": 2},
	})

	rec := do(t, http.MethodGet, "/api/issues", nil)
	if rec.Code != http.StatusOK {
		t.Fatalf("got %d want 200", rec.Code)
	}

	var got IssueResponse
	if err := json.Unmarshal(rec.Body.Bytes(), &got); err != nil {
		t.Fatalf(`响应不是 {"items":[...]} 结构: %v`, err)
	}
	if len(got.Items) != 2 {
		t.Fatalf("应有 2 条, got %d", len(got.Items))
	}
}

func TestUpdateIssueStatus(t *testing.T) {
	cases := []struct {
		name string
		// useSeededID 为 true 时用库里真实 ID，否则用不存在的 ID
		useSeededID bool
		status      string
		want        int
	}{
		{"改为 in_progress", true, "in_progress", http.StatusOK},
		{"改为 resolved", true, "resolved", http.StatusOK},
		{"非法状态", true, "done", http.StatusBadRequest},
		{"ID 不存在", false, "open", http.StatusNotFound},
	}

	for _, c := range cases {
		t.Run(c.name, func(t *testing.T) {
			resetStore(t)
			issue := seedIssue(t)
			id := issue.ID
			if !c.useSeededID {
				id = "ISS-999999"
			}

			rec := do(t, http.MethodPatch, "/api/issues/"+id, map[string]string{"status": c.status})
			if rec.Code != c.want {
				t.Fatalf("状态码 got %d want %d, body=%s", rec.Code, c.want, rec.Body.String())
			}
			if c.want != http.StatusOK {
				return
			}
			var got Issue
			if err := json.Unmarshal(rec.Body.Bytes(), &got); err != nil {
				t.Fatalf("响应解析失败: %v", err)
			}
			if got.Status != c.status {
				t.Errorf("状态未更新: want %q got %q", c.status, got.Status)
			}
			if got.ID != issue.ID {
				t.Errorf("ID 被篡改: %q", got.ID)
			}
		})
	}
}

// 持久化：写一条 → 清空内存模拟重启 → load() → 数据与 ID 序列都应恢复
func TestPersistenceAcrossRestart(t *testing.T) {
	resetStore(t)
	created := seedIssue(t)

	mu.Lock()
	issues = make(map[string]Issue)
	seq = 0
	mu.Unlock()
	if err := load(); err != nil {
		t.Fatalf("load 失败: %v", err)
	}

	rec := do(t, http.MethodGet, "/api/issues", nil)
	var got IssueResponse
	if err := json.Unmarshal(rec.Body.Bytes(), &got); err != nil {
		t.Fatalf("响应解析失败: %v", err)
	}
	if len(got.Items) != 1 {
		t.Fatalf("重启后应有 1 条, got %d", len(got.Items))
	}
	if got.Items[0].ID != created.ID {
		t.Errorf("ID 未恢复: want %q got %q", created.ID, got.Items[0].ID)
	}

	// 序列恢复后再创建，ID 应递增而不是从 1 重来
	next := seedIssue(t)
	if next.ID == created.ID {
		t.Errorf("seq 未恢复，生成了重复 ID %q", next.ID)
	}
}
