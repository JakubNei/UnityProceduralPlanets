
#Key technological points
first a base (planetary) height map is provided or generated
uses chunked LOD quad tree
normal maps are in model space
each chunk is located on planet inside are of 4 direction unit vectors
mesh heights must be able to be generated on CPU only, so it can be used on dedicated servers
things generated on GPU are only visual candy

#Chunk generation steps
CPU: generate height map from base planetary height map using bicubic sampling
CPU: generate mesh vertices from bicubic sampling from planetary height map
GPU: upscale CPU mesh height map, add very small high frequency noise to hide imprecision artefacts
GPU: generate slope map from height map
GPU: height map: add noise based on slope
GPU: generate slope map from height map
GPU: generate diffuse map based on slope map
GPU: generate normal map based on height map


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
```
/// <summary>
/// transforms unitCube position into unitSphere,
/// implementation license: public domain,
/// uses math from http://mathproofs.blogspot.cz/2005/07/mapping-cube-to-sphere.html
/// </summary>
/// <param name="unitCube">unitCube.xyz is in inclusive range [-1, 1]</param>
/// <returns></returns>
public static Vector3 UnitCubeToUnitSphere(Vector3 unitCube)
{
	var unitCubePow2 = new Vector3(unitCube.x * unitCube.x, unitCube.y * unitCube.y, unitCube.z * unitCube.z);
	var unitCubePow2Div2 = unitCubePow2 / 2;
	var unitCubePow2Div3 = unitCubePow2 / 3;
	var unitSphere = new Vector3(
		unitCube.x * Mathf.Sqrt(1 - unitCubePow2Div2.y - unitCubePow2Div2.z + unitCubePow2.y * unitCubePow2Div3.z),
		unitCube.y * Mathf.Sqrt(1 - unitCubePow2Div2.z - unitCubePow2Div2.x + unitCubePow2.z * unitCubePow2Div3.x),
		unitCube.z * Mathf.Sqrt(1 - unitCubePow2Div2.x - unitCubePow2Div2.y + unitCubePow2.x * unitCubePow2Div3.y)
	);
	return unitSphere;
}
```


#Chunk height maps
Chunk height maps can have adjusted min max, based on the range they need:
```
float chunkMapHeight = (realPlanetHeight - _heightMin) / (_heightMax - _heightMin);

float realPlanetHeight = chunkMapHeight * (_heightMax - _heightMin) + _heightMin;
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

