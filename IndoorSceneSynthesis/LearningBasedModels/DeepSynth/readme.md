# Deep-Synth on 3D-FRONT dataset

## Prerequisites
1. Copy the the **deep-synth** folder from original [deep-synth](https://github.com/brownvc/deep-synth) github into this folder.
2. Follow the preprocessing steps in **CIndoorSceneSynthesis/3DFrontToolBox** to process 3D-FRONT dataset into the correct training format.

## Train

After preprocessing the 3D-FRONT dataset into the correct format, you may specify the dataroot by setting **args,data**, and refer to 

```
train_continue.ipynb
```

to get the **continuation model** in deep-synth, refer to 

```
train_location.ipynb
```

to get the **continuation model** in deep-synth, refer to 

```
train_rotation.ipynb
```

to get the **continuation model** in deep-synth. 

We refer readers to [Deep Convolutional Priors for Indoor Scene Synthesis](https://kwang-ether.github.io/pdf/deepsynth.pdf) for more details.
