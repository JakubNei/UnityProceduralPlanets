
#Key technological points
first a base (planetary) height map is provided or generated
uses chunked LOD quad tree
normal maps are in model space
each chunk is located on planet inside are of 4 direction vectors

#Chunk generation steps
generate height map from base planetary height map using bicubic sampling
height map: add very small high frequency noise to hide the bicubic sampling imprecision artefacts
generate slope map from height map
height map: add noise based on slope
generate slope map from height map
generate diffuse map based on slope map
generate normal map based on height map
generate mesh based on height map


# Planet detail subdivision (chunked LOD quad tree)
You can use ether squares or triangles for chunks.
Triangles appear to have better mesh, but it's finicky to figure out their texturing coordinates, plus you use only half of texture for each tringular chunk.

Squares:
Naive cube to sphere function is 

unitSphere = normalize(unitCube);

this however brings in distortions, better distortion-less unit cube to unit sphere is:
```
// -1 <= unitCube.x && unitCube.x <= 1
// -1 <= unitCube.y && unitCube.y <= 1
// -1 <= unitCube.z && unitCube.z <= 1
// uses math from http://mathproofs.blogspot.cz/2005/07/mapping-cube-to-sphere.html
// implementation license: public domain
float3 unitCubeToUnitSphere(float3 unitCube)
{
	float3 unitCubePow2 = unitCube * unitCube;
	float3 unitCubePow2Div2 = unitCubePow2 / 2;
	float3 unitCubePow2Div3 = unitCubePow2 / 3;
	return unitCube * sqrt(1 - unitCubePow2Div2.yzx - unitCubePow2Div2.zxy + unitCubePow2.yzx * unitCubePow2Div3.zxy);
}
```




#biomes


## simple, beginner steps
sea
beach
ice
desert
boreal

## idea links
https://planetaryannihilation.gamepedia.com/Biome
https://starbounder.org/Biome

## more complex
https://en.wikipedia.org/wiki/Geographical_zone
ice
tundra
boreal
warm
subtropical
tropical
lava?

plain
mountain
forest
water

### combinations
ice_plain
ice_mountain
tundra_plain
tundra_mountain
tundra_forest
boreal_plain
boreal_mountain
boreal_forest
warm_plain
warm_mountain
warm_forest
subtropical_plain
subtropical_mountain
subtropical_forest
tropical_plain
tropical_mountain
tropical_forest

