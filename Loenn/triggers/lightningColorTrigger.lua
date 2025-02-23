local jautils = require("mods").requireFromPlugin("libraries/jautils")

local lightningColorTrigger = {}
lightningColorTrigger.name = "FrostHelper/LightningColorTrigger"

jautils.createPlacementsPreserveOrder(lightningColorTrigger, "normal", {
    { "width", 16 },
    { "height", 16 },
    { "color1", "fcf579", "color" },
    { "color2", "8cf7e2", "color" },
    { "fillColor", "ffffff", "color" },
    { "fillColorMultiplier", 0.1 },
    { "persistent", false },
})

return lightningColorTrigger