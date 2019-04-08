# TrafficSym
Traffic simulation using Lattice Boltzmann Method (LBM) for trajectories

See [this youtube video](https://www.youtube.com/watch?v=rK6cf9I1Hfw) for more details.

![video thumbnail](https://img.youtube.com/vi/rK6cf9I1Hfw/0.jpg)


# Instructions
Simulation is controlled by 3 tabs (labeled 0, 1, 2 in upper left corner of screen). To change it, press [0, 1, 2] accordingly.

Each tab has its purpouse:
## 0: Route configuration helper
To Be Explained...
## 1: LBM simulation for route configs
- A turns on/off automatic LBM saving and changing index
- S saves current LBM vectors to a file
- Arrow Up/Down changes current route confid ("tabLBM index")
Before running traffic simulation, each route has to have vector maps generated from start to end so that traffic knows how to drive.
Let the LBM simulation run, and once a stabilised vector stream reaches start position, change route index to next one untill all routes have vector maps generated
## 2: Traffic simulation
- A turns on/off automatic traffic spawning
- Q brute-spawns traffic 
- Mouse click selects nearest car
- Arrows override selected car driving control


# TODO
- Visualize and extend route config editor on "0" tab
- Write documentation about creating maps and settings
