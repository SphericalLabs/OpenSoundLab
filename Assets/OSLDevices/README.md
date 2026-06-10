# OpenSoundLab Devices

OpenSoundLab devices are registered through collection manifests and normalized at runtime by `OSLDeviceRegistry`. Device scripts, prefabs, menu previews, save/load and network spawning should resolve devices through the registry instead of hardcoded lists.

## Collections

A collection owns device identity and loading metadata. A category controls where a device appears in the menu. A source controls distribution context.

```
Collection: io.sphericals.osl.core.soundgenerator
Category:   SoundGenerator
Source:     Core
```

All runtime-visible device manifests use `OSLDeviceCollectionManifest`.

## Core devices

Core devices are included in the base app. They are grouped into category/domain-owned collections, with one collection folder and one collection manifest per group. `Core` is the source/product context, not a single owning collection id.

```
Assets/OSLDevices/Core/<Category>/
  Scripts/
    <DeviceId>/
      <device scripts>.cs
  Resources/
    <collectionId>/
      <collectionId>.asset
      Prefabs/<DeviceId>.prefab
      MenuPrefabs/<DeviceId>_Menu.prefab
      Textures/<DeviceId>Symbol.png
```

Example:

```
Assets/OSLDevices/Core/SoundGenerator/
  Scripts/
    Oscillator/
    Noise/
  Resources/
    io.sphericals.osl.core.soundgenerator/
      io.sphericals.osl.core.soundgenerator.asset
      Prefabs/
      MenuPrefabs/
      Textures/
```

Core collection ids use `io.sphericals.osl.core.<collection>`:

```
io.sphericals.osl.core.various
io.sphericals.osl.core.interface
io.sphericals.osl.core.mixing
io.sphericals.osl.core.modulationprocessor
io.sphericals.osl.core.modulationgenerator
io.sphericals.osl.core.soundprocessor
io.sphericals.osl.core.sampler
io.sphericals.osl.core.rhythm
io.sphericals.osl.core.soundgenerator
```

Core scripts should save canonical collection-scoped ids by calling `setDeviceType(data)` in `GetData()`. The registry resolves the canonical id from manifest data-type metadata and keeps local-id lookup for base-app devices so old saves that contain short ids such as `Oscillator` continue to resolve.

## Add-ons and packages

Add-ons use the same collection manifest schema as Core. Add-on collection ids use `<maker>.osl.addons.<collection>`, for example `com.company.osl.addons.weirdsynth`. Project-local add-ons live under:

```
Assets/OSLDevices/Addons/<collectionId>/
  Scripts/
    <DeviceId>/
  Resources/
    <collectionId>/
      <collectionId>.asset
      Prefabs/
      MenuPrefabs/
      Textures/
```

Embedded/private packages should use the same internal shape:

```
Packages/<PackageName>/OSLDevices/<collectionId>/
  Scripts/
  Resources/
    <collectionId>/
      <collectionId>.asset
      Prefabs/
      MenuPrefabs/
      Textures/
```

The registry discovers collection manifests with Unity `Resources` and editor-side layout filtering. Supported manifest locations are:

```
Assets/OSLDevices/Core/<Collection>/Resources/<collectionId>/
Assets/OSLDevices/Addons/<collectionId>/Resources/<collectionId>/
Packages/<PackageName>/OSLDevices/<collectionId>/Resources/<collectionId>/
```

## Access flags, Store SKUs and local development

Collection `source` describes where a collection comes from:

- `Core`: official base-app devices.
- `OfficialPackage`: official bundled modules and public examples.
- `Local`: local developer devices inside this project.
- `Package`: local developer devices inside embedded packages.

`Local` and `Package` are for tinkering. They are available as `LocalDeveloperOnly` when the application id ends with `OpenMultiLab`. In normal `OpenSoundLab` builds they are hidden from menus and unavailable devices load as ghosts, preserving the patch instead of deleting devices or cables. Use this path for personal experiments, workshops and private research builds.

Device product flags live on each device entry:

- `enabled`: registers the device at all.
- `showInMenu`: shows the device in normal menu categories.
- `isPurchasable`: marks the device as something official Store UI may present.
- `requiresEntitlement`: locks the device unless ownership or host-session access is present.
- `storeSku`: primary logical Store SKU.
- `storeSkus`: extra logical SKUs, usually bundles.

Included devices use `isPurchasable = false`, `requiresEntitlement = false` and empty SKU fields. Official paid devices use `isPurchasable = true`, `requiresEntitlement = true` and one or more catalog SKUs. A bundle is represented by putting the same SKU on several devices.

`requiresEntitlement` controls runtime access. `isPurchasable` controls whether official Store UI may present a purchase path.

| isPurchasable | requiresEntitlement | Meaning |
|---|---|---|
| false | false | Included device. No Store UI and no lock. |
| true | true | Normal paid device. Store UI may present it and access requires ownership. |
| false | true | Gated but not directly sellable. Use for bundle-only, grant-only, beta, event, education or host-session content. |
| true | false | Purchasable/promoted but currently included. Use sparingly for trial, launch, edition or transition cases. |

At runtime each device resolves to `Available`, `Locked` or `Missing`. Available devices also carry a reason: `Included`, `Owned`, `LocalDeveloperOnly` or `HostSessionOwned`. `Locked` means the device is known but not owned. `Missing` means it is not available in this build or registry, for example a local developer device opened in an `OpenSoundLab` build. Locked and missing devices load as ghosts so patch structure survives until ownership or content availability changes.

Define selectable SKUs once in `Assets/OSLDevices/Store/Resources/OSLStoreProductCatalog.asset`. Device manifests still serialize SKU strings, but the inspector reads this catalog and presents dropdowns. Keep the public catalog empty or demo-only. Real platform product IDs, unreleased product names and launch plans belong in private deployment data, then official builds map platform ownership back to the logical SKUs used by `OSLDeviceEntitlements`.

Official OpenSoundLab uses `Core` for base devices, `OfficialPackage` for bundled public examples and private Store integration for official paid devices. Third-party developers may add `Local` or `Package` collections for their own work, but OSLLv1 does not allow selling OpenSoundLab derivatives or publishing them through app stores such as Meta, SideQuest, Apple or Pico. Derivatives also need their own name unless explicit trademark permission is granted. Commercial music, media and art output made with the software is allowed by the license.

## Creating devices

Use `OpenSoundLab/Devices/Create Device` to create registry-backed device scaffolds. The wizard derives the collection id, device id, namespace, prefab paths and XML data type from the fields in the window.

For a new device it creates or updates the active collection folder. Core collections use the category folder, add-on collections use the stable collection id as their folder name.

```
Assets/OSLDevices/<Source>/<CollectionFolder>/Scripts/<DeviceId>/
Assets/OSLDevices/<Source>/<CollectionFolder>/Resources/<collectionId>/<collectionId>.asset
Assets/OSLDevices/<Source>/<CollectionFolder>/Resources/<collectionId>/Prefabs/<DeviceId>.prefab
Assets/OSLDevices/<Source>/<CollectionFolder>/Resources/<collectionId>/MenuPrefabs/<DeviceId>_Menu.prefab
Assets/OSLDevices/<Source>/<CollectionFolder>/Resources/<collectionId>/Textures/<DeviceId>Symbol.png
```

After Unity recompiles, the scaffold postprocessor finishes menu-preview setup. After that you have to link device-specific Unity references in the inspector.

## Menu spawning

Menu spawning starts from the registry, not from hardcoded device lists.

`Assets/Scripts/Menu/menuManager.cs` asks `OSLDeviceRegistry` for menu-visible devices by category and creates a `menuItem` for each registration. `Assets/Scripts/Menu/menuItem.cs` loads the menu prefab, symbol and preview metadata from the registration. When the user selects an item, it spawns by canonical device id.

Local mode instantiates the registered runtime prefab directly. Networked mode delegates to `Assets/Scripts/Networking/NetworkSpawnManager.cs`, which resolves the same registry id on host or client. Spawn offsets also come from the registration.

## XML loading

Save files should store canonical collection-scoped device ids. Device scripts normally call `setDeviceType(data)` in `GetData()` so `Assets/Scripts/CoreClasses/SaveLoadInterface.cs` can save the registry-normalized id.

When loading XML, `Assets/Scripts/System/xmlSaveLoad.cs` builds the serializer from registry-visible data types. `SaveLoadInterface.cs` resolves prefabs through `OSLDeviceRegistry`, so current ids, local core ids, legacy device ids, prefab names and prefab resource paths can all map back to the correct prefab. `Assets/Scripts/System/xmlUpdate.cs` handles older XML roots through `legacyXmlRootNames` and generated registry aliases.

Compatibility metadata belongs in manifests: `legacyDeviceIds`, `legacyDataTypeNames` and `legacyXmlRootNames`. `showInMenu` controls menu display. Avoid adding new hardcoded device lists, `DeviceType` entries, Mirror `spawnPrefabs` entries or `[XmlInclude]` registrations.
