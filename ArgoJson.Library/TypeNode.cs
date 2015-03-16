using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;

namespace ArgoJson
{
    internal struct TypeNode
    {
        #region Delegates

        static MethodInfo WriteString = typeof(StringWriter)
            .GetMethod("Write", new[] { typeof(string) });

        static MethodInfo WriteChar = typeof(StringWriter)
            .GetMethod("Write", new[] { typeof(char) });

        static MethodInfo MoveNext = typeof(IEnumerator)
            .GetMethod("MoveNext");

        internal delegate void SerializeValue(object value, StringWriter writer);

        #endregion
        
        #region Fields

        internal readonly Expression<Action<object, StringWriter>> _expression;

        internal readonly Action<object, StringWriter> _serialize;

        #endregion
        
        static Expression<Action<object, StringWriter>> BuildObjectSerializer(Type owner)
        {
            // Get properties
            var props       = owner.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var parentParam = Expression.Parameter(typeof(object));
            var writerParam = Expression.Parameter(typeof(StringWriter));
            var parentVar   = Expression.Variable(owner);

            var declarations = new List<ParameterExpression>(capacity: props.Length) {
                parentVar
            };

            var comma = Expression.Call(writerParam, WriteChar, Expression.Constant(','));

            var expressions = new List<Expression>(capacity: props.Length + 1) {
                // "var parent = ({type})parentObj;"
                Expression.Assign(parentVar, Expression.Convert(parentParam, owner)),
                
                // "{"
                Expression.Call(writerParam, WriteChar, Expression.Constant('{'))
            };

            for (int i = 0, commaAt = 0; i < props.Length; ++i)
            {
                var prop         = props[i];
                var propTypeHash = prop.PropertyType.GetHashCode();
                var ignored      = prop.GetCustomAttribute(typeof(JsonIgnoreAttribute));
                
                // This property is ignored
                if (ignored != null) 
                    continue;

                if (commaAt++ > 0)
                    expressions.Add(comma);

                // Attempt to find handling type
                TypeNode node;
                if (Serializer._types.TryGetValue(propTypeHash, out node) == false)
                {
                    // Create a new handler for this type as it is not recognized
                    node = new TypeNode(prop.PropertyType);

                    //Add new handler for this type
                    Serializer._types.Add(propTypeHash, node);
                }

                // Append serializer to expression
                expressions.Add(Expression.Call(writerParam, WriteString,
                    Expression.Constant("\"" + prop.Name + "\":")));

                var getter = Expression.PropertyOrField(parentVar, prop.Name);
                
                // Visit node expression
                var blockBody = new ChildVisitor(writerParam, Expression.Convert(getter, typeof(object)))
                    .Visit(node._expression.Body);

                // Appends new block
                expressions.Add(blockBody);
            }
            
            // "}"
            expressions.Add(
                Expression.Call(writerParam, WriteChar, Expression.Constant('}'))
            );

            // Build body of lambda
            var body = Expression.Block(
                declarations,
                expressions
            );

            return Expression.Lambda<Action<object, StringWriter>>(
                body, parentParam, writerParam
            );
        }

        static Expression<Action<object, StringWriter>> BuildArraySerializerForIter(Type owner, Type generic)
        {
            var parentParam    = Expression.Parameter(typeof(object));
            var writerParam    = Expression.Parameter(typeof(StringWriter));
            var isFirstParam   = Expression.Parameter(typeof(bool));
            var parentVar      = Expression.Variable(owner);
            var subType        = generic.GetGenericArguments()[0];
            var subTypeHash    = subType.GetHashCode();
            var enumeratorType = typeof(IEnumerator<>).MakeGenericType(subType);
            var enumerator     = Expression.Parameter(enumeratorType);

            // Get properties / methods
            var GetEnumerator = generic.GetMethod("GetEnumerator");
            var Dispose       = Helpers.GetDispose(enumeratorType);

            var declarations = new List<ParameterExpression>(capacity: 20) {
                parentVar, enumerator, isFirstParam
            };

            var expressions = new List<Expression>(capacity: 20) {
                // "var parent = ({type})parentObj;"
                Expression.Assign(parentVar, Expression.Convert(parentParam, owner)),

                // "var isFirst = false"
                Expression.Assign(isFirstParam, Expression.Constant(true)),
                
                // "["
                Expression.Call(writerParam, WriteChar, Expression.Constant('['))
            };

            var comma = Expression.Call(writerParam, WriteChar, Expression.Constant(','));

            // Attempt to find handling type
            TypeNode node;
            if (Serializer._types.TryGetValue(subTypeHash, out node) == false)
            {
                // Create a new handler for this type as it is not recognized
                node = new TypeNode(subType);

                //Add new handler for this type
                Serializer._types.Add(subTypeHash, node);
            }

            // Iterate through all members and add them
            var endLoop = Expression.Label();
            expressions.Add(
                Expression.Assign(enumerator, Expression.Call(parentVar, GetEnumerator))
            );

            // {ienumerator}.Current
            var getter = Expression.PropertyOrField(enumerator, "Current");

            // Visit node expression
            var blockBody = new ChildVisitor(writerParam, Expression.Convert(getter, typeof(object)))
                .Visit(node._expression.Body);

            var checkComma = Expression.IfThen(
                Expression.Equal(isFirstParam, Expression.Constant(false)), 
                comma);

            var ifElse = Expression.IfThenElse(
                Expression.Call(enumerator, MoveNext),                           // if (...)
                Expression.Block(                                                // {
                    checkComma,                                                  //      if (isFirst == false) {comma}
                    Expression.Assign(isFirstParam, Expression.Constant(false)), //      isFirst = false;
                    blockBody                                                    //      {blockBody}
                ),                                                               // }
                Expression.Goto(endLoop)                                         // else break;
            );

            expressions.Add(Expression.Loop(ifElse, endLoop, Expression.Label()));
            expressions.Add(Expression.Call(enumerator, Dispose));

            // "]"
            expressions.Add(
                Expression.Call(writerParam, WriteChar, Expression.Constant(']'))
            );

            // Build body of lambda
            var body = Expression.Block(
                declarations,
                expressions
            );

            return Expression.Lambda<Action<object, StringWriter>>(
                body, 
                parentParam, writerParam
            );
        }

        //private static Expression<Action<object, StringWriter>> BuildArraySerializerForCollection(Type type, Type subType)
        //{
        //    // Build body of lambda
        //    var body = Expression.Block(
        //        declarations,
        //        expressions
        //    );

        //    return Expression.Lambda<Action<object, StringWriter>>(
        //        body,
        //        parentParam, writerParam
        //    );
        //}

        private static Expression<Action<object, StringWriter>> BuildArraySerializerForArray(Type owner, Type subType)
        {
            var parentParam    = Expression.Parameter(typeof(object));
            var writerParam    = Expression.Parameter(typeof(StringWriter));
            var parentVar      = Expression.Variable(owner);

            var declarations = new List<ParameterExpression>(capacity: 20) {
                parentVar
            };

            var expressions = new List<Expression>(capacity: 20) {
            };

            // Build body of lambda
            var body = Expression.Block(
                declarations,
                expressions
            );

            return Expression.Lambda<Action<object, StringWriter>>(
                body,
                parentParam, writerParam
            );
        }

        internal TypeNode(Type type)
        {
            bool nullable = false;

            reprocess:

            if (type.IsPrimitive)
            {
                switch (type.Name)
                {
                    case "Boolean":
                        if (nullable)
                            _expression = (value, writer) => writer.Write(value == null ? "null" : (bool)value ? "true" : "false");
                        else
                            _expression = (value, writer) => writer.Write((bool)value);
                        break;

                    default:
                        if (nullable)
                            _expression = (value, writer) => writer.Write(value == null ? "null" : value);
                        else
                            _expression = (value, writer) => writer.Write(value);
                        break;
                }
            }
            else
            {
                switch (type.Name)
                {
                    case "Nullable`1":
                        type     = Nullable.GetUnderlyingType(type);
                        nullable = true;
                        goto reprocess;

                    case "String":
                        _expression = (value, writer) =>
                            writer.Write(value == null ? "null" : "\"" + Helpers.Escape(value.ToString()) + "\"");
                        break;

                    case "DateTime":
                        if (nullable)
                            _expression = (value, writer) =>
                                writer.Write(value == null ? "null" : "\"" + ((DateTime)value).ToString("yyyy-MM-ddTHH:mm:ssK") + "\"");
                        else
                            _expression = (value, writer) =>
                                writer.Write("\"" + ((DateTime)value).ToString("yyyy-MM-ddTHH:mm:ssK") + "\"");
                        break;

                    case "Guid":
                        if (nullable)
                            _expression = (value, writer) => 
                                writer.Write(value == null ? "null" : "\"" + value + "\"");
                        else
                            _expression = (value, writer) =>
                                writer.Write("\"" + value + "\"");
                        break;

                    default:
                        Type subType;
                        if (type.IsArray)
                            _expression = BuildArraySerializerForArray(type, type.GetElementType());
                        //if (type.IsGenericType && type.IsOfGeneric(typeof(ICollection<>), out subType))
                        //    _expression = BuildArraySerializerForCollection(type, subType);
                        if (type.IsGenericType && type.IsOfGeneric(typeof(IEnumerable<>), out subType))
                            _expression = BuildArraySerializerForIter(type, subType);
                        else
                            _expression = BuildObjectSerializer(type);
                        break;
                }
            }
            
            _serialize = _expression.Compile();
        }
    }
}