# Visual Impairment Support

I made this mod for a friend of mine who plays Valheim and has significant vision impairment due to the loss of part of his optic nerve.

## Features

### Important Item Highlight
The mod adds a red outline to inventory slots containing weapons, armor, or tools. Shields, utility items, and ammunition are highlighted as well. Other items are left unoutlined. This is a visual change only; it does not affect item behavior or properties. The outlines make it easier to spot which items in his inventory are important and worth keeping.

### Item type and quantity announcer
Click the middle mouse button (M3) over an item in the player inventory or a container to hear its localized name and stack count using Windows text-to-speech. Click M3 over a recipe at a crafting station to hear the product name and its required materials and quantities. The ConfigurationManager settings put voice activation, spoken text, speed, volume, and a dropdown of installed speech voices under `Voice`. The outline's red, green, and blue channels are configurable separately under `Important Item Highlight`, each from 0 to 255, with a live color preview.

The voice feature uses Windows PowerShell 5.1 and Windows Speech. No custom executable is bundled; the mod starts the PowerShell installation included with Windows and uses voices installed in the operating system.

## Inventory

![Inventory example showing red outlines around weapons, armor, and tools](image.png)

If you also have a visual impairment and would like additional accessibility features, I am open to hearing your ideas and considering new features for the mod.

## Credits

Thank you Shadow for the the code for the item announcer.