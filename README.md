# SquatCounter
Beat Saber squat counter for OBS overlays

## Creating the overlay
- Recommended way is to add a Browser source to OBS and set the URL to http://catse.net/squatcount  
<sup>Tip: You can save that page to disk and use the local file as the URL, that way it'll work offline in case the site goes down sometimes</sup>
- Another option is to add a Text source to OBS, select "Read from file" in its properties and set the file path to `YourBeatSaberFolder\UserData\SquatCount.txt`. This method is not as good since there will be some delay between counter updates
## Configuration
In case you want to make adjustments to the squat detection behaviour here's an explanation of the config file UserData/SquatCounter.json  
The default config file looks something like this:  
```
{  
  "Timestamp": 1659327359,  
  "StandingThreshold": 0.2,  
  "SquatThreshold": 0.0  
}
```
The threshold values are height in meters relative to the bottom edge of squat walls. By default, a squat is detected as soon as your head goes below a squat wall, after that you have to get up to reach at least 0.2m above the bottom of the wall before another squat can be registered.

![image](https://user-images.githubusercontent.com/45233053/182373884-e1408ba8-2c10-43b9-b991-49c32bbf11eb.png)
