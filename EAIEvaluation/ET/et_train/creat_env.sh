

git clone https://github.com/alexpashevich/E.T..git ET

# create env
conda create --name et_env python=3.7
conda activate et_env

export ET_ROOT=$(pwd)/ET2
export ET_LOGS=$ET_ROOT/logs
export ET_DATA=$ET_ROOT/data
export PYTHONPATH=$PYTHONPATH:$ET_ROOT

pip install -r requirements.txt

# load pretrained models
wget http://pascal.inrialpes.fr/data2/apashevi/et_checkpoints.zip
unzip et_checkpoints.zip
mv pretrained $ET_LOGS/

# get console
sudo apt-get install xorg openbox
cd ..
cd ..
git clone https://github.com/askforalfred/alfred
cd alfred
sudo python scripts/startx.py 0


# render training data
cd $ET_ROOT
python -m alfred.data.create_lmdb with args.visual_checkpoint=$ET_LOGS/pretrained/fasterrcnn_model.pth args.data_output=lmdb_human args.vocab_path=$ET_ROOT/files/human.vocab

# add language description to generated data

# render 45k data
# python et_train/add_lan_desc.py
python -m alfred.data.create_lmdb with args.data_input=generated_45k_1 args.visual_checkpoint=$ET_LOGS/pretrained/fasterrcnn_model.pth args.data_output=lmdb_synth_45K args.vocab_path=$ET_ROOT/files/human.vocab

# render new data
# python et_train/add_lan_desc.py
# python -m alfred.data.create_lmdb with args.visual_checkpoint=$ET_LOGS/pretrained/fasterrcnn_model.pth args.data_output=lmdb_human args.vocab_path=$ET_ROOT/files/human.vocab


