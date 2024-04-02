//
// Button.cs: Button control
//
// Authors:
//   Miguel de Icaza (miguel@gnome.org)
//

namespace Terminal.Gui;

/// <summary>Button is a <see cref="View"/> that provides an item that invokes raises the <see cref="View.Accept"/> event.</summary>
/// <remarks>
///     <para>
///         Provides a button showing text that raises the <see cref="View.Accept"/> event when clicked on with a mouse or
///         when the user presses SPACE, ENTER, or the <see cref="View.HotKey"/>. The hot key is the first letter or digit
///         following the first underscore ('_') in the button text.
///     </para>
///     <para>Use <see cref="View.HotKeySpecifier"/> to change the hot key specifier from the default of ('_').</para>
///     <para>
///         When the button is configured as the default (<see cref="IsDefault"/>) and the user presses the ENTER key, if
///         no other <see cref="View"/> processes the key, the <see cref="Button"/>'s <see cref="View.Accept"/> event will
///         be fired.
///     </para>
/// </remarks>
public class Button : View
{
    private readonly Rune _leftBracket;
    private readonly Rune _leftDefault;
    private readonly Rune _rightBracket;
    private readonly Rune _rightDefault;
    private bool _isDefault;

    /// <summary>Initializes a new instance of <see cref="Button"/> using <see cref="LayoutStyle.Computed"/> layout.</summary>
    /// <remarks>The width of the <see cref="Button"/> is computed based on the text length. The height will always be 1.</remarks>
    public Button ()
    {
        TextAlignment = TextAlignment.Centered;
        VerticalTextAlignment = VerticalTextAlignment.Middle;

        _leftBracket = Glyphs.LeftBracket;
        _rightBracket = Glyphs.RightBracket;
        _leftDefault = Glyphs.LeftDefaultIndicator;
        _rightDefault = Glyphs.RightDefaultIndicator;

        // Ensures a height of 1 if AutoSize is set to false
        Height = 1;

        CanFocus = true;
        AutoSize = true;

        // Override default behavior of View
        AddCommand (Command.HotKey, () =>
                                     {
                                         SetFocus ();
                                         return !OnAccept ();
                                     });

        KeyBindings.Add (Key.Space, Command.HotKey);
        KeyBindings.Add (Key.Enter, Command.HotKey);

        TitleChanged += Button_TitleChanged;
        MouseClick += Button_MouseClick;
        InvertColorsOnPress = true;
    }

    [CanBeNull]
    private ColorScheme _savedColorScheme;

    private void Button_MouseEvent (object sender, MouseEventEventArgs e)
    {
        // Default behavior is to invoke Accept (via HotKey) on clicked.
        if (!WantContinuousButtonPressed &&
            Application.MouseGrabView != this &&
            e.MouseEvent.Flags.HasFlag (MouseFlags.Button1Clicked))
        {
            e.Handled = InvokeCommand (Command.HotKey) == true;
            return;
        }

        if (e.MouseEvent.Flags.HasFlag (MouseFlags.Button1Pressed))
        {
            // If WantContinuousButtonPressed is true, and this is not the first pressed event,
            // invoke Accept (via HotKey)
            if (WantContinuousButtonPressed && Application.MouseGrabView == this)
            {
                e.Handled = InvokeCommand (Command.HotKey) == true;
                return;
            }

            // The first time we get pressed event, grab the mouse and invert the colors
            if (Application.MouseGrabView != this)
            {
                Application.GrabMouse (this);
                _savedColorScheme = ColorScheme;
                var cs = new ColorScheme (new Attribute (ColorScheme.Normal.Background, ColorScheme.Normal.Foreground));
                ColorScheme = cs;

                // Set the focus, but don't invoke Accept
                SetFocus ();
            }
        }

        if (e.MouseEvent.Flags.HasFlag (MouseFlags.Button1Released))
        {
            // When the mouse is released, if WantContinuousButtonPressed is set, invoke Accept one last time.
            if (WantContinuousButtonPressed)
            {
                e.Handled = InvokeCommand (Command.HotKey) == true;
            }

            if (Application.MouseGrabView == this)
            {
                Application.UngrabMouse ();
                if (_savedColorScheme is { })
                {
                    ColorScheme = _savedColorScheme;
                    _savedColorScheme = null;
                }
            }
        }
    }

    /// <inheritdoc />
    protected internal override bool OnMouseLeave (MouseEvent e)
    {
        return base.OnMouseLeave (e);
    }

    private void Button_MouseClick (object sender, MouseEventEventArgs e)
    {
        e.Handled = InvokeCommand (Command.HotKey) == true;
    }

    private void Button_TitleChanged (object sender, StateEventArgs<string> e)
    {
        base.Text = e.NewValue;
        TextFormatter.HotKeySpecifier = HotKeySpecifier;
    }

    /// <inheritdoc />
    public override string Text
    {
        get => base.Title;
        set => base.Text = base.Title = value;
    }

    /// <inheritdoc />
    public override Rune HotKeySpecifier
    {
        get => base.HotKeySpecifier;
        set => TextFormatter.HotKeySpecifier = base.HotKeySpecifier = value;
    }

    /// <summary>Gets or sets whether the <see cref="Button"/> is the default action to activate in a dialog.</summary>
    /// <value><c>true</c> if is default; otherwise, <c>false</c>.</value>
    public bool IsDefault
    {
        get => _isDefault;
        set
        {
            _isDefault = value;
            UpdateTextFormatterText ();
            OnResizeNeeded ();
        }
    }

    /// <summary></summary>
    public bool NoDecorations { get; set; }

    /// <summary></summary>
    public bool NoPadding { get; set; }

    /// <inheritdoc/>
    public override bool OnEnter (View view)
    {
        Application.Driver.SetCursorVisibility (CursorVisibility.Invisible);

        return base.OnEnter (view);
    }

    /// <inheritdoc/>
    public override void PositionCursor ()
    {
        if (HotKey.IsValid && Text != "")
        {
            for (var i = 0; i < TextFormatter.Text.GetRuneCount (); i++)
            {
                if (TextFormatter.Text [i] == Text [0])
                {
                    Move (i, 0);

                    return;
                }
            }
        }

        base.PositionCursor ();
    }

    /// <inheritdoc/>
    protected override void UpdateTextFormatterText ()
    {
        if (NoDecorations)
        {
            TextFormatter.Text = Text;
        }
        else if (IsDefault)
        {
            TextFormatter.Text = $"{_leftBracket}{_leftDefault} {Text} {_rightDefault}{_rightBracket}";
        }
        else
        {
            if (NoPadding)
            {
                TextFormatter.Text = $"{_leftBracket}{Text}{_rightBracket}";
            }
            else
            {
                TextFormatter.Text = $"{_leftBracket} {Text} {_rightBracket}";
            }
        }
    }
}
