


// BIOMES


struct BiomeData {
	float3 dir;
	float2 slopeXY;
	float humidity;
	float altidute;
};

// ChunkBiomeSelector.compute
int selectBiome(BiomeData d)
{
	float slope = saturate(length(d.slopeXY));


	float distanceToPoles = smoothstep(0.4, 1, abs(d.dir.z));
	float snowWeight = d.altidute + distanceToPoles + snoise(d.dir * 100, 5, 2) * 0.1;


	if (slope > 0.3)
		return 0; // rock
	else {
		if (snowWeight > 1.5)
			return 1; // snow
		else if (snowWeight > 1.2)
			return 2; // tundra
		else if (slope < 0.25)
			return 3; // grass
		else
			return 4; // clay
	}
}


// Biome1AdjustHeight.compute
float adjustHeight(int biome, BiomeData d)
{
	float slope = saturate(length(d.slopeXY));

	if (biome == 0)
		return (1 - abs(snoise(d.dir * 100, 30, 1.5))) * slope * 0.1;

	return 0;
}


float3 getDiffuseColor(int biome, BiomeData d)
{
	if (biome == 3)
		return float3(121, 136, 69) / float3(255, 255, 255); // grass

	if (biome == 4)
		return float3(139, 133, 75) / float3(255, 255, 255); // clay

	if (biome == 0)
		return float3(100, 100, 100) / float3(255, 255, 255); // rock

	if (biome == 1)
		return float3(1, 1, 1); // white

	if (biome == 2)
		return float3(0.5, 0.5, 0.5); // grey

	return float3(1, 0, 0); // red
}