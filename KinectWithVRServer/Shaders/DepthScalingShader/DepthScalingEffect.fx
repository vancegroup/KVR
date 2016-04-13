sampler2D input : register(s0);
float minimum : register(c0);
float maximum : register(c1);

float4 main(float2 uv : TEXCOORD) : COLOR
{
	float4 inColor = tex2D(input, uv);
	float4 returnColor = inColor;
	float range = maximum - minimum;
	returnColor.r = clamp((inColor.r - minimum) / range, 0, 1);
	returnColor.g = clamp((inColor.g - minimum) / range, 0, 1);
	returnColor.b = clamp((inColor.b - minimum) / range, 0, 1);

	//return inColor;
	return returnColor;
}