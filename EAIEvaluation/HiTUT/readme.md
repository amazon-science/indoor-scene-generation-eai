# HiTUT evaluation on Luminous

## Prerequisites

1. Import the the **HiTUT** folder from original [HiTUT github](https://github.com/594zyc/HiTUT) into this folder.

```
git clone https://github.com/594zyc/HiTUT
conda create --name hitut python=3.7
conda activate hitut
cd HiTUT/
pip install -r requirements.txt
```
2. Download required models only

```
echo "Downloading pretrained Mask-RCNN models"
gdown https://drive.google.com/uc?id=11p9x-TXSaLkdvWQGg6vKlLXlAo08vW4a -O models/detector/mrcnn_object.pth
gdown https://drive.google.com/uc?id=1CiIGJxhH6z9Up5Yqqj-XLdSqdD-uAiD7 -O models/detector/mrcnn_receptacle.pth

mkdir exp
echo "Downloading the trained HiTUT model"
gdown https://drive.google.com/uc?id=1ykVUiXOrFTqHIdaOsyYEKeqZQzMngPR0 -O exp/Jan27-roberta-mix.zip
unzip exp/Jan27-roberta-mix.zip -d exp
```

3. download data
```
echo "Downloading raw trajectory data (71MB) and our preprocessed data (2.6GB)"
cd data
gdown https://drive.google.com/uc?id=1i9BxkrktNqt4TWkzwF1S7qf_ASgG894b -O full_2.1.0_pp.tar.gz
tar -zxf full_2.1.0_pp.tar.gz
cd ..
```

## Eval

```Python
python eval_hitut.py --build-path YOUR_BUILD_PATH --model-path YOUR_MODEL_PATH --trial-path YOUR_TRIAL_PATH 
```
where **YOUR_BUILD_PATH** locates the executable Unity file for evalutation; **YOUR_MODEL_PATH** locates checkpoint of the model, and **YOUR_TRIAL_PATH** locates the task folder.