﻿using System.Collections.Generic;
using System.Linq;
using FigmaSharp;
using FigmaSharp.Models;
using Svg;

namespace FigmaAltseed.Converter
{
	internal record SvgFileInfo(SvgDocument Document, string FilePath);

	internal class JsonToSvg
	{
		public IEnumerable<SvgFileInfo> ExtractSvgImages(FigmaCanvas figma)
		{
			// ルール： Fill または Strokeの存在する, Textではないノードのみを描画する
			// Figma上で非表示のものは描画しない
			// Fill が Image のものは描画しない

			return figma.children.SelectMany(ExtractSvgImages);
		}

		private IEnumerable<SvgFileInfo> ExtractSvgImages(FigmaNode pivot)
		{
			var result = pivot.GetChildren<FigmaNode>()
				.SelectMany(ExtractSvgImages);

			if (!pivot.visible)
			{
				return result;
			}

			if (pivot is FigmaFrame frame && RenderFrame2(frame) is {} svg1)
			{
				result = result.Append(new SvgFileInfo(svg1, pivot.GetImageAssetPath()));
			}
			else if (pivot is FigmaVector vector && RenderVector2(vector) is {} svg2)
			{
				result = result.Append(new SvgFileInfo(svg2, pivot.GetImageAssetPath()));
			}

			return result;
		}

		private SvgDocument? RenderVector2(FigmaVector node)
		{
			if (node is FigmaText)
			{
				return null;
			}

			var (doc, rect) = CreateRectangleSvg(node.absoluteBoundingBox);
			var (_, updated1) = ApplyFill(rect, node.fills);
			var (_, updated2) = ApplyStroke(rect, node.strokes, node.strokeWeight);

			if (node is RectangleVector rv)
			{
				ApplyCornerRadius(rect, rv.cornerRadius);
			}

			if (updated1 || updated2)
			{
				return doc;
			}

			return null;
		}

		private SvgDocument? RenderFrame2(FigmaFrame node)
		{
			var (doc, rect) = CreateRectangleSvg(node.absoluteBoundingBox);
			var (_, updated1) = ApplyFill(rect, node.fills);
			var (_, updated2) = ApplyStroke(rect, node.strokes, node.strokeWeight);
			ApplyCornerRadius(rect, node.cornerRadius);

			if (updated1 || updated2)
			{
				return doc;
			}

			return null;
		}

		private (SvgDocument document, SvgRectangle rectangle) CreateRectangleSvg(Rectangle bound)
		{
			var doc = new SvgDocument()
			{
				X = new SvgUnit(SvgUnitType.Pixel, 0),
				Y = new SvgUnit(SvgUnitType.Pixel, 0),
				Width = new SvgUnit(SvgUnitType.Pixel, bound.Width),
				Height = new SvgUnit(SvgUnitType.Pixel, bound.Height),
			};

			var rectangle = new SvgRectangle()
			{
				X = new SvgUnit(SvgUnitType.Pixel, 0),
				Y = new SvgUnit(SvgUnitType.Pixel, 0),
				Width = new SvgUnit(SvgUnitType.Pixel, bound.Width),
				Height = new SvgUnit(SvgUnitType.Pixel, bound.Height),
			};

			doc.Children.Add(rectangle);

			return (doc, rectangle);
		}

		private (SvgRectangle self, bool updated) ApplyFill(SvgRectangle rectangle, FigmaPaint[] paints)
		{
			if (!paints.Any())
			{
				return (rectangle, false);
			}

			var fill = paints[0];

			if (fill.type == "IMAGE")
			{
				return (rectangle, false);
			}

			rectangle.Fill = new SvgColourServer(fill.color.ToDotNet());

			return (rectangle, true);
		}

		private (SvgRectangle self, bool updated) ApplyStroke(SvgRectangle rectangle, FigmaPaint[] color, int width)
		{
			if (!color.Any())
			{
				return (rectangle, false);
			}

			var stroke = color[0];

			rectangle.Stroke = new SvgColourServer(stroke.color.ToDotNet());
			rectangle.StrokeWidth = new SvgUnit(SvgUnitType.Pixel, width);

			// ストロークの太さの分だけ調整
			var wpx = new SvgUnit(SvgUnitType.Pixel, width);

			rectangle.X += wpx / 2;
			rectangle.Y += wpx / 2;
			rectangle.OwnerDocument.Width += wpx;
			rectangle.OwnerDocument.Height += wpx;

			return (rectangle, true);
		}

		private void ApplyCornerRadius(SvgRectangle rectangle, float cornerRadius)
		{
			rectangle.CornerRadiusX = cornerRadius;
			rectangle.CornerRadiusY = cornerRadius;
		}
	}
}
