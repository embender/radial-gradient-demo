using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace radialdemo
{
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }
        
        private Compositor _compositor;

        // create the two visuals that make up this visual effect
        private SpriteVisual _vis;
        private SpriteVisual _pulseVis;

        // create radial gradient brushes that will be switched out on one visual
        private CompositionRadialGradientBrush _mainBrush;
        private CompositionRadialGradientBrush _pulseBrush;

        // create the color stops for every individual brush
        private CompositionColorGradientStop _MBGradientStop1;
        private CompositionColorGradientStop _MBGradientStop2;
        private CompositionColorGradientStop _MBGradientStop3;
        
        private CompositionColorGradientStop _PBGradientStop1;
        private CompositionColorGradientStop _PBGradientStop2;

        // create the various colors that will be used by the various radial gradient brushes
        private Color _warmColor1 = Colors.MediumVioletRed;
        private Color _warmColor2 = Colors.LightGoldenrodYellow;
        private Color _warmColor3 = Colors.LightPink;

        private Color _coolColor1 = Colors.Teal;
        private Color _coolColor2 = Colors.BlueViolet;
        private Color _coolColor3 = Colors.Plum;
        
        private Color _innerPulseColor = Colors.Transparent;
        private Color _outerPulseColor = Colors.AliceBlue;

        // all of the animations that will be used 
        private ExpressionAnimation _ellipseCenterAnim;
        private ScalarKeyFrameAnimation _offsetAnim;
        private ColorKeyFrameAnimation _stop1Anim;
        private ColorKeyFrameAnimation _stop2Anim;
        private ColorKeyFrameAnimation _pulseColorAnim;
        private Vector3KeyFrameAnimation _pulseScaleAnim;
        private ScalarKeyFrameAnimation _pulseStop1OffsetAnim;
        private ScalarKeyFrameAnimation _pulseStop2OffsetAnim;

        private Vector3 my_pointer;

        private CompositionPropertySet _hoverPositionPropertySet;

        Boolean _isAnimationOn = false;

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            // create the compositor
            _compositor = ElementCompositionPreview.GetElementVisual(this).Compositor;

            // create what captures the pointer position
            _hoverPositionPropertySet = ElementCompositionPreview.GetPointerPositionPropertySet(r2);
            
            // create the two visuals
            _vis = _compositor.CreateSpriteVisual();
            _pulseVis = _compositor.CreateSpriteVisual();

            // create the main brush with warm colors
            _mainBrush = _compositor.CreateRadialGradientBrush();
            _mainBrush.EllipseCenter = new Vector2(.5f, .5f);
            _mainBrush.EllipseRadius = new Vector2(.5f, .5f);
           
            _MBGradientStop1 = _compositor.CreateColorGradientStop();
            _MBGradientStop1.Offset = 0;
            _MBGradientStop1.Color = _warmColor1;

            _MBGradientStop2 = _compositor.CreateColorGradientStop();
            _MBGradientStop2.Offset = .1f;
            _MBGradientStop2.Color = _warmColor2;

            _MBGradientStop3 = _compositor.CreateColorGradientStop();
            _MBGradientStop3.Offset = 1;
            _MBGradientStop3.Color = _warmColor3;

            _mainBrush.ColorStops.Add(_MBGradientStop1);
            _mainBrush.ColorStops.Add(_MBGradientStop2);
            _mainBrush.ColorStops.Add(_MBGradientStop3);
            
            // create the brush for the pulse visual
            _pulseBrush = _compositor.CreateRadialGradientBrush();
            _pulseBrush.EllipseCenter = new Vector2(.5f, .5f);
            _pulseBrush.EllipseRadius = new Vector2(.5f, .5f);

            _PBGradientStop1 = _compositor.CreateColorGradientStop();
            _PBGradientStop1.Offset = 0;
            _PBGradientStop1.Color = _innerPulseColor;

            _PBGradientStop2 = _compositor.CreateColorGradientStop();
            _PBGradientStop2.Offset = 1;
            _PBGradientStop2.Color = _innerPulseColor;

            _pulseBrush.ColorStops.Add(_PBGradientStop1);
            _pulseBrush.ColorStops.Add(_PBGradientStop2);

            // finish setting properties of the first visual
            _vis.Size = new Vector2(300, 300);
            _vis.Offset = new Vector3(((float)r2.ActualWidth / 2), ((float)r2.ActualHeight / 2), 0);
            _vis.AnchorPoint = new Vector2(.5f, .5f);
            _vis.Brush = _mainBrush;

            // finish setting properties of the pulsing visual
            _pulseVis.Size = new Vector2(500, 500);
            _pulseVis.Offset = new Vector3(((float)r1.ActualWidth / 2), ((float)r1.ActualHeight / 2), 0);
            _pulseVis.AnchorPoint = new Vector2(.5f, .5f);
            _pulseVis.Brush = _pulseBrush;

            // create the clip that makes the visuals circular
            CompositionGeometricClip gClip = _compositor.CreateGeometricClip();
            CompositionEllipseGeometry circle = _compositor.CreateEllipseGeometry();
            circle.Radius = new Vector2(_vis.Size.X / 2, _vis.Size.Y / 2);
            circle.Center = new Vector2(_vis.Size.X / 2, _vis.Size.Y / 2);
            gClip.Geometry = circle;

            _vis.Clip = gClip;

            CompositionGeometricClip gClip2 = _compositor.CreateGeometricClip();
            CompositionEllipseGeometry circle2 = _compositor.CreateEllipseGeometry();
            circle2.Radius = new Vector2(_pulseVis.Size.X / 2, _pulseVis.Size.Y / 2);
            circle2.Center = new Vector2(_pulseVis.Size.X / 2, _pulseVis.Size.Y / 2);
            gClip2.Geometry = circle2;

            _pulseVis.Clip = gClip2;

            // set the pointer
            my_pointer = new Vector3(((float)r1.ActualWidth / 2), ((float)r1.ActualHeight / 2), 0);

            // set the visuals in the tree
            ElementCompositionPreview.SetElementChildVisual(r2, _vis);
            ElementCompositionPreview.SetElementChildVisual(r1, _pulseVis);

            // ellipse center follows mouse
            _ellipseCenterAnim = _compositor.CreateExpressionAnimation("Vector2(p.Position.X / 500.0f, p.Position.Y / 500.0f)");  
            _ellipseCenterAnim.SetReferenceParameter("p", _hoverPositionPropertySet);
            _mainBrush.StartAnimation("EllipseCenter", _ellipseCenterAnim);

            // second stop is animated for "pulsing" effect within the first visual that runs constantly 
            _offsetAnim = _compositor.CreateScalarKeyFrameAnimation();
            _offsetAnim.InsertKeyFrame(0, 0);
            _offsetAnim.InsertKeyFrame(1f, 1f);
            _offsetAnim.Duration = TimeSpan.FromSeconds(2);
            _offsetAnim.IterationCount = 50;

            _MBGradientStop2.StartAnimation("Offset", _offsetAnim);

            // set up the animation for the backing pulse visual
            // animate the color 
            _pulseColorAnim = _compositor.CreateColorKeyFrameAnimation();
            _pulseColorAnim.InsertKeyFrame(0, _innerPulseColor);
            _pulseColorAnim.InsertKeyFrame(.99f, _outerPulseColor);
            _pulseColorAnim.InsertKeyFrame(1, _innerPulseColor);
            _pulseColorAnim.Duration = TimeSpan.FromSeconds(1);
            _pulseColorAnim.IterationBehavior = AnimationIterationBehavior.Forever;

            _PBGradientStop1.StartAnimation("Color", _pulseColorAnim);

            // animate offset of first stop 
            _pulseStop1OffsetAnim = _compositor.CreateScalarKeyFrameAnimation();
            _pulseStop1OffsetAnim.InsertKeyFrame(0, 0);
            _pulseStop1OffsetAnim.InsertKeyFrame(1f, 1f);
            _pulseStop1OffsetAnim.Duration = TimeSpan.FromSeconds(1);
            _pulseStop1OffsetAnim.IterationBehavior = AnimationIterationBehavior.Forever;

            _PBGradientStop1.StartAnimation("Offset", _pulseStop1OffsetAnim);

            // animate offset of second stop
            _pulseStop2OffsetAnim = _compositor.CreateScalarKeyFrameAnimation();
            _pulseStop2OffsetAnim.InsertKeyFrame(0, 0);
            _pulseStop2OffsetAnim.InsertKeyFrame(1f, 1f);
            _pulseStop2OffsetAnim.Duration = TimeSpan.FromSeconds(1);
            _pulseStop2OffsetAnim.IterationBehavior = AnimationIterationBehavior.Forever;
            _pulseStop2OffsetAnim.DelayTime = TimeSpan.FromSeconds(.25f);

            _PBGradientStop2.StartAnimation("Offset", _pulseStop2OffsetAnim);

            _pulseScaleAnim = _compositor.CreateVector3KeyFrameAnimation();
            _pulseScaleAnim.InsertKeyFrame(0, Vector3.Zero);
            _pulseScaleAnim.InsertKeyFrame(1, Vector3.One);
            _pulseScaleAnim.Duration = TimeSpan.FromSeconds(1);
            _pulseScaleAnim.IterationBehavior = AnimationIterationBehavior.Forever;
            
            _pulseVis.StartAnimation("Scale", _pulseScaleAnim);
        }

        // when the first visual is clicked, it switches the brush on the visual from warm to cool colors and animates those color stops 
        private void OnClick(object sender, RoutedEventArgs e)
        {
            if (_isAnimationOn == false)
            {
                // animate the first stop of the first visual
                _stop1Anim = _compositor.CreateColorKeyFrameAnimation();
                _stop1Anim.InsertKeyFrame(0, _warmColor1);
                _stop1Anim.InsertKeyFrame(.5f, _warmColor3);
                _stop1Anim.InsertKeyFrame(1, _warmColor1);
                _stop1Anim.Duration = TimeSpan.FromSeconds(4);
                _stop1Anim.IterationBehavior = AnimationIterationBehavior.Forever;

                _MBGradientStop1.StartAnimation("Color", _stop1Anim);

                // animate the second stop of the first visual
                _stop2Anim = _compositor.CreateColorKeyFrameAnimation();
                _stop2Anim.InsertKeyFrame(0, _warmColor3);
                _stop2Anim.InsertKeyFrame(1, _warmColor2);
                _stop2Anim.Duration = TimeSpan.FromSeconds(2);
                _stop2Anim.IterationBehavior = AnimationIterationBehavior.Forever;

                _MBGradientStop2.StartAnimation("Color", _stop2Anim);

                _isAnimationOn = true;
            }
            else
            {
                // animate the first stop of the first visual
                _stop1Anim = _compositor.CreateColorKeyFrameAnimation();
                _stop1Anim.InsertKeyFrame(0, _coolColor1);
                _stop1Anim.InsertKeyFrame(.5f, _coolColor3);
                _stop1Anim.InsertKeyFrame(1, _coolColor1);
                _stop1Anim.Duration = TimeSpan.FromSeconds(4);
                _stop1Anim.IterationBehavior = AnimationIterationBehavior.Forever;

                _MBGradientStop1.StartAnimation("Color", _stop1Anim);

                // animate the second stop of the first visual
                _stop2Anim.InsertKeyFrame(0, _coolColor3);
                _stop2Anim.InsertKeyFrame(1, _coolColor2);
                _stop2Anim.Duration = TimeSpan.FromSeconds(2);
                _stop2Anim.IterationBehavior = AnimationIterationBehavior.Forever;

                _MBGradientStop2.StartAnimation("Color", _stop2Anim);

                _isAnimationOn = false;
            }
        }

        private Vector3 r1_PointerMoved(object sender, PointerRoutedEventArgs e)
        {          
            var x = e.GetCurrentPoint(r2).Position.X;
            var y = e.GetCurrentPoint(r2).Position.Y;

            my_pointer = new Vector3((float)x, (float)y, 0);

            _mainBrush.EllipseCenter = new Vector2(my_pointer.X, my_pointer.Y);
            
            return my_pointer;
        }
        
    }
}
