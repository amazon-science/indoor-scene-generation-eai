# Deep-Synth on 3D-FRONT dataset

## Prerequisites
1. Copy the the **3D-SLN** folder from original [3D-SLN](https://github.com/aluo-x/3D_SLN) github into this folder.
2. Follow the preprocessing steps in **CIndoorSceneSynthesis/3DFrontToolBox** to process 3D-FRONT dataset into the correct training format.

## Train

After preprocessing the 3D-FRONT dataset into the correct format, you may specify the dataroot by setting **args._train.data**, and run

```
python train_3dsln.py
```

We refer readers to [End-to-End Optimization of Scene Layout](http://3dsln.csail.mit.edu/) for more details.
