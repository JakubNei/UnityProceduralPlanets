


// BIOMES

/*
all of this could be moved into seperate compute shaders for each biome
public class BiomeProcessor
{
	public int biomeIndex;
	public ComputeShader generateWeight;
	public ComputeShader adjustHeight;
	public ComputeShader adjustColor;
}
top 2 weighted biomes would then adjust height and color ?
*/


struct BiomeData {
	float3 dir;
	float slope;
};





struct BiomeResult {
	int biome1Index;
	float biome1Weight;
	int biome2Index;
	float biome2Weight;
};

BiomeResult selectBiome(BiomeData data)
{
	data.slope = _chunkSlopeMap[id.xy].x * 7;




	BiomeResult result;
	
	result.biome1Weight = 1;



	float slope = data.slope;
	float altidute = height01;
	float3 biomeAdjustmentNoise = snoise_grad(pos / 50, 10, 1.4);
	//biomeAdjustmentNoise = 0;


	float snowWeight = smoothstep(0.2, 0, slope) * (smoothstep(0.8, 1, abs(dir.z)) * 2 + altidute * 2);
	float tundraWeight = smoothstep(0.8, 0, slope) * (smoothstep(0.5, 1, abs(dir.z)) + altidute + 0.01*biomeAdjustmentNoise.z);
	float rockWeight = smoothstep(0.5, 1, slope)*5 + 0.003*biomeAdjustmentNoise.x;
	float clayWeight = smoothstep(0, 0.2, slope)*0.3 + 0.02*biomeAdjustmentNoise.y;
	float grassWeight = smoothstep(0.8, 0, slope) + 0.05*biomeAdjustmentNoise.z;



	if (slope > 0.3) {
		result.biome1Index = 0; // rock
		result.biome1Weight = smoothstep(0.3, 0.5, slope);
	}
	else {
		if (snowWeight > 1.5) {
			result.biome1Index = 1; // snow
			result.biome1Weight = smoothstep(1.5, 1.7, snowWeight);
		}
		else if (snowWeight > 1.2) {
			result.biome1Index = 2; // tundra
			result.biome1Weight = smoothstep(1.2, 1.3, snowWeight);
		}
		else if (slope > 0.2) {
			result.biome1Index = 4; // clay
			result.biome1Weight = smoothstep(0.2, 0.3, slope);
		} else {
			result.biome1Index = 3; // grass
			result.biome1Weight = smoothstep(0.3, 0.2, slope);
		}
	}

	// select seconday biome that is different from primary
	if (slope > 0.3 && result.biome1Index != 0) {
		result.biome2Index = 0; // rock
	}
	else {
		if (snowWeight > 1.5 && result.biome1Index != 1) {
			result.biome2Index = 1; // snow
		}
		else if (snowWeight > 1.2 && result.biome1Index != 2) {
			result.biome2Index = 2; // tundra
		}
		else if (slope > 0.2 && result.biome1Index != 4) {
			result.biome2Index = 4; // clay
		}
		else {
			result.biome2Index = 3; // grass
		}
	}

	result.biome2Weight = 1 - result.biome1Weight;

	return result;
}





float adjustHeight(int biomeIndex, BiomeData data)
{
	if (biomeIndex == 0) { // rock
		float slope = saturate(length(data.slopeXY));
		return (1 - abs(snoise(data.dir * 50, 15, 1.5))) * 0.03; // * slope
	}

	if (biomeIndex == 2) { // tundra
		return snoise(data.dir * 10, 15, 1.5) * 0.0001;
	}

	if (biomeIndex == 3) { // grass
		return snoise(data.dir * 300, 15, 2) * 0.001;
	}

	if (biomeIndex == 4) { // clay
		return snoise(data.dir * 30, 5, 1.5) * 0.003;
	}

	return 0;
}

// Biome1AdjustHeight.compute
float adjustHeight(BiomeResult biome, BiomeData data)
{
	//DEBUG
	//return 0;

	float height =
		adjustHeight(biome.biome1Index, data) * biome.biome1Weight +
		adjustHeight(biome.biome2Index, data) * biome.biome2Weight;

	return height;
}





float3 getDiffuseColor(int biomeIndex, BiomeData data)
{
	if (biomeIndex == 3)
		return float3(121, 136, 69) / float3(255, 255, 255); // grass

	if (biomeIndex == 4)
		return float3(139, 133, 75) / float3(255, 255, 255); // clay

	if (biomeIndex == 0)
		return float3(100, 100, 100) / float3(255, 255, 255); // rock

	if (biomeIndex == 1)
		return float3(1, 1, 1); // white

	if (biomeIndex == 2)
		return float3(0.5, 0.5, 0.5); // grey

	return float3(1, 0, 0); // red
}

float3 getDiffuseColor(BiomeResult biome, BiomeData data)
{
	//return getDiffuseColor(biome.biome1Index, data);

	float3 color =
		getDiffuseColor(biome.biome1Index, data) * biome.biome1Weight +
		getDiffuseColor(biome.biome2Index, data) * biome.biome2Weight;

	return color;
}
