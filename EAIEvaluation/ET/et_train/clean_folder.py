import os
import shutil
from tqdm.auto import tqdm

 

train_root = "data/new_genereted716/train/"

for train_task in tqdm(os.listdir(train_root)):
    if "clean" in train_task:
        trial_folder = os.path.join(train_root, train_task)
        shutil.rmtree(trial_folder)