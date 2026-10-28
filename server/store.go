package main

import (
	"encoding/json"
	"errors"
	"fmt"
	"os"
	"sort"
	"sync"
)

// 数据落在本地 JSON 文件，服务重启后 load() 恢复 —— 对应课题第 7 条
// 用 var 而不是 const：测试里会把它指向临时目录，避免污染真实数据
var dataFile = "issues.json"

var (
	mu     sync.Mutex //互斥锁，保证并发安全
	issues = make(map[string]Issue)
	seq    int //问题ID自增序列
)

// persistFile 是磁盘文件的结构：map 无法直接序列化，落盘转成数组
type persistFile struct {
	Seq    int     `json:"seq"`
	Issues []Issue `json:"issues"`
}

func load() error {
	data, err := os.ReadFile(dataFile)
	if errors.Is(err, os.ErrNotExist) {
		return nil // 首次启动，空库不算错误
	}
	if err != nil {
		return err
	}
	var pf persistFile
	if err := json.Unmarshal(data, &pf); err != nil {
		return err
	}
	seq = pf.Seq
	for _, it := range pf.Issues {
		issues[it.ID] = it
	}
	return nil
}

// saveLocked 写盘。约定：调用方必须已持有 mu，函数名以 Locked 结尾就是这个含义
// 先写临时文件再 rename，保证写一半崩溃也不会损坏旧数据
func saveLocked() error {
	list := make([]Issue, 0, len(issues))
	for _, it := range issues {
		list = append(list, it)
	}
	data, err := json.MarshalIndent(persistFile{Seq: seq, Issues: list}, "", "  ")
	if err != nil {
		return err
	}
	tmp := dataFile + ".tmp"
	if err := os.WriteFile(tmp, data, 0o644); err != nil {
		return err
	}
	return os.Rename(tmp, dataFile)
}

// listAll 返回按创建时间倒序的全量列表（新的在前，方便 React 直接展示）
func listAll() []Issue {
	mu.Lock() 
	defer mu.Unlock()
	out := make([]Issue, 0, len(issues))
	for _, it := range issues {
		out = append(out, it)
	}
	sort.Slice(out, func(i, j int) bool { return out[i].CreatedAt > out[j].CreatedAt })
	return out
}

// nextID 生成唯一 ID。约定：调用方必须已持有 mu
func nextID() string {
	seq++
	return fmt.Sprintf("ISS-%06d", seq)
}
