﻿// This code is a C# port from python library Terminal Text Effects  https://github.com/ChrisBuilds/terminaltexteffects/

namespace Terminal.Gui;

using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Describes the pattern that a <see cref="Gradient"/> results in e.g. <see cref="Vertical"/>, <see cref="Horizontal"/> etc
/// </summary>
public enum GradientDirection
{
    /// <summary>
    /// Color varies along Y axis but is constant on X axis.
    /// </summary>
    Vertical,

    /// <summary>
    /// Color varies along X axis but is constant on Y axis.
    /// </summary>
    Horizontal,


    /// <summary>
    /// Color varies by distance from center (i.e. in circular ripples)
    /// </summary>
    Radial,

    /// <summary>
    /// Color varies by X and Y axis (i.e. a slanted gradient)
    /// </summary>
    Diagonal
}

/// <summary>
/// Describes
/// </summary>
public class Gradient
{
    public List<Color> Spectrum { get; private set; }
    private readonly bool _loop;
    private readonly List<Color> _stops;
    private readonly List<int> _steps;


    public Gradient (IEnumerable<Color> stops, IEnumerable<int> steps, bool loop = false)
    {
        _stops = stops.ToList ();
        if (_stops.Count < 1)
            throw new ArgumentException ("At least one color stop must be provided.");

        _steps = steps.ToList ();
        if (_steps.Any (step => step < 1))
            throw new ArgumentException ("Steps must be greater than 0.");

        _loop = loop;
        Spectrum = GenerateGradient (_steps);
    }

    /// <summary>
    /// Returns the color to use at the given part of the spectrum
    /// </summary>
    /// <param name="fraction">Proportion of the way through the spectrum, must be between 
    /// 0 and 1 (inclusive).  Returns the last color if <paramref name="fraction"/> is
    /// <see cref="double.NaN"/>.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public Color GetColorAtFraction (double fraction)
    {
        if (double.IsNaN (fraction))
        {
            return Spectrum.Last ();
        }

        if (fraction < 0 || fraction > 1)
        {
            throw new ArgumentOutOfRangeException (nameof (fraction), "Fraction must be between 0 and 1.");
        }

        int index = (int)(fraction * (Spectrum.Count - 1));
        return Spectrum [index];
    }

    private List<Color> GenerateGradient (IEnumerable<int> steps)
    {
        List<Color> gradient = new List<Color> ();
        if (_stops.Count == 1)
        {
            for (int i = 0; i < steps.Sum (); i++)
            {
                gradient.Add (_stops [0]);
            }
            return gradient;
        }

        if (_loop)
        {
            _stops.Add (_stops [0]);
        }

        var colorPairs = _stops.Zip (_stops.Skip (1), (start, end) => new { start, end });
        var stepsList = _steps.ToList ();

        foreach (var (colorPair, thesteps) in colorPairs.Zip (stepsList, (pair, step) => (pair, step)))
        {
            gradient.AddRange (InterpolateColors (colorPair.start, colorPair.end, thesteps));
        }

        return gradient;
    }

    private IEnumerable<Color> InterpolateColors (Color start, Color end, int steps)
    {
        for (int step = 0; step <= steps; step++)
        {
            double fraction = (double)step / steps;
            int r = (int)(start.R + fraction * (end.R - start.R));
            int g = (int)(start.G + fraction * (end.G - start.G));
            int b = (int)(start.B + fraction * (end.B - start.B));
            yield return new Color (r, g, b);
        }
    }

    /// <summary>
    /// <para>
    /// Creates a mapping starting at 0,0 and going to <paramref name="maxRow"/> and <paramref name="maxColumn"/>
    /// (inclusively) using the supplied <paramref name="direction"/>.
    /// </para>
    /// <para>
    /// Note that this method is inclusive i.e. passing 1/1 results in 4 mapped coordinates.
    /// </para>
    /// </summary>
    /// <param name="maxRow"></param>
    /// <param name="maxColumn"></param>
    /// <param name="direction"></param>
    /// <returns></returns>
    public Dictionary<Point, Color> BuildCoordinateColorMapping (int maxRow, int maxColumn, GradientDirection direction)
    {
        var gradientMapping = new Dictionary<Point, Color> ();

        switch (direction)
        {
            case GradientDirection.Vertical:
                for (int row = 0; row <= maxRow; row++)
                {
                    double fraction = maxRow == 0 ? 1.0 : (double)row / maxRow;
                    Color color = GetColorAtFraction (fraction);
                    for (int col = 0; col <= maxColumn; col++)
                    {
                        gradientMapping [new Point (col, row)] = color;
                    }
                }
                break;

            case GradientDirection.Horizontal:
                for (int col = 0; col <= maxColumn; col++)
                {
                    double fraction = maxColumn == 0 ? 1.0 : (double)col / maxColumn;
                    Color color = GetColorAtFraction (fraction);
                    for (int row = 0; row <= maxRow; row++)
                    {
                        gradientMapping [new Point (col, row)] = color;
                    }
                }
                break;

            case GradientDirection.Radial:
                for (int row = 0; row <= maxRow; row++)
                {
                    for (int col = 0; col <= maxColumn; col++)
                    {
                        double distanceFromCenter = FindNormalizedDistanceFromCenter (maxRow, maxColumn, new Point (col, row));
                        Color color = GetColorAtFraction (distanceFromCenter);
                        gradientMapping [new Point (col, row)] = color;
                    }
                }
                break;

            case GradientDirection.Diagonal:
                for (int row = 0; row <= maxRow; row++)
                {
                    for (int col = 0; col <= maxColumn; col++)
                    {
                        double fraction = ((double)row * 2 + col) / (maxRow * 2 + maxColumn);
                        Color color = GetColorAtFraction (fraction);
                        gradientMapping [new Point (col, row)] = color;
                    }
                }
                break;
        }

        return gradientMapping;
    }

    private double FindNormalizedDistanceFromCenter (int maxRow, int maxColumn, Point coord)
    {
        double centerX = maxColumn / 2.0;
        double centerY = maxRow / 2.0;
        double dx = coord.X - centerX;
        double dy = coord.Y - centerY;
        double distance = Math.Sqrt (dx * dx + dy * dy);
        double maxDistance = Math.Sqrt (centerX * centerX + centerY * centerY);
        return distance / maxDistance;
    }
}