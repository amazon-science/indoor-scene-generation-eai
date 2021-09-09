# ET evaluation on Luminous

## Prerequisites

1. Import the the **ET** folder from original [ET github](https://github.com/alexpashevich/E.T.) into this folder.

Clone repo:
```bash
$ git clone https://github.com/alexpashevich/E.T..git ET
$ export ET_ROOT=$(pwd)/ET
$ export ET_LOGS=$ET_ROOT/logs
$ export ET_DATA=$ET_ROOT/data
$ export PYTHONPATH=$PYTHONPATH:$ET_ROOT
```
Install requirements:
```bash
$ virtualenv -p $(which python3.7) et_env
$ source et_env/bin/activate

$ cd $ET_ROOT
$ pip install --upgrade pip
$ pip install -r requirements.txt
```

Download ET checkpoints
```bash
$ wget http://pascal.inrialpes.fr/data2/apashevi/et_checkpoints.zip
$ unzip et_checkpoints.zip
$ mv pretrained $ET_LOGS/
```

## Eval

```Python
python eval_et.py --build-path YOUR_BUILD_PATH --model-path YOUR_MODEL_PATH --trial-path YOUR_TRIAL_PATH 
```
where **YOUR_BUILD_PATH** locates the executable Unity file for evalutation; **YOUR_MODEL_PATH** locates checkpoint of the model, and **YOUR_TRIAL_PATH** locates the task folder.