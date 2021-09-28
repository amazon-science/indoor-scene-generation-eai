import os, sys, random, json, pprint, time, shutil, csv
from itertools import chain
import argparse
import logging
import numpy as np
import torch
from torch.utils.tensorboard import SummaryWriter

from transformers import AdamW
from transformers.optimization import get_linear_schedule_with_warmup


sys.path.append(os.path.join(os.environ['ALFRED_ROOT']))

from data.dataset import AlfredDataset, AlfredPyTorchDataset

from models.config.configs import Config
from models.model.mmt import MultiModalTransformer
from models.train.optimizer import NoamOpt
from models.utils.bert_utils import bert_config

from tqdm import tqdm

def train_one_epoch(model, optimizer, data_loader, counts, loss_weights=None):
    model.train()

    count = 0
    logging.info('Training [Level: %s]' % (model.args.train_level))
    epoch_start_time = time.time()
    logging.info('************ epoch: %d ************' %(counts['epoch']))
    for batch in tqdm(data_loader):
        # count += 1
        # if count == 30:
        #     break
        if model.args.use_bert and random.random() > 0.5:
            continue

        task_type = batch['batch_type'][0]

        enable_mask = 'mani' in task_type
        enable_navi_aux = 'navi' in task_type and model.args.auxiliary_loss_navi

        type_loss, arg_loss, mask_loss, navi_losses = model(batch)
        navi_loss = sum([l for l in navi_losses.values()]) if enable_navi_aux else 0

        if model.args.weigh_loss:
            weights = loss_weights[task_type]
            w = [torch.exp(-i) for i in weights]
            loss = w[0] * type_loss + w[1] * arg_loss + w[2] * mask_loss + w[3] * navi_loss+0.5*weights.sum()
            record_loss = (type_loss + arg_loss + mask_loss + navi_loss).item()
        else:
            loss = type_loss + arg_loss + mask_loss + navi_loss
            record_loss = loss.item()

        iter_num = counts['iter_%s'%task_type]
        local_iter_num = counts['iter_%s'%task_type] % counts['dlen_%s'%task_type]

        if local_iter_num %(max(counts['dlen_%s'%task_type]//30,1)) == 0:
            lr = optimizer.param_groups[0]["lr"]
            mask_str = 'mask: %.4f |'%mask_loss.item() if enable_mask else ''
            navi_str = 'vis: %.4f |rea: %.4f |prog: %.4f |'%(navi_losses['visible'].item(),
                navi_losses['reached'].item(), navi_losses['progress'].item()) if enable_navi_aux else ''
            logging.info('[%8s iter%4d] loss total: %.4f |type: %.4f |arg: %.4f |%s%slr: %.1e'%(
                task_type, local_iter_num, record_loss, type_loss.item(), arg_loss.item(), mask_str, navi_str, lr))

            writer.add_scalar('train_loss/%s/total'%task_type, record_loss, iter_num)
            writer.add_scalar('train_loss/%s/type'%task_type, type_loss.item(), iter_num)
            writer.add_scalar('train_loss/%s/arg'%task_type, arg_loss.item(), iter_num)
            if enable_mask:
                writer.add_scalar('train_loss/low/mask', mask_loss.item(), iter_num)
            if enable_navi_aux:
                for k, v in navi_losses.items():
                    writer.add_scalar('train_loss/low/%s'%k, v.item(), iter_num)
            if model.args.weigh_loss:
                writer.add_scalars('weights/%s'%task_type, {'type': 1/w[0], 'arg': 1/w[1], 'mask': 1/w[2]}, iter_num)
                if local_iter_num %(counts['dlen_%s'%task_type]//30 * 6) == 0:
                    weights = '(' + ' '.join(['%.3f'%i.item() for i in w]) + ')'
                    logging.info('%s loss weights 1/var: (type, arg, mask)=%s'%(task_type, weights))
            writer.flush()

        counts['iter_%s'%task_type] += 1

        optimizer.zero_grad()
        loss.backward()
        optimizer.step()
        if 'scheduler' in counts:
            counts['scheduler'].step()

    et = '%.1fm'%((time.time() - epoch_start_time)/60)
    tt = time.time() - counts['start_time']
    tt = '%dh%dm'%(tt//3600, tt//60%60)
    logging.info('[%s] epoch %d finished (epoch time: %s | total time: %s)' %(model.args.train_level,
        counts['epoch'], et, tt))


def validation_check(model, split, task_type, data_loader, epoch):
    model.eval()

    enable_mask = 'mani' in task_type
    enable_navi_aux = 'navi' in task_type and model.args.auxiliary_loss_navi

    type_correct, arg_correct, mask_correct, cnt, arg_cnt, mask_cnt, navi_cnt =0, 0, 0, 0, 0, 0, 0
    navi_correct = {'visible': 0, 'reached': 0, 'progress': 0}
    for idx, batch in enumerate(data_loader):
        type_preds, arg_preds, mask_preds, navi_preds, labels = model(batch, False)

        cnt += len(type_preds)
        type_correct += (type_preds==labels['type']).sum().item()
        # if epoch > 4:
        #     print('type errors:')
        #     labelsss = labels['type'][type_preds!=labels['type']]
        #     print([(i.item(), labelsss[idx].item()) for idx, i in enumerate(type_preds[type_preds!=labels['type']])])
        if 'high' in task_type:
            arg_correct += (arg_preds==labels['arg']).sum().item()
        elif 'low' in task_type:
            arg_idx = labels['arg'] != -1
            arg_correct += (arg_preds[arg_idx]==labels['arg'][arg_idx]).sum().item()
            arg_cnt += arg_idx.sum().item()
            # if epoch > 4:
            #     print('arg errors:')
            #     labelsss = labels['arg'][arg_preds!=labels['arg']]
            #     print([(i.item(), labelsss[idx].item()) for idx, i in enumerate(arg_preds[arg_preds!=labels['arg']])])
            if enable_mask:
                mask_idx = labels['mask'] != -1
                mask_correct += (mask_preds[mask_idx]==labels['mask'][mask_idx]).sum().item()
                mask_cnt += mask_idx.sum().item()
            if enable_navi_aux:
                navi_idx = labels['visible'] != -1
                navi_preds['visible'][navi_idx]==labels['visible'][navi_idx]
                navi_correct['visible'] += (navi_preds['visible'][navi_idx]==labels['visible'][navi_idx]).sum().item()
                navi_correct['reached'] += (navi_preds['reached'][navi_idx]==labels['reached'][navi_idx]).sum().item()
                navi_correct['progress'] += (navi_preds['progress'][navi_idx]-labels['progress'][navi_idx]).square().sum().item()
                navi_cnt += navi_idx.sum().item()

    type_accu = type_correct/cnt
    arg_accu = arg_correct/cnt if 'high' in task_type else arg_correct/(arg_cnt + 1e-8)
    mask_accu = mask_correct/(mask_cnt+1e-8) if enable_mask else 0
    mask_str = ' |mask %.3f'%(mask_accu) if enable_mask else ''
    navi_str = ''
    if enable_navi_aux:
        visible_accu = navi_correct['visible']/(navi_cnt+1e-8)
        reached_accu = navi_correct['reached']/(navi_cnt+1e-8)
        progress_mse = navi_correct['progress']/(navi_cnt+1e-8)
        navi_str = '|vis: %.3f |rea: %.3f |prog: %.4f'%(visible_accu, reached_accu, progress_mse)

    logging.info('Validation [%12s %8s] accuracy type: %.3f |arg: %.3f%s%s'%(
        split, task_type, type_accu, arg_accu, mask_str, navi_str))

    writer.add_scalar('valid_accu/%s/%s_type'%(task_type, split), type_accu, epoch)
    writer.add_scalar('valid_accu/%s/%s_arg'%(task_type, split), arg_accu, epoch)
    if enable_mask:
        writer.add_scalar('valid_accu/%s/%s_mask'%(task_type, split), mask_accu, epoch)
    if enable_navi_aux:
        writer.add_scalar('valid_accu/%s/%s_visible'%(task_type, split), visible_accu, epoch)
        writer.add_scalar('valid_accu/%s/%s_reached'%(task_type, split), reached_accu, epoch)
        writer.add_scalar('valid_accu/%s/%s_progress'%(task_type, split), progress_mse, epoch)

    return type_accu, arg_accu, mask_accu


def mixed_loader(loaders, shuffle=True):
    iterators, batch_idx = [], []
    cnt = 0
    for dn, loader in loaders.items():
        batch_idx += [cnt] * len(loader)
        iterators.append(iter(loader))
        cnt += 1
    if shuffle:
        random.shuffle(batch_idx)
    for idx in batch_idx:
        yield next(iterators[idx])
    return

if __name__ == '__main__':
    # args and init
    formatter = argparse.ArgumentDefaultsHelpFormatter
    parser = argparse.ArgumentParser(formatter_class=formatter)
    Config(parser)

    #-----------------------------------

    args = parser.parse_args()
    args.gpu = True
    args.use_bert = True
    args.dropout = 0.1
    args.enable_feat_posture = True
    args.train_proportion = 100
    args.valid_metric = "type"
    args.focal_loss = True
    args.emb_init = "xavier"
    args.lr = 5e-5
    args.batch = 12
    args.bert_lr_schedule = True
    args.early_stop = 2
    args.seed = 999

    args.bert_model = "roberta"
    args.train_level = 'low'
    args.pp_data = 'data/full_2.1.0_pp/'
    args.low_data = "navi"
    
    args.exp_temp  = 'Aug19Thr'
    #-----------------------------------

    bert_config(args)

    # set seeds
    torch.manual_seed(args.seed)
    torch.cuda.manual_seed(args.seed)
    random.seed(args.seed)
    np.random.seed(args.seed)
    torch.backends.cudnn.benchmark = False
    torch.backends.cudnn.deterministic = True

    # temporary experimental settings
    temp = args.exp_temp   #'Jan10Sat'
    hyperparameters = [ 'batch', 'lr', 'enc_layer_num', 'enable_feat_vis_his', 'enable_feat_posture','auxiliary_loss_navi']

    # make experimental directory
    weigh_loss_str = '' if not args.weigh_loss else 'wl_'
    model_str = '%s_%s_%s_E-%s%dd_L%d_H%d_det-%s_dp%.1f_di%.1f_%s%s_lr%.0e_%.3f_%s_sd%d'%(
        args.name_temp, args.train_level, args.low_data, args.emb_init, args.emb_dim,
        args.enc_layer_num, args.hidden_dim, args.detector_type,  args.dropout, args.drop_input,
        weigh_loss_str, args.lr_scheduler, args.lr, args.beta2, args.valid_metric, args.seed)
    args.exp_path = os.path.join('exp', temp, model_str)
    if not os.path.isdir(args.exp_path):
        os.makedirs(args.exp_path)

    # set logger
    log_dir = os.path.join(args.exp_path, 'train_log.log')
    log_handlers = [logging.StreamHandler(), logging.FileHandler(log_dir)]
    logging.basicConfig(handlers=log_handlers, level=logging.INFO,
        format='%(asctime)s %(message)s', datefmt='%Y-%m-%d %H:%M:%S')
    logging.info(pprint.pformat(args.__dict__))
    with open(os.path.join(args.exp_path, 'config.json'), 'w') as f:
        json.dump(args.__dict__, f, indent=2)

    # setup tensorboard
    tb_log_write_dir = os.path.join('tb_log', temp, model_str)
    if os.path.exists(tb_log_write_dir):
        shutil.rmtree(tb_log_write_dir)
    writer = SummaryWriter(tb_log_write_dir)


    # load alfred data and build pytorch data sets and loaders
    alfred_data = AlfredDataset(args)

    train_loaders = {}
    if args.train_level != 'low':
        train_high = AlfredPyTorchDataset(alfred_data, 'train', 'high', args)
        train_loader_high = torch.utils.data.DataLoader(train_high,
            batch_size=args.batch, shuffle=True, num_workers=2, pin_memory=True)
        train_loaders['high'] = train_loader_high

    if args.train_level != 'high' and args.low_data != 'mani':
        train_low_navi = AlfredPyTorchDataset(alfred_data, 'train', 'low_navi', args)
        train_loader_low_navi = torch.utils.data.DataLoader(train_low_navi,
            batch_size=args.batch, shuffle=True, num_workers=2, pin_memory=True)
        train_loaders['low_navi'] = train_loader_low_navi

    if args.train_level != 'high' and args.low_data != 'navi':
        train_low_mani = AlfredPyTorchDataset(alfred_data, 'train', 'low_mani', args)
        train_loader_low_mani = torch.utils.data.DataLoader(train_low_mani,
            batch_size=args.batch, shuffle=True, num_workers=2, pin_memory=True)
        train_loaders['low_mani'] = train_loader_low_mani


    valid_names = ['valid_seen', 'valid_unseen']
    task_types = ['high'] if args.train_level != 'low' else []
    if args.train_level != 'high' and args.low_data != 'navi':
        task_types.append('low_mani')
    if args.train_level != 'high' and args.low_data != 'mani':
        task_types.append('low_navi')
    valid_loaders = {}
    for split in valid_names:
        for tt in task_types:
            bs = args.batch
            # bs = 400 if args.train_proportion != 100 else args.batch
            valid_set = AlfredPyTorchDataset(alfred_data, split, tt, args)
            valid_loaders[split+'-'+tt] = torch.utils.data.DataLoader(valid_set,
                batch_size=bs, shuffle=True, num_workers=2, pin_memory=True)

    # setup model
    model = MultiModalTransformer(args, alfred_data)
    model.to(model.device)

    # learnable weights for multiple loss terms
    log_var = None
    if args.weigh_loss:
        log_var = {k: torch.zeros(4, device=model.device, requires_grad=True) for k in task_types}

    # construct an optimizer
    params = [p for p in model.parameters() if p.requires_grad]
    if args.weigh_loss:
        params += list(log_var.values())
    lr = args.lr if args.lr_scheduler == 'step' else 0
    if not args.use_bert:
        if args.lr_scheduler == 'step':
            optimizer = torch.optim.Adam(params, lr=lr, betas=(0.9, args.beta2), eps=1e-9)
            lr_scheduler = torch.optim.lr_scheduler.StepLR(optimizer,
                                                       step_size=args.step_decay_epoch,
                                                       gamma=args.step_decay_factor)
        elif args.lr_scheduler == 'noam':
            optimizer = NoamOpt(args.hidden_dim, args.noam_lr_factor, args.noam_warmup_iter,
                optimizer, writer=None)
    else:
        optimizer = AdamW(params, lr=lr)
        if args.bert_lr_schedule:
            batch_num = sum([len(i) for i in train_loaders.values()])
            num_training_steps = args.epoch * batch_num
            num_warmup_steps = batch_num // 2
            print('num_train:', num_training_steps, 'num_warmup:', num_warmup_steps)
            scheduler = get_linear_schedule_with_warmup(
                optimizer, num_warmup_steps=num_warmup_steps,
                num_training_steps=num_training_steps
            )

    # some initilizations before loop
    best_valid, fail = {'seen':0 , 'unseen': 0}, {'seen':0 , 'unseen': 0}
    counts = {'start_time': time.time(), 'epoch': 0}
    for lt, ld in train_loaders.items():
        counts['dlen_%s'%lt] = len(ld)
        counts['iter_%s'%lt] = 0
    if args.use_bert and args.bert_lr_schedule:
        counts['scheduler'] = scheduler

    # start training loop
    for epoch in range(args.epoch):
        counts['epoch'] = epoch
        if len(train_loaders) > 1:
            train_loader = mixed_loader(train_loaders)
        else:
            train_loader, = train_loaders.values()

        train_one_epoch(model, optimizer, train_loader, counts, loss_weights=log_var)
        if args.lr_scheduler in ['step', 'noam']:   # for comparison
            writer.add_scalar('lr', optimizer.param_groups[0]["lr"], epoch)
        # validation check
        valid_accu = {}
        with torch.no_grad():
            for dataset_type, loader in valid_loaders.items():
                split, tt = dataset_type.split('-')
                type_accu, arg_accu, mask_accu = validation_check(
                    model, split, tt, loader, epoch)

                valid_accu[dataset_type + '-' + 'type'] = type_accu
                valid_accu[dataset_type + '-' + 'arg'] = arg_accu
                valid_accu[dataset_type + '-' + 'mask'] =mask_accu

            vs_level = 'low' if args.train_level == 'mix' else args.train_level
            if vs_level == 'low':
                vs_level += '_mani' if args.low_data == 'mani' else '_navi'
            valid_scores = {
                'seen': valid_accu['valid_seen-%s-%s'%(vs_level, args.valid_metric)],
                'unseen': valid_accu['valid_unseen-%s-%s'%(vs_level, args.valid_metric)],
            }

            valid_check_failed = False
            for valid_type, valid_score in valid_scores.items():
                logging.info("%s valid score  (%s %s): %.3f" %(valid_type, args.train_level,
                        args.valid_metric, valid_score))
                writer.add_scalar('valid_accu/valid_score_%s'%valid_type, valid_score, epoch)

                if (valid_score - best_valid[valid_type]) >= 1e-3:
                    best_valid[valid_type] = valid_score
                    best_accus = valid_accu
                    fail[valid_type] = 0
                    save_dir = os.path.join(args.exp_path, "model_best_%s.pth"%valid_type)
                    torch.save(model.state_dict(), save_dir)
                    torch.save({'epoch': epoch,
                                       'optimizer': optimizer.state_dict(),
                                       'loss_weight': log_var,
                                       'score': valid_score}, save_dir.replace('model_', 'state_'))
                    logging.info("[valid %s] New best score! Model of epoch %d saved."%(valid_type, epoch))
                else:
                    fail[valid_type] += 1
                    logging.info("[valid %s] score does not get better for %d epochs"%(valid_type, fail[valid_type]))
                    valid_check_failed = True
            writer.flush()

            if args.lr_scheduler == 'step' and not args.use_bert and valid_check_failed:
                lr_scheduler.step()

            if fail['seen'] >= args.early_stop and fail['unseen'] >= args.early_stop:
                tt = time.time() - counts['start_time']
                save_dir = os.path.join(args.exp_path, "model_final.pth")
                torch.save(model.state_dict(), save_dir)
                torch.save({'epoch': epoch,
                                   'optimizer': optimizer.state_dict(),
                                   'loss_weight': log_var,
                                   'score': valid_scores}, save_dir.replace('model_', 'state_'))
                logging.info("Training early stopped. Total time: %dh%dm"%(tt//3600, tt//60%60))
                writer.add_hparams({k:args.__dict__[k] for k in hyperparameters},
                      {'metric/best_valid_seen': valid_scores['seen'],
                       'metric/best_valid_unseen': valid_scores['unseen']})

                write_head = not os.path.exists(temp+'_results.csv')
                with open(temp+'_results.csv', 'a') as rf:
                    writer = csv.DictWriter(rf, fieldnames=['name', 'proportion', 'seed'] + list(best_accus.keys()))
                    best_accus['name'] = model_str
                    best_accus['proportion'] = args.train_proportion
                    best_accus['seed'] = args.seed
                    if write_head:
                        writer.writeheader()
                    writer.writerows([best_accus])
                quit()

    tt = time.time() - counts['start_time']
    logging.info("Training stopped. Total time: %dh%dm"%(tt//3600, tt//60%60))
    save_dir = os.path.join(args.exp_path, "model_final_ep%d.pth"%(epoch))
    torch.save(model.state_dict(), save_dir)
    torch.save({'epoch': epoch,
                       'optimizer': optimizer.state_dict(),
                       'loss_weight': log_var,
                       'score': valid_scores}, save_dir.replace('model_', 'state_'))
    writer.add_hparams({k:args.__dict__[k] for k in hyperparameters},
                      {'metric/best_valid_seen': valid_scores['seen'],
                       'metric/best_valid_unseen': valid_scores['unseen']})

    write_head = not os.path.exists(temp+'_results.csv')
    with open(temp+'_results.csv', 'a') as rf:
        writer = csv.DictWriter(rf, fieldnames=['name', 'proportion', 'seed'] + list(best_accus.keys()))
        best_accus['name'] = model_str
        best_accus['proportion'] = args.train_proportion
        best_accus['seed'] = args.seed
        if write_head:
            writer.writeheader()
        writer.writerows([best_accus])