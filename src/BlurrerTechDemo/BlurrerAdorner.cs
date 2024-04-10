using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace BlurrerTechDemo;

internal class BlurrerAdorner
{
    private readonly IWpfTextView _textView;
    private readonly IAdornmentLayer _codeBlurrerLayer;

    private readonly Brush _blurBrush;

    public BlurrerAdorner(IWpfTextView textView)
    {
        _textView = textView;

        // Get blur layer defined in the <ref>BlurrerCreationListener</ref> class
        _codeBlurrerLayer = textView.GetAdornmentLayer("CodeBlurrer");

        var b = new BlurEffect();
        // Get current background brush for the text view. We'll use it to create the blur effect.
        // NOTE: This works poorly if the background is not a solid color.

        _blurBrush = _textView.Background.Clone();
        _blurBrush.Opacity = 0.8;

        // Register for the events when we need to update the overlay adorner. For this example,
        // we want to update when the user scrolls or resizes the window, or when a text selection is made/modified.
        textView.LayoutChanged += OnLayoutChanged;
        textView.Selection.SelectionChanged += OnSelectionChanged;
    }

    /// <summary>
    /// The event called when the layout of the text view changes.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    void OnLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
    {
        try
        {
            // Update the blur effect when the layout changes
            BlurText();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex.Message);
            throw;
        }
    }

    /// <summary>
    /// The event called when the selection in the text view changes.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    void OnSelectionChanged(object sender, EventArgs e)
    {
        try
        {
            // Update the blur effect when the selection changes
            BlurText();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Blurs the text that is not selected in the text view.
    /// </summary>
    private void BlurText()
    {
        // Clear the blur layer
        _codeBlurrerLayer.RemoveAllAdornments();

        // Exit out if nothing is selected
        if (_textView.Selection.IsEmpty)
        {
            return;
        }

        var selectedSpans = _textView.Selection.SelectedSpans.ToList();

        // Invert selected spans
        var notSelectedSpans = new List<SnapshotSpan>();

        int topEnd = 0;
        foreach (var selectedSpan in selectedSpans)
        {
            if( selectedSpan.Start.Position == selectedSpan.End.Position)
            {
                continue;
            }

            var start = selectedSpan.Start.Position;

            if (start > topEnd)
            {
                var newSpan = new SnapshotSpan(_textView.TextSnapshot, Span.FromBounds(topEnd, start));
                notSelectedSpans.Add(newSpan);
            }
            topEnd = selectedSpan.End.Position;
        }

        // Add the last not selected span
        var lastSpan = new SnapshotSpan(_textView.TextSnapshot, Span.FromBounds(topEnd, _textView.TextSnapshot.Length));
        notSelectedSpans.Add(lastSpan);

        // Blur the not selected spans
        foreach (var span in notSelectedSpans)
        {
            BlurSpan(span);
        }
    }


    /// <summary>
    /// Blurs the text between the specified start and stop indices.
    /// </summary>
    /// <param name="startIndex">The start index of the text to blur.</param>
    /// <param name="stopIndex">The stop index of the text to blur.</param>
    /// <remarks>
    /// This might be more interesting for a "general" use case than the selected text logic above.
    /// This method does NOT clear any previous blurred texts, but only adds to them. Call RemoveAllAdornments on the layer to clear them.</remarks>
    private void BlurText(int startIndex, int stopIndex)
    {
        var lines = _textView.TextViewLines;

        // Get a snapshot of the text to blur
        var textSnapshot = new SnapshotSpan(_textView.TextSnapshot, Span.FromBounds(startIndex, stopIndex));

        BlurSpan(textSnapshot);
    }

    /// <summary>
    /// Blurs the text within the specified span.
    /// </summary>
    /// <param name="span">The span of the text to blur.</param>
    /// <remarks>This does the actual "work" of putting an overlay above the text</remarks>
    private void BlurSpan(SnapshotSpan span)
    {
        // Get the "document coordinates" of the text in the span
        var geometry = _textView.TextViewLines.GetMarkerGeometry(span);

        if (geometry != null)
        {
            // Create a drawing for the blur effect based on the geometry of the text
            var drawing = new GeometryDrawing(_blurBrush, null, geometry);
            drawing.Freeze();

            var drawingImage = new DrawingImage(drawing);
            drawingImage.Freeze();

            var image = new Image
            {
                Source = drawingImage,
                //Effect = new BlurEffect() { Radius = 20, KernelType = KernelType.Gaussian }
            };

            // Align the image with the top of the bounds of the text geometry
            Canvas.SetLeft(image, geometry.Bounds.Left);
            Canvas.SetTop(image, geometry.Bounds.Top);

            // Add the image to the layer in the view
            _codeBlurrerLayer.AddAdornment(AdornmentPositioningBehavior.TextRelative, span, null, image, null);
        }
    }

}
