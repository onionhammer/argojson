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

        #endregion
        
        #region Fields

        internal readonly Expression<Action<object, StringWriter>> _expression;

        internal readonly Action<object, StringWriter> _serialize;

        #endregion

        #region Serializers

        static Expression<Action<object, StringWriter>> BuildObjectSerializer(Type owner)
        {
            // Get properties
            var props       = owner.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var parentParam = Expression.Parameter(typeof(object));
            var writerParam = Expression.Parameter(typeof(StringWriter));
            var parentVar   = Expression.Variable(owner);

            ParameterExpression[] declarations = {
                parentVar
            };

            var comma = Expression.Call(writerParam, WriteChar, Expression.Constant(','));

            var expressions = new List<Expression>(capacity: props.Length * 3) {
                // "var parent = ({type})parentObj;"
                Expression.Assign(parentVar, Expression.Convert(parentParam, owner)),
                
                // "{"
                Expression.Call(writerParam, WriteChar, Expression.Constant('{'))
            };

            var isFirst = true;
            for (int i = 0; i < props.Length; ++i)
            {
                var prop         = props[i];
                var propTypeHash = prop.PropertyType.GetHashCode();
                var ignored      = prop.GetCustomAttribute(typeof(JsonIgnoreAttribute));
                
                // This property is ignored
                if (ignored != null || prop.PropertyType == typeof(object)) 
                    continue;

                if (isFirst == false)
                {
                    expressions.Add(comma);
                    isFirst = false;
                }

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
            var parentParam = Expression.Parameter(typeof(object));
            var writerParam = Expression.Parameter(typeof(StringWriter));
            var parentVar   = Expression.Variable(owner);

            var isFirstParam   = Expression.Parameter(typeof(bool));
            var subType        = generic.GetGenericArguments()[0];
            var GetEnumerator  = generic.GetMethod("GetEnumerator");
            var subTypeHash    = subType.GetHashCode();
            var enumeratorType = typeof(IEnumerator<>).MakeGenericType(subType);
            var enumerator     = Expression.Parameter(enumeratorType);

            // Get properties / methods
            var Dispose = Helpers.GetDispose(enumeratorType);

            ParameterExpression[] declarations = {
                parentVar, enumerator, isFirstParam
            };

            var expressions = new List<Expression>(capacity: 15) {
                // "var parent = ({type})parentObj;"
                Expression.Assign(parentVar, Expression.Convert(parentParam, owner)),

                // "var isFirst = false"
                Expression.Assign(isFirstParam, Expression.Constant(true)),
                
                // "["
                Expression.Call(writerParam, WriteChar, Expression.Constant('[')),

                // "var enumerator = parent.GetEnumerator()"
                Expression.Assign(enumerator, Expression.Call(parentVar, GetEnumerator))
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

            // Swap {ienumerator}.Current for value in expression
            var currentExpr = Expression.PropertyOrField(enumerator, "Current");
            var blockBody   = new ChildVisitor(writerParam, currentExpr)
                .Visit(node._expression.Body);

            var checkComma = Expression.IfThen(
                Expression.Not(isFirstParam),
                Expression.Block(
                    comma,                                                       // writer.Write(',');
                    Expression.Assign(isFirstParam, Expression.Constant(false))  // isFirst = false;
                ));

            var ifElse = Expression.IfThenElse(
                Expression.Call(enumerator, MoveNext),                           // if (...)
                Expression.Block(                                                // {
                    checkComma,                                                  //      if (isFirst == false) {{comma}; isFirst = false}
                    blockBody                                                    //      {blockBody}
                ),                                                               // }
                Expression.Break(endLoop)                                        // else break;
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

        static Expression<Action<object, StringWriter>> BuildArraySerializerForList(Type owner, Type generic)
        {
            var parentParam = Expression.Parameter(typeof(object));
            var writerParam = Expression.Parameter(typeof(StringWriter));
            var parentVar   = Expression.Variable(owner);

            var iParam      = Expression.Parameter(typeof(int));
            var subType     = generic.GetGenericArguments()[0];
            var lengthParam = Expression.Parameter(typeof(int));
            var subTypeHash = subType.GetHashCode();
            var getItem     = generic.GetMethod("get_Item");

            ParameterExpression[] declarations = {
                parentVar, iParam, lengthParam
            };

            var expressions = new List<Expression>(capacity: 6) {
                // "var parent = ({type})parentObj;"
                Expression.Assign(parentVar, Expression.Convert(parentParam, owner)),

                // "var i = 0;
                Expression.Assign(iParam, Expression.Constant(0)),

                // "var length = parent.Count";
                Expression.Assign(lengthParam, Expression.Property(parentVar, "Count")),

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

            // Swap {ienumerator}.Current for value in expression
            var arrayAccess = Expression.Call(parentVar, getItem, iParam);
            var blockBody   = new ChildVisitor(writerParam, arrayAccess)
                .Visit(node._expression.Body);

            var checkComma = Expression.IfThen(
                Expression.NotEqual(iParam, Expression.Constant(0)),
                comma);

            var ifStmt = Expression.IfThen(
                Expression.Equal(iParam, lengthParam),                       // if (i == length)
                Expression.Break(endLoop)                                    //     break;
            );

            var loopBody = Expression.Block(
                ifStmt,
                checkComma,                                                  // if (i != 0) {{comma}}
                blockBody,                                                   // {body}
                Expression.Assign(iParam, Expression.Increment(iParam))      // ++i
            );

            expressions.Add(Expression.Loop(loopBody, endLoop, Expression.Label()));

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

        static Expression<Action<object, StringWriter>> BuildArraySerializerForArray(Type owner, Type subType)
        {
            var parentParam = Expression.Parameter(typeof(object));
            var writerParam = Expression.Parameter(typeof(StringWriter));
            var parentVar   = Expression.Variable(owner);

            var iParam      = Expression.Parameter(typeof(int));
            var lengthParam = Expression.Parameter(typeof(int));
            var subTypeHash = subType.GetHashCode();

            ParameterExpression[] declarations = {
                parentVar, iParam, lengthParam
            };

            var expressions = new List<Expression>(capacity: 6) {
                // "var parent = ({type})parentObj;"
                Expression.Assign(parentVar, Expression.Convert(parentParam, owner)),

                // "var i = 0;
                Expression.Assign(iParam, Expression.Constant(0)),

                // "var length = parent.length";
                Expression.Assign(lengthParam, Expression.ArrayLength(parentVar)),

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

            // Swap {ienumerator}.Current for value in expression
            var elementAt   = Expression.ArrayAccess(parentVar, iParam);
            var blockBody   = new ChildVisitor(writerParam, elementAt)
                .Visit(node._expression.Body);

            var checkComma = Expression.IfThen(
                Expression.NotEqual(iParam, Expression.Constant(0)),
                comma);

            var ifStmt = Expression.IfThen(
                Expression.Equal(iParam, lengthParam),                       // if (i == length)
                Expression.Break(endLoop)                                    //     break;
            );

            var loopBody = Expression.Block(
                ifStmt,
                checkComma,                                                  // if (i != 0) {{comma}}
                blockBody,                                                   // {body}
                Expression.Assign(iParam, Expression.Increment(iParam))      // ++i
            );

            expressions.Add(Expression.Loop(loopBody, endLoop, Expression.Label()));

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

        #endregion

        #region Constructor

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
                            _expression = (value, writer) => writer.Write(value == null ? "null" : value as string);
                        else
                            _expression = (value, writer) => writer.Write(value as string);
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
                            writer.Write(value == null ? "null" : "\"" + Helpers.Escape(value as string) + "\"");
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
                        else if (type.IsGenericType && type.IsOfGeneric(typeof(IList<>), out subType))
                            _expression = BuildArraySerializerForList(type, subType);
                        else if (type.IsGenericType && type.IsOfGeneric(typeof(IEnumerable<>), out subType))
                            _expression = BuildArraySerializerForIter(type, subType);
                        else
                            _expression = BuildObjectSerializer(type);
                        break;
                }
            }

            _serialize = Helpers.CompileToType(type, _expression) 
                as Action<object, StringWriter>;
        }

        #endregion
    }
}