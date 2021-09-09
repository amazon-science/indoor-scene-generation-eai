## Quickstart

Follow the instruction to generate indoor scene by CSSG on Unity Editor:

1. Clone AI2Thor:
```bash
$ git clone https://github.com/allenai/ai2thor
$ cd ai2thor/unity/Assets 
```

2. Clone the **Luminous** folder into *unity/Assets/* and rename it as *Custom*

```bash
$ mv Luminous/ Custom/ 
```

3. Follow the image instructions to generate scenes from CDF:

![cssg_1](../../Documents/imgs/cssg_1.png)
First, locate the **clickme.unity** scene file and select the **SceneBuilder** in the game view

![cssg_2](../../Documents/imgs/cssg_2.png)

Then, select the **room type** in the inspector and click **Load merged json \& generate from all scenes** button to generate scenes based on CDFs.

![cssg_3](../../Documents/imgs/cssg_3.png)
The generated scenes are saved into **Custom/BuildScenes/**.



## Algorithms

|   Algorithm   | Scene Graph Inference | Scene Generation | Constrained | RGBD rendering |
|:-------------:|:---------------------:|:----------------:|:-----------:|:--------------:|
|     PlanIT    |           ✓           |         x        |      x      |        x       |
|     Grains    |           x           |         ✓        |      ✓      |        ✓       |
|     3DSLN     |           x           |         ✓        |      ✓      |        ✓       |
| Human-Centric |           x           |         ✓        |      x      |        x       |
|      CSSG     |           ✓           |         ✓        |      ✓      |        ✓       |


Following algorithms original based on [SUNCG](https://sscnet.cs.princeton.edu/) have been implemented in this repository on [3D-Front](https://arxiv.org/abs/2011.09127):
- [Grains (Generative Recursive Autoencoders for INdoor Scenes)](https://arxiv.org/pdf/1807.09193.pdf)
<!-- - [PlanIT (planning and instantiating indoor scenes with relation graph and spatial prior networks)](https://dl.acm.org/doi/pdf/10.1145/3306346.3322941) -->
- [3D-SLN (End-to-End Optimization of Scene Layout)](http://3dsln.csail.mit.edu/papers/3dsln_cvpr.pdf)
- [ST-AOG (Human-centric Indoor Scene Synthesis Using Stochastic Grammar)](https://arxiv.org/pdf/1808.08473.pdf)



