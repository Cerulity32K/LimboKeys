# LimboKeys
A recreation of the key maze in chron44 and MindCap's final part in the Geometry Dash level Limbo.

# How to play
Upon launching the game, 8 white circles will appear and grow into place. One circle will flash green. This is the key you must follow.\
After 8 beats, the key will stop, and all keys will start moving. They will swap places with one another for 16 beats.\
After that, they will chaotically move into a spinning circle formation for 16 more beats, before ending the shuffling and slowing down.\
You must choose the key you were following. An end screen will show after.\
`Success` will show if the flashing green key at the beginning is the same one that was chosen at the end.\
`Try again` will show if the chosen key was incorrect, and the correct key will be shown underneath.

# Customization
You can modify the `config.json` file however you like. These are the properties:\
spinSpeed: How fast the end circle spins.\
rampSpeed: How fast the keys will speed up their shuffling in the last 16 beats of the game.\
circleWidth: Horizontal radius (in pixels) of the ending circle.\
circleHeight: Vertical radius (in pixels) of the ending circle.\
xSpacing: How horizontally far apart (in pixels) the keys are in the starting grid.\
ySpacing: How vertically far apart (in pixels) the keys are in the starting grid.\
keyRadius: The radius (in pixels) of the keys.\
winScale: The scaling factor of the window. Affects everything. Use for easy adjustment if the scaling is wrong.\
textScale: The scaling factor of the text.\
shakeStrength: The strength of the shaking in the last few beats of the game.\
easing: Can be "linear", "sine", "sineIn", "sineOut", or "backOut".\
debug: Prints out information for debugging purposes.
