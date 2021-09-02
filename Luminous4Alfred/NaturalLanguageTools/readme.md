# Natural Language Tool

We also provide a natural language tool to generate natural language description along **high-level** actions in ALFRED challenge. We directly apply the **Speaker** model provided by [ET](https://github.com/alexpashevich/E.T.), while providing a customized data preprocessing pipline which provides low-level actions, high-level descriptions and agent's camera images for train the model. For more information about the **Spear**, we refer to [Episodic Transformers](https://github.com/alexpashevich/E.T.) for more details.

## Prerequisites

1. Import the the **ET** folder from original [ET github](https://github.com/alexpashevich/E.T.) into this folder.

2. [ET BUG REPORT] fix the bug in ET's [Speaker](https://github.com/alexpashevich/E.T./blob/e9cfa45bb7babfa3c8218c1268f27c7fad158129/alfred/model/speaker.py#L228), change line 228 into

```python
lang_cur[j].append(tokens_out[j, -1].item())
```
## Data processing

We follow the process of ET to preprocess the [ALFRED](https://github.com/askforalfred/alfred) dataset, however, we abandon the process to provide the **lmdb** format to load the dataset, and keep the preprocessed dataset as the original forms suggested by ALFRED: convolutional features, language tokens, and action labels.

Comment line line 204-227 in [create_lmdb.py](https://github.com/alexpashevich/E.T./blob/e9cfa45bb7babfa3c8218c1268f27c7fad158129/alfred/data/create_lmdb.py#L204) and run

```python
create_lmdb.py
```
To the preprocessed dataset for ALFRED challenge.

Or, we provide the processed dataset to train the language model by [this link](????)

## Train Natural Language model

To train the Speaker to generate natural language descriptions, run

```
python train_speaker.py
```

by specifying the variable include $ET_ROOT, ET_DATA, ET_LOGS$. We also provide a pre-trained Speaker from [this link](????)

## Language generations

We provides three ways to generate natural language description for ALFRED trajectories Set the *LANGUAGE_CHOICE* in *add_lan_desc.py* as *Templete/Natural/Mix* to switch between the three ways.

- **Templete**: to generate descriptions from a language template, training the Speaker is **not** required.
- **Natural**: to generate descriptions from the Speaker we trained.
- **Mix**: to generate descriptions for interactions from *Template*, and navigation descriptions from *Natural*, it is also the way to use for testing the SOTA models for ALFRED challenge.

After setting the pre-processed ALFRED/Luminous dataset and set the *DATASET_ROOT* in the script, run

```python
python et_train/add_lan_desc.py --dataset-root YOUR_DATASET_ROOT --language-choice YOUR_LANGUAGE_CHOICE
```

to generate the language descriptions. This process will rename the original **traj_data.json** as **~traj_data.json** and generate a new **traj_data.json** as training data.
