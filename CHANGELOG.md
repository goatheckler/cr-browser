# Changelog

## [Unreleased]

### Features

* add custom OCI registry support with detection endpoint
  - New `POST /api/registries/detect` endpoint validates custom registry URLs for OCI Distribution v2 compatibility
  - `CustomOciRegistryClient` extends `OciRegistryClientBase` to support any OCI-compliant registry
  - Frontend `CustomRegistryInput` component with URL validation and detection flow
  - Query parameter `customRegistryUrl` added to `/api/registries/{type}/{owner}/images` and `/api/registries/{type}/{owner}/{image}/tags` endpoints
  - Custom registry type doesn't support catalog listing (empty for `/images` endpoint)
  - E2E tests cover detection dialog, URL validation, and error handling

### Added

* Registry URL display field for all registry types (built-in and custom)
* Check button for custom registry validation in main UI
* Conditional enabling/disabling of controls based on custom registry validation state
* Validation modal for custom registries triggered from main UI
* 18 new E2E tests for custom registry validation workflow
* Tests for registry URL display, validation flow, URL changes, validation failures, and registry type switching

### Changed

* Custom registry validation now happens in main UI instead of browse modal
* Owner and image fields are cleared when custom registry URL changes
* Browse and search buttons disabled until custom registry is validated
* Updated existing custom registry tests to match new validation-first workflow

### Fixed

* Registry URL display now correctly shows custom registry URL when selected
* Custom registry modal pre-fills with URL from main page
* URL field remains editable after validation to allow corrections
* Validation state properly resets when custom registry URL is modified

## [1.1.3](https://github.com/goatheckler/ghcr-browser/compare/v1.1.2...v1.1.3) (2025-10-06)


### Bug Fixes

* remove skip condition to allow release creation on release PR merge ([fe700c0](https://github.com/goatheckler/ghcr-browser/commit/fe700c00745c5cca7d83f46fdf759406a0a3bfb4))
* remove skip condition to allow release creation on release PR merge ([381e6b6](https://github.com/goatheckler/ghcr-browser/commit/381e6b6b0310b7d4c80ad8a10ad4b821656ea30a))

## [1.1.2](https://github.com/goatheckler/ghcr-browser/compare/v1.1.1...v1.1.2) (2025-10-06)


### Bug Fixes

* remove broken branch check that prevented build workflow from ru… ([9ddc569](https://github.com/goatheckler/ghcr-browser/commit/9ddc5699b213c2499c3b6e41d2c67a12fd6c381e))
* remove broken branch check that prevented build workflow from running on releases ([5fc6423](https://github.com/goatheckler/ghcr-browser/commit/5fc6423e8f2e80a550b73e4fdd4d21fdc1857138))
* use PAT for release-please to enable workflow triggers on releas… ([1140b5b](https://github.com/goatheckler/ghcr-browser/commit/1140b5bc533e27074d40d1ad40db36662bc77854))
* use PAT for release-please to enable workflow triggers on release PRs ([a3c78b9](https://github.com/goatheckler/ghcr-browser/commit/a3c78b9176d540af1482ed38df8394c48a613fb9))

## [1.1.1](https://github.com/goatheckler/ghcr-browser/compare/v1.1.0...v1.1.1) (2025-10-06)


### Bug Fixes

* use startsWith instead of contains to prevent skipping release P… ([1b94e3c](https://github.com/goatheckler/ghcr-browser/commit/1b94e3c7ce3e769fff45f1a985ca6c2bf030fa56))
* use startsWith instead of contains to prevent skipping release PR merges ([65820cd](https://github.com/goatheckler/ghcr-browser/commit/65820cdb0770d671f8286cd3910464ff10fe877d))

## [1.1.0](https://github.com/goatheckler/ghcr-browser/compare/v1.0.0...v1.1.0) (2025-10-06)


### Features

* auto-merge release-please PRs to enable automatic releases ([09437fa](https://github.com/goatheckler/ghcr-browser/commit/09437fa00d8a8d22ebeb6d97378588e094ea0390))
* configure release-please to skip PR and create releases directly ([34a0e1a](https://github.com/goatheckler/ghcr-browser/commit/34a0e1aee74a96b710a442fcddde0e673604dd49))


### Bug Fixes

* add checkout step for gh cli commands ([3156187](https://github.com/goatheckler/ghcr-browser/commit/3156187967bfe91cc7b60e5b98da7cab6d8028d5))
* explicitly set release-please config file paths ([134b7fb](https://github.com/goatheckler/ghcr-browser/commit/134b7fb2e1a2e807e6d82618173659dd1fdd1bcc))
* extract PR number from release-please JSON output ([dd420b6](https://github.com/goatheckler/ghcr-browser/commit/dd420b616ca8f3a5822abf2768dd15e35108be38))
* extract PR number from release-please JSON output ([200f858](https://github.com/goatheckler/ghcr-browser/commit/200f858545ff23f263bbad0f7a13b487121bfcf5))
* prevent infinite loop by checking commit message instead of pusher ([63097ba](https://github.com/goatheckler/ghcr-browser/commit/63097ba849d935954d9d4d5f7ac3c060b62b1fd9))
* prevent release-please infinite loop by skipping when pushed by github-actions bot ([45c33ed](https://github.com/goatheckler/ghcr-browser/commit/45c33edc1d52dbff99a6f2b74e4f1b4c63438bc8))
* remove unsupported FontAwesome icons from mermaid diagram ([3722ea5](https://github.com/goatheckler/ghcr-browser/commit/3722ea52856bac2d02094cbbd780db16cfa52e97))
* remove unsupported FontAwesome icons from mermaid diagram ([2686f32](https://github.com/goatheckler/ghcr-browser/commit/2686f32fdbe3948f9f7c6da1939844b11f4c83c9))
* restore PR-based release workflow with auto-merge ([0ff74dc](https://github.com/goatheckler/ghcr-browser/commit/0ff74dcdc725ee882c10693a1fdcf157c39848f2))
* restore PR-based release workflow with auto-merge ([9f7a168](https://github.com/goatheckler/ghcr-browser/commit/9f7a168b16e04e08f228a9354341f22d20438acc))


### Maintenance

* test release-please permissions ([4ddec1d](https://github.com/goatheckler/ghcr-browser/commit/4ddec1d60e3943de6b63f8d20af62c21386ec146))
* trigger release-please test ([11675d9](https://github.com/goatheckler/ghcr-browser/commit/11675d9e66776684d3f1952b72056d1d4a2b38ab))
* trigger release-please test ([d31c22a](https://github.com/goatheckler/ghcr-browser/commit/d31c22af8daa787d19e362c957c3b52f0dfedcd8))
