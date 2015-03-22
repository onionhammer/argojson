using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace ArgoJson
{
    internal struct SerializerNode
    {
        #region Delegates

        static MethodInfo WriteString = typeof(TextWriter)
            .GetMethod("Write", new[] { typeof(string) });

        static MethodInfo WriteChar = typeof(TextWriter)
            .GetMethod("Write", new[] { typeof(char) });

        static MethodInfo MoveNext = typeof(IEnumerator)
            .GetMethod("MoveNext");

        #endregion
        
        #region Fields

        private static readonly AssemblyBuilder _assemblyBuilder;

        private static readonly ModuleBuilder _assemblyModule;

        private static readonly Dictionary<Type, SerializerNode> _types;

        private readonly Expression<Action<object, TextWriter>> _expression;

        public readonly Action<object, TextWriter> _serialize;
        
        #endregion

        #region Methods

        public static void GetHandler(Type type, out SerializerNode node)
        {
            // Attempt to find handling type
            if (_types.TryGetValue(type, out node) == false)
            {
                // Create a new handler for this type as it is not recognized
                node = new SerializerNode(type);

                //Add new handler for this type
                _types.Add(type, node);
            }
        }

        #endregion

        #region Serializers

        static Expression<Action<object, TextWriter>> BuildObjectSerializer(Type owner)
        {
            // Get properties
            var props       = owner.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var parentParam = Expression.Parameter(typeof(object));
            var writerParam = Expression.Parameter(typeof(TextWriter));
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
                var prop     = props[i];
                var propType = prop.PropertyType;
                var ignored  = prop.GetCustomAttribute(typeof(JsonIgnoreAttribute));
                
                // This property is ignored
                if (ignored != null || propType == typeof(object)) 
                    continue;

                if (isFirst == false)
                    expressions.Add(comma);
                else
                    isFirst = false;

                // Attempt to find handling type
                SerializerNode node;
                GetHandler(propType, out node);

                // Append serializer to expression
                expressions.Add(Expression.Call(writerParam, WriteString,
                    Expression.Constant("\"" + prop.Name + "\":")));

                var getter = Expression.PropertyOrField(parentVar, prop.Name);
                
                // Visit node expression
                var blockBody = new SerializerVisitor(writerParam, Expression.Convert(getter, typeof(object)))
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

            return Expression.Lambda<Action<object, TextWriter>>(
                body, parentParam, writerParam
            );
        }

        /// <summary>
        /// Produces an expression tree to serialize the input array type
        /// </summary>
        /// <param name="type">The array type being serialized</param>
        /// <param name="subType">The type of objects held within the array</param>
        /// <param name="getLength">An expression to retrieve the length of the array</param>
        /// <param name="getItem">An expresion to get the item from an array</param>
        static Expression<Action<object, TextWriter>> BuildArraySerializer(Type type, Type subType, 
            Func<Expression, Expression> getLength,
            Func<Expression, Expression, Expression> getItem)
        {
            var parentParam = Expression.Parameter(typeof(object));
            var writerParam = Expression.Parameter(typeof(TextWriter));
            var parentVar   = Expression.Variable(type);

            var iParam      = Expression.Parameter(typeof(int));
            var lengthParam = Expression.Parameter(typeof(int));

            ParameterExpression[] declarations = {
                parentVar, iParam, lengthParam
            };

            var expressions = new List<Expression>(capacity: 6) {
                // "var parent = ({type})parentObj;"
                Expression.Assign(parentVar, Expression.Convert(parentParam, type)),

                // "var i = 0;
                Expression.Assign(iParam, Expression.Constant(0)),

                // "var length = parent.Length";
                Expression.Assign(lengthParam, getLength(parentVar)),

                // "["
                Expression.Call(writerParam, WriteChar, Expression.Constant('['))
            };

            var comma = Expression.Call(writerParam, WriteChar, Expression.Constant(','));

            // Attempt to find handling type
            SerializerNode node;
            GetHandler(subType, out node);

            // Iterate through all members and add them
            var endLoop = Expression.Label();

            // Swap parent[i] for 'value' in expression
            var blockBody = new SerializerVisitor(writerParam, getItem(parentVar, iParam))
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

            return Expression.Lambda<Action<object, TextWriter>>(
                body,
                parentParam, writerParam
            );
        }

        static Expression<Action<object, TextWriter>> BuildArraySerializerForIter(Type type, Type generic)
        {
            var parentParam = Expression.Parameter(typeof(object));
            var writerParam = Expression.Parameter(typeof(TextWriter));
            var parentVar   = Expression.Variable(type);

            var isFirstParam   = Expression.Parameter(typeof(bool));
            var subType        = generic.GetGenericArguments()[0];
            var GetEnumerator  = generic.GetMethod("GetEnumerator");
            var enumeratorType = typeof(IEnumerator<>).MakeGenericType(subType);
            var enumerator     = Expression.Parameter(enumeratorType);

            // Get properties / methods
            var Dispose = Helpers.GetDispose(enumeratorType);

            ParameterExpression[] declarations = {
                parentVar, enumerator, isFirstParam
            };

            var expressions = new List<Expression>(capacity: 15) {
                // "var parent = ({type})parentObj;"
                Expression.Assign(parentVar, Expression.Convert(parentParam, type)),

                // "var isFirst = false"
                Expression.Assign(isFirstParam, Expression.Constant(true)),
                
                // "["
                Expression.Call(writerParam, WriteChar, Expression.Constant('[')),

                // "var enumerator = parent.GetEnumerator()"
                Expression.Assign(enumerator, Expression.Call(parentVar, GetEnumerator))
            };

            var comma = Expression.Call(writerParam, WriteChar, Expression.Constant(','));

            // Attempt to find handling type
            SerializerNode node;
            GetHandler(subType, out node);

            // Iterate through all members and add them
            var endLoop = Expression.Label();

            // Swap {ienumerator}.Current for value in expression
            var currentExpr = Expression.PropertyOrField(enumerator, "Current");
            var blockBody   = new SerializerVisitor(writerParam, currentExpr)
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

            return Expression.Lambda<Action<object, TextWriter>>(
                body, 
                parentParam, writerParam
            );
        }

        /// <summary>
        /// Produces an expression tree to serialize the input list type
        /// </summary>
        /// <param name="type">The array type being serialized</param>
        /// <param name="generic">The generic item type of the array</param>
        static Expression<Action<object, TextWriter>> BuildArraySerializerForList(Type type, Type generic)
        {
            var subType = generic.GetGenericArguments()[0];
            var getItem = generic.GetMethod("get_Item");

            return BuildArraySerializer(
                type, subType,
                (parent) => Expression.Property(parent, "Count"),
                (parent, i) => Expression.Call(parent, getItem, i)
            );
        }

        /// <summary>
        /// Produces an expression tree to serialize the input array type
        /// </summary>
        /// <param name="type">The array type being serialized</param>
        /// <param name="subType">The type of objects held within the array</param>
        static Expression<Action<object, TextWriter>> BuildArraySerializerForArray(Type type, Type subType)
        {
            return BuildArraySerializer(
                type, subType,
                (parent) => Expression.ArrayLength(parent),
                (parent, i) => Expression.ArrayAccess(parent, i)
            );
        }

        #endregion

        #region Constructor

        static SerializerNode()
        {
            _assemblyModule = Helpers.CreateModule(out _assemblyBuilder);
            
            // Initialize several basic types
            _types = new Dictionary<Type, SerializerNode>(capacity: 16)
            {
                { typeof(int),      new SerializerNode(typeof(int)) },
                { typeof(bool),     new SerializerNode(typeof(bool)) },
                { typeof(float),    new SerializerNode(typeof(float)) },
                { typeof(string),   new SerializerNode(typeof(string)) },
                { typeof(Guid),     new SerializerNode(typeof(Guid)) },
                { typeof(DateTime), new SerializerNode(typeof(DateTime)) },
            };
        }

        internal SerializerNode(Type type)
        {
            bool nullable = false;

            reprocess:

            if (type.IsPrimitive)
            {
                if (nullable)
                    _expression = (value, writer) => writer.Write(value == null ? "null" : value.ToString());
                else
                    _expression = (value, writer) => writer.Write(value.ToString());
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

            _serialize = _assemblyModule.CompileToType(type, _expression)
                as Action<object, TextWriter>;
        }

        #endregion
    }
}