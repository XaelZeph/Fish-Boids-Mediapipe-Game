Assignment \#2  
Interactive Computation Report  
Introduction \+ Motivation:   
During class time, we were shown past submissions for this assignment. I was particularly inspired by one assignment where a fish boids system interacted with mouse inputs. I was also motivated by the Boids system I created during N-body, as I had already coded in repelling forces and a centering force (if they go off screen, they swim back on); so the bodies (fish) both swim towards each other, but refuse to clump up, leading to a better swimming visualization.  
(No image for the fish game, sorry\!)  
	I expanded on their project by (1) adding body detection (MediaPipe) for an even more challenging, immersive experience and (2) adding an actual game component. For the game component, I was inspired by the Chicken Boids game inside of the course materials. I liked how simple the gameplay was, yet addictive. (I saw multiple students spend minutes playing the game, getting sucked-in to get that last chicken in.)

(GITIMAGES/chickenboids.png)
(Chicken Boid Game)  
However, instead I wanted the entities to follow *my* movement, rather than run away. This interaction reminded me of certain Real-Time Strategy (RTS) games (such as Warcraft or Starcraft), where you build armies and bring them into battle by clicking for their next position. Certain games let you build massive armies, and I always found the boid system’s movement from the frantic clicking forward, then backwards, then forwards again to maneuver against enemy armies oddly satisfying. When I was bored, I would often click in a circle, just to see how they ran around to chase my next position. I wanted to create something similar, where you’re tasked to keep a group of fish safe; to maneuver them into safe positions away from enemies.

(GITIMAGES/Warcraft-III-5-Iconic-Human-Army-Compositions-to-Use-GP5.jpg)
(Warcraft Gameplay, from [Warcraft III: 5 Iconic Human Army Compositions to Use](https://www.keengamer.com/articles/features/others/warcraft-iii-5-iconic-human-army-compositions-to-use/))

Methods:  
I’m very proud of my unique code modifications for my n-body assignment, so I reused it with these specific major changes:   
(1) Mediapipe will now spawn 2 hands; when the hands are closed, they do not interact with the fish, when the hands are open they “drop food,” aka spawn in a spherical mass which slowly degrades over time. There should be 2 assets to reflect this open and closed hands. Since the number of food increases and decreases (possibly 0 at times), for efficiency, use List which allows for mutable sizing. For simplicity, “closed” is the pinch bool calculated in base Mediapipe given in course materials.  
(2) Food should be generated at a set rate, otherwise generating every frame the hand is open can cause an overload of food (leading to unintended behavior and frame drops from increased calculations). Food should also have a set degradation rate. Min size was also set, so that negligible food (i.e. m \< 10), doesn’t clog up space (and increase calculation time).   
(3) The fish’s acceleration towards the food should linearly increase depending on the distance to the food. For example, the farther the fish, the faster it should run towards the food.  
(4) Once the fish gets close enough to the food (radius around food), slow down. Otherwise, it will accelerate PAST the food, usually causing continuous acceleration (as they run past the food, they are once again far away, so they accelerate even faster).  
(5) To account for *possible* continuous acceleration, set max velocity. Since the fish is also decelerating near the food, it may also slow to a stop; this is not behavior we want, so also set min velocity.  
(6) Collisions with the predator should delete (‘eat’) the fish. Due to the amount of fish being spawned, instead of creating colliders, just calculate distance between fish and predator. Since the fish population is also changing, change the fish array into a List to decrease time complexity.  
(7) The fish must look in the rotation that they are swimming in. I found that setting rotation speed to twelve (12) allowed my computational fish to mimic the jerky/flicking movements of real smaller fish (like minnows). I found this very charming (utilizing a computational limit).  
(8) Enemy behavior should spawn randomly, then linearly move towards a negative version of the spawn location (this is to force it to move towards the center of the screen, where the player is most likely to be). However, I did find that the random spawning had players scrambling; perhaps because the player is assuming the enemy to target the fish’s current location, rather than a random one? Another issue with random destinations is when there’s no food, and the fish spread out, it’s unlikely for the enemy to hit any of them (leading to if the player does nothing, they’ll win). For future testing, see if these end destinations may work better: averaged location of the fish, averaged location of the food, where the player hand currently is (may have issues if both hands are on detected).  
(9) The difficulty of the game can be increased / decreased by these parameters: time needed to stay alive, speed of enemy, coalescence radius around the food, speed of fish, food drop rate, food degradation rate.

(GITIMAGES/fishdiagram.jpg)
(Diagram of design justifications)  
Other possible future implementations: interactable obstacles the fish must go through/around to escape the predator.