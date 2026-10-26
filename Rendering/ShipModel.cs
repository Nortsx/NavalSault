using System.Collections.Generic;

namespace NavalSault.Rendering;

// A ship's 3D model in the content pipeline, and how to fit it onto the footprint.
// Build models with FbxImporter and ModelProcessor, .obj files included: OpenAssetImporter leaves their materials
// unconverted and the game can't load the result.
// Models are expected up along +Y with the waterline at Y = 0; YawDegrees turns one whose bow isn't along -Z
// (90 for a bow along +X, 180 for +Z). The model is scaled evenly to fit the footprint and centred on it,
// so the units it was made in don't matter.
// TintedMeshes names the meshes painted in the ship's colour; null paints the whole model. Untinted meshes keep their
// own materials. A tinted material's colour is multiplied by the ship's, so paint it white to get the ship's colour as-is.
public record ShipModel(string AssetName, float YawDegrees = 0f, IReadOnlySet<string> TintedMeshes = null);
