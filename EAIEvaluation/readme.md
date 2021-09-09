# EAI  Evaluation for Alfred challenge

In section, we present how to evaluation three state-of-the-art model on Luminous generated scenes.

- MOCA
- ET
- HiTUT 

## Prerequisites

1. Build scenes from Unity Editor scenes and by referring to **IndoorSceneSynthesis/ConstraintStochasticIndoorSceneGeneration/** part, and record **YOUR_BUILD_PATH** as Unity execution for evaluaiton.

2. Get the trajectories path by referring to **Luminous4Alfred/TaskSolver/** part, and record **YOUR_TRIAL_PATH** as the folder path for task trajectories.

The *eval_xx.py* can be *eval_et.py*, *eval_moca.py* or *eval_hitut.py* in the subfolders.