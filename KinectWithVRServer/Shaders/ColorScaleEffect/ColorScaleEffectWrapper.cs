using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Effects;
using System.Windows.Media;
using System.Windows;

namespace KinectWithVRServer.Shaders
{
    public class ColorScaleEffect : ShaderEffect
    {
        private static PixelShader colorScalePixelShader = new PixelShader() { UriSource = new Uri(@"pack://application:,,,/KinectWithVRServer;component/Shaders/ColorScaleEffect/ColorScaleEffect.ps") };

        public ColorScaleEffect()
        {
            PixelShader = colorScalePixelShader;
            UpdateShaderValue(InputProperty);
            UpdateShaderValue(MinimumProperty);
            UpdateShaderValue(MaximumProperty);
        }

        //Set the input property (this will be used as a sampler2D in the shader to get the original image color)
        public Brush Input
        {
            get { return (Brush)GetValue(InputProperty); }
            set { SetValue(InputProperty, value); }
        }
        public static readonly DependencyProperty InputProperty = ShaderEffect.RegisterPixelShaderSamplerProperty("Input", typeof(ColorScaleEffect), 0);

        //Set the minimum value property
        public float Minimum
        {
            get { return (float)GetValue(MinimumProperty); }
            set { SetValue(MinimumProperty, (double)value); }
        }
        public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register("Minimum", typeof(double), typeof(ColorScaleEffect), new UIPropertyMetadata(0.0, PixelShaderConstantCallback(0)));

        //Set the maximum value property
        public float Maximum
        {
            get { return (float)GetValue(MaximumProperty); }
            set { SetValue(MaximumProperty, (double)value); }
        }
        public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register("Maximum", typeof(double), typeof(ColorScaleEffect), new UIPropertyMetadata(0.0, PixelShaderConstantCallback(1)));
    }
}
