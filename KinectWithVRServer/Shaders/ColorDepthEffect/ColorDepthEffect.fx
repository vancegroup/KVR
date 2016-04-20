sampler2D input : register(s0);
float minimum : register(c0);
float maximum : register(c1);

float4 main(float2 uv : TEXCOORD) : COLOR
{
	float4 inColor = tex2D(input, uv);
	float4 outColor = float4(0.0, 0.0, 0.0, 1.0);

	//Unpack the data and convert to 16-bit grayscale
	float temp = (inColor.g * 255.0 + inColor.b) / 256.0;

	//Scale the image so it fills the full range
	float range = maximum - minimum;
	float qtr = 0.25 * range;
	[flatten] if (temp < minimum)
	{
		float interpol = temp / minimum;
		outColor.rgb = lerp(float3(0.0, 0.0, 0.0), float3(0.0, 0.0, 1.0), float3(interpol, interpol, interpol));
	}
	else if (temp < minimum + 0.25 * range)
	{
		float interpol = (temp - minimum) / qtr;
		outColor.rgb = lerp(float3(0.0, 0.0, 1.0), float3(0.0, 1.0, 1.0), float3(interpol, interpol, interpol));
	}
	else if (temp < minimum + 0.5 * range)
	{
		float interpol = (temp - minimum - qtr) / qtr;
		outColor.rgb = lerp(float3(0.0, 1.0, 1.0), float3(0.0, 1.0, 0.0), float3(interpol, interpol, interpol));
	}
	else if (temp < minimum + 0.75 * range)
	{
		float interpol = (temp - minimum - 2 * qtr) / qtr;
		outColor.rgb = lerp(float3(0.0, 1.0, 0.0), float3(1.0, 1.0, 0.0), float3(interpol, interpol, interpol));
	}
	else if (temp < maximum)
	{
		float interpol = (temp - minimum - 3 * qtr) / qtr;
		outColor.rgb = lerp(float3(1.0, 1.0, 0.0), float3(1.0, 0.0, 0.0), float3(interpol, interpol, interpol));
	}
	else
	{
		float interpol = (temp - maximum) / (1.0 - maximum);
		outColor.rgb = lerp(float3(1.0, 0.0, 0.0), float3(1.0, 0.0, 1.0), float3(interpol, interpol, interpol));
	}

	return outColor;
}