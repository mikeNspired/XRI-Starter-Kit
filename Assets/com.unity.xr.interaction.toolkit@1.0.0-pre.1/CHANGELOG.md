# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

<!-- Headers should be listed in this order: Added, Changed, Deprecated, Removed, Fixed, Security -->

## [1.0.0-pre.1] - 2020-11-14

### Removed
- Removed anchor control deadzone properties from XR Controller (Action-based) used by Ray Interactor, it should now be configured on the Actions themselves

## [0.10.0-preview.7] - 2020-11-03

### Added
- Added multi-object editing support to all Editors

### Fixed
- Fixed Inspector foldouts to keep expanded state when clicking between GameObjects

## [0.10.0-preview.6] - 2020-10-30

### Added
- Added support for haptic impulses in XR Controller (Action-based)

### Fixed
- Fixed issue with actions not being considered pressed the frame after triggered
- Fixed issue where an AR test would fail due to the size of the Game view
- Fixed exception when adding an Input Action Manager while playing

## [0.10.0-preview.5] - 2020-10-23

### Added
- Added sample containing default set of input actions and presets

### Fixed
- Fixed issue with PrimaryAxis2D input from mouse not moving the scrollbars on UI as expected. ([1278162](https://issuetracker.unity3d.com/product/unity/issues/guid/1278162))
- Fixed issue where Bezier Curve did not take into account controller tilt. ([1245614](https://issuetracker.unity3d.com/product/unity/issues/guid/1245614))
- Fixed issue where a socket's hover mesh was offset. ([1285693](https://issuetracker.unity3d.com/product/unity/issues/guid/1285693))
- Fixed issue where disabling parent before `XRGrabInteractable` child was causing an error in `OnSelectCanceling`

## [0.10.0-preview.4] - 2020-10-14

### Fixed
- Fixed migration of a renamed field in interactors

## [0.10.0-preview.3] - 2020-10-14

### Added
- Added ability to control whether the line will always be cut short at the first raycast hit, even when invalid, to the Interactor Line Visual ([1252532](https://issuetracker.unity3d.com/product/unity/issues/guid/1252532))

### Changed
- Renamed `OnSelectEnter`, `OnSelectExit`, `OnSelectCancel`, `OnHoverEnter`, `OnHoverExit`, `OnFirstHoverEnter`, and `OnLastHoverExit` to `OnSelectEntered`, `OnSelectExited`, `OnSelectCanceled`, `OnHoverEntered`, `OnHoverExited`, `OnFirstHoverEntered`, and `OnLastHoverExited` respectively.
- Replaced some `ref` parameters with `out` parameters in `ILineRenderable`; callers should replace `ref` with `out`

### Fixed
- Fixed Tracked Device Graphic Raycaster not respecting the Raycast Target property of UGUI Graphic when unchecked ([1221300](https://issuetracker.unity3d.com/product/unity/issues/guid/1221300))
- Fixed XR Ray Interactor flooding the console with assertion errors when sphere cast is used ([1259554](https://issuetracker.unity3d.com/product/unity/issues/guid/1259554), [1266781](https://issuetracker.unity3d.com/product/unity/issues/guid/1266781))
- Fixed foldouts in the Inspector to expand or collapse when clicking the label, not just the icon ([1259683](https://issuetracker.unity3d.com/product/unity/issues/guid/1259683))
- Fixed created objects having a duplicate name of a sibling ([1259702](https://issuetracker.unity3d.com/product/unity/issues/guid/1259702))
- Fixed created objects not being selected automatically ([1259682](https://issuetracker.unity3d.com/product/unity/issues/guid/1259682))
- Fixed XRUI Input Module component being duplicated in EventSystem GameObject after creating it from UI Canvas menu option ([1218216](https://issuetracker.unity3d.com/product/unity/issues/guid/1218216))
- Fixed missing AudioListener on created XR Rig Camera ([1241970](https://issuetracker.unity3d.com/product/unity/issues/guid/1241970))
- Fixed several issues related to creating objects from the GameObject menu, such as broken undo/redo and proper use of context object
- Fixed issue where GameObjects parented under an `XRGrabInteractable` did not retain their local position and rotation when drawn as a Socket Interactor Hover Mesh ([1256693](https://issuetracker.unity3d.com/product/unity/issues/guid/1256693))
- Fixed issue where Interaction callbacks (`OnSelectEnter`, `OnSelectExit`, `OnHoverEnter`, and `OnHoverExit`) are triggered before interactor and interactable objects are updated. ([1231662](https://issuetracker.unity3d.com/product/unity/issues/guid/1231662), [1228907](https://issuetracker.unity3d.com/product/unity/issues/guid/1228907), [1231482](https://issuetracker.unity3d.com/product/unity/issues/guid/1231482))

## [0.10.0-preview.2] - 2020-08-26

### Added
- Added XR Device Simulator and sample assets for simulating an XR HMD and controllers using keyboard & mouse

## [0.10.0-preview.1] - 2020-08-10

### Added
- Added continuous move and turn locomotion

### Changed
- Changed accesibility levels to avoid `protected` fields, instead exposed through properties
- Components that use Input System actions no longer automatically enable or disable them. Add the `InputActionManager` component to a GameObject in a scene and use the Inspector to reference the `InputActionAsset` you want to automatically enable at startup.
- Some properties have been renamed from PascalCase to camelCase to conform with coding standard; the API Updator should update usage automatically in most cases

### Fixed
- Fixed compilation issue when AR Foundation package is also installed
- Fixed the Interactor Line Visual lagging behind the controller ([1264748](https://issuetracker.unity3d.com/product/unity/issues/guid/1264748))
- Fixed Socket Interactor not creating default hover materials, and backwards usage of the materials ([1225734](https://issuetracker.unity3d.com/product/unity/issues/guid/1225734))
- Fixed Tint Interactable Visual to allow it to work with objects that have multiple materials
- Improved Tint Interactable Visual to not create a material instance when Emission is enabled on the material

## [0.9.9-preview.3] - 2020-06-24

### Changed
- In progress changes to visibilty

## [0.9.9-preview.2] - 2020-06-22

### Changed
- Hack week version push.

## [0.9.9-preview.1] - 2020-06-04

### Changed
- Swaps axis for feature API anchor manipulation

### Fixed
- Fixed controller recording not working
- Start controller recording at 0 time so you dont have to wait for the recording to start playing.

## [0.9.9-preview] - 2020-06-04

### Added
- Added Input System support
- Added abiltiy to query the controller from the interactor

### Changed
- Changed a number of members and properties to be `protected` rather than `private`
- Changed to remove `sealed` from a number of classes.

## [0.9.4-preview] - 2020-04-01

### Fixed
- Fixed to allow 1.3.X or 2.X versions of legacy input helpers to work with the XR Interaction Toolkit.

## [0.9.3-preview] - 2020-01-23

### Added
- Added pose provider support to XR Controller
- Added abiilty to put objects back to their original hierarchy position when dropping them
- Made teleport configurable to use either activate or select
- Removed need for box colliders behind UI to stop line visuals from drawing through them

### Fixed
- Fixed minor documentation issues
- Fixed passing from hand to hand of objects using direct interactors
- Fixed null ref in controller states clear
- Fixed no "OnRelease" even for Activate on Grabbable

## [0.9.2-preview] - 2019-12-17

### Changed
- Rolled LIH version back until 1.3.9 is on production.

## [0.9.1-preview] - 2019-12-12

### Fixed
- Documentation image fix

## [0.9.0-preview] - 2019-12-06

### Changed
- Release candidate

## [0.0.9-preview] - 2019-12-06

### Changed
- Further release prep

## [0.0.8-preview] - 2019-12-05

### Changed
- Pre-release release.

## [0.0.6-preview] - 2019-10-15

### Changed
- Changes to README.md file

### Fixed
- Further CI/CD fixes.

## [0.0.5-preview] - 2019-10-03

### Changed
- Renamed everything to com.unity.xr.interaction.toolkit / XR Interaction Toolkit

### Fixed
- Setup CI correctly.

## [0.0.4-preview] - 2019-05-08

### Changed
- Bump package version for CI tests.

## [0.0.3-preview] - 2019-05-07

### Added
- Initial preview release of the XR Interaction framework.
