# About
This is where I try to refine and improve on everything I've learned in my [Procedural planets generator](https://github.com/aeroson/procedural-planets-generator). The idea is that this will be license free Earth sized comprehensive Unity GPU-mostly procedural planets and procedural universe generator. So people can #MakeGamesNotProceduralPlanets


# Key technological points
- A base (planetary) height map is provided or generated, which defines the basic planet shape
- Uses chunked LOD quad tree
- Chunk normal maps are in model space
- Each chunk is located on planet inside area of 4 direction unit vectors

# Chunk generation steps
1. GPU: generate chunk height map from base planetary height map using bicubic sampling
2. GPU: chunk height map: add very small high frequency noise to hide samping imprecision artefacts
3. GPU: generate chunk slope map from chunk height map
4. GPU: chunk height map: add noise based on chunk slope map
5. GPU: get chunk mesh vertices from chunk height map
6. GPU->CPU: download chunk mesh vertices from GPU to CPU
7. GPU: generate chunk slope map from chunk height map
8. GPU: generate chunk diffuse map based on chunk slope map
9. GPU: generate chunk normal map based on chunk height map
10. CPU: create chunk mesh from downloaded chunk mesh vertices

Chunk mesh vertices could be generated on CPU only, so it can be used on dedicated servers. Things generated on GPU should be only to add eye candy. Currently everything is generated on GPU.

# Planet detail subdivision (chunked LOD quad tree)
You can use ether squares or triangles for chunks shape. Triangles appear to have better mesh, but it's finicky to figure out their texturing coordinates, plus you use only half of texture for each tringular chunk.

Squares:
Naive cube to sphere function is 
```
unitSphere = normalize(unitCube);
```
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


# Chunk height maps
Chunk height maps can have adjusted min max, based on the range they need. That is what the FindTextureMinMax is for.
```
float chunkMapHeight = (realPlanetHeight - _heightMin) / (_heightMax - _heightMin);

float realPlanetHeight = chunkMapHeight * (_heightMax - _heightMin) + _heightMin;
```


# Links
[Normal maps blending aproaches](http://blog.selfshadow.com/publications/blending-in-detail/)

[fBM, Billowy turbulence, Ridged turbulence, IQ Noise](http://www.decarpentier.nl/scape-procedural-basics)

[Introduction series to making procedural worlds](https://acko.net/blog/making-worlds-introduction/)

[Outerra: Bicubic sampling, Slope depedent noise example](http://www.outerra.com/procedural/demo.html)

[Screenshot: Chunk skirts in Elite Dangerous](https://image.prntscr.com/image/ftDCxkimQK6uRgKqs9WBdg.png)

