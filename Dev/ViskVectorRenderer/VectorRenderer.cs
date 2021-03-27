﻿using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using FigmaVisk.Capability;
using Svg;
using Visklusa.Abstraction.Notation;

namespace ViskVectorRenderer
{
	internal class VectorRenderer
	{
		public IEnumerable<RenderResult> Run(Element[] elements)
		{
			foreach (var element in elements)
			{
				if (element.GetCapability<Paint>() is {} paint)
				{
					yield return RenderRoundedRectangle(element, paint);
				}
				else
				{
					yield return new SkipRenderResult(element);
				}
			}
		}

		private RenderResult RenderRoundedRectangle(Element element, Paint paint)
		{
			if (element.GetCapability<BoundingBox>() is not {} bound
				|| (paint.Fill == Fill.Blank && paint.Stroke == Stroke.Blank))
			{
				var caps = element.Capabilities.ToList();
				caps.Remove(paint);
				var elm = new Element(element.Id, caps.ToArray());
				return new SkipRenderResult(elm);
			}

			var doc = new SvgDocument()
			{
				X = Pixel(0), Y = Pixel(0), Width = Pixel(bound.Width), Height = Pixel(bound.Height)
			};

			var rectangle = new SvgRectangle()
			{
				X = Pixel(0), Y = Pixel(0), Width = Pixel(bound.Width), Height = Pixel(bound.Height),
				Fill = new SvgColourServer(GetColor(paint.Fill)),
				Stroke = new SvgColourServer(GetColor(paint.Stroke)),
				StrokeWidth = Pixel(paint.Stroke.Weight),
			};

			if (element.GetCapability<RoundedRectangle>() is {} rounded)
			{
				rectangle.CornerRadiusX = Pixel(rounded.LeftBottom);
				rectangle.CornerRadiusY = Pixel(rounded.LeftBottom);
			}

			doc.Children.Add(rectangle);

			var newCaps = element.Capabilities
				.Append(new FigmaVisk.Capability.Image($"rendered_{element.Id}"))
				.ToArray();
			var newElement = element with {Capabilities = newCaps };

			return new SuccessRenderResult(newElement, doc);
		}

		private SvgUnit Pixel(float value)
		{
			return new SvgUnit(SvgUnitType.Pixel, value);
		}

		private Color GetColor(Fill f)
		{
			var (r, g, b, a) = f;
			var (a1, r1, g1, b1) = (a, r, g, b).Map(x => (int)(x * 255));
			return Color.FromArgb(a1, r1, g1, b1);
		}

		private Color GetColor(Stroke s)
		{
			var (r, g, b, a, w) = s;
			var (a1, r1, g1, b1) = (a, r, g, b).Map(x => (int) (x * 255));
			return Color.FromArgb(a1, r1, g1, b1);
		}
	}
}
