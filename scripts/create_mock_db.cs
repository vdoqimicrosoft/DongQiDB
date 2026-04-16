using Microsoft.Data.Sqlite;
using System;
using System.IO;

var dbPath = Path.Combine("D:/data", "testdb.db");
if (File.Exists(dbPath)) File.Delete(dbPath);

var connectionString = $"Data Source={dbPath}";

using var conn = new SqliteConnection(connectionString);
conn.Open();

var createTables = @"
CREATE TABLE users (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL,
    email TEXT NOT NULL UNIQUE,
    age INTEGER,
    city TEXT,
    created_at TEXT DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE orders (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    user_id INTEGER NOT NULL,
    product TEXT NOT NULL,
    amount REAL NOT NULL,
    status TEXT DEFAULT 'pending',
    created_at TEXT DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (user_id) REFERENCES users(id)
);

CREATE TABLE products (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL,
    category TEXT,
    price REAL NOT NULL,
    stock INTEGER DEFAULT 0
);

CREATE TABLE categories (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL UNIQUE,
    description TEXT
);
";

using (var cmd = new SqliteCommand(createTables, conn))
{
    cmd.ExecuteNonQuery();
}

var cities = new[] { "北京", "上海", "深圳", "广州", "杭州", "成都", "武汉", "西安", "南京", "重庆" };
var statuses = new[] { "pending", "completed", "cancelled", "shipped" };
var categories = new[] { ("电子产品", "各类电子设备"), ("服装", "男女服装"), ("食品", "各类食品"), ("图书", "图书音像"), ("家居", "家居用品") };

var random = new Random(42);

// 插入 categories
foreach (var (name, desc) in categories)
{
    using var cmd = new SqliteCommand($"INSERT INTO categories (name, description) VALUES ('{name}', '{desc}')", conn);
    cmd.ExecuteNonQuery();
}

// 插入 products
var products = new[] {
    ("iPhone 15", "电子产品", 7999.0, 100),
    ("MacBook Pro", "电子产品", 14999.0, 50),
    ("AirPods Pro", "电子产品", 1999.0, 200),
    ("T恤", "服装", 199.0, 500),
    ("牛仔裤", "服装", 399.0, 300),
    ("连衣裙", "服装", 599.0, 150),
    ("有机苹果", "食品", 29.9, 1000),
    ("矿泉水", "食品", 2.5, 5000),
    ("《C#高级编程》", "图书", 199.0, 200),
    ("《算法导论》", "图书", 159.0, 100),
    ("办公桌", "家居", 899.0, 50),
    ("人体工学椅", "家居", 1299.0, 80),
};
foreach (var (name, cat, price, stock) in products)
{
    using var cmd = new SqliteCommand($"INSERT INTO products (name, category, price, stock) VALUES ('{name}', '{cat}', {price}, {stock})", conn);
    cmd.ExecuteNonQuery();
}

// 插入 users
var names = new[] {
    "张三", "李四", "王五", "赵六", "孙七", "周八", "吴九", "郑十",
    "王小明", "李小红", "张伟", "刘洋", "陈静", "杨帆", "黄磊", "林志玲",
    "刘德华", "周杰伦", "张学友", "王菲", "邓超", "黄晓明", "孙俪", "胡歌"
};
var emails = new List<string>();
foreach (var name in names)
{
    var email = $"{name.ToLower().Replace(" ", "")}@example.com";
    emails.Add(email);
    var age = random.Next(18, 65);
    var city = cities[random.Next(cities.Length)];
    using var cmd = new SqliteCommand($"INSERT INTO users (name, email, age, city) VALUES ('{name}', '{email}', {age}, '{city}')", conn);
    cmd.ExecuteNonQuery();
}

// 插入 orders
for (int i = 0; i < 100; i++)
{
    var userId = random.Next(1, names.Length + 1);
    var product = products[random.Next(products.Length)];
    var amount = product.Item3 * random.Next(1, 5);
    var status = statuses[random.Next(statuses.Length)];
    using var cmd = new SqliteCommand($"INSERT INTO orders (user_id, product, amount, status) VALUES ({userId}, '{product.Item1}', {amount}, '{status}')", conn);
    cmd.ExecuteNonQuery();
}

Console.WriteLine("Mock data created successfully!");
Console.WriteLine($"Users: {names.Length}");
Console.WriteLine($"Products: {products.Length}");
Console.WriteLine($"Orders: 100");
Console.WriteLine($"Categories: {categories.Length}");
