package main

import (
	"encoding/json"
	"net/http"
	"strings"
	"time"
)

var validPriorities = map[string]bool{"low": true, "medium": true, "high": true}
//存储相关的优先级
var validStatuses = map[string]bool{"open": true, "in_progress": true, "resolved": true}
//存储相关的状态

func writeJSON(w http.ResponseWriter, code int, v any) {
	w.Header().Set("Content-Type", "application/json; charset=utf-8")
	w.WriteHeader(code)                  //必须显式写状态码，否则永远返回默认 200
	_ = json.NewEncoder(w).Encode(v) //把指针v指向的值编码为JSON并写入HTTP响应体中
}

// 统一错误格式 {"error": "...", "message": "..."}，Unity 端会取 message 显示给用户
func writeErr(w http.ResponseWriter, code int, msg string) {
	writeJSON(w, code, ErrorResponse{Error: http.StatusText(code), Message: msg})
}

// GET /api/health —— 服务运行状态检查
func handleHealth(w http.ResponseWriter, r *http.Request) {
	writeJSON(w, http.StatusOK, map[string]string{"status": "ok"})
}

// /api/issues 入口：GET 列表、POST 创建
func handleIssues(w http.ResponseWriter, r *http.Request) {
	switch r.Method {
	case http.MethodGet:
		handleListIssues(w, r)
	case http.MethodPost:
		handleCreateIssue(w, r)
	default:
		w.Header().Set("Allow", "GET, POST")
		writeErr(w, http.StatusMethodNotAllowed, "仅支持 GET / POST")
	}
}

func handleListIssues(w http.ResponseWriter, _ *http.Request) {
	writeJSON(w, http.StatusOK, IssueResponse{Items: listAll()})
}

// POST /api/issues —— 接收 Unity 上报，标题必填、priority 枚举校验
func handleCreateIssue(w http.ResponseWriter, r *http.Request) {
	var req CreateIssueRequest
	if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
		writeErr(w, http.StatusBadRequest, "请求体不是合法 JSON："+err.Error())
		return
	}
	req.Title = strings.TrimSpace(req.Title)
	if req.Title == "" {
		writeErr(w, http.StatusBadRequest, "标题不能为空")
		return
	}
	if !validPriorities[req.Priority] {
		writeErr(w, http.StatusBadRequest, "priority 必须是 low / medium / high")
		return
	}

	now := time.Now().Format(time.RFC3339)
	mu.Lock()
	issue := Issue{
		ID:          nextID(),
		Title:       req.Title,
		Description: req.Description,
		Priority:    req.Priority,
		Status:      "open", 
		Position:    req.Position,
		CreatedAt:   now,
		UpdatedAt:   now,
	}
	issues[issue.ID] = issue
	err := saveLocked()
	mu.Unlock()

	if err != nil {
		writeErr(w, http.StatusInternalServerError, "保存失败："+err.Error())
		return
	}
	writeJSON(w, http.StatusCreated, issue) // 必须返回完整对象，Unity 靠 id 判断成功
}

// PATCH /api/issues/{id} —— 按问题 ID 修改处理状态
func handleIssue(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodPatch && r.Method != http.MethodPut {
		w.Header().Set("Allow", "PATCH, PUT")
		writeErr(w, http.StatusMethodNotAllowed, "仅支持 PATCH / PUT")
		return
	}
	id := strings.TrimPrefix(r.URL.Path, "/api/issues/")
	if id == "" {
		writeErr(w, http.StatusBadRequest, "缺少问题 ID")
		return
	}

	var req UpdateIssueRequest
	if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
		writeErr(w, http.StatusBadRequest, "请求体不是合法 JSON："+err.Error())
		return
	}
	if !validStatuses[req.Status] {
		writeErr(w, http.StatusBadRequest, "status 必须是 open / in_progress / resolved")
		return
	}

	mu.Lock()
	issue, ok := issues[id] 
	if !ok {
		mu.Unlock()
		writeErr(w, http.StatusNotFound, "问题不存在："+id)
		return
	}
	issue.Status = req.Status 
	issue.UpdatedAt = time.Now().Format(time.RFC3339)
	issues[id] = issue
	err := saveLocked() 
	mu.Unlock()

	if err != nil { //如果保存失败
		writeErr(w, http.StatusInternalServerError, "保存失败："+err.Error())
		return
	}
	writeJSON(w, http.StatusOK, issue)
}
