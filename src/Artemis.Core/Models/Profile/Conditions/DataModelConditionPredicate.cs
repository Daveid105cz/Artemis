﻿using System;
using Artemis.Core.DataModelExpansions;
using Artemis.Storage.Entities.Profile.Abstract;
using Artemis.Storage.Entities.Profile.Conditions;
using Newtonsoft.Json;

namespace Artemis.Core
{
    /// <summary>
    ///     A predicate in a data model condition using either two data model values or one data model value and a
    ///     static value
    /// </summary>
    public class DataModelConditionPredicate : DataModelConditionPart
    {
        /// <summary>
        ///     Creates a new instance of the <see cref="DataModelConditionPredicate" /> class
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="predicateType"></param>
        public DataModelConditionPredicate(DataModelConditionPart parent, ProfileRightSideType predicateType)
        {
            Parent = parent;
            PredicateType = predicateType;
            Entity = new DataModelConditionPredicateEntity();

            Initialize();
        }

        internal DataModelConditionPredicate(DataModelConditionPart parent, DataModelConditionPredicateEntity entity)
        {
            Parent = parent;
            Entity = entity;
            PredicateType = (ProfileRightSideType) entity.PredicateType;

            Initialize();
        }

        /// <summary>
        ///     Gets or sets the predicate type
        /// </summary>
        public ProfileRightSideType PredicateType { get; set; }

        /// <summary>
        ///     Gets the operator
        /// </summary>
        public ConditionOperator? Operator { get; private set; }

        /// <summary>
        ///     Gets the path of the left property
        /// </summary>
        public DataModelPath? LeftPath { get; private set; }

        /// <summary>
        ///     Gets the path of the right property
        /// </summary>
        public DataModelPath? RightPath { get; private set; }

        /// <summary>
        ///     Gets the right static value, only used it <see cref="PredicateType" /> is
        ///     <see cref="ProfileRightSideType.Static" />
        /// </summary>
        public object? RightStaticValue { get; private set; }

        internal DataModelConditionPredicateEntity Entity { get; set; }

        /// <summary>
        ///     Updates the left side of the predicate
        /// </summary>
        /// <param name="path">The path pointing to the left side value inside the data model</param>
        public void UpdateLeftSide(DataModelPath? path)
        {
            if (path != null && !path.IsValid)
                throw new ArtemisCoreException("Cannot update left side of predicate to an invalid path");

            LeftPath?.Dispose();
            LeftPath = path != null ? new DataModelPath(path) : null;

            ValidateOperator();
            ValidateRightSide();
        }

        /// <summary>
        ///     Updates the right side of the predicate, makes the predicate dynamic and re-compiles the expression
        /// </summary>
        /// <param name="path">The path pointing to the right side value inside the data model</param>
        public void UpdateRightSideDynamic(DataModelPath? path)
        {
            if (path != null && !path.IsValid)
                throw new ArtemisCoreException("Cannot update right side of predicate to an invalid path");

            RightPath?.Dispose();
            RightPath = path != null ? new DataModelPath(path) : null;

            PredicateType = ProfileRightSideType.Dynamic;
        }

        /// <summary>
        ///     Updates the right side of the predicate, makes the predicate static and re-compiles the expression
        /// </summary>
        /// <param name="staticValue">The right side value to use</param>
        public void UpdateRightSideStatic(object? staticValue)
        {
            PredicateType = ProfileRightSideType.Static;
            RightPath?.Dispose();
            RightPath = null;

            // If the left side is empty simply apply the value, any validation will wait
            if (LeftPath == null || !LeftPath.IsValid)
            {
                RightStaticValue = staticValue;
                return;
            }

            // If the left path is valid we can expect a type
            Type leftSideType = LeftPath.GetPropertyType()!;

            // If not null ensure the types match and if not, convert it
            if (staticValue != null && staticValue.GetType() == leftSideType)
                RightStaticValue = staticValue;
            else if (staticValue != null)
                RightStaticValue = Convert.ChangeType(staticValue, leftSideType);
            // If null create a default instance for value types or simply make it null for reference types
            else if (leftSideType.IsValueType)
                RightStaticValue = Activator.CreateInstance(leftSideType);
            else
                RightStaticValue = null;
        }

        /// <summary>
        ///     Updates the operator of the predicate and re-compiles the expression
        /// </summary>
        /// <param name="conditionOperator"></param>
        public void UpdateOperator(ConditionOperator? conditionOperator)
        {
            // Calling CreateExpression will clear compiled expressions
            if (conditionOperator == null)
            {
                Operator = null;
                return;
            }

            // No need to clear compiled expressions, without a left data model they are already null
            if (LeftPath == null || !LeftPath.IsValid)
            {
                Operator = conditionOperator;
                return;
            }

            Type leftType = LeftPath.GetPropertyType()!;
            if (!conditionOperator.SupportsType(leftType))
                throw new ArtemisCoreException($"Cannot apply operator {conditionOperator.GetType().Name} to this predicate because " +
                                               $"it does not support left side type {leftType.Name}");

            Operator = conditionOperator;
        }

        /// <inheritdoc />
        public override bool Evaluate()
        {
            if (Operator == null || LeftPath == null || !LeftPath.IsValid)
                return false;

            // Compare with a static value
            if (PredicateType == ProfileRightSideType.Static)
            {
                object? leftSideValue = LeftPath.GetValue();
                if (leftSideValue != null && leftSideValue.GetType().IsValueType && RightStaticValue == null)
                    return false;

                return Operator.Evaluate(leftSideValue, RightStaticValue);
            }

            if (RightPath == null || !RightPath.IsValid)
                return false;

            // Compare with dynamic values
            return Operator.Evaluate(LeftPath.GetValue(), RightPath.GetValue());
        }

        /// <inheritdoc />
        public override string ToString()
        {
            if (PredicateType == ProfileRightSideType.Dynamic)
                return $"[Dynamic] {LeftPath} {Operator.Description} {RightPath}";
            return $"[Static] {LeftPath} {Operator.Description} {RightStaticValue}";
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            ConditionOperatorStore.ConditionOperatorAdded -= ConditionOperatorStoreOnConditionOperatorAdded;
            ConditionOperatorStore.ConditionOperatorRemoved -= ConditionOperatorStoreOnConditionOperatorRemoved;

            LeftPath?.Dispose();
            RightPath?.Dispose();

            base.Dispose(disposing);
        }

        /// <inheritdoc />
        internal override bool EvaluateObject(object target)
        {
            return false;
        }

        internal override void Save()
        {
            // Don't save an invalid state
            if (LeftPath != null && !LeftPath.IsValid || RightPath != null && !RightPath.IsValid)
                return;

            Entity.PredicateType = (int) PredicateType;

            LeftPath?.Save();
            Entity.LeftPath = LeftPath?.Entity;
            RightPath?.Save();
            Entity.RightPath = RightPath?.Entity;

            Entity.RightStaticValue = JsonConvert.SerializeObject(RightStaticValue);

            if (Operator != null)
            {
                Entity.OperatorPluginGuid = Operator.PluginInfo.Guid;
                Entity.OperatorType = Operator.GetType().Name;
            }
        }

        internal void Initialize()
        {
            ConditionOperatorStore.ConditionOperatorAdded += ConditionOperatorStoreOnConditionOperatorAdded;
            ConditionOperatorStore.ConditionOperatorRemoved += ConditionOperatorStoreOnConditionOperatorRemoved;

            // Left side
            if (Entity.LeftPath != null)
                LeftPath = new DataModelPath(null, Entity.LeftPath);

            // Operator
            if (Entity.OperatorPluginGuid != null)
            {
                ConditionOperator? conditionOperator = ConditionOperatorStore.Get(Entity.OperatorPluginGuid.Value, Entity.OperatorType)?.ConditionOperator;
                if (conditionOperator != null)
                    UpdateOperator(conditionOperator);
            }

            // Right side dynamic
            if (PredicateType == ProfileRightSideType.Dynamic && Entity.RightPath != null)
                RightPath = new DataModelPath(null, Entity.RightPath);
            // Right side static
            else if (PredicateType == ProfileRightSideType.Static && Entity.RightStaticValue != null)
                try
                {
                    if (LeftPath != null && LeftPath.IsValid)
                    {
                        // Use the left side type so JSON.NET has a better idea what to do
                        Type leftSideType = LeftPath.GetPropertyType()!;
                        object? rightSideValue;

                        try
                        {
                            rightSideValue = JsonConvert.DeserializeObject(Entity.RightStaticValue, leftSideType);
                        }
                        // If deserialization fails, use the type's default
                        catch (JsonSerializationException e)
                        {
                            DeserializationLogger.LogPredicateDeserializationFailure(this, e);
                            rightSideValue = Activator.CreateInstance(leftSideType);
                        }

                        UpdateRightSideStatic(rightSideValue);
                    }
                    else
                    {
                        // Hope for the best...
                        UpdateRightSideStatic(JsonConvert.DeserializeObject(Entity.RightStaticValue));
                    }
                }
                catch (JsonReaderException)
                {
                    // ignored
                    // TODO: Some logging would be nice
                }
        }

        internal override DataModelConditionPartEntity GetEntity()
        {
            return Entity;
        }

        private void ValidateOperator()
        {
            if (LeftPath == null || !LeftPath.IsValid || Operator == null)
                return;

            Type leftType = LeftPath.GetPropertyType()!;
            if (!Operator.SupportsType(leftType))
                Operator = null;
        }

        private void ValidateRightSide()
        {
            Type? leftType = LeftPath?.GetPropertyType();
            if (PredicateType == ProfileRightSideType.Dynamic)
            {
                if (RightPath == null || !RightPath.IsValid)
                    return;

                Type rightSideType = RightPath.GetPropertyType()!;
                if (leftType != null && !leftType.IsCastableFrom(rightSideType))
                    UpdateRightSideDynamic(null);
            }
            else
            {
                if (RightStaticValue != null && (leftType == null || leftType.IsCastableFrom(RightStaticValue.GetType())))
                    UpdateRightSideStatic(RightStaticValue);
                else
                    UpdateRightSideStatic(null);
            }
        }

        #region Event handlers

        private void ConditionOperatorStoreOnConditionOperatorAdded(object? sender, ConditionOperatorStoreEvent e)
        {
            ConditionOperator conditionOperator = e.Registration.ConditionOperator;
            if (Entity.OperatorPluginGuid == conditionOperator.PluginInfo.Guid && Entity.OperatorType == conditionOperator.GetType().Name)
                UpdateOperator(conditionOperator);
        }

        private void ConditionOperatorStoreOnConditionOperatorRemoved(object? sender, ConditionOperatorStoreEvent e)
        {
            if (e.Registration.ConditionOperator != Operator)
                return;

            Operator = null;
        }

        #endregion
    }
}