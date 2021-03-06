﻿using System;
using System.Windows;
using System.Windows.Media;
using Artemis.Core;
using Artemis.Core.Services;
using Artemis.UI.Services.Interfaces;
using Artemis.UI.Shared.Services;
using SkiaSharp;
using SkiaSharp.Views.WPF;

namespace Artemis.UI.Services
{
    public class LayerEditorService : ILayerEditorService
    {
        private readonly ISettingsService _settingsService;

        public LayerEditorService(ISettingsService settingsService, IProfileEditorService profileEditorService)
        {
            _settingsService = settingsService;
        }

        /// <inheritdoc />
        public Rect GetLayerBounds(Layer layer)
        {
            // Adjust the render rectangle for the difference in render scale
            double renderScale = _settingsService.GetSetting("Core.RenderScale", 0.5).Value;
            return new Rect(
                layer.Bounds.Left / renderScale * 1,
                layer.Bounds.Top / renderScale * 1,
                Math.Max(0, layer.Bounds.Width / renderScale * 1),
                Math.Max(0, layer.Bounds.Height / renderScale * 1)
            );
        }

        /// <inheritdoc />
        public Point GetLayerAnchorPosition(Layer layer, SKPoint? positionOverride = null)
        {
            SKRect layerBounds = GetLayerBounds(layer).ToSKRect();
            SKPoint positionProperty = layer.Transform.Position.CurrentValue;
            if (positionOverride != null)
                positionProperty = positionOverride.Value;

            // Start at the center of the shape
            Point position = new Point(layerBounds.MidX, layerBounds.MidY);

            // Apply translation
            position.X += positionProperty.X * layerBounds.Width;
            position.Y += positionProperty.Y * layerBounds.Height;

            return position;
        }

        /// <inheritdoc />
        public TransformGroup GetLayerTransformGroup(Layer layer)
        {
            SKRect layerBounds = GetLayerBounds(layer).ToSKRect();

            // Apply transformation like done by the core during layer rendering.
            // The order in which translations are applied are different because the UI renders the shape inside
            // the layer using the structure of the XAML while the Core has to deal with that by applying the layer
            // position to the translation
            Point anchorPosition = GetLayerAnchorPosition(layer);
            SKPoint anchorProperty = layer.Transform.AnchorPoint.CurrentValue;

            // Translation originates from the unscaled center of the shape and is tied to the anchor
            double x = anchorPosition.X - layerBounds.MidX - anchorProperty.X * layerBounds.Width;
            double y = anchorPosition.Y - layerBounds.MidY - anchorProperty.Y * layerBounds.Height;

            TransformGroup transformGroup = new TransformGroup();
            transformGroup.Children.Add(new TranslateTransform(x, y));
            transformGroup.Children.Add(new ScaleTransform(layer.Transform.Scale.CurrentValue.Width / 100f, layer.Transform.Scale.CurrentValue.Height / 100f, anchorPosition.X, anchorPosition.Y));
            transformGroup.Children.Add(new RotateTransform(layer.Transform.Rotation.CurrentValue, anchorPosition.X, anchorPosition.Y));

            return transformGroup;
        }

        /// <inheritdoc />
        public SKPath GetLayerPath(Layer layer, bool includeTranslation, bool includeScale, bool includeRotation, SKPoint? anchorOverride = null)
        {
            SKRect layerBounds = GetLayerBounds(layer).ToSKRect();

            // Apply transformation like done by the core during layer rendering (same differences apply as in GetLayerTransformGroup)
            SKPoint anchorPosition = GetLayerAnchorPosition(layer).ToSKPoint();
            if (anchorOverride != null)
                anchorPosition = anchorOverride.Value;

            SKPoint anchorProperty = layer.Transform.AnchorPoint.CurrentValue;

            // Translation originates from the unscaled center of the shape and is tied to the anchor
            float x = anchorPosition.X - layerBounds.MidX - anchorProperty.X * layerBounds.Width;
            float y = anchorPosition.Y - layerBounds.MidY - anchorProperty.Y * layerBounds.Height;

            SKPath path = new SKPath();
            path.AddRect(layerBounds);
            if (includeTranslation)
                path.Transform(SKMatrix.MakeTranslation(x, y));
            if (includeScale)
                path.Transform(SKMatrix.MakeScale(layer.Transform.Scale.CurrentValue.Width / 100f, layer.Transform.Scale.CurrentValue.Height / 100f, anchorPosition.X, anchorPosition.Y));
            if (includeRotation)
                path.Transform(SKMatrix.MakeRotationDegrees(layer.Transform.Rotation.CurrentValue, anchorPosition.X, anchorPosition.Y));

            return path;
        }

        /// <inheritdoc />
        public SKPoint GetScaledPoint(Layer layer, SKPoint point, bool absolute)
        {
            double renderScale = _settingsService.GetSetting("Core.RenderScale", 0.5).Value;
            if (absolute)
            {
                return new SKPoint(
                    100f / layer.Bounds.Width * ((float) (point.X * renderScale) - layer.Bounds.Left) / 100f,
                    100f / layer.Bounds.Height * ((float) (point.Y * renderScale) - layer.Bounds.Top) / 100f
                );
            }

            return new SKPoint(
                100f / layer.Bounds.Width * (float) (point.X * renderScale) / 100f,
                100f / layer.Bounds.Height * (float) (point.Y * renderScale) / 100f
            );
        }

        public SKPoint GetDragOffset(Layer layer, SKPoint dragStart)
        {
            // Figure out what the top left will be if the shape moves to the current cursor position
            SKPoint scaledDragStart = GetScaledPoint(layer, dragStart, true);
            SKPoint tempAnchor = GetLayerAnchorPosition(layer, scaledDragStart).ToSKPoint();
            SKPoint tempTopLeft = GetLayerPath(layer, true, true, true, tempAnchor)[0];

            // Get the shape's position
            SKPoint topLeft = GetLayerPath(layer, true, true, true)[0];

            // The difference between the two is the offset
            return topLeft - tempTopLeft;
        }
    }
}