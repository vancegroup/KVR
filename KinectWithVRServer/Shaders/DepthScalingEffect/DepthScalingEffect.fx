sampler2D input : register(s0);
float minimum : register(c0);
float maximum : register(c1);

float4 main(float2 uv : TEXCOORD) : COLOR
{
	float4 inColor = tex2D(input, uv);

	//Unpack the data and convert to 16-bit grayscale
	float temp = (inColor.g * 255.0 + inColor.b) / 256.0;

	//Scale the image so it fills the full range
	float range = maximum - minimum;
	float temp2 = clamp((temp - minimum) / range, 0, 1);

	return float4(temp2, temp2, temp2, 1.0);
}