# HiTUT evaluation on Luminous

## Prerequisites

1. Import the the **HiTUT** folder from original [HiTUT github](https://github.com/594zyc/HiTUT) into this folder.

## Eval

```Python
python eval_hitut.py --build-path YOUR_BUILD_PATH --model-path YOUR_MODEL_PATH --trial-path YOUR_TRIAL_PATH 
```
where **YOUR_BUILD_PATH** locates the executable Unity file for evalutation; **YOUR_MODEL_PATH** locates checkpoint of the model, and **YOUR_TRIAL_PATH** locates the task folder.