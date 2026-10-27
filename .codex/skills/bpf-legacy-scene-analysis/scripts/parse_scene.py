# -*- coding: utf-8 -*-
"""Parse Unity scene YAML: build GameObject/Transform tree, extract components."""
import re, sys, io, json, pickle
from collections import defaultdict

SCENE = r"D:\gitea_obj\_unity projection\gitea_mrtk_sync_old_4\Assets\_syc_resource\_my_scenes\sync\MRTKsync.unity"

doc_re = re.compile(r'^--- !u!(\d+) &(\d+)\s*$')
name_re = re.compile(r'^  m_Name: (.*)$')
go_comp_re = re.compile(r'^  - component: \{fileID: (\d+)\}')
father_re = re.compile(r'^  m_Father: \{fileID: (\d+)\}')
children_re = re.compile(r'^  - \{fileID: (\d+)\}')
script_guid_re = re.compile(r'^  m_Script: \{fileID: 11500000, guid: ([0-9a-f]+), type: 3\}')

gameobjects = {}      # fileID -> {name, components:[comp fileIDs]}
transforms = {}       # fileID -> {go, father, children[]}
mono = {}             # fileID -> {guid, go}

cur_class = None
cur_id = None
cur_kind = None  # 'go' or 'transform' or 'mono'
buf_name = None
in_children = False

with io.open(SCENE, 'r', encoding='utf-8', errors='replace') as f:
    for line in f:
        m = doc_re.match(line)
        if m:
            # save previous handled inline already
            cur_class = int(m.group(1)); cur_id = int(m.group(2))
            in_children = False
            if cur_class == 1:
                cur_kind = 'go'
                gameobjects[cur_id] = {'name': '', 'components': []}
            elif cur_class == 4 or cur_class == 224:
                cur_kind = 'transform'
                transforms[cur_id] = {'go': 0, 'father': 0, 'children': []}
            elif cur_class == 114:
                cur_kind = 'mono'
                mono[cur_id] = {'guid': '', 'go': 0}
            else:
                cur_kind = None
            continue
        if cur_kind is None:
            continue
        if cur_kind == 'go':
            m = name_re.match(line)
            if m:
                gameobjects[cur_id]['name'] = m.group(1).strip()
            elif line.startswith('  - component: {fileID: '):
                m = go_comp_re.match(line)
                if m:
                    gameobjects[cur_id]['components'].append(int(m.group(1)))
            elif line.startswith('  m_Component:'):
                pass
        elif cur_kind == 'transform':
            if line.startswith('  m_GameObject: {fileID: '):
                transforms[cur_id]['go'] = int(line.split('{fileID: ')[1].split('}')[0])
            elif line.startswith('  m_Father: {fileID: '):
                transforms[cur_id]['father'] = int(line.split('{fileID: ')[1].split('}')[0])
            elif line.startswith('  m_Children:'):
                in_children = True
            elif in_children and line.startswith('  - {fileID: '):
                m = children_re.match(line)
                if m:
                    transforms[cur_id]['children'].append(int(m.group(1)))
            elif line.startswith('  m_'):
                in_children = False
        elif cur_kind == 'mono':
            if line.startswith('  m_GameObject: {fileID: '):
                mono[cur_id]['go'] = int(line.split('{fileID: ')[1].split('}')[0])
            else:
                m = script_guid_re.match(line)
                if m:
                    mono[cur_id]['guid'] = m.group(1)

# build go -> transform map
go_transform = {}
for tid, t in transforms.items():
    if t['go']:
        go_transform[t['go']] = tid

data = {
    'gameobjects': gameobjects,
    'transforms': transforms,
    'mono': mono,
    'go_transform': go_transform,
}
with io.open(r"D:\gitea_obj\_github_obj\github_gitea_bpf_714\.workbuddy\tmp_scene_parse.pkl", 'wb') as pf:
    pickle.dump(data, pf)

print('GameObjects:', len(gameobjects))
print('Transforms:', len(transforms))
print('MonoBehaviours:', len(mono))

# find changjing-4 under SHIXUN
shixun_ids = [fid for fid, g in gameobjects.items() if g['name'] == 'SHIXUN']
print('SHIXUN ids:', shixun_ids)
cj4_ids = [fid for fid, g in gameobjects.items() if g['name'] in ('changjing-4', 'ChangJing-4')]
print('changjing-4 ids:', cj4_ids)
for fid in cj4_ids:
    tid = go_transform.get(fid)
    if tid:
        father = transforms[tid]['father']
        fname = gameobjects.get(transforms[father]['go'], {}).get('name') if father else None
        print(f'  changjing-4 fileID={fid} transform={tid} father={fname}')
