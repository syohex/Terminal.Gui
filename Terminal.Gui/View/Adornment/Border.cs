namespace Terminal.Gui;

/// <summary>The Border for a <see cref="View"/>.</summary>
/// <remarks>
///     <para>
///         Renders a border around the view with the <see cref="View.Title"/>. A border using <see cref="LineStyle"/>
///         will be drawn on the sides of <see cref="Thickness"/> that are greater than zero.
///     </para>
///     <para>
///         The <see cref="View.Title"/> of <see cref="Adornment.Parent"/> will be drawn based on the value of
///         <see cref="Thickness.Top"/>:
///     </para>
///     <para>
///         If <c>1</c>:
///         <code>
/// ┌┤1234├──┐
/// │        │
/// └────────┘
/// </code>
///     </para>
///     <para>
///         If <c>2</c>:
///         <code>
///  ┌────┐
/// ┌┤1234├──┐
/// │        │
/// └────────┘
/// </code>
///     </para>
///     <para>
///         If <c>3</c>:
///         <code>
///  ┌────┐
/// ┌┤1234├──┐
/// │└────┘  │
/// │        │
/// └────────┘
/// </code>
///     </para>
///     <para/>
///     <para>See the <see cref="Adornment"/> class.</para>
/// </remarks>
public class Border : Adornment
{
    private LineStyle? _lineStyle;

    /// <inheritdoc/>
    public Border ()
    { /* Do nothing; A parameter-less constructor is required to support all views unit tests. */
    }

    /// <summary>
    ///    The close button for the border. Set to <see cref="Button.Visible"/>, to <see langword="true"/> to enable.
    /// </summary>
    public Button CloseButton { get; internal set; }

    /// <inheritdoc/>
    public Border (View parent) : base (parent)
    {
        /* Do nothing; View.CreateAdornment requires a constructor that takes a parent */
        Parent = parent;
    }

    /// <inheritdoc/>
    public override void BeginInit ()
    {
        base.BeginInit ();

        if (Parent is { })
        {
            CloseButton = new Button ()
            {
                Text = "X", // So it's not visible to not break unit tests
                Y = 0,
                CanFocus = true,
                NoDecorations = false,
                Visible = false,
            };
            //CloseButton.BorderStyle = LineStyle.Single;
            //CloseButton.Border.Thickness = new (1, 0, 1, 0);
            CloseButton.X = Pos.AnchorEnd () - (Pos.Right (CloseButton) - Pos.Left (CloseButton)) + Thickness.Left + 1; // +1 for the border
            Add (CloseButton);
            CloseButton.Accept += (s, e) => {
                                      e.Cancel = Parent.InvokeCommand (Command.QuitToplevel) == true;
                                  };
        }
    }

    /// <summary>
    ///     The color scheme for the Border. If set to <see langword="null"/>, gets the <see cref="Adornment.Parent"/>
    ///     scheme. color scheme.
    /// </summary>
    public override ColorScheme ColorScheme
    {
        get
        {
            if (base.ColorScheme is { })
            {
                return base.ColorScheme;
            }

            return Parent?.ColorScheme;
        }
        set
        {
            base.ColorScheme = value;
            Parent?.SetNeedsDisplay ();
        }
    }

    /// <summary>
    ///     Sets the style of the border by changing the <see cref="Thickness"/>. This is a helper API for setting the
    ///     <see cref="Thickness"/> to <c>(1,1,1,1)</c> and setting the line style of the views that comprise the border. If
    ///     set to <see cref="LineStyle.None"/> no border will be drawn.
    /// </summary>
    public LineStyle LineStyle
    {
        get
        {
            if (_lineStyle.HasValue)
            {
                return _lineStyle.Value;
            }

            // TODO: Make Border.LineStyle inherit from the SuperView hierarchy
            // TODO: Right now, Window and FrameView use CM to set BorderStyle, which negates
            // TODO: all this.
            return Parent.SuperView?.BorderStyle ?? LineStyle.None;
        }
        set => _lineStyle = value;
    }

    /// <inheritdoc/>
    public override void OnDrawContent (Rectangle contentArea)
    {
        base.OnDrawContent (contentArea);

        if (Thickness == Thickness.Empty)
        {
            return;
        }

        //Driver.SetAttribute (Colors.ColorSchemes ["Error"].Normal);
        Rectangle screenBounds = BoundsToScreen (contentArea);

        //OnDrawSubviews (bounds); 

        // TODO: v2 - this will eventually be two controls: "BorderView" and "Label" (for the title)

        // The border adornment (and title) are drawn at the outermost edge of border;
        // For Border
        // ...thickness extends outward (border/title is always as far in as possible)
        // PERF: How about a call to Rectangle.Offset?
        Rectangle borderBounds = new (
                                      screenBounds.X + Math.Max (0, Thickness.Left - 1),
                                      screenBounds.Y + Math.Max (0, Thickness.Top - 1),
                                      Math.Max (
                                                0,
                                                screenBounds.Width
                                                - Math.Max (
                                                            0,
                                                            Math.Max (0, Thickness.Left - 1)
                                                            + Math.Max (0, Thickness.Right - 1)
                                                           )
                                               ),
                                      Math.Max (
                                                0,
                                                screenBounds.Height
                                                - Math.Max (
                                                            0,
                                                            Math.Max (0, Thickness.Top - 1)
                                                            + Math.Max (0, Thickness.Bottom - 1)
                                                           )
                                               )
                                     );

        int topTitleLineY = borderBounds.Y;
        int titleY = borderBounds.Y;
        var titleBarsLength = 0; // the little vertical thingies

        int maxTitleWidth = Math.Max (
                                      0,
                                      Math.Min (
                                                Parent.TitleTextFormatter.FormatAndGetSize ().Width,
                                                Math.Min (screenBounds.Width - 4, borderBounds.Width - 4)
                                               )
                                     );
        Parent.TitleTextFormatter.Size = new (maxTitleWidth, 1);

        int sideLineLength = borderBounds.Height;
        bool canDrawBorder = borderBounds is { Width: > 0, Height: > 0 };

        if (!string.IsNullOrEmpty (Parent?.Title))
        {
            if (Thickness.Top == 2)
            {
                topTitleLineY = borderBounds.Y - 1;
                titleY = topTitleLineY + 1;
                titleBarsLength = 2;
            }

            // ┌────┐
            //┌┘View└
            //│
            if (Thickness.Top == 3)
            {
                topTitleLineY = borderBounds.Y - (Thickness.Top - 1);
                titleY = topTitleLineY + 1;
                titleBarsLength = 3;
                sideLineLength++;
            }

            // ┌────┐
            //┌┘View└
            //│
            if (Thickness.Top > 3)
            {
                topTitleLineY = borderBounds.Y - 2;
                titleY = topTitleLineY + 1;
                titleBarsLength = 3;
                sideLineLength++;
            }
        }

        if (canDrawBorder && Thickness.Top > 0 && maxTitleWidth > 0 && !string.IsNullOrEmpty (Parent?.Title))
        {
            Parent.TitleTextFormatter.Draw (
                                            new (borderBounds.X + 2, titleY, maxTitleWidth, 1),
                                            Parent.HasFocus ? Parent.GetFocusColor () : Parent.GetNormalColor (),
                                            Parent.HasFocus ? Parent.GetFocusColor () : Parent.GetHotNormalColor ());
        }

        if (canDrawBorder && LineStyle != LineStyle.None)
        {
            LineCanvas lc = Parent?.LineCanvas;

            bool drawTop = Thickness.Top > 0 && Frame.Width > 1 && Frame.Height > 1;
            bool drawLeft = Thickness.Left > 0 && (Frame.Height > 1 || Thickness.Top == 0);
            bool drawBottom = Thickness.Bottom > 0 && Frame.Width > 1;
            bool drawRight = Thickness.Right > 0 && (Frame.Height > 1 || Thickness.Top == 0);

            Attribute prevAttr = Driver.GetAttribute ();

            if (ColorScheme is { })
            {
                Driver.SetAttribute (GetNormalColor ());
            }
            else
            {
                Driver.SetAttribute (Parent.GetNormalColor ());
            }

            if (drawTop)
            {
                // ╔╡Title╞═════╗
                // ╔╡╞═════╗
                if (borderBounds.Width < 4 || string.IsNullOrEmpty (Parent?.Title))
                {
                    // ╔╡╞╗ should be ╔══╗
                    lc.AddLine (
                                new (borderBounds.Location.X, titleY),
                                borderBounds.Width,
                                Orientation.Horizontal,
                                LineStyle,
                                Driver.GetAttribute ()
                               );
                }
                else
                {
                    // ┌────┐
                    //┌┘View└
                    //│
                    if (Thickness.Top == 2)
                    {
                        lc.AddLine (
                                    new (borderBounds.X + 1, topTitleLineY),
                                    Math.Min (borderBounds.Width - 2, maxTitleWidth + 2),
                                    Orientation.Horizontal,
                                    LineStyle,
                                    Driver.GetAttribute ()
                                   );
                    }

                    // ┌────┐
                    //┌┘View└
                    //│
                    if (borderBounds.Width >= 4 && Thickness.Top > 2)
                    {
                        lc.AddLine (
                                    new (borderBounds.X + 1, topTitleLineY),
                                    Math.Min (borderBounds.Width - 2, maxTitleWidth + 2),
                                    Orientation.Horizontal,
                                    LineStyle,
                                    Driver.GetAttribute ()
                                   );

                        lc.AddLine (
                                    new (borderBounds.X + 1, topTitleLineY + 2),
                                    Math.Min (borderBounds.Width - 2, maxTitleWidth + 2),
                                    Orientation.Horizontal,
                                    LineStyle,
                                    Driver.GetAttribute ()
                                   );
                    }

                    // ╔╡Title╞═════╗
                    // Add a short horiz line for ╔╡
                    lc.AddLine (
                                new (borderBounds.Location.X, titleY),
                                2,
                                Orientation.Horizontal,
                                LineStyle,
                                Driver.GetAttribute ()
                               );

                    // Add a vert line for ╔╡
                    lc.AddLine (
                                new (borderBounds.X + 1, topTitleLineY),
                                titleBarsLength,
                                Orientation.Vertical,
                                LineStyle.Single,
                                Driver.GetAttribute ()
                               );

                    // Add a vert line for ╞
                    lc.AddLine (
                                new (
                                     borderBounds.X
                                     + 1
                                     + Math.Min (borderBounds.Width - 2, maxTitleWidth + 2)
                                     - 1,
                                     topTitleLineY
                                    ),
                                titleBarsLength,
                                Orientation.Vertical,
                                LineStyle.Single,
                                Driver.GetAttribute ()
                               );

                    // Add the right hand line for ╞═════╗
                    lc.AddLine (
                                new (
                                     borderBounds.X
                                     + 1
                                     + Math.Min (borderBounds.Width - 2, maxTitleWidth + 2)
                                     - 1,
                                     titleY
                                    ),
                                borderBounds.Width - Math.Min (borderBounds.Width - 2, maxTitleWidth + 2),
                                Orientation.Horizontal,
                                LineStyle,
                                Driver.GetAttribute ()
                               );
                }
            }

            if (drawLeft)
            {
                lc.AddLine (
                            new (borderBounds.Location.X, titleY),
                            sideLineLength,
                            Orientation.Vertical,
                            LineStyle,
                            Driver.GetAttribute ()
                           );
            }

            if (drawBottom)
            {
                lc.AddLine (
                            new (borderBounds.X, borderBounds.Y + borderBounds.Height - 1),
                            borderBounds.Width,
                            Orientation.Horizontal,
                            LineStyle,
                            Driver.GetAttribute ()
                           );
            }

            if (drawRight)
            {
                lc.AddLine (
                            new (borderBounds.X + borderBounds.Width - 1, titleY),
                            sideLineLength,
                            Orientation.Vertical,
                            LineStyle,
                            Driver.GetAttribute ()
                           );
            }

            Driver.SetAttribute (prevAttr);

            // TODO: This should be moved to LineCanvas as a new BorderStyle.Ruler
            if ((ConsoleDriver.Diagnostics & ConsoleDriver.DiagnosticFlags.FrameRuler)
                == ConsoleDriver.DiagnosticFlags.FrameRuler)
            {
                // Top
                var hruler = new Ruler { Length = screenBounds.Width, Orientation = Orientation.Horizontal };

                if (drawTop)
                {
                    hruler.Draw (new (screenBounds.X, screenBounds.Y));
                }

                // Redraw title 
                if (drawTop && maxTitleWidth > 0 && !string.IsNullOrEmpty (Parent?.Title))
                {
                    Parent.TitleTextFormatter.Draw (
                                                    new (borderBounds.X + 2, titleY, maxTitleWidth, 1),
                                                    Parent.HasFocus ? Parent.GetFocusColor () : Parent.GetNormalColor (),
                                                    Parent.HasFocus ? Parent.GetFocusColor () : Parent.GetNormalColor ());
                }

                //Left
                var vruler = new Ruler { Length = screenBounds.Height - 2, Orientation = Orientation.Vertical };

                if (drawLeft)
                {
                    vruler.Draw (new (screenBounds.X, screenBounds.Y + 1), 1);
                }

                // Bottom
                if (drawBottom)
                {
                    hruler.Draw (new (screenBounds.X, screenBounds.Y + screenBounds.Height - 1));
                }

                // Right
                if (drawRight)
                {
                    vruler.Draw (new (screenBounds.X + screenBounds.Width - 1, screenBounds.Y + 1), 1);
                }
            }
        }

        //base.OnDrawContent (contentArea);
    }
}
