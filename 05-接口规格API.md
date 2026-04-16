# API接口规格文档

## 1. API概述

### 1.1 基本信息

| 项目 | 说明 |
|-----|------|
| Base URL | `/api/v1` |
| 认证方式 | JWT Bearer Token |
| Content-Type | `application/json` |
| 字符编码 | UTF-8 |
| API版本 | v1 |

### 1.2 通用请求头

```
Authorization: Bearer {token}
Content-Type: application/json
X-Request-Id: {request-id}     # 可选，请求追踪ID
X-Client-Version: {version}    # 可选，客户端版本
```

### 1.3 通用响应头

```
X-Request-Id: {request-id}
X-RateLimit-Remaining: {count}
X-RateLimit-Reset: {timestamp}
```

---

## 2. 认证接口

### 2.1 登录

```
POST /api/v1/auth/login
```

**请求体:**
```json
{
    "username": "string",
    "password": "string"
}
```

**成功响应 (200):**
```json
{
    "success": true,
    "data": {
        "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
        "expiresAt": "2026-04-17T12:00:00Z",
        "tokenType": "Bearer"
    },
    "timestamp": "2026-04-16T12:00:00Z"
}
```

**错误响应 (401):**
```json
{
    "success": false,
    "errorCode": "UNAUTHORIZED",
    "message": "用户名或密码错误",
    "requestId": "req_abc123",
    "timestamp": "2026-04-16T12:00:00Z"
}
```

---

### 2.2 刷新Token

```
POST /api/v1/auth/refresh
```

**请求体:**
```json
{
    "refreshToken": "string"
}
```

**成功响应 (200):**
```json
{
    "success": true,
    "data": {
        "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
        "expiresAt": "2026-04-17T12:00:00Z"
    },
    "timestamp": "2026-04-16T12:00:00Z"
}
```

---

## 3. 健康检查接口

### 3.1 整体健康检查

```
GET /health
```

**成功响应 (200):**
```json
{
    "status": "Healthy",
    "checks": {
        "api": "Healthy",
        "system-db": "Healthy",
        "redis": "Healthy",
        "ai-service": "Healthy"
    },
    "timestamp": "2026-04-16T12:00:00Z"
}
```

**降级响应 (503):**
```json
{
    "status": "Degraded",
    "checks": {
        "api": "Healthy",
        "system-db": "Healthy",
        "redis": "Unhealthy",
        "ai-service": "Healthy"
    },
    "timestamp": "2026-04-16T12:00:00Z"
}
```

---

### 3.2 就绪检查

```
GET /health/ready
```

**成功响应 (200):**
```json
{
    "status": "Ready",
    "timestamp": "2026-04-16T12:00:00Z"
}
```

---

### 3.3 存活检查

```
GET /health/live
```

**成功响应 (200):**
```json
{
    "status": "Alive",
    "timestamp": "2026-04-16T12:00:00Z"
}
```

---

## 4. 数据库连接接口

### 4.1 获取连接列表

```
GET /api/v1/connections
Authorization: Bearer {token}
```

**查询参数:**
| 参数 | 类型 | 必填 | 说明 |
|-----|------|------|------|
| page | int | 否 | 页码，默认1 |
| pageSize | int | 否 | 每页数量，默认20 |
| keyword | string | 否 | 搜索关键词 |

**成功响应 (200):**
```json
{
    "success": true,
    "data": [
        {
            "connectionId": "conn_001",
            "name": "生产数据库",
            "type": "PostgreSQL",
            "host": "192.168.1.100",
            "port": 5432,
            "database": "mydb",
            "isEnabled": true,
            "createdAt": "2026-04-01T10:00:00Z",
            "lastConnectedAt": "2026-04-16T11:30:00Z"
        }
    ],
    "pagination": {
        "pageIndex": 1,
        "pageSize": 20,
        "totalCount": 5,
        "totalPages": 1
    },
    "timestamp": "2026-04-16T12:00:00Z"
}
```

---

### 4.2 创建连接

```
POST /api/v1/connections
Authorization: Bearer {token}
```

**请求体:**
```json
{
    "name": "生产数据库",
    "type": "PostgreSQL",
    "host": "192.168.1.100",
    "port": 5432,
    "database": "mydb",
    "username": "readonly_user",
    "password": "secret_password",
    "maxConnections": 10,
    "commandTimeout": 30,
    "isEnabled": true
}
```

**成功响应 (201):**
```json
{
    "success": true,
    "data": {
        "connectionId": "conn_002",
        "name": "生产数据库",
        "type": "PostgreSQL",
        "host": "192.168.1.100",
        "port": 5432,
        "database": "mydb",
        "isEnabled": true,
        "createdAt": "2026-04-16T12:00:00Z"
    },
    "timestamp": "2026-04-16T12:00:00Z"
}
```

**错误响应 (400):**
```json
{
    "success": false,
    "errorCode": "VALIDATION_ERROR",
    "message": "参数验证失败",
    "details": {
        "field": "host",
        "reason": "Host不能为空"
    },
    "requestId": "req_abc123",
    "timestamp": "2026-04-16T12:00:00Z"
}
```

---

### 4.3 获取连接详情

```
GET /api/v1/connections/{connectionId}
Authorization: Bearer {token}
```

**成功响应 (200):**
```json
{
    "success": true,
    "data": {
        "connectionId": "conn_001",
        "name": "生产数据库",
        "type": "PostgreSQL",
        "host": "192.168.1.100",
        "port": 5432,
        "database": "mydb",
        "username": "readonly_user",
        "maxConnections": 10,
        "commandTimeout": 30,
        "isEnabled": true,
        "createdAt": "2026-04-01T10:00:00Z",
        "lastConnectedAt": "2026-04-16T11:30:00Z"
    },
    "timestamp": "2026-04-16T12:00:00Z"
}
```

---

### 4.4 更新连接

```
PUT /api/v1/connections/{connectionId}
Authorization: Bearer {token}
```

**请求体:**
```json
{
    "name": "生产数据库(更新)",
    "host": "192.168.1.101",
    "port": 5432,
    "database": "mydb",
    "username": "readonly_user",
    "password": "new_password",
    "maxConnections": 20,
    "commandTimeout": 60,
    "isEnabled": true
}
```

**成功响应 (200):**
```json
{
    "success": true,
    "data": {
        "connectionId": "conn_001",
        "name": "生产数据库(更新)",
        "updatedAt": "2026-04-16T12:00:00Z"
    },
    "timestamp": "2026-04-16T12:00:00Z"
}
```

---

### 4.5 删除连接

```
DELETE /api/v1/connections/{connectionId}
Authorization: Bearer {token}
```

**成功响应 (204):** No Content

---

### 4.6 测试连接

```
POST /api/v1/connections/test
Authorization: Bearer {token}
```

**请求体:**
```json
{
    "type": "PostgreSQL",
    "host": "192.168.1.100",
    "port": 5432,
    "database": "mydb",
    "username": "readonly_user",
    "password": "secret_password",
    "timeout": 10
}
```

**成功响应 (200):**
```json
{
    "success": true,
    "data": {
        "connected": true,
        "serverVersion": "PostgreSQL 15.2",
        "latency": 25,
        "message": "连接成功"
    },
    "timestamp": "2026-04-16T12:00:00Z"
}
```

**失败响应 (200):**
```json
{
    "success": true,
    "data": {
        "connected": false,
        "errorCode": "CONNECTION_FAILED",
        "message": "连接超时"
    },
    "timestamp": "2026-04-16T12:00:00Z"
}
```

---

## 5. Schema接口

### 5.1 获取Schema列表

```
GET /api/v1/schema/{connectionId}
Authorization: Bearer {token}
```

**成功响应 (200):**
```json
{
    "success": true,
    "data": {
        "connectionId": "conn_001",
        "schemas": [
            {
                "name": "public",
                "tables": 25,
                "views": 5
            },
            {
                "name": "audit",
                "tables": 3,
                "views": 1
            }
        ]
    },
    "timestamp": "2026-04-16T12:00:00Z"
}
```

---

### 5.2 获取表列表

```
GET /api/v1/schema/{connectionId}/tables
Authorization: Bearer {token}
```

**查询参数:**
| 参数 | 类型 | 必填 | 说明 |
|-----|------|------|------|
| schema | string | 否 | Schema名称，默认public |
| page | int | 否 | 页码 |
| pageSize | int | 否 | 每页数量 |

**成功响应 (200):**
```json
{
    "success": true,
    "data": [
        {
            "tableName": "users",
            "schema": "public",
            "type": "table",
            "rowCount": 10000,
            "comment": "用户表"
        },
        {
            "tableName": "orders",
            "schema": "public",
            "type": "table",
            "rowCount": 50000,
            "comment": "订单表"
        }
    ],
    "pagination": {
        "pageIndex": 1,
        "pageSize": 50,
        "totalCount": 30,
        "totalPages": 1
    },
    "timestamp": "2026-04-16T12:00:00Z"
}
```

---

### 5.3 获取表结构

```
GET /api/v1/schema/{connectionId}/tables/{tableName}
Authorization: Bearer {token}
```

**查询参数:**
| 参数 | 类型 | 必填 | 说明 |
|-----|------|------|------|
| schema | string | 否 | Schema名称 |

**成功响应 (200):**
```json
{
    "success": true,
    "data": {
        "tableName": "users",
        "schema": "public",
        "comment": "用户表",
        "columns": [
            {
                "name": "id",
                "type": "bigint",
                "isNullable": false,
                "isPrimaryKey": true,
                "defaultValue": null,
                "maxLength": null,
                "precision": null,
                "scale": null,
                "comment": "用户ID"
            },
            {
                "name": "username",
                "type": "varchar",
                "isNullable": false,
                "isPrimaryKey": false,
                "defaultValue": null,
                "maxLength": 100,
                "precision": null,
                "scale": null,
                "comment": "用户名"
            },
            {
                "name": "email",
                "type": "varchar",
                "isNullable": true,
                "isPrimaryKey": false,
                "defaultValue": null,
                "maxLength": 255,
                "precision": null,
                "scale": null,
                "comment": "邮箱"
            },
            {
                "name": "created_at",
                "type": "timestamp",
                "isNullable": false,
                "isPrimaryKey": false,
                "defaultValue": "CURRENT_TIMESTAMP",
                "maxLength": null,
                "precision": 6,
                "scale": null,
                "comment": "创建时间"
            }
        ],
        "indexes": [
            {
                "name": "idx_users_email",
                "columns": ["email"],
                "isUnique": true,
                "isPrimary": false
            }
        ]
    },
    "timestamp": "2026-04-16T12:00:00Z"
}
```

---

## 6. 查询执行接口

### 6.1 执行查询

```
POST /api/v1/query/execute
Authorization: Bearer {token}
```

**请求体:**
```json
{
    "connectionId": "conn_001",
    "sql": "SELECT * FROM users WHERE created_at > '2026-01-01'",
    "timeout": 30,
    "maxRows": 1000,
    "pageSize": 20,
    "pageIndex": 0
}
```

**成功响应 (200):**
```json
{
    "success": true,
    "data": {
        "columns": [
            {"name": "id", "type": "bigint"},
            {"name": "username", "type": "varchar"},
            {"name": "email", "type": "varchar"},
            {"name": "created_at", "type": "timestamp"}
        ],
        "rows": [
            [1, "user1", "user1@example.com", "2026-01-15T10:30:00Z"],
            [2, "user2", "user2@example.com", "2026-01-20T14:20:00Z"]
        ],
        "rowCount": 2,
        "executionTime": 125,
        "pageIndex": 0,
        "pageSize": 20,
        "hasMore": false
    },
    "timestamp": "2026-04-16T12:00:00Z"
}
```

**错误响应 (400):**
```json
{
    "success": false,
    "errorCode": "QUERY_EXECUTION_ERROR",
    "message": "SQL执行错误",
    "details": {
        "sqlState": "42P01",
        "message": "relation \"users\" does not exist"
    },
    "requestId": "req_abc123",
    "timestamp": "2026-04-16T12:00:00Z"
}
```

**超时响应 (408):**
```json
{
    "success": false,
    "errorCode": "QUERY_TIMEOUT",
    "message": "查询超时",
    "details": {
        "timeout": 30,
        "elapsed": 30000
    },
    "requestId": "req_abc123",
    "timestamp": "2026-04-16T12:00:00Z"
}
```

---

### 6.2 解释查询

```
POST /api/v1/query/explain
Authorization: Bearer {token}
```

**请求体:**
```json
{
    "connectionId": "conn_001",
    "sql": "SELECT * FROM users WHERE email = 'test@example.com'"
}
```

**成功响应 (200):**
```json
{
    "success": true,
    "data": {
        "plan": "Seq Scan on users  (cost=0.00..35.50 rows=10 width=84)\n  Filter: ((email)::text = 'test@example.com'::text)",
        "estimatedCost": 35.50,
        "estimatedRows": 10,
        "executionTime": 5
    },
    "timestamp": "2026-04-16T12:00:00Z"
}
```

---

## 7. AI接口

### 7.1 Text-to-SQL

```
POST /api/v1/ai/text-to-sql
Authorization: Bearer {token}
```

**请求体:**
```json
{
    "connectionId": "conn_001",
    "naturalLanguage": "查一下2026年注册的用户有多少",
    "includeSchema": true,
    "sessionId": "sess_001"
}
```

**成功响应 (200):**
```json
{
    "success": true,
    "data": {
        "sql": "SELECT COUNT(*) AS user_count FROM users WHERE created_at >= '2026-01-01'",
        "explanation": "查询2026年1月1日以来注册的用户总数",
        "parameters": [],
        "confidence": 0.95
    },
    "timestamp": "2026-04-16T12:00:00Z"
}
```

**带结果的响应:**
```json
{
    "success": true,
    "data": {
        "sql": "SELECT COUNT(*) AS user_count FROM users WHERE created_at >= '2026-01-01'",
        "explanation": "查询2026年1月1日以来注册的用户总数",
        "parameters": [],
        "confidence": 0.95,
        "result": {
            "user_count": 1523
        }
    },
    "timestamp": "2026-04-16T12:00:00Z"
}
```

---

### 7.2 SQL转自然语言

```
POST /api/v1/ai/sql-to-text
Authorization: Bearer {token}
```

**请求体:**
```json
{
    "connectionId": "conn_001",
    "sql": "SELECT DATE(created_at) AS date, COUNT(*) AS cnt FROM users GROUP BY DATE(created_at) ORDER BY date DESC LIMIT 7"
}
```

**成功响应 (200):**
```json
{
    "success": true,
    "data": {
        "explanation": "这条SQL查询最近7天每天的新用户注册数量，按日期降序排列。\n\n查询逻辑：\n1. 将created_at字段按日期分组\n2. 统计每天的用户数量\n3. 按日期降序排列\n4. 限制返回最近7天的数据",
        "keywords": ["用户注册", "每日统计", "趋势分析"]
    },
    "timestamp": "2026-04-16T12:00:00Z"
}
```

---

### 7.3 SQL优化

```
POST /api/v1/ai/sql-optimize
Authorization: Bearer {token}
```

**请求体:**
```json
{
    "connectionId": "conn_001",
    "sql": "SELECT * FROM users, orders WHERE users.id = orders.user_id"
}
```

**成功响应 (200):**
```json
{
    "success": true,
    "data": {
        "originalSql": "SELECT * FROM users, orders WHERE users.id = orders.user_id",
        "optimizedSql": "SELECT u.*, o.* \nFROM users u\nINNER JOIN orders o ON u.id = o.user_id",
        "suggestions": [
            "使用ANSI JOIN语法替代逗号连接，提高可读性",
            "明确指定列名而非SELECT *，避免不必要的数据传输",
            "建议在orders.user_id上添加索引以提升连接性能"
        ],
        "estimatedImprovement": "查询性能提升约30%"
    },
    "timestamp": "2026-04-16T12:00:00Z"
}
```

---

### 7.4 获取会话列表

```
GET /api/v1/ai/sessions
Authorization: Bearer {token}
```

**查询参数:**
| 参数 | 类型 | 必填 | 说明 |
|-----|------|------|------|
| page | int | 否 | 页码 |
| pageSize | int | 否 | 每页数量 |

**成功响应 (200):**
```json
{
    "success": true,
    "data": [
        {
            "sessionId": "sess_001",
            "connectionId": "conn_001",
            "title": "用户数据分析",
            "messageCount": 10,
            "createdAt": "2026-04-15T10:00:00Z",
            "lastActivityAt": "2026-04-16T11:30:00Z"
        }
    ],
    "pagination": {
        "pageIndex": 1,
        "pageSize": 20,
        "totalCount": 5,
        "totalPages": 1
    },
    "timestamp": "2026-04-16T12:00:00Z"
}
```

---

### 7.5 创建会话

```
POST /api/v1/ai/sessions
Authorization: Bearer {token}
```

**请求体:**
```json
{
    "connectionId": "conn_001",
    "title": "用户数据分析"
}
```

**成功响应 (201):**
```json
{
    "success": true,
    "data": {
        "sessionId": "sess_002",
        "connectionId": "conn_001",
        "title": "用户数据分析",
        "createdAt": "2026-04-16T12:00:00Z"
    },
    "timestamp": "2026-04-16T12:00:00Z"
}
```

---

### 7.6 获取会话消息

```
GET /api/v1/ai/sessions/{sessionId}/messages
Authorization: Bearer {token}
```

**成功响应 (200):**
```json
{
    "success": true,
    "data": [
        {
            "messageId": "msg_001",
            "role": "user",
            "content": "查一下2026年注册的用户有多少",
            "createdAt": "2026-04-16T10:00:00Z"
        },
        {
            "messageId": "msg_002",
            "role": "assistant",
            "content": "SELECT COUNT(*) AS user_count FROM users WHERE created_at >= '2026-01-01'",
            "sql": "SELECT COUNT(*) AS user_count FROM users WHERE created_at >= '2026-01-01'",
            "explanation": "查询2026年1月1日以来注册的用户总数",
            "createdAt": "2026-04-16T10:00:05Z"
        },
        {
            "messageId": "msg_003",
            "role": "user",
            "content": "其中付费用户有多少",
            "createdAt": "2026-04-16T10:01:00Z"
        },
        {
            "messageId": "msg_004",
            "role": "assistant",
            "content": "SELECT COUNT(*) AS paid_user_count FROM users WHERE created_at >= '2026-01-01' AND is_paid = true",
            "sql": "SELECT COUNT(*) AS paid_user_count FROM users WHERE created_at >= '2026-01-01' AND is_paid = true",
            "explanation": "在之前的查询基础上，增加付费用户筛选条件",
            "createdAt": "2026-04-16T10:01:05Z"
        }
    ],
    "timestamp": "2026-04-16T12:00:00Z"
}
```

---

## 8. DDL接口

### 8.1 获取索引列表

```
GET /api/v1/ddl/indexes
Authorization: Bearer {token}
```

**查询参数:**
| 参数 | 类型 | 必填 | 说明 |
|-----|------|------|------|
| connectionId | string | 是 | 连接ID |
| schema | string | 否 | Schema名称 |
| table | string | 否 | 表名 |

**成功响应 (200):**
```json
{
    "success": true,
    "data": [
        {
            "indexName": "idx_users_email",
            "schema": "public",
            "tableName": "users",
            "columns": ["email"],
            "isUnique": true,
            "isPrimary": false
        }
    ],
    "timestamp": "2026-04-16T12:00:00Z"
}
```

---

### 8.2 创建索引

```
POST /api/v1/ddl/indexes
Authorization: Bearer {token}
```

**请求体:**
```json
{
    "connectionId": "conn_001",
    "schema": "public",
    "tableName": "users",
    "indexName": "idx_users_phone",
    "columns": ["phone"],
    "isUnique": false
}
```

**成功响应 (201):**
```json
{
    "success": true,
    "data": {
        "indexName": "idx_users_phone",
        "ddl": "CREATE INDEX idx_users_phone ON public.users (phone)"
    },
    "timestamp": "2026-04-16T12:00:00Z"
}
```

---

### 8.3 删除索引

```
DELETE /api/v1/ddl/indexes/{indexName}
Authorization: Bearer {token}
```

**查询参数:**
| 参数 | 类型 | 必填 | 说明 |
|-----|------|------|------|
| connectionId | string | 是 | 连接ID |
| schema | string | 否 | Schema名称 |

**成功响应 (200):**
```json
{
    "success": true,
    "data": {
        "indexName": "idx_users_phone",
        "ddl": "DROP INDEX idx_users_phone"
    },
    "timestamp": "2026-04-16T12:00:00Z"
}
```

---

## 9. 导出接口

### 9.1 导出CSV

```
POST /api/v1/export/csv
Authorization: Bearer {token}
```

**请求体:**
```json
{
    "connectionId": "conn_001",
    "sql": "SELECT * FROM users WHERE created_at > '2026-01-01'",
    "options": {
        "delimiter": ",",
        "encoding": "UTF-8",
        "includeHeaders": true
    }
}
```

**成功响应 (200):**
```json
{
    "success": true,
    "data": {
        "fileName": "export_users_20260416.csv",
        "rowCount": 1523,
        "size": 45678
    },
    "timestamp": "2026-04-16T12:00:00Z"
}
```

**文件下载响应:**
```
Content-Type: text/csv
Content-Disposition: attachment; filename="export_users_20260416.csv"
```

---

### 9.2 导出Excel

```
POST /api/v1/export/excel
Authorization: Bearer {token}
```

**请求体:**
```json
{
    "connectionId": "conn_001",
    "sql": "SELECT * FROM users WHERE created_at > '2026-01-01'",
    "options": {
        "sheetName": "用户数据",
        "freezeHeader": true,
        "autoFitColumns": true
    }
}
```

**成功响应 (200):**
```json
{
    "success": true,
    "data": {
        "fileName": "export_users_20260416.xlsx",
        "rowCount": 1523,
        "size": 125678
    },
    "timestamp": "2026-04-16T12:00:00Z"
}
```

---

## 10. 错误码详细说明

| 错误码 | HTTP状态码 | 说明 | 解决方案 |
|--------|-----------|------|---------|
| SUCCESS | 200 | 成功 | - |
| VALIDATION_ERROR | 400 | 参数验证错误 | 检查请求参数 |
| CONNECTION_FAILED | 400 | 数据库连接失败 | 检查连接配置 |
| CONNECTION_NOT_FOUND | 404 | 连接不存在 | 确认连接ID |
| QUERY_TIMEOUT | 408 | 查询超时 | 优化SQL或增加超时时间 |
| QUERY_EXECUTION_ERROR | 400 | SQL执行错误 | 检查SQL语法 |
| UNAUTHORIZED | 401 | 未授权 | 登录获取Token |
| FORBIDDEN | 403 | 禁止访问 | 检查权限 |
| RESOURCE_NOT_FOUND | 404 | 资源不存在 | 确认资源存在 |
| INTERNAL_ERROR | 500 | 内部错误 | 联系技术支持 |
| AI_SERVICE_ERROR | 502 | AI服务错误 | 检查AI配置 |
| RATE_LIMIT_EXCEEDED | 429 | 请求过于频繁 | 降低请求频率 |

---

## 11. 限流说明

| 接口类型 | 限制 | 窗口 |
|---------|------|------|
| 普通API | 100请求 | 1分钟 |
| AI接口 | 20请求 | 1分钟 |
| 导出接口 | 10请求 | 1分钟 |

**限流响应 (429):**
```json
{
    "success": false,
    "errorCode": "RATE_LIMIT_EXCEEDED",
    "message": "请求过于频繁，请稍后再试",
    "details": {
        "retryAfter": 30
    },
    "requestId": "req_abc123",
    "timestamp": "2026-04-16T12:00:00Z"
}
```

---

## 12. Swagger/OpenAPI配置

### 12.1 Swagger端点

| 端点 | 说明 |
|-----|------|
| `/swagger` | Swagger UI首页 |
| `/swagger/v1/swagger.json` | OpenAPI 3.0规范 |
| `/swagger/v1/swagger.yaml` | OpenAPI YAML格式 |

### 12.2 Swagger配置

```csharp
// Program.cs
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "DongQiDB Text-to-SQL API",
        Version = "v1",
        Description = "自然语言转SQL数据库查询工具"
    });
    
    // 添加JWT认证
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});
```
