package main

import (
	"log"
	"net/http"
)

// CORS 中间件：React 跑在 5173 端口，跨端口就是跨域，必须放行
// OPTIONS 预检请求直接返回 200，否则浏览器发 PATCH 前的预检会失败
func cors(next http.Handler) http.Handler {
	return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		w.Header().Set("Access-Control-Allow-Origin", "*")
		w.Header().Set("Access-Control-Allow-Methods", "GET, POST, PATCH, PUT, OPTIONS")
		w.Header().Set("Access-Control-Allow-Headers", "Content-Type")
		if r.Method == http.MethodOptions {
			w.WriteHeader(http.StatusOK)
			return
		}
		next.ServeHTTP(w, r)
	})
}

func main() {
	// 启动时从 issues.json 恢复数据，重启不丢
	if err := load(); err != nil {
		log.Fatalf("加载数据失败: %v", err)
	}

	mux := http.NewServeMux() // 创建一个新的 ServeMux，用于注册路由和处理函数
	mux.HandleFunc("/api/health", handleHealth)	// GET /api/health 健康检查
	mux.HandleFunc("/api/issues", handleIssues)   // GET 列表 / POST 创建
	mux.HandleFunc("/api/issues/", handleIssue)   // PATCH /api/issues/{id} 改状态

	log.Println("Server is running on port :8080")
	log.Fatal(http.ListenAndServe(":8080", cors(mux)))
}
