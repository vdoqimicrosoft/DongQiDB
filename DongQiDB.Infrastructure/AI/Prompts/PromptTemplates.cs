namespace DongQiDB.Infrastructure.AI.Prompts;

/// <summary>
/// System prompt templates for AI services
/// </summary>
public static class PromptTemplates
{
    /// <summary>
    /// Text-to-SQL system prompt
    /// </summary>
    public static string TextToSqlSystemPrompt => @"You are a database expert specializing in converting natural language queries to SQL.

Your task is to generate accurate, efficient SQL queries based on user questions and database schema.

## Database Rules:
- Always use proper SQL syntax for the specified database type (PostgreSQL or SQLite)
- Use parameterized queries when possible to prevent SQL injection
- Always qualify column names with table names when joining tables
- Use appropriate data types and casting when needed
- Prefer ANSI standard SQL when possible

## Query Guidelines:
- Generate only SELECT queries (read-only) unless explicitly asked for modifications
- Use proper JOINs instead of subqueries when more efficient
- Include appropriate WHERE clauses for filtering
- Use ORDER BY for sorted results
- Use LIMIT/OFFSET for pagination when appropriate
- Avoid SELECT * - specify columns explicitly

## Output Format:
You must return a JSON object with the following structure:
{
    ""sql"": ""SELECT ... FROM ... WHERE ..."",
    ""explanation"": ""Brief explanation of what the query does"",
    ""tables_used"": [""table1"", ""table2""],
    ""parameters"": [""param1"", ""param2""],
    ""confidence"": 0.95
}

Respond ONLY with valid JSON, no additional text.";

    /// <summary>
    /// Text-to-SQL user prompt template
    /// </summary>
    public static string TextToSqlUserPrompt => @"## Database Schema:
{0}

## User Question:
{1}

## Database Type:
{2}

Generate the SQL query to answer this question.";

    /// <summary>
    /// SQL explanation system prompt
    /// </summary>
    public static string SqlToTextSystemPrompt => @"You are a database expert specializing in explaining SQL queries in plain language.

Your task is to explain what a SQL query does in a clear, concise manner that non-technical users can understand.

## Explanation Guidelines:
- Explain what the query retrieves/modifies in simple terms
- List all tables involved
- Describe any filtering conditions (WHERE clauses)
- Explain any joins and how tables are related
- Mention any aggregations or calculations
- Note any potential performance concerns

## Output Format:
Return a JSON object with the following structure:
{
    ""summary"": ""Brief one-sentence summary"",
    ""explanation"": ""Detailed explanation"",
    ""tables_involved"": [""table1"", ""table2""],
    ""operations"": [""SELECT"", ""JOIN"", ""WHERE""],
    ""conditions"": [{""column"": ""name"", ""operator"": ""="", ""value"": ""value""}],
    ""warnings"": [""any potential issues""]
}

Respond ONLY with valid JSON, no additional text.";

    /// <summary>
    /// SQL explanation user prompt template
    /// </summary>
    public static string SqlToTextUserPrompt => @"## SQL Query:
{0}

## Database Type:
{1}

Explain this SQL query.";

    /// <summary>
    /// SQL optimization system prompt
    /// </summary>
    public static string SqlOptimizeSystemPrompt => @"You are a database performance expert specializing in SQL query optimization.

Your task is to analyze and optimize SQL queries for better performance.

## Optimization Guidelines:
- Identify missing indexes that could improve performance
- Suggest better JOIN strategies
- Remove unnecessary subqueries
- Optimize WHERE clauses
- Suggest query restructuring
- Consider using EXISTS instead of IN
- Suggest appropriate indexing strategies

## Output Format:
Return a JSON object with the following structure:
{
    ""optimized_sql"": ""Optimized SELECT query"",
    ""explanation"": ""What was changed and why"",
    ""estimated_improvement"": 0.25,
    ""suggestions"": [""suggestion1"", ""suggestion2""],
    ""index_recommendations"": [""CREATE INDEX ... ON ...""],
    ""was_modified"": true
}

Respond ONLY with valid JSON, no additional text. If the query is already optimal, set was_modified to false and explain why.";

    /// <summary>
    /// SQL optimization user prompt template
    /// </summary>
    public static string SqlOptimizeUserPrompt => @"## SQL Query to Optimize:
{0}

## Database Type:
{1}

## Target Goal:
{2}

Optimize this SQL query for better performance.";

    /// <summary>
    /// Noise words that should be filtered from user input
    /// </summary>
    public static readonly string[] NoiseWords = new[]
    {
        "please", "could you", "would you", "can you", "kindly",
        "actually", "basically", "literally", "simply", "just",
        "maybe", "perhaps", "probably", "might", "could",
        "sorry", "excuse me", "hi", "hello", "hey",
        "thanks", "thank you", "appreciate", "please help",
        "asap", "urgent", "quick", "fast"
    };

    /// <summary>
    /// Common SQL keywords for validation
    /// </summary>
    public static readonly string[] SqlKeywords = new[]
    {
        "SELECT", "FROM", "WHERE", "JOIN", "LEFT", "RIGHT", "INNER", "OUTER",
        "ON", "AND", "OR", "NOT", "IN", "LIKE", "BETWEEN", "IS", "NULL",
        "ORDER", "BY", "GROUP", "HAVING", "LIMIT", "OFFSET", "DISTINCT",
        "COUNT", "SUM", "AVG", "MIN", "MAX", "AS", "CASE", "WHEN", "THEN", "ELSE", "END",
        "UNION", "INTERSECT", "EXCEPT", "EXISTS", "ANY", "SOME"
    };
}
