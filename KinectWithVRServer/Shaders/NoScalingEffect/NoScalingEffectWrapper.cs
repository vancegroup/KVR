using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Effects;
using System.Windows.Media;
using System.Windows;

namespace KinectWithVRServer.Shaders
{
    public class NoScalingEffect : ShaderEffect
    {
        private static PixelShader noScalingPixelShader = new PixelShader() { UriSource = new Uri(@"pack://application:,,,/KinectWithVRServer;component/Shaders/NoScalingEffect/NoScalingEffect.ps") };

        public NoScalingEffect()
        {
            PixelShader = noScalingPixelShader;
            UpdateShaderValue(InputProperty);
        }

        //Set the input property (this will be used as a sampler2D in the shader to get the original image color)
        public Brush Input
        {
            get { return (Brush)GetValue(InputProperty); }
            set { SetValue(InputProperty, value); }
        }
        public static readonly DependencyProperty InputProperty = ShaderEffect.RegisterPixelShaderSamplerProperty("Input", typeof(NoScalingEffect), 0);
    }
}
