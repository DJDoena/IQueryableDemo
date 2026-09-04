using System.Linq.Expressions;
using System.Text;

namespace QueryableTest.SqlQueryable;

// Fictional example of how a real IQueryProvider (e.g. EF Core) would take
// the same expression tree that CarQueryProvider.Evaluate interprets
// in-memory, and instead translate it into a SQL WHERE clause fragment.
// This class doesn't actually run against a database - it just demonstrates
// where/how the ExpressionVisitor pattern turns predicate expressions into
// SQL text, including mapping the ConsoleColor enum down to the int column
// value a real "Cars" table would store.
internal class CarSqlExpressionVisitor : ExpressionVisitor
{
    private readonly StringBuilder _sql = new();

    public static string Translate(Expression predicateBody)
    {
        var visitor = new CarSqlExpressionVisitor();

        visitor.Visit(predicateBody);

        var sql = visitor._sql.ToString();

        return sql;
    }

    protected override Expression VisitBinary(BinaryExpression node)
    {
        _sql.Append('(');

        this.Visit(node.Left);

        var sqlOperator = node.NodeType switch
        {
            ExpressionType.Equal => "=",
            ExpressionType.NotEqual => "<>",
            ExpressionType.GreaterThan => ">",
            ExpressionType.GreaterThanOrEqual => ">=",
            ExpressionType.LessThan => "<",
            ExpressionType.LessThanOrEqual => "<=",
            ExpressionType.AndAlso => "AND",
            ExpressionType.OrElse => "OR",
            _ => throw new NotSupportedException($"Binary operator '{node.NodeType}' is not supported."),
        };

        _sql.Append($" {sqlOperator} ");

        this.Visit(node.Right);

        _sql.Append(')');

        return node;
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        // node.Member.Name maps directly to the column name in our fictional
        // "Cars" table (e.g. Color -> [Color], Doors -> [Doors]).
        _sql.Append($"[{node.Member.Name}]");

        return node;
    }

    protected override Expression VisitConstant(ConstantExpression node)
    {
        // This is where the enum -> int translation happens. A real "Cars"
        // table has no idea what a ConsoleColor is, only the integer value
        // that was stored for it, so ConsoleColor.Red becomes just "4".
        if (node.Value is Enum enumValue)
        {
            var underlyingValue = Convert.ChangeType(enumValue, enumValue.GetTypeCode());

            _sql.Append(underlyingValue);
        }
        else if (node.Value is string stringValue)
        {
            _sql.Append($"'{stringValue}'");
        }
        else
        {
            _sql.Append(node.Value);
        }

        return node;
    }
}
