# Swagger API 使用教程

## 访问地址
```
http://localhost:5000/swagger
```

---

## 1. 认证 (Authentication)

### 1.1 获取 Token

在使用其他API之前，需要先登录获取JWT Token。

**接口**: `POST /api/v1/auth/login`

**请求体**:
```json
{
  "username": "admin",
  "password": "admin123"
}
```

**响应**:
```json
{
  "isSuccess": true,
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIs...",
    "expiresAt": "2026-04-16T15:00:00Z"
  }
}
```

**在Swagger中操作**:
1. 展开 `Auth` -> `POST /api/v1/auth/login`
2. 点击 "Try it out"
3. 在 Request body 中填入 JSON
4. 点击 "Execute"
5. 复制返回的 `accessToken`

---

## 2. 配置 Authorization Header

获取Token后，需要在Swagger中配置认证：

1. 点击页面顶部的 **Authorize** 按钮 (🔓)
2. 在弹窗中填入: `Bearer {accessToken}`
3. 点击 "Authorize" -> "Close"

---

## 3. 连接管理 (Connections)

### 3.1 创建连接

**接口**: `POST /api/v1/connections`

```json
{
  "name": "MyDatabase",
  "host": "D:/data/testdb.db",
  "database": "D:/data/testdb.db",
  "databaseType": 4,
  "port": 0,
  "username": "",
  "password": ""
}
```

> **databaseType**: 1=MySQL, 2=PostgreSQL, 3=SQLServer, 4=SQLite

**响应**:
```json
{
  "isSuccess": true,
  "data": {
    "id": 1,
    "name": "MyDatabase",
    "databaseType": 4
  }
}
```

### 3.2 获取连接列表

**接口**: `GET /api/v1/connections`

直接点击 "Execute" 即可，返回所有连接。

### 3.3 删除连接

**接口**: `DELETE /api/v1/connections/{id}`

将 `{id}` 替换为连接ID，点击 Execute。

---

## 4. 查询执行 (Query Execute)

### 4.1 执行SQL查询

**接口**: `POST /api/v1/query/execute`

```json
{
  "connectionId": 1,
  "sql": "SELECT * FROM users LIMIT 10",
  "page": 1,
  "pageSize": 10
}
```

**响应示例**:
```json
{
  "isSuccess": true,
  "data": {
    "success": true,
    "result": {
      "columns": [
        {"name": "id", "dataType": "Int64"},
        {"name": "name", "dataType": "String"},
        {"name": "email", "dataType": "String"}
      ],
      "rows": [
        {"id": 1, "name": "张三", "email": "zhangsan@example.com"},
        {"id": 2, "name": "李四", "email": "lisi@example.com"}
      ],
      "rowCount": 2
    }
  }
}
```

---

## 5. AI Text-to-SQL (自然语言转SQL)

这是核心功能！用自然语言描述需求，AI生成SQL。

### 5.1 Text-to-SQL

**接口**: `POST /api/v1/ai/text-to-sql`

```json
{
  "connectionId": 1,
  "userQuestion": "查询所有用户"
}
```

**示例问题**:
| 自然语言 | 说明 |
|---------|------|
| `list all users` | 查询所有用户 |
| `count users by city` | 按城市统计用户数量 |
| `find expensive products` | 查找高价产品 |
| `show pending orders` | 显示待处理订单 |
| `统计每个订单状态的金额` | 按状态统计订单金额 |

**响应**:
```json
{
  "isSuccess": true,
  "data": {
    "sqlQuery": "SELECT id, name, email, age, city FROM users",
    "explanation": "Retrieves all users with their basic information",
    "tablesUsed": ["users"],
    "confidence": 0.95
  }
}
```

### 5.2 执行生成的SQL

将 `sqlQuery` 字段中的SQL复制到 Query Execute 接口执行。

### 5.3 SQL转自然语言

**接口**: `POST /api/v1/ai/sql-to-text`

```json
{
  "sqlQuery": "SELECT * FROM users WHERE age > 18",
  "databaseType": "sqlite"
}
```

### 5.4 SQL优化

**接口**: `POST /api/v1/ai/sql-optimize`

```json
{
  "sqlQuery": "SELECT * FROM users u, orders o WHERE u.id = o.user_id",
  "databaseType": "sqlite"
}
```

---

## 6. AI 会话管理

### 6.1 创建会话

**接口**: `POST /api/v1/ai/sessions`

```json
{
  "title": "用户分析",
  "connectionId": 1,
  "databaseType": "sqlite"
}
```

### 6.2 获取会话列表

**接口**: `GET /api/v1/ai/sessions`

### 6.3 获取会话消息

**接口**: `GET /api/v1/ai/sessions/{id}/messages`

---

## 7. 数据导出

### 7.1 导出CSV

**接口**: `POST /api/v1/export/csv`

```json
{
  "connectionId": 1,
  "sql": "SELECT * FROM users"
}
```

**响应**: 返回CSV文件下载

### 7.2 导出Excel

**接口**: `POST /api/v1/export/excel`

```json
{
  "connectionId": 1,
  "sql": "SELECT * FROM users"
}
```

**响应**: 返回Excel文件下载

---

## 8. Schema 查询

### 8.1 获取所有表

**接口**: `GET /api/v1/schema/{connId}/tables`

### 8.2 获取完整Schema

**接口**: `GET /api/v1/schema/{connId}`

---

## 9. 健康检查

### 9.1 健康状态

**接口**: `GET /healthz`

无需认证，直接访问。

---

## 10. 常用示例

### 示例1: 查询并导出
1. `POST /api/v1/ai/text-to-sql` -> 输入 "查询所有订单"
2. 复制返回的 `sqlQuery`
3. `POST /api/v1/query/execute` -> 验证SQL正确
4. `POST /api/v1/export/csv` -> 导出结果

### 示例2: 数据分析
1. `POST /api/v1/ai/text-to-sql` -> 输入 "按城市统计用户数量"
2. `POST /api/v1/query/execute` -> 填入生成的SQL
3. 查看分析结果

### 示例3: 多表关联
1. `POST /api/v1/ai/text-to-sql` -> 输入 "查询用户及其所有订单"
2. AI会自动生成JOIN查询

---

## 11. 数据库连接配置

### SQLite
```json
{
  "name": "SQLite数据库",
  "host": "D:/data/testdb.db",
  "database": "D:/data/testdb.db",
  "databaseType": 4
}
```

### PostgreSQL
```json
{
  "name": "PostgreSQL",
  "host": "localhost",
  "port": 5432,
  "database": "mydb",
  "username": "postgres",
  "password": "your_password",
  "databaseType": 2
}
```

---

## 12. 错误处理

响应格式:
```json
{
  "isSuccess": false,
  "errorCode": 4001,
  "errorMessage": "CREATE statements are not allowed"
}
```

### 常见错误码
| 错误码 | 说明 |
|--------|------|
| 1006 | 验证失败 |
| 4001 | SQL验证失败 |
| 2001 | 数据库类型不支持 |
| 2002 | AI服务错误 |

---

## 13. 限流说明

- 限制: 100请求/分钟
- 超过限制返回: 429 Too Many Requests

---

## 14. 测试数据库

已创建测试数据库: `D:/data/testdb.db`

**表结构**:
```sql
users (id, name, email, age, city, created_at)
orders (id, user_id, product, amount, status, created_at)
products (id, name, category, price, stock)
categories (id, name, description)
```

**测试查询**:
- `SELECT * FROM users`
- `SELECT * FROM orders`
- `SELECT city, COUNT(*) FROM users GROUP BY city`
