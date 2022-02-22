# Field-of-Vision-R

## Installation
```R
detach("package:OPI", unload = TRUE)
install.packages("~/GitHub/OPI-R/pkg/OPI_2.9.3.tar.gz", repos = NULL, type = "source")
```

## Library
```R
require("OPI")
chooseOpi("Vive")
```

## opiInitialize
Opens a connection to the socket server. The Vive application must already be running.
```R
opiInitialize()
```

## opiSetBackground
Updates the background and fixation point with the following parameters. Parameters with a "default" value are optional.
| Parameters      | Description |
| -----------     | ----------- |
| lum             | Currently ignored. Background opacity, as a percentage, between 0.00 and 100.00 (default).|
| color           | Color of the background, can be 'black' (default), 'white', 'yellow', 'clear', 'grey', 'gray', 'magenta', 'cyan', 'red', 'blue', or 'green'.  |
| rgb             | Color of the background as string of RGB values separated by spaces, such as '255 255 255', optional, overrides color parameter. |
| fixation        | Can only be cross at the moment.  |
| fix_cx, fix_cy  | Fixation (x, y) coordinate positions. |
| fix_sx, fix_sy  | Dimensions of fixation target as scalar multipliers.  |
| fix_lum         | Fixation opacity, as a percentage, between 0.00 and 100.00 (default). |
| fix_color       | Color of the fixation target, can be 'white' (default), 'black', 'yellow', 'clear', 'grey', 'gray', 'magenta', 'cyan', 'red', 'blue', or 'green'. |
| fix_rgb         | Color of the fixation target as string of RGB values separated by spaces, such as '255 255 255', optional, overrides fix_color parameter. |
| eye             | Sets the background configuration, can be 'left', 'right', or 'both' (default). |

| Returns      | Description |
| -----------     | ----------- |
| Boolean             | True on success, false on failure. |
```R
opiSetBackground(lum=100, color="black", rgb="NULL NULL NULL", fixation="Cross", 
                 fix_cx=0.0, fix_cy=0.0, fix_sx=1.0, fix_sy=1.0, fix_lum=100.00,
                 fix_color="white", fix_rgb="NULL NULL NULL", eye="both")
```

## opiPresent
Presents a stimulus defined with the following parameters. Parameters with a "default" value are optional.
| Parameters      | Description |
| -----------     | ----------- |
| stim            | The stimulus object to present. |
| stim$x          | Fixation x coordinate, such as 1.0. |
| stim$y          | Fixation y coordinate, such as 1.0. |
| stim$level      | Stimulus level, as a percentage of opacity, between 0.00 and 100.00. |
| stim$duration   | Stimulus duration in milliseconds, such as 2000. |
| stim$responseWindow  | Stimulus response window in milliseconds, such as 3500.  |
| stim$size       | Stimulus size, as a scalar multiplier, 1.0 (default). |
| stim$eye        | Eye to present to, can be 'left', 'right', or 'both' (default). |
| stim$color      | Color of the stimulus, can be 'white' (default), 'black', 'yellow', 'clear', 'grey', 'gray', 'magenta', 'cyan', 'red', 'blue', or 'green'. |
| stim$rgb        | Color of the stimulus as string of RGB values separated by spaces, such as '255 255 255', optional, overrides stim$color parameter. |

| Returns      | Description |
| -----------     | ----------- |
| int             | Seen (1) or Unseen (0) |
| int             | Time, in milliseconds, of response, measured from presentation start. |

```R
stim <- list(x=10, y=10, level=100, size=1.0, color="white",
             duration=3500, responseWindow=3500, eye="left")
class(stim) <- "opiStaticStimulus"
opiPresent(stim)
```

## opiClose
Closes the connection and shuts down the application.
| Returns      | Description |
| -----------     | ----------- |
| Boolean             | True on success, false on failure. |

```R
opiClose()
```
