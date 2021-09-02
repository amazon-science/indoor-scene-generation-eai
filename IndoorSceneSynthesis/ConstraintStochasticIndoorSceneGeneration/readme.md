![logo](Documents/imgs/logo.png)

# Luminous Indoor Scene
[![Build Status](https://travis-ci.org/chainer/chainerrl.svg?branch=master)](https://travis-ci.org/chainer/chainerrl)
[![Coverage Status](https://coveralls.io/repos/github/chainer/chainerrl/badge.svg?branch=master)](https://coveralls.io/github/chainer/chainerrl?branch=master)
[![Documentation Status](https://readthedocs.org/projects/chainerrl/badge/?version=latest)](http://chainerrl.readthedocs.io/en/latest/?badge=latest)
[![PyPI](https://img.shields.io/pypi/v/chainerrl.svg)](https://pypi.python.org/pypi/chainerrl)

**Luminous Indoor Scene** (LIS) is a deep reinforcement learning library that implements various state-of-the-art algorithms in Python for indoor-scene synthesis. 

LIS integretes it code based on several datasets and platforms(e.g. [AI2Thor](https://ai2thor.allenai.org/)), and reinplemented state-of-the-art algorithms on [3D-front](https://ai2thor.allenai.org/).

![eg1](Documents/imgs/1.png)
![eg2](Documents/imgs/2.png)
![eg3](Documents/imgs/3.png)

## Installation

LIS is tested with Python 3.7. For other requirements, see [requirements.txt](requirements.txt).

LIS can be installed via PyPI:
```
pip install lis
```

It can also be installed from the source code:
```
python setup.py install
```
Refer to [Installation](http://chainerrl.readthedocs.io/en/latest/install.html) for more information on installation. 

## Getting started

You can try [ChainerRL Quickstart Guide](examples/quickstart/quickstart.ipynb) first, or check the [examples](examples) ready for Atari 2600 and Open AI Gym.

For more information, you can refer to [ChainerRL's documentation](http://chainerrl.readthedocs.io/en/latest/index.html).

## Table of contents

1. [Datasets](#datasets-and-platforms)
2. [Algorithms](#algorithms)
    - [Reimplementation](#subsection-a)
    - [Evaludation metrics](#subsection-b)
3. [Tools](#Tools)
    - [Rendering in Unity](#rendering-in-unity)
    - [Rendering in Blender](#Rendering-in-blender)


## Platforms


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
  - reimplementation details: 
- [PlanIT (planning and instantiating indoor scenes with relation graph and spatial prior networks)](https://dl.acm.org/doi/pdf/10.1145/3306346.3322941)
  - reimplementation details: 
- [3D-SLN (End-to-End Optimization of Scene Layout)](http://3dsln.csail.mit.edu/papers/3dsln_cvpr.pdf)
  - reimplementation details: 
- [ST-AOG (Human-centric Indoor Scene Synthesis Using Stochastic Grammar)](https://arxiv.org/pdf/1808.08473.pdf)
  - reimplementation details: 

<!-- ## Acknowledgement

This work would	not	have been possible without the	financial support of the ... We	are	grateful	to	all	of	those	with	whom	I	have	had	the	pleasure	to	work	during	this	and	other related	projects.	


## Contribution

Any kind of contribution to our work would be highly appreciated! If you are interested in contributing to ChainerRL, please read [CONTRIBUTING.md](CONTRIBUTING.md).

## License

[MIT License](LICENSE).

## Citations

To cite our work in publications, please cite our [JMLR paper](https://www.jmlr.org):


## Python API Instructions: random sample scenes for living rooms and bedrooms.
## 0. Drag this folder into unity/Assets/

(Obsolate) 1. Open and run the python notebook (for open Unity Editor): 
Python/Python Run Unity.ipynb)

## 2. Open and run the python notebook (for build scenes): 
Python/Socket-Server.ipynb

where you can specify the command including

*clear building scenes* : clean the scenes in the building setting.

*change build path:<path>* : change the path for building scenes.

*change task file path:<path>* : the json file from task game design.

*change random seed:<seed>* : specify the random seed.

*sample a new scene* : sample a new scene.

*build all scenes* : bulid all scenes.

*get building scenes* : get the name of building scenes.

*close client* : close unity editor.

## 2. Open and run the python notebook (for test): 
Python/Test Build.ipynb


# 2. Unity Instructions

Under the main folder, there are
## 1. ClickMe.unity
The main scene for generating scenes from json file;
composed by one **SceneBuilder** with two scipts: **CSceneBuilderTool** and **CSceneCopierTool**.

## 2. Basic Scene Folders: 
*Bedrooms*, *LivingRooms*, *Kitchens* are the folder for basic scenes, each folder contains 30 scenes w.r.t. ai2thor original scenes. Each scene is composed of **FPSController**, **PhysicsSceneManager**, **DebugCanvasPhysics**, **Structure**(with StructureAttr such as floor/window/curtain/door) and **Light**.

## 3.Customscripts and CustomEditor
Custom editor scripts and custom scripts:

### 3.1 In *Custom Editor*:

**CDecorationPlacer**: for debug placing decorations

**CEditorWindow**: (obsolate) see **CSceneConnectionTool**

**CFurniturePlacer**: for debug placing furniture(floor objects)

**CObjectPlacer**: for debug placing objects(non-floor objects)

**CSceneBuilder**: for debug building scenes

**CSceneConnectionTool**: [IMPORTANT] start socket-client connections with Python scripts(server) when opening Unity Editor

**CSceneCopier**: for copy scene from ai2thor(copy lights, structures, e.t.c only)

### 3.2 In *Custom Scripts*:

**CDecorationPlacerTool**: for placing decorations

**CFurniturePlacerTool**: for placing furniture

**CFurniturePool**: for holding furniture prefabs

**CJsonRule**: for reading and holding json task descriptions

**CObjecterPlacerTool**: for placing objects

**CObjectPool**: for holding object prefabs

**CParams**: [IMPORTANT] parameters to be tuned

**CRule**: addtional rules added for generating custom scenes

**CSceneBuilderTool**: for building scenes

**CSceneCopierool**: for copying scenes

**UUtils**: utility functions to calculate computer graphics problems.

## 4. Json

Json task examples

## 5. KitchenObjs

Kitchen CounterTop prefabs

## 6.Python

(See Python API part)

## 7. Rules

**BasicRule.csv**: basic rules for decoration

**FurnitureObjectRule.csv**: basic rules for furniture

**ObjectRule.csv**: basic rules for object

## 8. SceneRandomizer

Tool prefabs for building scenes

**DecorationPlacer**: link to **CDecorationPlacerTool**

**FurniturePlacer**: link to **CFurniturePlacerTool**

**ObjectPlacer**: link to **CObjectPlacerTool**

**SceneBuilder**: link to **CSceneBuilder** -->
