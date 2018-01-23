// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Dynamic;
using Microsoft.CSharp.RuntimeBinder.Semantics;

namespace Microsoft.CSharp.RuntimeBinder
{
    /// <summary>
    /// Represents a dynamic indexer access in C#, providing the binding semantics and the details about the operation. 
    /// Instances of this class are generated by the C# compiler.
    /// </summary>
    internal sealed class CSharpGetIndexBinder : GetIndexBinder, ICSharpBinder
    {
        public string Name =>  SpecialNames.Indexer;

        public BindingFlag BindingFlags => BindingFlag.BIND_RVALUEREQUIRED;

        public Expr DispatchPayload(RuntimeBinder runtimeBinder, ArgumentObject[] arguments, LocalVariableSymbol[] locals)
        {
            Expr indexerArguments = runtimeBinder.CreateArgumentListEXPR(arguments, locals, 1, arguments.Length);
            return runtimeBinder.BindProperty(this, arguments[0], locals[0], indexerArguments);
        }

        public void PopulateSymbolTableWithName(SymbolTable symbolTable, Type callingType, ArgumentObject[] arguments)
            => symbolTable.PopulateSymbolTableWithName(SpecialNames.Indexer, null, arguments[0].Type);

        public bool IsBinderThatCanHaveRefReceiver => true;

        public Type CallingContext { get; }

        public bool IsChecked => false;

        private readonly CSharpArgumentInfo[] _argumentInfo;

        CSharpArgumentInfo ICSharpBinder.GetArgumentInfo(int index) => _argumentInfo[index];

        private readonly RuntimeBinder _binder;

        /// <summary>
        /// Initializes a new instance of the <see cref="CSharpGetIndexBinder" />.
        /// </summary>
        /// <param name="callingContext">The <see cref="System.Type"/> that indicates where this operation is defined.</param>
        /// <param name="argumentInfo">The sequence of <see cref="CSharpArgumentInfo"/> instances for the arguments to this operation.</param>
        public CSharpGetIndexBinder(
            Type callingContext,
            IEnumerable<CSharpArgumentInfo> argumentInfo) :
            base(BinderHelper.CreateCallInfo(ref argumentInfo, 1)) // discard 1 argument: the target object
        {
            CallingContext = callingContext;
            _argumentInfo = argumentInfo as CSharpArgumentInfo[];
            _binder = RuntimeBinder.GetInstance();
        }

        /// <summary>
        /// Performs the binding of the dynamic get index operation if the target dynamic object cannot bind.
        /// </summary>
        /// <param name="target">The target of the dynamic get index operation.</param>
        /// <param name="indexes">The arguments of the dynamic get index operation.</param>
        /// <param name="errorSuggestion">The binding result to use if binding fails, or null.</param>
        /// <returns>The <see cref="DynamicMetaObject"/> representing the result of the binding.</returns>
        public override DynamicMetaObject FallbackGetIndex(DynamicMetaObject target, DynamicMetaObject[] indexes, DynamicMetaObject errorSuggestion)
        {
#if ENABLECOMBINDER
            DynamicMetaObject com;
            if (!BinderHelper.IsWindowsRuntimeObject(target) && ComBinder.TryBindGetIndex(this, target, indexes, out com))
            {
                return com;
            }
#endif
            BinderHelper.ValidateBindArgument(target, nameof(target));
            BinderHelper.ValidateBindArgument(indexes, nameof(indexes));
            return BinderHelper.Bind(this, _binder, BinderHelper.Cons(target, indexes), _argumentInfo, errorSuggestion);
        }
    }
}
