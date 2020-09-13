﻿using System;
using SkiaSharp;

namespace Artemis.Core
{
    /// <inheritdoc />
    public class FloatDataBindingConverter : FloatDataBindingConverter<float>
    {
    }

    /// <inheritdoc />
    /// <typeparam name="T">The type of layer property this converter is applied to</typeparam>
    public class FloatDataBindingConverter<T> : DataBindingConverter<T, float>
    {
        /// <summary>
        ///     Creates a new instance of the <see cref="FloatDataBindingConverter{T}" /> class
        /// </summary>
        public FloatDataBindingConverter()
        {
            SupportsSum = true;
            SupportsInterpolate = true;
        }

        /// <inheritdoc />
        public override float Sum(float a, float b)
        {
            return a + b;
        }

        /// <inheritdoc />
        public override float Interpolate(float a, float b, double progress)
        {
            var diff = a - b;
            return (float) (a + diff * progress);
        }

        /// <inheritdoc />
        public override void ApplyValue(float value)
        {
            if (SetExpression == null)
                return;

            if (DataBinding.LayerProperty.PropertyDescription.MaxInputValue is float max)
                value = Math.Min(value, max);
            if (DataBinding.LayerProperty.PropertyDescription.MinInputValue is float min)
                value = Math.Max(value, min);

            var test = new SKSize(5,5);
            test.Width = 10;

            SetExpression(DataBinding.LayerProperty.CurrentValue, value);
        }

        private void Mehtest(ref SKSize test)
        {
            test.Width = 20;
        }

        /// <inheritdoc />
        public override float GetValue()
        {
            return GetExpression(DataBinding.LayerProperty.CurrentValue);
        }
    }
}