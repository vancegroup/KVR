sampler2D input : register(s0);

float4 main(float2 uv : TEXCOORD) : COLOR
{
	//Get the original pixel color
	float4 inColor = tex2D(input, uv);
	
	//Unpack the data and convert to 16-bit grayscale
	float temp = (inColor.g * 255.0 + inColor.b) / 256.0;
	temp = clamp(temp, 0, 1);

	//We aren't doing any shading in this case, so just return the original image color
	return float4(temp, temp, temp, 1.0);
}