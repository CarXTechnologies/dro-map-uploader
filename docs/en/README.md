# MapUploader documentation

**English** · [Русский](../ru/README.md)

These pages take you from an empty Unity scene to a map that you test in CarX Drift Racing Online 2 and publish to mod.io.

For a short overview, the quick start and the limits at a glance, see the [MapUploader README](../../README.md).

## Pages

Read the pages in this order the first time. Each page ends with links to the neighbouring pages and to this contents page.

1. [Get started](getting-started.md): Clone the project with its submodule, install Unity 6000.3.19f1, open the **MapBuilder** window and choose its interface language.
1. [Set up the scene](scene-setup.md): Create the map folder and scene, then set up models, materials, colliders, dynamic objects, LOD groups and lighting so that they export correctly.
1. [Add markers](markers.md): Use the GameMarkerData component to place spawn points, mark road surfaces, animate objects and replace materials with render core variants.
1. [Minimap, icon and preview](minimap-and-images.md): Add the Minimap component, capture the minimap, icon and preview images with CaptureCamera, and check the image requirements.
1. [Fill in the map settings](map-settings.md): Create a map config with **+ New map** and fill in the **Map** tab: name, version, summary, description, images, authors and URL.
1. [Build the map](build.md): Choose the scene and the Binary or Wavefront format, build the map, tune build optimization and find the build cache.
1. [Fix validation problems](validation.md): Run **Validate map**, read the results in the **Map Validation** window and see what each check covers.
1. [Test the map in the game](test-in-game.md): Export the build to a map folder inside the `Mods` folder next to the game executable on PC, drive the map in free ride or in an online room, check that your copy of the game loads local maps, and go through the checklist before you publish. <!-- src: game -->
1. [Publish to mod.io](publish.md): Sign in to mod.io, create a publication and make it public, update it after you change the map and delete it when you no longer need it.
1. [Troubleshooting](troubleshooting.md): Find the cause and the fix for validation messages, build errors in the Console, greyed-out controls, problems in the game, upload and setup problems, and read answers to common questions.
1. [Reference](reference.md): Look up limits, supported components, road surfaces, optimization defaults, the menus the project adds, where settings are stored and the English–Russian glossary.

## For integrators

You don't need these pages to make or publish a map. They describe the mod file format and the publishing layer for people who extend MapUploader or read its output.

- [Mod format: Wavefront and Binary](advanced/mod-format.md): Learn how a built map is stored: the Binary `mod.cxmod` container, the Wavefront file tree and how the game reads a mod folder.
- [Publishing vendors (for integrators)](advanced/publishing-vendors.md): Learn how the publishing layer works with vendor SDKs and how to configure or add a vendor; the mod.io setup for CarX Drift Racing Online 2 already ships with the project.

## Next steps

← [MapUploader README](../../README.md) · [Get started](getting-started.md) →
