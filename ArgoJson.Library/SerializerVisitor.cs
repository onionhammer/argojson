using System.Linq.Expressions;

namespace ArgoJson
{
    internal class SerializerVisitor : ExpressionVisitor
    {
        private ParameterExpression _writer;
        private Expression _value;

        public SerializerVisitor(ParameterExpression writer, Expression value)
        {
            _writer = writer;
            _value  = value;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            switch (node.Type.Name)
            {
                case "StringWriter":
                    return _writer;

                case "Object":
                    return _value;
            }

            return base.VisitParameter(node);
        }
    }
}
