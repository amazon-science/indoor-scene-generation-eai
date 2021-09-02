# Task Parser: generation indoor scene description parts in CDF from ALFRED

The task parser directly load the *traj_data.json* from ALFRED urls **without** downloading the ALFRED dataset to get the indoor-scene information.

change the *write_folder_root* in *json2cdf.py* to specify the folder you need to save the CDFs. Then refer to 

```python
python json2cdf.py --train-save-path YOUR_TRAINING_DATA_SAVE_FOLDER --valid-save-path YOUR_VALIDATION_DATA_SAVE_FOLDER --test-save-path YOUR_TESTING_DATA_SAVE_FOLDER
```

to obtain the indoor scene descrption part and task description part of CDF for ALFRED challenge.