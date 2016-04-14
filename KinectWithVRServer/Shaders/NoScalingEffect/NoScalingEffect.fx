sampler2D input : register(s0);

float4 main(float2 uv : TEXCOORD) : COLOR
{
	//Get the original pixel color
	float4 inColor = tex2D(input, uv);

	//We aren't doing any shading in this case, so just return the original image color
	return inColor;
}