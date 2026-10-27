#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""
scene_tools.py — 巨型 Unity 场景 YAML 分析工具（BPF 旧工程 / 主工程通用）

为什么需要它：MRTKsync.unity 约 158MB / 540 万行 / 4 万个 GameObject，
用文本编辑器或 Unity MCP 逐物体查询都不可行。本脚本一次流式扫描建立
缓存，之后所有查询都基于 JSON 缓存，秒级返回。

用法（Windows + Git Bash，务必先修 PATH，见 SKILL.md「环境陷阱」）：

  # 1) 建立缓存（首次约 20s，之后复用）
  python scene_tools.py build  <scene.unity>  [cache.json]
  # 2) 列出某个名字的所有同名 GameObject 及其父路径、active、子物体数
  python scene_tools.py who    <cache.json> <name>
  # 3) 列出某 GameObject 的直接子物体（带同级下标，可用于验证 uniqueID 的 [n]）
  python scene_tools.py kids   <cache.json> <name|goFileID>
  # 4) 导出某 GameObject 的子树（带组件与脚本名），maxdepth 控制深度
  python scene_tools.py tree   <cache.json> <name|goFileID> [maxdepth] [--filtered]
  # 5) 导出子树内所有 MonoBehaviour 的序列化字段（排查 prefabA / spawnPosition 等）
  python scene_tools.py mb     <cache.json> <name|goFileID> [skipScript,skipScript...]
  # 6) 提取子树内所有非空 UI 文本（UILabel m_Text / TMP m_text）
  python scene_tools.py text   <cache.json> <name|goFileID>
  # 7) 输出若干物体的 m_LocalPosition（配合 --parent 可查其父路径）
  python scene_tools.py pos    <cache.json> <name1,name2,...>
  # 8) 列出所有根物体（m_Father 为空），快速摸清场景顶层
  python scene_tools.py roots  <cache.json>

组件类型 map 说明：Unity class id → 名称；MonoBehaviour(114) 的 m_Script guid
通过 --guidroots 指定的目录下 *.meta 反查为脚本名。默认会扫描旧工程与主工程。
"""
import collections
import io
import json
import os
import re
import sys

# ---------------------------------------------------------------- guid 反查
GUIDROOTS_DEFAULT = [
    r'D:\gitea_obj\_unity projection\gitea_mrtk_sync_old_4\Assets',
    r'D:\gitea_obj\_unity projection\gitea_mrtk_sync_old_4\Library\PackageCache',
    r'D:\gitea_obj\_unity projection\gitea_mrtk_sync_old_4\Packages',
    r'D:\gitea_obj\_github_obj\github_gitea_bpf_714\Assets',
    r'D:\gitea_obj\_github_obj\github_gitea_bpf_714\Library\PackageCache',
]

UNITY_CLASS = {
    1: 'GameObject', 4: 'Transform', 20: 'Camera', 23: 'MeshRenderer', 33: 'MeshFilter',
    64: 'MeshCollider', 65: 'BoxCollider', 82: 'AudioSource', 108: 'Light',
    114: 'MonoBehaviour', 135: 'SphereCollider', 136: 'CapsuleCollider',
    154: 'TerrainCollider', 222: 'CanvasRenderer', 223: 'Canvas', 224: 'RectTransform',
    225: 'CanvasGroup',
}
TRANS_CLASSES = (4, 224)
COMPONENT_CLASSES = tuple(sorted(set(UNITY_CLASS) - {1, 4, 224}))

doc_re = re.compile(r'^--- !u!(\d+) &(\d+)')
go_name_re = re.compile(r'^  m_Name: (.*)$')
comp_item_re = re.compile(r'^  - component: \{fileID: (\d+)\}')
mgo_re = re.compile(r'^  m_GameObject: \{fileID: (\d+)\}\s*$')
father_re = re.compile(r'^  m_Father: \{fileID: (\d+)\}\s*$')
children_hdr_re = re.compile(r'^  m_Children:\s*$')
child_re = re.compile(r'^  - \{fileID: (\d+)\}\s*$')
script_re = re.compile(r'^  m_Script: \{fileID: \d+, guid: ([0-9a-f]{32})')
pos_re = re.compile(r'^  m_LocalPosition: (.*)$')


def unyaml(s):
    """还原 Unity YAML 中的 \\uXXXX 转义与首尾双引号（中文名/中文文本必需）。
    两种写法都存在，需自动判别：
      a) \\u4EFB → 直接是目标码点（unicode_escape 一步到位）
      b) \\u00E8\\u00B4\\u00A6 → 是 UTF-8 字节被逐字节转义（需再 latin-1→utf-8）
    仅当整串为纯 ASCII 时才尝试，避免误伤字面 UTF-8 中文。"""
    if s is None:
        return s
    s = s.strip()
    if len(s) >= 2 and s[0] == '"' and s[-1] == '"':
        s = s[1:-1]
    if '\\u' not in s:
        return s
    try:
        s.encode('ascii')
    except UnicodeEncodeError:
        return s  # 含字面非 ASCII，已是可读文本
    try:
        step1 = s.encode('latin-1', 'backslashreplace').decode('unicode_escape')
    except Exception:
        return s
    if any(ord(c) > 0xFF for c in step1):
        return step1  # 情况 a：已是目标码点
    try:
        step2 = step1.encode('latin-1').decode('utf-8')
        return step2
    except Exception:
        return step1


def build_cache(scene, cache):
    with io.open(cache + '.scene', 'w', encoding='utf-8') as f:
        f.write(scene)
    go_names, go_tags, go_active, go_comps = {}, {}, {}, collections.defaultdict(list)
    comp_class, comp_script, trans_owner = {}, {}, {}
    trans_children, trans_father = collections.defaultdict(list), {}
    cur = cid = state = None
    with io.open(scene, 'r', encoding='utf-8', errors='replace') as f:
        for line in f:
            m = doc_re.match(line)
            if m:
                cur = int(m.group(1)); cid = int(m.group(2)); state = None
                comp_class[cid] = cur
                continue
            if state == 'comp':
                mm = comp_item_re.match(line)
                if mm:
                    go_comps[cid].append(int(mm.group(1))); continue
                state = None
            elif state == 'child':
                mm = child_re.match(line)
                if mm:
                    trans_children[cid].append(int(mm.group(1))); continue
                state = None
            if cur == 1:
                if line.startswith('  m_Name: '):
                    go_names[cid] = unyaml(line[10:].rstrip('\r\n'))
                elif line.startswith('  m_TagString: '):
                    go_tags[cid] = unyaml(line[15:].rstrip('\r\n'))
                elif line.startswith('  m_IsActive: '):
                    go_active[cid] = line[14:].rstrip('\r\n')
                elif line.startswith('  m_Component:'):
                    state = 'comp'
            elif cur in TRANS_CLASSES:
                if line.startswith('  m_GameObject: '):
                    mm = mgo_re.match(line)
                    if mm:
                        trans_owner[cid] = int(mm.group(1))
                    continue
                if children_hdr_re.match(line):
                    state = 'child'; continue
                mm = father_re.match(line)
                if mm:
                    trans_father[cid] = int(mm.group(1)); continue
            elif cur in COMPONENT_CLASSES:
                mm = script_re.match(line)
                if mm:
                    comp_script[cid] = mm.group(1); continue
    data = {
        'scene': scene,
        'go_names': {str(k): v for k, v in go_names.items()},
        'go_tags': {str(k): v for k, v in go_tags.items()},
        'go_active': {str(k): v for k, v in go_active.items()},
        'go_comps': {str(k): v for k, v in go_comps.items()},
        'comp_class': {str(k): v for k, v in comp_class.items()},
        'comp_script': {str(k): v for k, v in comp_script.items()},
        'trans_owner': {str(k): v for k, v in trans_owner.items()},
        'trans_children': {str(k): v for k, v in trans_children.items()},
        'trans_father': {str(k): v for k, v in trans_father.items()},
    }
    with io.open(cache, 'w', encoding='utf-8') as f:
        json.dump(data, f)
    sys.stderr.write('cached %d GameObjects -> %s\n' % (len(go_names), cache))


def load_cache(cache):
    with io.open(cache, 'r', encoding='utf-8') as f:
        d = json.load(f)
    scene = d.get('scene')
    if not scene:
        # 兼容旧版/第三方缓存的旁挂文件，以及命令行 --scene= 覆盖
        side = cache + '.scene'
        if os.path.exists(side):
            scene = io.open(side, 'r', encoding='utf-8').read().strip()
    return {
        'scene': scene,
        'go_names': {int(k): v for k, v in d['go_names'].items()},
        'go_tags': {int(k): v for k, v in d['go_tags'].items()},
        'go_active': {int(k): v for k, v in d['go_active'].items()},
        'go_comps': {int(k): [int(x) for x in v] for k, v in d['go_comps'].items()},
        'comp_class': {int(k): v for k, v in d['comp_class'].items()},
        'comp_script': {int(k): v for k, v in d['comp_script'].items()},
        'trans_owner': {int(k): int(v) for k, v in d['trans_owner'].items()},
        'trans_children': {int(k): [int(x) for x in v] for k, v in d['trans_children'].items()},
        'trans_father': {int(k): int(v) for k, v in d['trans_father'].items()},
    }


def build_guidmap():
    cache = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'guidmap.json')
    if os.path.exists(cache):
        with io.open(cache, 'r', encoding='utf-8') as f:
            return json.load(f)
    gm = {}
    for root in GUIDROOTS_DEFAULT:
        if not os.path.isdir(root):
            continue
        for dirpath, _dirnames, filenames in os.walk(root):
            for fn in filenames:
                if fn.endswith('.meta'):
                    try:
                        t = io.open(os.path.join(dirpath, fn), 'r',
                                    encoding='utf-8', errors='replace').read(300)
                    except Exception:
                        continue
                    mm = re.search(r'guid:\s*([0-9a-f]{32})', t)
                    if mm:
                        gm.setdefault(mm.group(1), fn[:-5])
    with io.open(cache, 'w', encoding='utf-8') as f:
        json.dump(gm, f)
    return gm


class Scene(object):
    def __init__(self, cache):
        d = load_cache(cache)
        self.__dict__.update(d)
        self.gm = build_guidmap()
        self.go_to_trans = {}
        for t, g in self.trans_owner.items():
            self.go_to_trans.setdefault(g, t)
        self.by_name = collections.defaultdict(list)
        for g, n in self.go_names.items():
            self.by_name[n].append(g)

    def path(self, go):
        t = self.go_to_trans.get(go); out = []; seen = set()
        while t and t not in seen:
            seen.add(t)
            out.append(self.go_names.get(self.trans_owner.get(t), '?'))
            t = self.trans_father.get(t)
        return '/'.join(reversed(out))

    def resolve(self, token, prefer=None):
        """name 或 goFileID -> [goFileID...]；prefer 为优先匹配的父路径子串，
        全局 UNDER 变量（--under=）可强制只保留路径包含该子串的命中。"""
        if token.isdigit():
            g = int(token)
            return [g] if g in self.go_names else []
        hits = self.by_name.get(token, [])
        if UNDER:
            hits = [g for g in hits if UNDER in self.path(g)]
        if prefer:
            p = [g for g in hits if prefer in self.path(g)]
            if p:
                return p
        return hits

    def labels(self, go):
        out = []
        for c in self.go_comps.get(go, []):
            if c in self.comp_script:
                gd = self.comp_script[c]
                out.append(self.gm.get(gd, 'guid:' + gd[:8]))
            else:
                out.append(UNITY_CLASS.get(self.comp_class.get(c), 'c%s' % self.comp_class.get(c)))
        return out

    def descendants(self, go):
        seen, stack = set(), [go]
        while stack:
            g = stack.pop()
            if g in seen:
                continue
            seen.add(g)
            for c in self.trans_children.get(self.go_to_trans.get(g), []):
                gc = self.trans_owner.get(c)
                if gc is not None:
                    stack.append(gc)
        return seen

    def brief(self, go):
        ex = []
        tag = self.go_tags.get(go)
        if tag and tag != 'Untagged':
            ex.append('TAG=' + tag)
        if self.go_active.get(go) == '0':
            ex.append('INACTIVE')
        return ', '.join(ex + self.labels(go))


NOISE_LABELS = ('MeshFilter', 'MeshRenderer', 'CanvasRenderer')


def is_noise(s, trans):
    g = s.trans_owner.get(trans)
    if g is None or s.trans_children.get(trans):
        return False
    if s.go_tags.get(g) not in (None, 'Untagged'):
        return False
    labs = s.labels(g)
    return all(l in NOISE_LABELS or l.startswith('guid:') for l in labs)


def cmd_build(a):
    build_cache(a[0], a[1] if len(a) > 1 else 'scene_cache.json')


def cmd_who(a):
    s = Scene(a[0])
    for g in s.by_name.get(a[1], []):
        print('go=%-22d active=%s tag=%s kids=%d  %s' % (
            g, s.go_active.get(g), s.go_tags.get(g),
            len(s.trans_children.get(s.go_to_trans.get(g), [])), s.path(g)))


def cmd_kids(a):
    s = Scene(a[0])
    for g in s.resolve(*a[1:2]) or s.resolve(a[1], 'SHIXUN'):
        t = s.go_to_trans.get(g)
        print('== %s go=%d active=%s children=%d' % (
            s.go_names.get(g), g, s.go_active.get(g), len(s.trans_children.get(t, []))))
        for i, c in enumerate(s.trans_children.get(t, [])):
            gc = s.trans_owner.get(c)
            print('   [%d] %-28s active=%s kids=%d' % (
                i, s.go_names.get(gc, '?'), s.go_active.get(gc), len(s.trans_children.get(c, []))))


def cmd_tree(a):
    s = Scene(a[0])
    maxdepth = int(a[2]) if len(a) > 2 else 4
    filtered = '--filtered' in a
    out = []

    def rec(t, d):
        g = s.trans_owner.get(t)
        if g is None:
            return
        nm = s.go_names.get(g, '<unnamed>')
        labs = s.labels(g)
        if filtered and labs and all(l in NOISE_LABELS for l in labs) and not s.trans_children.get(t):
            return
        out.append('%s%s  [%s]' % ('  ' * d, nm, s.brief(g)))
        if d >= maxdepth:
            k = s.trans_children.get(t, [])
            if k:
                out.append('%s  ...(%d children)' % ('  ' * d, len(k)))
            return
        for c in s.trans_children.get(t, []):
            rec(c, d + 1)

    for g in s.resolve(a[1], 'SHIXUN'):
        print('#### %s go=%d  %s' % (s.go_names.get(g), g, s.path(g)))
        for c in s.trans_children.get(s.go_to_trans.get(g), []):
            rec(c, 1)
    print('\n'.join(out))


def cmd_mb(a):
    s = Scene(a[0])
    skip = set(a[2].split(',')) if len(a) > 2 else set()
    for g in s.resolve(a[1], 'SHIXUN'):
        desc = s.descendants(g)
        break
    else:
        return
    scene = s.scene
    cur = owner = gd = None
    body = []
    out = []

    def flush():
        if cur == 114 and owner in desc:
            nm = s.gm.get(gd, 'guid:' + str(gd)[:8])
            if nm not in skip:
                keep = [l for l in body if not re.match(
                    r'^  (m_ObjectHideFlags|m_CorrespondingSourceObject|m_PrefabInstance|'
                    r'm_PrefabAsset|m_GameObject|m_EditorClassIdentifier)', l)]
                out.append('%s\n  >> %s (%s)\n%s' % (s.path(owner), nm, gd, '\n'.join(keep)))

    with io.open(scene, 'r', encoding='utf-8', errors='replace') as f:
        for line in f:
            if line.startswith('--- !u!'):
                flush()
                cur = int(doc_re.match(line).group(1)); owner = gd = None; body = []
                continue
            if cur != 114:
                continue
            body.append(line.rstrip('\r\n'))
            if line.startswith('  m_GameObject: '):
                mm = mgo_re.match(line)
                if mm:
                    owner = int(mm.group(1))
                continue
            mm = script_re.match(line)
            if mm:
                gd = mm.group(1)
    flush()
    print('\n\n'.join(out))


TEXT_GUIDS = {
    '5f7201a12d95ffc409449d95f23cf332': 'UILabel',
    '3069a00b8c364df395994d7d379e0a99': 'TMP',
}


def cmd_text(a):
    s = Scene(a[0])
    for g in s.resolve(a[1], 'SHIXUN'):
        desc = s.descendants(g)
        break
    else:
        return
    cur = owner = gd = None
    field = None
    buf = []
    res = []

    def flush():
        if field and buf:
            v = unyaml(''.join(buf))
            if v.strip():
                res.append((owner, field, v))

    with io.open(s.scene, 'r', encoding='utf-8', errors='replace') as f:
        for line in f:
            if line.startswith('--- !u!'):
                flush(); field = None; buf = []
                cur = int(doc_re.match(line).group(1)); owner = gd = None
                continue
            if cur != 114:
                continue
            if field is not None:
                if line.startswith('  m_'):
                    flush(); field = None
                else:
                    buf.append(line.strip())
                    continue
            if line.startswith('  m_GameObject: '):
                mm = mgo_re.match(line)
                if mm:
                    owner = int(mm.group(1))
                continue
            mm = script_re.match(line)
            if mm:
                gd = mm.group(1); continue
            if gd in TEXT_GUIDS and line.startswith('  m_text: '):
                field = 'TMP'; buf = [line[10:].rstrip('\r\n')]
            elif gd in TEXT_GUIDS and line.startswith('  m_Text: '):
                field = 'UILabel'; buf = [line[10:].rstrip('\r\n')]
    flush()
    n = 0
    for owner, fld, v in res:
        if owner is None or owner not in desc:
            continue
        n += 1
        print('%s\n      <%s> %s' % (s.path(owner), fld, v.replace('\n', ' \\n ')))
    sys.stderr.write('non-empty texts: %d\n' % n)


def cmd_pos(a):
    s = Scene(a[0])
    want = {}
    for name in a[1].split(','):
        for g in s.resolve(name):
            t = s.go_to_trans.get(g)
            if t:
                want[t] = (name, g)
    cur = cid = None
    p = None
    out = []

    with io.open(s.scene, 'r', encoding='utf-8', errors='replace') as f:
        for line in f:
            if line.startswith('--- !u!'):
                if cur == 4 and cid in want and p:
                    out.append((want[cid], p))
                cur = int(doc_re.match(line).group(1)); cid = int(doc_re.match(line).group(2)); p = None
                continue
            if cur == 4 and cid in want:
                mm = pos_re.match(line)
                if mm:
                    p = mm.group(1).rstrip('\r\n')
    if cur == 4 and cid in want and p:
        out.append((want[cid], p))
    for (name, g), pp in out:
        print('%-24s %-40s %s' % (name, pp, s.path(g)))


def cmd_roots(a):
    s = Scene(a[0])
    rows = []
    for t, g in s.trans_owner.items():
        if not s.trans_father.get(t):
            rows.append((s.go_names.get(g, '?'), g, len(s.trans_children.get(t, []))))
    rows.sort(key=lambda x: -x[2])
    for n, g, k in rows:
        print('%-45s go=%-22d kids=%-4d active=%s' % (n, g, k, s.go_active.get(g)))


COMMANDS = {
    'build': cmd_build, 'who': cmd_who, 'kids': cmd_kids, 'tree': cmd_tree,
    'mb': cmd_mb, 'text': cmd_text, 'pos': cmd_pos, 'roots': cmd_roots,
}

UNDER = ''

if __name__ == '__main__':
    argv = [x for x in sys.argv if not x.startswith('--scene=') and not x.startswith('--under=')]
    override = [x for x in sys.argv if x.startswith('--scene=')]
    unders = [x for x in sys.argv if x.startswith('--under=')]
    if unders:
        UNDER = unders[0].split('=', 1)[1]
    if len(argv) < 2 or argv[1] not in COMMANDS:
        print(__doc__)
        sys.exit(1)
    if override and len(argv) > 2:
        # 允许 --scene=<path> 为旧缓存补齐场景路径
        cache = argv[2]
        side = cache if cache.endswith('.scene') else cache + '.scene'
        with io.open(side, 'w', encoding='utf-8') as f:
            f.write(override[0].split('=', 1)[1])
    COMMANDS[argv[1]](argv[2:])
