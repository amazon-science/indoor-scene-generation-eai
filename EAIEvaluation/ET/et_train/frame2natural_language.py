import os

os.environ['ET_ROOT'] =  '/home/ubuntu/research3/ET'
os.environ['ET_DATA'] = '/home/ubuntu/research3/ET/data/'
os.environ['ET_LOGS'] = '/home/ubuntu/research3/ET/logs/'

from et_train.custom_dataset import *

from alfred.utils import data_util, helper_util, model_util

from alfred.model.train import prepare, create_model, load_data, wrap_datasets, process_vocabs

from alfred.utils.data_util import tensorize_and_pad
from alfred.utils import helper_util

from et_train.custom_dataset import *

from torch.utils.tensorboard import SummaryWriter

args = helper_util.AttrDict(
{'seed': 2, 'resume': True, 'profile': False, 'batch': 8, 'epochs': 10, 
'optimizer': 'adamw', 'weight_decay': 0.33, 'lr': {'init': 0.0001, 
'profile': 'linear', 'decay_epoch': 10, 'decay_scale': 0.1, 
'final': 1e-05, 'cycle_epoch_up': 0, 'cycle_epoch_down': 0, 'warmup_epoch': 0, 
'warmup_scale': 1}, 'action_loss_wt': 1.0, 'object_loss_wt': 1.0, 
'subgoal_aux_loss_wt': 0.1, 'progress_aux_loss_wt': 0.1, 
'entropy_wt': 0.0, 'demb': 768, 'encoder_heads': 12, 
'encoder_layers': 2, 'num_input_actions': 1, 
'encoder_lang': {'shared': True, 'layers': 2, 'pos_enc': True, 'instr_enc': False}, 
'decoder_lang': {'layers': 2, 'heads': 12, 'demb': 768, 'dropout': 0.1, 'pos_enc': True}, 
'detach_lang_emb': False, 'dropout': {'lang': 0.0, 'vis': 0.3, 'emb': 0.0, 
'transformer': {'encoder': 0.1, 'action': 0.0}}, 
'enc': {'pos': True, 'pos_learn': False, 'token': False, 'dataset': False}, 
'name': 'pretrained', 'model': 'speaker', 'device': 'cuda', 'num_workers': 12, 
'pretrained_path': None, 'fast_epoch': False, 'data': {'train': ['lmdb_human'], 
'valid': [], 'length': 30000, 'ann_type': ['lang, frames']}, 'dout': os.environ['ET_LOGS'] + '/pretrained'}
)

# warining: resume has to be true, otherwise all record shall be deleted

print(args)

# set seeds
torch.manual_seed(args.seed)
torch.cuda.manual_seed(args.seed)
random.seed(args.seed)
np.random.seed(args.seed)
torch.backends.cudnn.benchmark = False
torch.backends.cudnn.deterministic = True

# Writer will output to ./runs/ directory by default
writer = SummaryWriter()

batch_size = args.batch

dataset = CustomNaturalLanguageDataset("data/lmdb_i/", args)
dataset.name = "lmdb_human_speaker"

print(len(dataset))

loader_args = {
        'num_workers': 0,
        'drop_last': (torch.cuda.device_count() > 1),
        'collate_fn': helper_util.identity}

weights = [1 / len(dataset)] * len(dataset)
num_samples = 30000
sampler = torch.utils.data.WeightedRandomSampler(
    weights, num_samples=num_samples, replacement=True)

loader = torch.utils.data.DataLoader(dataset, batch_size = batch_size, sampler=sampler, **loader_args)

# assign vocabs to datasets and check their sizes for nn.Embeding inits
embs_ann, vocab_out = process_vocabs([dataset], args)
print(embs_ann)

# create the model
model, optimizer, prev_train_info = create_model(args, embs_ann, vocab_out)
# optimizer
optimizer, schedulers = model_util.create_optimizer_and_schedulers(0, model.args, model.parameters(), optimizer)

print(model.model)

# load validation dataset
valid_dataset = CustomNaturalLanguageDataset("data/lmdb_i/", args, split="valid")
valid_dataset.name = "lmdb_human_speaker"
valid_loader = torch.utils.data.DataLoader(valid_dataset, batch_size = batch_size,  **loader_args)
valid_best_loss = 1e6

# train on additional data
total_step = 0
for ep in range(args.epochs + 1):
    model.train()
    batch_mix_train_loss = []
    for batches in tqdm(zip(loader), total=num_samples // batch_size):
        losses_train_batches = 0
        total_step += 1
        for c, batch in enumerate(batches):
            traj_data, input_dict, gt_dict = tensorize_and_pad(
                    batch, model.args.device, model.pad)
            
            model_out = model.model.forward(
                vocab=dataset.vocab_in,
                action=gt_dict['action'], **input_dict)

            losses_train = model.model.compute_batch_loss(model_out, gt_dict)
            losses_train_batches += sum([v for v in losses_train.values()])
            
        # do the gradient step
        optimizer.zero_grad()
        losses_train_batches.backward()
        optimizer.step()
        
        batch_mix_train_loss.append(losses_train_batches.item())

        if total_step % 200 == 0:
            writer.add_scalar("train loss", np.mean(batch_mix_train_loss), total_step)
            
        #break
        
    model_util.adjust_lr(optimizer, model.args, ep, schedulers)
    model.eval()
    losses_valid_list = []
    for batch in tqdm(valid_loader):
        traj_data, input_dict, gt_dict = tensorize_and_pad(
                    batch, model.args.device, model.pad)
            
        # clip token
        # input_dict['lang'] = torch.clip(input_dict['lang'], max=len(dataset.vocab_in) - 1)

        model_out = model.model.forward(
            dataset.vocab_in,
            action=gt_dict['action'], **input_dict)

        losses_valid = model.model.compute_batch_loss(model_out, gt_dict)
        losses_valid_batch = sum([v for v in losses_valid.values()])

        losses_valid_list.append(losses_valid_batch.item())

    # record validation loss
    valid_loss_mean = np.mean(losses_valid_list)
    writer.add_scalar("valid loss", valid_loss_mean, ep)

    if valid_loss_mean < valid_best_loss:
        valid_best_loss = valid_loss_mean

        if not os.path.exists(model.args.dout):
            os.mkdir(model.args.dout)

        # save the checkpoint
        print('Saving models...')
        model_util.save_model(model, 'speaker_816_{}.pth'.format("best"), {}, optimizer=optimizer)
        #model_util.save_model(self, 'latest.pth', stats, symlink=True)
        
