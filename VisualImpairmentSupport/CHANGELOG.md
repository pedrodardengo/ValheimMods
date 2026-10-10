# Changelog

## 1.2.1 - 2026-10-10

### Added

- Added a live ConfigurationManager color preview for the RGB outline sliders.
- Added a dropdown of installed Windows speech voices, labeled with their locale.

## 1.2.0 - 2026-10-10

### Added

- Added separate red, green, and blue sliders (0-255) for important item outlines.
- Put voice activation, spoken text, speed, and volume together under the English `Voice` category.

### Removed

- Removed the optional total item count, manual voice selection, and separate middle-click toggle.

## 1.1.2 - 2026-10-09

### Removed

- Removed the bundled `ItemAnnouncer.Speaker.exe`; voice announcements now use the Windows PowerShell 5.1 host.

## 1.1.1 - 2026-10-09

### Added

- Added voice announcements for items under the cursor when pressing the middle mouse button (M3).
- Integrated item announcements with the inventory accessibility mod.
- Added the bundled Windows text-to-speech helper.

### Improved

- Organized source code by feature under `src/`.
- Kept inventory item highlights for weapons, armor, tools, shields, utility items, and ammunition.