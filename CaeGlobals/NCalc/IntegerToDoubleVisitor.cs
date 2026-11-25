// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Globalization;
using NCalc.Domain;


namespace CaeGlobals
{
    // NCalc keeps a number written without a decimal point as an Int32, so integer arithmetic silently overflows,
    // 210000*20000 = -94967296, and a number too large for an Int32 is kept as a Single. Visiting a parsed
    // equation turns every such number into a double
    public class IntegerToDoubleVisitor : LogicalExpressionVisitor
    {
        // Methods
        public override void Visit(LogicalExpression expression)
        {
        }
        public override void Visit(TernaryExpression expression)
        {
            expression.LeftExpression.Accept(this);
            expression.MiddleExpression.Accept(this);
            expression.RightExpression.Accept(this);
        }
        public override void Visit(BinaryExpression expression)
        {
            expression.LeftExpression.Accept(this);
            expression.RightExpression.Accept(this);
        }
        public override void Visit(UnaryExpression expression)
        {
            expression.Expression.Accept(this);
        }
        public override void Visit(ValueExpression expression)
        {
            if (expression.Value is float single)
            {
                // The shortest text of a Single gives back the number as it was written when it had at most
                // seven significant digits: 2100000000000 is kept as 2100000063488, its text is 2.1E+12
                expression.Value = double.Parse(single.ToString("R", CultureInfo.InvariantCulture),
                                                CultureInfo.InvariantCulture);
                expression.Type = NCalc.Domain.ValueType.Float;
            }
            else if (expression.Value is int || expression.Value is long)
            {
                expression.Value = Convert.ToDouble(expression.Value);
                expression.Type = NCalc.Domain.ValueType.Float;
            }
        }
        public override void Visit(Function function)
        {
            foreach (LogicalExpression argument in function.Expressions) argument.Accept(this);
        }
        public override void Visit(Identifier identifier)
        {
        }
    }
}
