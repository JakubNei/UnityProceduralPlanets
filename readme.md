
#Key technological points
first a base (planetary) height map is provided or generated
uses chunked LOD quad tree
normal maps are in model space
each node is located on planet inside are of 4 direction vectors

#Node generation steps
generate height map from base planetary height map using bicubic sampling
height map: add very small high frequency noise to hide the bicubic sampling imprecision artefacts
generate slope map from height map
height map: add noise based on slope
generate slope map from height map
generate diffuse map based on slope map
generate normal map based on height map
generate mesh based on height map





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

